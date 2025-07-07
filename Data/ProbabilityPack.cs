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
        public string? PrefabName;
        public string? ClassName;
        public int Probability = 100;

        public ProbabilityEntryType Type()
        {
            if (!string.IsNullOrEmpty(PrefabName))
                return ProbabilityEntryType.Prefab;
            if (!string.IsNullOrEmpty(ClassName))
                return ProbabilityEntryType.Class;
            throw new InvalidOperationException("ProbabilityPackEntry must have either PrefabName or ClassName set.");
        }

        
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
            Probability = probability;
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
            Entries = new List<ProbabilityPackEntry>();
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
                return new ProbabilityPack("Missing");
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

        public bool TryGetEntry(string name, out ProbabilityPackEntry? entry)
        {
            // If prefab override, return it
            if (TryGetPrefabEntry(name, out entry))
            {
                return true;
            }
            // Check for class value
            List<string> classes = VehicleClass.GetClassesForPrefab(name);
            if (classes.Count > 0)
            {
                // If there are multiple classes, return the first one
                foreach (var className in classes)
                {
                    entry = Entries.Find(e => e.ClassName == className);
                    if (entry != null)
                        return true;
                }
            }
            return false;
        }

        public bool TryGetPrefabEntry(string prefabName, out ProbabilityPackEntry? entry)
        {
            entry = Entries.Find(e => e.PrefabName == prefabName);
            if (entry != null)
                return true;
            return false;
        }
        
        public bool TryGetClassEntry(string className, out ProbabilityPackEntry? entry)
        {
            entry = Entries.Find(e => e.ClassName == className);
            if (entry != null)
                return true;
            return false;
        }
        
        public bool TryGetProbability(string prefabName, out int probability)
        {
            probability = 0;
            if (TryGetEntry(prefabName, out var entry))
            {
                probability = entry.Probability;
                return true;
            }

            return false;
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