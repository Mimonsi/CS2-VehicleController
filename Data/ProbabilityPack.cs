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

        /// <summary>
        /// Determines if this entry targets a single prefab or an entire vehicle class.
        /// </summary>
        /// <returns>The type of probability override this entry represents.</returns>
        /// <exception cref="InvalidOperationException">Thrown when neither <see cref="PrefabName"/> nor <see cref="ClassName"/> are set.</exception>
        public ProbabilityEntryType Type()
        {
            if (!string.IsNullOrEmpty(PrefabName))
                return ProbabilityEntryType.Prefab;
            if (!string.IsNullOrEmpty(ClassName))
                return ProbabilityEntryType.Class;
            throw new InvalidOperationException("ProbabilityPackEntry must have either PrefabName or ClassName set.");
        }

        
        /// <summary>Parameterless constructor for serialization.</summary>
        public ProbabilityPackEntry()
        {

        }
        
        /// <summary>
        /// Creates a new probability entry for a prefab or vehicle class.
        /// </summary>
        /// <param name="type">Indicates if the entry is for a prefab or class.</param>
        /// <param name="name">Name of the prefab or class.</param>
        /// <param name="probability">Probability value to apply.</param>
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

        /// <summary>
        /// Creates a new probability pack container.
        /// </summary>
        /// <param name="name">Display name of the pack.</param>
        /// <param name="version">Optional version for compatibility checks.</param>
        public ProbabilityPack(string name, int version = 1)
        {
            Name = name;
            Version = version;
            Entries = new List<ProbabilityPackEntry>();
        }

        /// <summary>
        /// Returns a small example pack demonstrating the file format.
        /// </summary>
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
        
        /// <summary>
        /// Loads a probability pack from disk.
        /// </summary>
        /// <param name="name">File name without extension.</param>
        /// <returns>Loaded probability pack or a placeholder if missing.</returns>
        public static ProbabilityPack LoadFromFile(string name)
        {
            Mod.Logger.Info("Loading probability pack " + name);
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

        /// <summary>
        /// Saves this pack into the user's ModsData folder.
        /// </summary>
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
                Mod.Logger.Error($"Error saving probability pack {Name}: {ex.Message}");
                throw;
            }

            Mod.Logger.Info($"Saved probability pack {Name} to {path}");
        }
        
        /// <summary>
        /// Checks if the pack already contains an entry for the given prefab or class.
        /// </summary>
        /// <param name="name">Prefab or class name.</param>
        /// <returns>True if an entry exists.</returns>
        public bool Contains(string name)
        {
            return Entries.Exists(e => e.PrefabName == name || e.ClassName == name);
        }
        
        /// <summary>
        /// Adds or updates a prefab entry in the pack.
        /// </summary>
        /// <remarks>
        /// The actual implementation is currently disabled. Once implemented this method
        /// should insert the entry or modify the existing one.
        /// </remarks>
        public bool AddEntry(string prefabName, int probability)
        {
            // TODO: implement storage of new entries
            return true;
        }

        /// <summary>
        /// Attempts to retrieve an entry either for a prefab or one of its classes.
        /// </summary>
        /// <param name="name">Prefab name to lookup.</param>
        /// <param name="entry">Returns the found entry.</param>
        /// <returns>True if an entry was found.</returns>
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

        /// <summary>
        /// Looks for an entry that specifically targets the given prefab.
        /// </summary>
        public bool TryGetPrefabEntry(string prefabName, out ProbabilityPackEntry? entry)
        {
            entry = Entries.Find(e => e.PrefabName == prefabName);
            if (entry != null)
                return true;
            return false;
        }
        
        /// <summary>
        /// Looks for an entry for the specified vehicle class.
        /// </summary>
        public bool TryGetClassEntry(string className, out ProbabilityPackEntry? entry)
        {
            entry = Entries.Find(e => e.ClassName == className);
            if (entry != null)
                return true;
            return false;
        }
        
        /// <summary>
        /// Retrieves the probability value for a prefab if present.
        /// </summary>
        /// <param name="prefabName">Name of the prefab.</param>
        /// <param name="probability">Returns the probability value.</param>
        /// <returns>True when an entry was found.</returns>
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

        /// <summary>
        /// Enumerates all probability pack file names found in the ModsData folder.
        /// </summary>
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