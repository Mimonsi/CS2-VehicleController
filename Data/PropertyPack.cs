using System.Collections.Generic;
using System.IO;
using Colossal.PSI.Environment;
using Newtonsoft.Json;

namespace VehicleController.Data
{
    public record PropertyPackEntry
    {
        public string PrefabName;
        public int MaxSpeed;
        public int Acceleration;
        public int Braking;

        public PropertyPackEntry(string prefabName)
        {
            PrefabName = prefabName;
        }
        
        public static PropertyPackEntry Default()
        {
            return new PropertyPackEntry("default")
            {
                PrefabName = "Default",
            };
        }
    }
    
    public record PropertyPack
    {
        public int Version = 1;
        public string Name;
        private Dictionary<string, PropertyPackEntry>? _entries;
        
        public PropertyPack(string name, int version = 1)
        {
            Name = name;
            Version = version;
        }
        
        public static PropertyPack LoadFromFile(string name)
        {
            Mod.Logger.Info("Loading probability pack " + name);
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "property", name + ".json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Could not load property pack {path}, because the file doesn't exist");
            }
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<PropertyPack>(json) ?? throw new InvalidDataException($"Failed to deserialize property pack {name}");
        }
        
        public void SaveToFile()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "property", Name + ".json");
        }
        
        public PropertyPackEntry GetEntry(string prefabName)
        {
            if (_entries == null || !_entries.TryGetValue(prefabName, out var entry))
            {
                return PropertyPackEntry.Default(); // Default probability
            }
            return entry;
        }

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

        public static IEnumerable<string> GetPackNames()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleClass), "packs", "property");
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