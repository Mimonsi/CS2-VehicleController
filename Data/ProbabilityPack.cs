using System.Collections.Generic;
using System.IO;
using Colossal.PSI.Environment;
using Newtonsoft.Json;

namespace VehicleController.Data
{
    public record ProbabilityPackEntry
    {
        public string PrefabName;
        public int Probability = 100;

        public ProbabilityPackEntry(string prefabName)
        {
            PrefabName = prefabName;
        }
        
        public static ProbabilityPackEntry Default()
        {
            return new ProbabilityPackEntry("default")
            {
                PrefabName = "Default",
                Probability = 100
            };
        }
    }
    
    public record ProbabilityPack
    {
        public int Version = 1;
        public string Name;
        private Dictionary<string, ProbabilityPackEntry>? _entries;

        public ProbabilityPack(string name, int version = 1)
        {
            Name = name;
            Version = version;
        }
        
        public static ProbabilityPack LoadFromFile(string name)
        {
            Mod.log.Info("Loading probability pack " + name);
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "probability", name + ".json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Could not load probability pack {path}, because the file doesn't exist");
            }
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ProbabilityPack>(json) ?? throw new InvalidDataException($"Failed to deserialize probability pack {name}");
        }

        public void SaveToFile()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                "probability", Name + ".json");
        }
        
        public bool AddEntry(string prefabName, int probability)
        {
            if (_entries == null)
            {
                _entries = new Dictionary<string, ProbabilityPackEntry>();
                return true;
            }
            if (_entries.ContainsKey(prefabName))
            {
                return false;
            }

            _entries.Add(prefabName, new ProbabilityPackEntry(prefabName) { Probability = probability });
            return true;
        }
        
        public ProbabilityPackEntry GetEntry(string prefabName)
        {
            if (_entries == null || !_entries.TryGetValue(prefabName, out var entry))
            {
                return ProbabilityPackEntry.Default(); // Default probability
            }
            return entry;
        }
        
        public int GetProbability(string prefabName)
        {
            return GetEntry(prefabName).Probability;
        }

        public static ProbabilityPack Default()
        {
            return new ProbabilityPack
            {
                Name = "Default",
                _entries = new Dictionary<string, ProbabilityPackEntry>
                {
                    { "default", ProbabilityPackEntry.Default() }
                }
            };
        }

        public static IEnumerable<string> GetPackNames()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleClass), "packs", "probability");
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