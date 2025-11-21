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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ClassName;
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? PrefabName;
        public float MaxSpeed;
        public float Acceleration;
        public float Braking;
        
        /// <summary>
        /// Returns a placeholder entry containing default values.
        /// </summary>
        public static PropertyPackEntry Default()
        {
            return new PropertyPackEntry
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
        public Dictionary<string, PropertyPackEntry>? Entries;
        
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
            Mod.log.Info($"Loading property pack {name}");
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "property", name + ".json");
            Mod.log.Trace($"Loading path: {path}");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Could not load property pack {path}, because the file doesn't exist");
            }
            var json = File.ReadAllText(path);
            //Mod.log.Trace($"Pack file content: {json}");
            return JsonConvert.DeserializeObject<PropertyPack>(json) ?? throw new InvalidDataException($"Failed to deserialize property pack {name}");
        }
        
        /// <summary>
        /// Write a set of entries to a new pack file.
        /// </summary>
        public static void SaveEntriesToFile(Dictionary<string, PropertyPackEntry> entries, string name, int version=1)
        {
            var pack = new PropertyPack(name, version);
            pack.Entries = entries;
            pack.SaveToFile();
        }

        
        /// <summary>
        /// Persists the pack to a file in the ModsData folder.
        /// </summary>
        public void SaveToFile()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "property", Name + ".json");
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
            Mod.log.Info($"Saved property pack to {path}");
        }
        
        /// <summary>
        /// Retrieves the properties for a prefab, returning defaults if none are defined.
        /// </summary>
        public PropertyPackEntry? GetEntry(string prefabName)
        {
            if (Entries == null || !Entries.TryGetValue(prefabName, out var entry))
            {
                return null;
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
                Version =  1,
                Name = "Default",
                Entries = new Dictionary<string, PropertyPackEntry>
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