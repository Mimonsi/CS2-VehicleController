using System.Collections.Generic;
using System.IO;
using Colossal.PSI.Environment;
using Newtonsoft.Json;

namespace VehicleController.Data
{
    /// <summary>
    /// Holds vehicle property overrides for a single prefab.
    /// </summary>
    public record PropertyPackEntry
    {
        public string PrefabName;
        public int MaxSpeed;
        public int Acceleration;
        public int Braking;

        /// <summary>
        /// Creates a new entry for the given prefab.
        /// </summary>
        public PropertyPackEntry(string prefabName)
        {
            PrefabName = prefabName;
        }
        
        /// <summary>
        /// Returns a placeholder entry containing default values.
        /// </summary>
        public static PropertyPackEntry Default()
        {
            return new PropertyPackEntry("default")
            {
                PrefabName = "Default",
            };
        }
    }
    
    /// <summary>
    /// Collection of property overrides for multiple vehicle prefabs.
    /// </summary>
    public record PropertyPack
    {
        public int Version = 1;
        public string Name;
        private Dictionary<string, PropertyPackEntry>? _entries;
        
        /// <summary>
        /// Creates a new property pack.
        /// </summary>
        /// <param name="name">Display name of the pack.</param>
        /// <param name="version">Optional format version.</param>
        public PropertyPack(string name, int version = 1)
        {
            Name = name;
            Version = version;
        }
        
        /// <summary>
        /// Reads a pack definition from disk.
        /// </summary>
        public static PropertyPack LoadFromFile(string name)
        {
            Mod.log.Info("Loading probability pack " + name);
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "property", name + ".json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Could not load property pack {path}, because the file doesn't exist");
            }
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<PropertyPack>(json) ?? throw new InvalidDataException($"Failed to deserialize property pack {name}");
        }
        
        /// <summary>
        /// Persists the pack to a file in the ModsData folder.
        /// </summary>
        public void SaveToFile()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "property", Name + ".json");
        }
        
        /// <summary>
        /// Retrieves the properties for a prefab, returning defaults if none are defined.
        /// </summary>
        public PropertyPackEntry GetEntry(string prefabName)
        {
            if (_entries == null || !_entries.TryGetValue(prefabName, out var entry))
            {
                return PropertyPackEntry.Default();
            }
            return entry;
        }

        /// <summary>
        /// Returns a simple pack containing only default values.
        /// </summary>
        public static PropertyPack Default()
        {
            return new PropertyPack("Default")
            {
                _entries = new Dictionary<string, PropertyPackEntry>
                {
                    { "default", PropertyPackEntry.Default() }
                }
            };
        }

        /// <summary>
        /// Lists available property pack names in the ModsData folder.
        /// </summary>
        public static IEnumerable<string> GetPackNames()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs", "property");
            if (!Directory.Exists(path))
                return new List<string>();

            var files = Directory.GetFiles(path, "*.json");
            List<string> names = new List<string>();
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                names.Add(name);
            }

            return names;
        }
    }
}