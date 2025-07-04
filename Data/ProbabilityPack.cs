using System;
using System.Collections.Generic;
using System.IO;
using Colossal.PSI.Environment;
using Newtonsoft.Json;

namespace VehicleController.Data
{
    public enum ProbabilityEntryType
    {
        Prefab,
        Class
    }
    
    /// <summary>
    /// Can either be overriding probabilities for a prefab or a whole vehicle class
    /// </summary>
    public record ProbabilityPackEntry
    {
        public ProbabilityEntryType Type { get; set; } = ProbabilityEntryType.Prefab;
        public string? PrefabName;
        public string? ClassName;
        public int Probability = 100;

        
        public ProbabilityPackEntry()
        {

        }
        
        public ProbabilityPackEntry(ProbabilityEntryType type, string name, int probability)
        {
            if (type == ProbabilityEntryType.Prefab)
            {
                PrefabName = name;
            }
            else
            {
                ClassName = name;
            }
            Type = type;
            Probability = probability;
        }
        
        public static ProbabilityPackEntry Default()
        {
            return new ProbabilityPackEntry();
        }
    }
    
    public record ProbabilityPack
    {
        public int Version = 1;
        public string Name;
        public string? Description;
        public List<ProbabilityPackEntry> Entries;

        public ProbabilityPack(string name, int version = 1)
        {
            Name = name;
            Version = version;
        }

        public static ProbabilityPack Example()
        {
            var pack = new ProbabilityPack("Example")
            {
                Version = 1,
                Description = "This is an example probability pack",
                Entries = new List<ProbabilityPackEntry>
                {
                    { new(ProbabilityEntryType.Prefab, "Car01", 200)},
                    { new(ProbabilityEntryType.Class, "Pickup", 100)},
                }
            };
            return pack;
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
                "probability");
            var json = JsonConvert.SerializeObject(this, Formatting.Indented) ?? throw new InvalidDataException($"Failed to serialize probability pack {Name}");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            try
            {
                File.WriteAllText(Path.Combine(path, Name + ".json"), json);
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error saving probability pack {Name}: {ex.Message}");
                throw;
            }

            Mod.log.Info($"Saved probability pack {Name} to {path}");
        }
        
        public bool Contains(string name)
        {
            return Entries.Exists(e => e.PrefabName == name || e.ClassName == name);
        }
        
        public bool AddEntry(string prefabName, int probability)
        {
            /*if (Contains(prefabName))
            {
                return false;
            }

            Entries.Add(prefabName, new ProbabilityPackEntry(prefabName) { Probability = probability });*/
            return true;
        }
        
        public ProbabilityPackEntry GetEntry(string prefabName)
        {
            var entry = Entries.Find(e => e.PrefabName == prefabName || e.ClassName == prefabName);
            return entry;
        }
        
        public int GetProbability(string prefabName)
        {
            return GetEntry(prefabName).Probability;
        }

        public static ProbabilityPack Default()
        {
            return new ProbabilityPack("Default")
            {
                Entries = new List<ProbabilityPackEntry>
                {
                    { ProbabilityPackEntry.Default() }
                }
            };
        }

        public static IEnumerable<string> GetPackNames()
        {
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs", "probability");
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