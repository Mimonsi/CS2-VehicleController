using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Collections;
using Unity.Entities;
using VehicleController.Data;

namespace VehicleController.Systems
{
    /// <summary>
    /// Streams the vehicle tree (category → class → prefab, with resolved values and override
    /// provenance) to the Vehicle Manager window. Read-only for now; editing triggers come next.
    /// The tree is serialized to a JSON string and parsed on the UI side to avoid hand-writing
    /// nested IJsonWriter output.
    /// </summary>
    public partial class VehicleManagerUISystem : UISystemBase
    {
        private const string Group = "VehicleController.VehicleManager";
        private const float MsToKmh = 3.6f;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        private static ILog log;
        public static VehicleManagerUISystem Instance { get; private set; }

        private PrefabSystem _prefabSystem;
        private EntityQuery _vehicleQuery;
        private ValueBinding<string> _treeJson;
        private ValueBinding<string> _globalJson;
        private ValueBinding<string> _classesJson;
        private ValueBinding<string> _packsJson;
        private ValueBinding<string> _openRequest;
        private int _openNonce;

        // ---- DTOs (serialized to the UI; camelCased by the resolver) ------------

        private class AttrDto
        {
            public float Value;
            public bool Overridden;
            public string Source;
        }

        private class PrefabDto
        {
            public string Id;
            public string Name;
            public string ClassName;
            public bool Custom;
            public AttrDto Probability;
            public AttrDto MaxSpeed;
            public AttrDto Acceleration;
            public AttrDto Braking;
        }

        private class ClassDto
        {
            public string Name;
            public bool Editable;
            public bool Custom;
            public AttrDto Probability;
            public AttrDto MaxSpeed;
            public AttrDto Acceleration;
            public AttrDto Braking;
            public List<PrefabDto> Prefabs = new List<PrefabDto>();
        }

        private class CategoryDto
        {
            public string Key;
            public string Name;
            public List<ClassDto> Classes = new List<ClassDto>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;

            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _vehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = new[] { ComponentType.ReadOnly<CarData>(), ComponentType.ReadOnly<TrainData>() },
            });

            _treeJson = new ValueBinding<string>(Group, "treeJson", "[]");
            AddBinding(_treeJson);
            _globalJson = new ValueBinding<string>(Group, "globalJson", "{}");
            AddBinding(_globalJson);
            _classesJson = new ValueBinding<string>(Group, "classesJson", "[]");
            AddBinding(_classesJson);
            _packsJson = new ValueBinding<string>(Group, "packsJson", "{}");
            AddBinding(_packsJson);
            _openRequest = new ValueBinding<string>(Group, "openRequest", "{}");
            AddBinding(_openRequest);
            AddBinding(new TriggerBinding<string>(Group, "openManager", OnOpenManager));
            AddBinding(new TriggerBinding(Group, "refresh", RequestTreeUpdate));
            AddBinding(new TriggerBinding<string>(Group, "edit", OnEdit));
            AddBinding(new TriggerBinding<string>(Group, "classCmd", OnClassCmd));
            AddBinding(new TriggerBinding<string>(Group, "packCmd", OnPackCmd));

