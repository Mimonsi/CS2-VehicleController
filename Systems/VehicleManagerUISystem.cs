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
        private EntityQuery _instanceQuery;
        private ValueBinding<string> _treeJson;
        private ValueBinding<string> _classesJson;
        private ValueBinding<string> _packsJson;
        private ValueBinding<string> _openRequest;
        private ValueBinding<int> _selectedCount;
        private int _openNonce;
        private readonly System.Random _rng = new System.Random();

        // ---- DTOs (serialized to the UI; camelCased by the resolver) ------------

        private class AttrDto
        {
            public float Value;
            public bool Overridden;
            public string Source;
        }

        /// <summary>
        /// One property across the cascade, as the detail table renders it: the editing pack's own
        /// prefab value, its class value, the vanilla value, and the value actually in the game.
        /// <see cref="Winner"/> says which layer currently wins ("own" | "class" | "vanilla" | "pack"),
        /// with <see cref="Source"/> naming the pack when another active pack is responsible.
        /// </summary>
        private class FieldDto
        {
            public float? Own;
            public float? Cls;
            public float Vanilla;
            public float Effective;
            public string Winner;
            public string Source;
        }

        private class PrefabDto
        {
            public string Id;
            public string Name;
            public string ClassName;
            public bool Custom;
            public string Thumbnail;
            /// <summary>True only for naturally spawning vehicles (personal cars); others hide probability.</summary>
            public bool Spawns;
            /// <summary>True when the editing pack sets any prefab-level value here (tree marker).</summary>
            public bool Edited;
            public FieldDto Probability;
            public FieldDto MaxSpeed;
            public FieldDto Acceleration;
            public FieldDto Braking;
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
                Any = new[]
                {
                    ComponentType.ReadOnly<CarData>(),
                    ComponentType.ReadOnly<TrainData>(),
                    ComponentType.ReadOnly<WatercraftData>(),
                    ComponentType.ReadOnly<AircraftData>(),
                },
            });
            // Live vehicle instances in the world (for per-prefab counts + jump-to-instance).
            _instanceQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Game.Vehicles.Vehicle>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Game.Common.Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });

            _treeJson = new ValueBinding<string>(Group, "treeJson", "[]");
            AddBinding(_treeJson);
            _classesJson = new ValueBinding<string>(Group, "classesJson", "[]");
            AddBinding(_classesJson);
            _packsJson = new ValueBinding<string>(Group, "packsJson", "{}");
            AddBinding(_packsJson);
            _openRequest = new ValueBinding<string>(Group, "openRequest", "{}");
            AddBinding(_openRequest);
            _selectedCount = new ValueBinding<int>(Group, "selectedCount", 0);
            AddBinding(_selectedCount);
            AddBinding(new TriggerBinding<string>(Group, "openManager", OnOpenManager));
            AddBinding(new TriggerBinding(Group, "refresh", RequestTreeUpdate));
            AddBinding(new TriggerBinding<string>(Group, "edit", OnEdit));
            AddBinding(new TriggerBinding<string>(Group, "classCmd", OnClassCmd));
            AddBinding(new TriggerBinding<string>(Group, "packCmd", OnPackCmd));
            AddBinding(new TriggerBinding<string>(Group, "jumpTo", OnJumpTo));
            AddBinding(new TriggerBinding<string>(Group, "requestCount", OnRequestCount));

            log.Info($"VehicleManagerUISystem created with group {Group}.");
        }

        private class EditCmd
        {
            public string Op;
            public string Level;
            public string Key;
            public string[] Prefabs;
            public string Field;
            public float Value;
        }

        /// <summary>Applies an edit command sent from the UI (JSON) to the active pack.</summary>
        private void OnEdit(string json)
        {
            try
            {
                var cmd = JsonConvert.DeserializeObject<EditCmd>(json);
                var config = VehicleConfigSystem.Instance;
                if (cmd == null || config == null)
                    return;
                bool reset = cmd.Op == "reset";
                if (cmd.Level == "prefab" && cmd.Prefabs != null)
                    config.EditManyPrefabs(cmd.Prefabs, cmd.Field, reset, cmd.Value);
                else
                    config.Edit(cmd.Level, cmd.Key, cmd.Field, reset, cmd.Value);
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
            public string[] Prefabs;
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
                    case "assignMany": config.AssignClassMany(cmd.Prefabs, cmd.Class ?? ""); break;
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

        /// <summary>Applies a pack-bar command: stack management (add/remove/move/target) + pack ops.</summary>
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
                    case "add": config.AddToStack(cmd.Name); break;
                    case "remove": config.RemoveFromStack(cmd.Name); break;
                    case "toggle": config.TogglePack(cmd.Name); break;
                    case "moveUp": config.MovePack(cmd.Name, -1); break;
                    case "moveDown": config.MovePack(cmd.Name, +1); break;
                    case "setTarget": config.SetEditTarget(cmd.Name); break;
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
                _classesJson.Update(BuildClassesJson());
                _packsJson.Update(BuildPacksJson());
            }
            catch (Exception x)
            {
                log.Warn($"Failed to build vehicle tree: {x.Message}");
                _treeJson.Update("[]");
            }
        }

        // Pushes the live instance count of a single prefab to the UI. Requested when the
        // detail panel focuses a prefab, so the count reflects the world at selection time.
        private void OnRequestCount(string prefabName)
        {
            try
            {
                _selectedCount.Update(CollectInstances(prefabName).Count);
            }
            catch (Exception x)
            {
                log.Warn($"Count request for '{prefabName}' failed: {x.Message}");
            }
        }

        // Returns every live instance entity of the given prefab.
        private List<Entity> CollectInstances(string prefabName)
        {
            var matches = new List<Entity>();
            var instances = _instanceQuery.ToEntityArray(Allocator.Temp);
            foreach (var e in instances)
            {
                if (EntityManager.TryGetComponent<PrefabRef>(e, out var pr) &&
                    _prefabSystem.GetPrefabName(pr.m_Prefab) == prefabName)
                {
                    matches.Add(e);
                }
            }
            instances.Dispose();
            return matches;
        }

        // Moves the camera to follow a random live instance of the given prefab, using the same
        // orbit-follow mechanism the game uses when you follow a vehicle from its info panel.
        private void OnJumpTo(string prefabName)
        {
            try
            {
                var camera = World.GetExistingSystemManaged<Game.Rendering.CameraUpdateSystem>();
                if (camera?.orbitCameraController == null)
                    return;

                var matches = CollectInstances(prefabName);
                if (matches.Count == 0)
                {
                    log.Info($"Jump to instance: no live instance of '{prefabName}' found.");
                    return;
                }

                var target = matches[_rng.Next(matches.Count)];
                var orbit = camera.orbitCameraController;
                orbit.followedEntity = target;
                orbit.TryMatchPosition(camera.activeCameraController);
                camera.activeCameraController = orbit;
            }
            catch (Exception x)
            {
                log.Warn($"Jump to instance for '{prefabName}' failed: {x.Message}");
            }
        }

        // The pack library for the left column: active packs in priority order first (numbered),
        // then the inactive ones. Each carries its description and read-only flag.
        private static string BuildPacksJson()
        {
            var config = VehicleConfigSystem.Instance;
            var packs = new List<object>();
            var activeNames = new HashSet<string>();

            if (config?.ActiveStack != null)
            {
                foreach (var p in config.ActiveStack)
                {
                    activeNames.Add(p.Name);
                    packs.Add(new { name = p.Name, description = p.Description ?? "", readOnly = p.ReadOnly, active = true });
                }
            }

            // Inactive packs still on disk: read their metadata so the library can describe them.
            foreach (var name in VehiclePack.GetPackNames())
            {
                if (activeNames.Contains(name))
                    continue;
                string description = "";
                bool readOnly = false;
                try
                {
                    var p = VehiclePack.LoadFromFile(name);
                    description = p.Description ?? "";
                    readOnly = p.ReadOnly;
                }
                catch (Exception x) { log.Warn($"Could not read pack '{name}': {x.Message}"); }
                packs.Add(new { name, description, readOnly, active = false });
            }

            var editTarget = config?.EditTarget?.Name ?? "";
            return JsonConvert.SerializeObject(new { packs, editTarget });
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
            var stack = config.ActiveStack;
            var editTarget = config.EditTarget;

            var globals = config.CombinedGlobals();

            // Walks the stack to find the winning value for one field: bottom-most pack wins, and
            // within a pack a prefab override beats a class override. Returns which pack and level won.
            (float? Val, string Pack, string Level) WinField(
                string prefabName, List<string> classes, Func<VehicleOverride, float?> sel)
            {
                float? val = null; string pack = null; string level = null;
                foreach (var p in stack)
                {
                    if (p.PrefabOverrides.TryGetValue(prefabName, out var po))
                    {
                        var x = sel(po);
                        if (x != null) { val = x; pack = p.Name; level = "prefab"; continue; }
                    }
                    foreach (var cn in classes)
                    {
                        if (p.ClassOverrides.TryGetValue(cn, out var co))
                        {
                            var x = sel(co);
                            if (x != null) { val = x; pack = p.Name; level = "class"; break; }
                        }
                    }
                }
                return (val, pack, level);
            }

            // Builds the cascade row the detail table renders. `own`/`cls` are the EDITING pack's
            // own entries (what the two editable cells hold), independent of who currently wins.
            FieldDto MakeField(
                string prefabName, List<string> classes, string className,
                Func<VehicleOverride, float?> sel, float vanillaValue, float scale, float globalFactor)
            {
                var win = WinField(prefabName, classes, sel);

                float? own = null, cls = null;
                if (editTarget != null)
                {
                    if (editTarget.PrefabOverrides.TryGetValue(prefabName, out var po)) own = sel(po);
                    if (className != null && editTarget.ClassOverrides.TryGetValue(className, out var co)) cls = sel(co);
                }

                string winner = "vanilla";
                if (win.Val != null)
                {
                    bool mine = editTarget != null && win.Pack == editTarget.Name;
                    winner = mine ? (win.Level == "prefab" ? "own" : "class") : "pack";
                }

                return new FieldDto
                {
                    Own = own * scale,
                    Cls = cls * scale,
                    Vanilla = vanillaValue * scale,
                    Effective = (win.Val ?? vanillaValue) * scale * globalFactor,
                    Winner = winner,
                    Source = winner == "pack" ? win.Pack : null,
                };
            }

            // Probability is a percentage of vanilla (100 = vanilla) and only real for personal cars.
            FieldDto MakeProbField(string prefabName, List<string> classes, string className)
            {
                var win = WinField(prefabName, classes, o => o.ProbabilityPercent);

                float? own = null, cls = null;
                if (editTarget != null)
                {
                    if (editTarget.PrefabOverrides.TryGetValue(prefabName, out var po)) own = po.ProbabilityPercent;
                    if (className != null && editTarget.ClassOverrides.TryGetValue(className, out var co)) cls = co.ProbabilityPercent;
                }

                string winner = "vanilla";
                if (win.Val != null)
                {
                    bool mine = editTarget != null && win.Pack == editTarget.Name;
                    winner = mine ? (win.Level == "prefab" ? "own" : "class") : "pack";
                }

                return new FieldDto
                {
                    Own = own,
                    Cls = cls,
                    Vanilla = 100f,
                    Effective = (win.Val ?? 100f) * globals.ProbabilityFactor,
                    Winner = winner,
                    Source = winner == "pack" ? win.Pack : null,
                };
            }

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
            EnsureCat("air", "Aircraft");
            EnsureCat("ships", "Ships");
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
                else if (EntityManager.HasComponent<WatercraftData>(entity))
                {
                    catKey = "ships";
                    catName = "Ships";
                }
                else if (EntityManager.HasComponent<AircraftData>(entity))
                {
                    catKey = "air";
                    catName = "Aircraft";
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

                var classes = config.GetEffectiveClasses(prefabName);
                var className = classes.Count > 0 ? classes[0] : "Unclassified";

                // The game renders a thumbnail of the actual model on demand (works for custom assets too).
                string thumbnail = null;
                if (_prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
                    thumbnail = ImageSystem.GetThumbnail(prefabBase);

                var prefabDto = new PrefabDto
                {
                    Id = prefabName,
                    // Display the localized asset name (e.g. "Astra") instead of the internal id.
                    Name = LocaleHelper.Translate($"Assets.NAME[{prefabName}]", prefabName) ?? prefabName,
                    ClassName = className,
                    Thumbnail = thumbnail,
                    // Real "custom asset" (mod) detection needs prefab source info; not mislabeling
                    // vanilla-but-unclassified vehicles (trains/buses) as custom for now.
                    Custom = false,
                    // Spawn probability only exists for naturally spawning vehicles (personal cars).
                    Spawns = catKey == "cars",
                    Probability = MakeProbField(prefabName, classes, className),
                    MaxSpeed = MakeField(prefabName, classes, className, o => o.MaxSpeed, baseline.MaxSpeed, MsToKmh, globals.SpeedFactor),
                    Acceleration = MakeField(prefabName, classes, className, o => o.Acceleration, baseline.Acceleration, 1f, globals.AccelerationFactor),
                    Braking = MakeField(prefabName, classes, className, o => o.Braking, baseline.Braking, 1f, globals.BrakingFactor),
                };
                prefabDto.Edited = prefabDto.MaxSpeed.Own != null || prefabDto.Acceleration.Own != null
                                   || prefabDto.Braking.Own != null || prefabDto.Probability.Own != null;

                var cat = EnsureCat(catKey, catName);
                var idx = classIndex[catKey];
                if (!idx.TryGetValue(className, out var classDto))
                {
                    // Class-level values are edited per pack, so the tree shows the edit-target pack's class override.
                    VehicleOverride classOver = null;
                    editTarget?.ClassOverrides.TryGetValue(className, out classOver);
                    classDto = new ClassDto
                    {
                        Name = className,
                        // "Unclassified" is a UI-only bucket with no real members to share values with.
                        Editable = className != "Unclassified",
                        Custom = editTarget != null && editTarget.CustomClasses.Contains(className),
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
    }
}
