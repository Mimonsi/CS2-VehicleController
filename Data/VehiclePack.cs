using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.PSI.Environment;
using Newtonsoft.Json;

namespace VehicleController.Data
{
    /// <summary>
    /// A sparse set of value overrides for a single prefab or vehicle class.
    /// Any field left <c>null</c> means "inherit from the next level of the cascade".
    /// Speed/acceleration/braking are stored in the game's internal units (m/s, m/s²),
    /// matching CarData/TrainData; the UI converts to km/h for display.
    /// Probability is a percentage of the vanilla value (100 = vanilla).
    /// </summary>
    public class VehicleOverride
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ProbabilityPercent;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? MaxSpeed;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? Acceleration;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? Braking;

        [JsonIgnore]
        public bool IsEmpty =>
            ProbabilityPercent == null && MaxSpeed == null && Acceleration == null && Braking == null;

        public VehicleOverride Clone() => new VehicleOverride
        {
            ProbabilityPercent = ProbabilityPercent,
            MaxSpeed = MaxSpeed,
            Acceleration = Acceleration,
            Braking = Braking,
        };
    }

    /// <summary>Global multipliers applied on top of the resolved per-vehicle values.</summary>
    public class GlobalMultipliers
    {
        public float ProbabilityFactor = 1f;
        public float SpeedFactor = 1f;
        public float AccelerationFactor = 1f;
        public float BrakingFactor = 1f;

        public GlobalMultipliers Clone() => new GlobalMultipliers
        {
            ProbabilityFactor = ProbabilityFactor,
            SpeedFactor = SpeedFactor,
            AccelerationFactor = AccelerationFactor,
            BrakingFactor = BrakingFactor,
        };
    }

    /// <summary>The vanilla baseline for a prefab, read from the game once and fed into the resolver.</summary>
    public struct VanillaBaseline
    {
        public int Probability;
        public float MaxSpeed;
        public float Acceleration;
        public float Braking;
    }

    /// <summary>The final values the resolver produces, ready to be written to the ECS components.</summary>
    public struct ResolvedVehicle
    {
        public int Probability;
        public float MaxSpeed;
        public float Acceleration;
        public float Braking;
    }

    public enum MergeConflictPolicy
    {
        KeepMine,
        TakeTheirs,
    }

    /// <summary>
    /// A unified, sparse configuration for vehicle spawn probability and driving properties.
    /// Supersedes the split ProbabilityPack/PropertyPack. Only stores what the user changed;
    /// everything else resolves to the vanilla baseline through the cascade
    /// (prefab override → class override → vanilla), then global multipliers.
    /// </summary>
    public class VehiclePack
    {
        public const int CurrentVersion = 2;
        private const string SubFolder = "vehicle";

        public int Version = CurrentVersion;
        public string Name;
        public string? Description;

        /// <summary>When true this pack is a shipped/shared template and must be duplicated before editing.</summary>
        public bool ReadOnly;

        /// <summary>
        /// Multipliers applied after the cascade resolves. There is no UI for these; they exist so a
        /// hand-written or shipped pack can scale values. Multipliers of all active packs multiply
        /// together, so an untouched pack (all 1.0) has no effect.
        /// </summary>
        public GlobalMultipliers Global = new GlobalMultipliers();

        /// <summary>Overrides keyed by vehicle class name (see <see cref="VehicleClass"/>).</summary>
        public Dictionary<string, VehicleOverride> ClassOverrides = new Dictionary<string, VehicleOverride>();

        /// <summary>Overrides keyed by prefab name. Works for custom assets, which are keyed by their prefab name.</summary>
        public Dictionary<string, VehicleOverride> PrefabOverrides = new Dictionary<string, VehicleOverride>();

        /// <summary>
        /// Extra class memberships authored by the user, keyed by class name → prefab names.
        /// Supplements the built-in <see cref="VehicleClass"/> membership so that custom assets
        /// (which belong to no built-in class) can be assigned to a class.
        /// </summary>
        public Dictionary<string, List<string>> ClassMembership = new Dictionary<string, List<string>>();

        /// <summary>User-created class names that live in this pack, on top of the built-in <see cref="VehicleClass"/> names.</summary>
        public List<string> CustomClasses = new List<string>();

        [JsonConstructor]
        public VehiclePack(string name, int version = CurrentVersion)
        {
            Name = name;
            Version = version;
        }

        // ---- Cascade resolution (per-pack; the stack resolver lives in VehicleConfigSystem) -----

        private T? FirstClassValue<T>(List<string> classes, Func<VehicleOverride, T?> selector) where T : struct
        {
            foreach (var className in classes)
            {
                if (ClassOverrides.TryGetValue(className, out var over) && over != null)
                {
                    var value = selector(over);
                    if (value.HasValue)
                        return value;
                }
            }
            return null;
        }

        // ---- Per-pack field resolution (for the layered stack) ------------------
        // Each returns this pack's value for a field, or null if this pack doesn't define one:
        // its own prefab override wins, then the first class override over the given classes.
        // The stack resolver (VehicleConfigSystem) walks packs top→bottom and takes the first hit.

        public int? ProbabilityPercentFor(string prefabName, List<string> classes)
        {
            if (PrefabOverrides.TryGetValue(prefabName, out var po) && po.ProbabilityPercent != null)
                return po.ProbabilityPercent;
            return FirstClassValue<int>(classes, o => o.ProbabilityPercent);
        }

        public float? MaxSpeedFor(string prefabName, List<string> classes) => FieldFor(prefabName, classes, o => o.MaxSpeed);
        public float? AccelerationFor(string prefabName, List<string> classes) => FieldFor(prefabName, classes, o => o.Acceleration);
        public float? BrakingFor(string prefabName, List<string> classes) => FieldFor(prefabName, classes, o => o.Braking);

        public float? FieldFor(string prefabName, List<string> classes, Func<VehicleOverride, float?> selector)
        {
            if (PrefabOverrides.TryGetValue(prefabName, out var po))
            {
                var v = selector(po);
                if (v != null)
                    return v;
            }
            return FirstClassValue<float>(classes, selector);
        }

        /// <summary>Classes this pack explicitly assigns the prefab to (empty if none).</summary>
        public List<string> AssignedClasses(string prefabName)
        {
            var assigned = new List<string>();
            foreach (var pair in ClassMembership)
                if (pair.Value != null && pair.Value.Contains(prefabName))
                    assigned.Add(pair.Key);
            return assigned;
        }

        // ---- Editing helpers ----------------------------------------------------

        public void SetPrefabOverride(string prefabName, VehicleOverride over)
        {
            if (over == null || over.IsEmpty)
                PrefabOverrides.Remove(prefabName);
            else
                PrefabOverrides[prefabName] = over;
        }

        public void SetClassOverride(string className, VehicleOverride over)
        {
            if (over == null || over.IsEmpty)
                ClassOverrides.Remove(className);
            else
                ClassOverrides[className] = over;
        }

        public void AssignToClass(string className, string prefabName)
        {
            if (!ClassMembership.TryGetValue(className, out var list))
            {
                list = new List<string>();
                ClassMembership[className] = list;
            }
            if (!list.Contains(prefabName))
                list.Add(prefabName);
        }

        public static bool IsBuiltInClass(string name) => Array.IndexOf(VehicleClass.GetNames(), name) >= 0;

        public void CreateCustomClass(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!CustomClasses.Contains(name) && !IsBuiltInClass(name))
                CustomClasses.Add(name);
        }

        /// <summary>
        /// Assigns a prefab to exactly one class (removing any prior pack assignment first).
        /// An empty class name unassigns. Auto-creates the class as a custom class if needed.
        /// </summary>
        public void AssignPrefabToClass(string prefabName, string className)
        {
            foreach (var list in ClassMembership.Values)
                list.Remove(prefabName);

            if (!string.IsNullOrEmpty(className))
            {
                CreateCustomClass(className); // no-op if built-in or already exists
                AssignToClass(className, prefabName);
            }
            CleanEmptyMembership();
        }

        public void RenameCustomClass(string oldName, string newName)
        {
            if (!CustomClasses.Contains(oldName)) return; // built-in classes cannot be renamed
            if (string.IsNullOrWhiteSpace(newName) || CustomClasses.Contains(newName) || IsBuiltInClass(newName)) return;

            CustomClasses[CustomClasses.IndexOf(oldName)] = newName;
            if (ClassMembership.TryGetValue(oldName, out var mem)) { ClassMembership.Remove(oldName); ClassMembership[newName] = mem; }
            if (ClassOverrides.TryGetValue(oldName, out var ov)) { ClassOverrides.Remove(oldName); ClassOverrides[newName] = ov; }
        }

        public void DeleteCustomClass(string name)
        {
            if (!CustomClasses.Contains(name)) return; // built-in classes cannot be deleted
            CustomClasses.Remove(name);
            ClassMembership.Remove(name);
            ClassOverrides.Remove(name);
        }

        private void CleanEmptyMembership()
        {
            var empty = ClassMembership.Where(p => p.Value == null || p.Value.Count == 0).Select(p => p.Key).ToList();
            foreach (var k in empty)
                ClassMembership.Remove(k);
        }

        // ---- Merge --------------------------------------------------------------

        /// <summary>
        /// Merges another (sparse) pack into this one. Because packs are sparse this composes cleanly;
        /// overlapping entries are resolved by <paramref name="policy"/>.
        /// </summary>
        public void Merge(VehiclePack other, MergeConflictPolicy policy)
        {
            if (other == null) return;

            MergeOverrides(PrefabOverrides, other.PrefabOverrides, policy);
            MergeOverrides(ClassOverrides, other.ClassOverrides, policy);

            foreach (var name in other.CustomClasses)
                if (!CustomClasses.Contains(name))
                    CustomClasses.Add(name);

            foreach (var pair in other.ClassMembership)
            {
                foreach (var prefab in pair.Value)
                    AssignToClass(pair.Key, prefab);
            }
        }

        private static void MergeOverrides(
            Dictionary<string, VehicleOverride> mine,
            Dictionary<string, VehicleOverride> theirs,
            MergeConflictPolicy policy)
        {
            foreach (var pair in theirs)
            {
                if (!mine.ContainsKey(pair.Key) || policy == MergeConflictPolicy.TakeTheirs)
                    mine[pair.Key] = pair.Value.Clone();
            }
        }

        // ---- Persistence --------------------------------------------------------

        private static string PackFolder()
            => Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs", SubFolder);

        public static VehiclePack LoadFromFile(string name)
        {
            var path = Path.Combine(PackFolder(), name + ".json");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Could not load vehicle pack, file does not exist: {path}");

            var json = File.ReadAllText(path);
            var pack = JsonConvert.DeserializeObject<VehiclePack>(json)
                       ?? throw new InvalidDataException($"Failed to deserialize vehicle pack {name}");
            pack.Name = name;
            return pack;
        }

        public void SaveToFile()
        {
            if (ReadOnly)
            {
                Mod.log.Warn($"Refusing to save read-only vehicle pack '{Name}'. Duplicate it first.");
                return;
            }

            var folder = PackFolder();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path.Combine(folder, Name + ".json"), json);
            Mod.log.Info($"Saved vehicle pack '{Name}' with {PrefabOverrides.Count} prefab and {ClassOverrides.Count} class overrides.");
        }

        public VehiclePack Duplicate(string newName)
        {
            var copy = new VehiclePack(newName)
            {
                Description = Description,
                ReadOnly = false,
                Global = Global.Clone(),
                ClassOverrides = ClassOverrides.ToDictionary(p => p.Key, p => p.Value.Clone()),
                PrefabOverrides = PrefabOverrides.ToDictionary(p => p.Key, p => p.Value.Clone()),
                ClassMembership = ClassMembership.ToDictionary(p => p.Key, p => new List<string>(p.Value)),
                CustomClasses = new List<string>(CustomClasses),
            };
            return copy;
        }

        public static List<string> GetPackNames()
        {
            var folder = PackFolder();
            if (!Directory.Exists(folder))
                return new List<string>();
            return Directory.GetFiles(folder, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                // Files starting with "_" are internal state (e.g. _active.json), not packs.
                .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith("_"))
                .ToList();
        }

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static VehiclePack? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return JsonConvert.DeserializeObject<VehiclePack>(json);
        }

        public static void DeleteFile(string name)
        {
            var path = Path.Combine(PackFolder(), name + ".json");
            if (File.Exists(path))
                File.Delete(path);
        }

        // Remembers which pack is active across sessions (a plain-text marker, not a .json pack).
        private static string ActiveMarkerPath() => Path.Combine(PackFolder(), "_active.txt");

        public static void SaveActiveName(string name)
        {
            var folder = PackFolder();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            File.WriteAllText(ActiveMarkerPath(), name);
        }

        public static string? LoadActiveName()
        {
            var path = ActiveMarkerPath();
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }

        // ---- Active stack config -----------------------------------------------
        // The layered configuration: which packs are active, in priority order (index 0 = top),
        // which one edits target, and the single global multiplier set (lifted out of packs).

        private static string StackConfigPath() => Path.Combine(PackFolder(), "_active.json");

        public static void SaveStackConfig(StackConfig config)
        {
            var folder = PackFolder();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            File.WriteAllText(StackConfigPath(), JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        /// <summary>Loads the active-stack config, or null if none exists yet (caller falls back).</summary>
        public static StackConfig? LoadStackConfig()
        {
            var path = StackConfigPath();
            if (!File.Exists(path))
                return null;
            try { return JsonConvert.DeserializeObject<StackConfig>(File.ReadAllText(path)); }
            catch { return null; }
        }
    }

    /// <summary>Persisted layered configuration (see <see cref="VehiclePack.SaveStackConfig"/>).</summary>
    public class StackConfig
    {
        /// <summary>Active pack names, highest priority first (index 0 = top).</summary>
        public List<string> Packs = new List<string>();

        /// <summary>Name of the pack that edits are written to.</summary>
        public string? EditTarget;
    }
}