            log.Info($"VehicleManagerUISystem created with group {Group}.");
        }

        private class EditCmd
        {
            public string Op;
            public string Level;
            public string Key;
            public string Field;
            public float Value;
        }

        /// <summary>Applies an edit command sent from the UI (JSON) to the active pack.</summary>
        private void OnEdit(string json)
        {
            try
            {
                var cmd = JsonConvert.DeserializeObject<EditCmd>(json);
                if (cmd == null)
                    return;
                VehicleConfigSystem.Instance?.Edit(cmd.Level, cmd.Key, cmd.Field, cmd.Op == "reset", cmd.Value);
            }
            catch (Exception x)
            {
                log.Warn($"Failed to apply edit '{json}': {x.Message}");
            }
        }

        private class ClassCmd
        {
            public string Op;
            public string Prefab;
            public string Class;
            public string NewName;
        }

        /// <summary>Applies a class-management command (assign / rename / delete) from the UI.</summary>
        private void OnClassCmd(string json)
        {
            try
            {
                var cmd = JsonConvert.DeserializeObject<ClassCmd>(json);
                var config = VehicleConfigSystem.Instance;
                if (cmd == null || config == null)
                    return;
                switch (cmd.Op)
                {
                    case "assign": config.AssignClass(cmd.Prefab, cmd.Class ?? ""); break;
                    case "rename": config.RenameClass(cmd.Class, cmd.NewName); break;
                    case "delete": config.DeleteClass(cmd.Class); break;
                }
            }
            catch (Exception x)
            {
                log.Warn($"Failed to apply class command '{json}': {x.Message}");
            }
        }

        private class PackCmd
        {
            public string Op;
            public string Name;
        }

        /// <summary>Applies a pack-bar command (switch / new / duplicate / rename / delete / export / import).</summary>
        private void OnPackCmd(string json)
        {
            try
            {
                var cmd = JsonConvert.DeserializeObject<PackCmd>(json);
                var config = VehicleConfigSystem.Instance;
                if (cmd == null || config == null)
                    return;
                switch (cmd.Op)
                {
                    case "switch": config.SwitchPack(cmd.Name); break;
                    case "new": config.NewPack(cmd.Name); break;
                    case "duplicate": config.DuplicatePack(cmd.Name); break;
                    case "rename": config.RenamePack(cmd.Name); break;
                    case "delete": config.DeletePack(cmd.Name); break;
                    case "export": config.ExportActiveToClipboard(); break;
                    case "import": config.ImportFromClipboard(); break;
                }
            }
            catch (Exception x)
            {
                log.Warn($"Failed to apply pack command '{json}': {x.Message}");
            }
        }

        // Requested from a vehicle's Selected-Info panel: open the manager focused on this prefab.
        // A bumped nonce lets the UI react even when the same prefab is requested twice.
        private void OnOpenManager(string prefabName)
        {
            _openNonce++;
            _openRequest.Update(JsonConvert.SerializeObject(new { prefab = prefabName, nonce = _openNonce }));
        }

        /// <summary>Rebuilds the tree JSON and pushes it to the UI. Safe to call at any time.</summary>
        public void RequestTreeUpdate()
        {
            try
            {
                _treeJson.Update(BuildTreeJson());
                _globalJson.Update(BuildGlobalJson());
                _classesJson.Update(BuildClassesJson());
                _packsJson.Update(BuildPacksJson());
            }
            catch (Exception x)
            {
                log.Warn($"Failed to build vehicle tree: {x.Message}");
                _treeJson.Update("[]");
            }
        }

        private static string BuildGlobalJson()
        {
            var g = VehicleConfigSystem.Instance?.ActivePack?.Global;
            if (g == null)
                return "{}";
            return JsonConvert.SerializeObject(new
            {
                probability = g.ProbabilityFactor,
                speed = g.SpeedFactor,
                acceleration = g.AccelerationFactor,
                braking = g.BrakingFactor,
            });
        }

        // Active pack name + all available pack names for the pack bar.
        private static string BuildPacksJson()
        {
            var active = VehicleConfigSystem.Instance?.ActivePack?.Name ?? "Default";
            var packs = VehiclePack.GetPackNames();
            if (!packs.Contains(active))
                packs.Insert(0, active); // the active pack may be new/unsaved
            return JsonConvert.SerializeObject(new { active, packs });
        }

        // All assignable class names (built-in + this pack's custom classes) for the assign dropdown.
        private static string BuildClassesJson()
        {
            var list = new List<object>();
            foreach (var name in VehicleClass.GetNames())
                list.Add(new { name, custom = false });

            var pack = VehicleConfigSystem.Instance?.ActivePack;
            if (pack != null)
                foreach (var name in pack.CustomClasses)
                    list.Add(new { name, custom = true });

            return JsonConvert.SerializeObject(list);
        }

        private string BuildTreeJson()
        {
            var config = VehicleConfigSystem.Instance;
            if (config == null || !config.VanillaReady)
                return "[]";

            var vanilla = config.Vanilla;
            var pack = config.ActivePack;

            var cats = new List<CategoryDto>();
            var catByKey = new Dictionary<string, CategoryDto>();
            var classIndex = new Dictionary<string, Dictionary<string, ClassDto>>();

            CategoryDto EnsureCat(string key, string name)
            {
                if (!catByKey.TryGetValue(key, out var c))
                {
                    c = new CategoryDto { Key = key, Name = name };
                    catByKey[key] = c;
                    classIndex[key] = new Dictionary<string, ClassDto>();
                    cats.Add(c);
                }
                return c;
            }

            // Fixed display order.
            EnsureCat("cars", "Cars");
            EnsureCat("trains", "Trains");
            EnsureCat("service", "Service");

            var entities = _vehicleQuery.ToEntityArray(Allocator.Temp);
            var seen = new HashSet<string>();
            foreach (var entity in entities)
            {
                var prefabName = _prefabSystem.GetPrefabName(entity);
                if (!seen.Add(prefabName))
                    continue;
                if (!vanilla.TryGetValue(prefabName, out var baseline))
                    continue;

                string catKey, catName;
                if (EntityManager.HasComponent<TrainData>(entity))
                {
                    catKey = "trains";
                    catName = "Trains";
                }
                else if (EntityManager.HasComponent<PersonalCarData>(entity))
                {
                    catKey = "cars";
                    catName = "Cars";
                }
                else
                {
                    catKey = "service";
                    catName = "Service";
                }

                var classes = pack.GetEffectiveClasses(prefabName);
                var className = classes.Count > 0 ? classes[0] : "Unclassified";
                pack.PrefabOverrides.TryGetValue(prefabName, out var prefabOverride);

                var prefabDto = new PrefabDto
                {
                    Id = prefabName,
                    Name = prefabName,
                    ClassName = className,
                    // Real "custom asset" (mod) detection needs prefab source info; not mislabeling
                    // vanilla-but-unclassified vehicles (trains/buses) as custom for now.
                    Custom = false,
                    Probability = ProbAttr(prefabOverride, classes, pack),
                    MaxSpeed = FloatAttr(prefabOverride, classes, pack, o => o.MaxSpeed, baseline.MaxSpeed, MsToKmh),
                    Acceleration = FloatAttr(prefabOverride, classes, pack, o => o.Acceleration, baseline.Acceleration, 1f),
                    Braking = FloatAttr(prefabOverride, classes, pack, o => o.Braking, baseline.Braking, 1f),
                };

                var cat = EnsureCat(catKey, catName);
                var idx = classIndex[catKey];
                if (!idx.TryGetValue(className, out var classDto))
                {
                    pack.ClassOverrides.TryGetValue(className, out var classOver);
                    classDto = new ClassDto
                    {
                        Name = className,
                        // "Unclassified" is a UI-only bucket with no real members to share values with.
                        Editable = className != "Unclassified",
                        Custom = pack.CustomClasses.Contains(className),
                        Probability = ClassProbAttr(classOver),
                        MaxSpeed = ClassFloatAttr(classOver, o => o.MaxSpeed, MsToKmh),
                        Acceleration = ClassFloatAttr(classOver, o => o.Acceleration, 1f),
                        Braking = ClassFloatAttr(classOver, o => o.Braking, 1f),
                    };
                    idx[className] = classDto;
                    cat.Classes.Add(classDto);
                }
                classDto.Prefabs.Add(prefabDto);
            }

            // Stable alphabetical order and drop empty categories.
            foreach (var cat in cats)
            {
                cat.Classes.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                foreach (var cls in cat.Classes)
                    cls.Prefabs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            }
            var result = cats.Where(c => c.Classes.Count > 0).ToList();

            return JsonConvert.SerializeObject(result, JsonSettings);
        }

        // Class-level attributes reflect only the class override itself (a class has no single
        // vanilla baseline, since its members differ). Not-set = no value to display.
        private static AttrDto ClassProbAttr(VehicleOverride classOverride)
        {
            if (classOverride?.ProbabilityPercent != null)
                return new AttrDto { Value = classOverride.ProbabilityPercent.Value, Overridden = true, Source = "" };
            return new AttrDto { Value = 0, Overridden = false, Source = "" };
        }

        private static AttrDto ClassFloatAttr(VehicleOverride classOverride, Func<VehicleOverride, float?> selector, float scale)
        {
            if (classOverride != null)
            {
                var v = selector(classOverride);
                if (v != null)
                    return new AttrDto { Value = v.Value * scale, Overridden = true, Source = "" };
            }
            return new AttrDto { Value = 0, Overridden = false, Source = "" };
        }

        private static AttrDto ProbAttr(VehicleOverride prefabOverride, List<string> classes, VehiclePack pack)
        {
            if (prefabOverride?.ProbabilityPercent != null)
                return new AttrDto { Value = prefabOverride.ProbabilityPercent.Value, Overridden = true, Source = "" };
            foreach (var className in classes)
            {
                if (pack.ClassOverrides.TryGetValue(className, out var o) && o.ProbabilityPercent != null)
                    return new AttrDto { Value = o.ProbabilityPercent.Value, Overridden = false, Source = className };
            }
            return new AttrDto { Value = 100, Overridden = false, Source = "vanilla" };
        }

        private static AttrDto FloatAttr(
            VehicleOverride prefabOverride,
            List<string> classes,
            VehiclePack pack,
            Func<VehicleOverride, float?> selector,
            float vanillaValue,
            float scale)
        {
            if (prefabOverride != null)
            {
                var v = selector(prefabOverride);
                if (v != null)
                    return new AttrDto { Value = v.Value * scale, Overridden = true, Source = "" };
            }
            foreach (var className in classes)
            {
                if (pack.ClassOverrides.TryGetValue(className, out var o))
                {
                    var v = selector(o);
                    if (v != null)
                        return new AttrDto { Value = v.Value * scale, Overridden = false, Source = className };
                }
            }
            return new AttrDto { Value = vanillaValue * scale, Overridden = false, Source = "vanilla" };
        }
    }
}
