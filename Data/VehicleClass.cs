using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.PSI.Environment;
using Newtonsoft.Json;

namespace VehicleController.Data
{

    /// <summary>
    /// Groups vehicle prefabs into classes
    /// Can be used instead of per-prefab settings for probability and property packs
    /// Planned: Maybe allow subclasses in the future
    /// Planned: Allow player customization of classes
    /// TODO: Find out what other classes exist
    /// </summary>
    public class VehicleClass
    {
        public string Name;
        public int VanillaProbability;
        public string[] Prefabs;

        private static Dictionary<string, VehicleClass> _vehicleClasses;

        /// <summary>
        /// Initializes the static list of vehicle classes with builtin values. TODO: Load from json file
        /// </summary>
        static VehicleClass()
        {
            /*var vehicleClasses = new[]
            {
                new VehicleClass()
                {
                    Name = "Motorbike",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Motorbike01"}
                },
                new VehicleClass()
                {
                    Name = "Scooter",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Scooter01"}
                },
                new VehicleClass()
                {
                    Name = "City Car",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car01"}
                },
                new VehicleClass()
                {
                    Name = "Hatchback",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car02", "Car03"}
                },
                new VehicleClass()
                {
                    Name = "Minivan",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car04"}
                },
                new VehicleClass()
                {
                    Name = "Sedan",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car05", "Car06"}
                },
                new VehicleClass()
                {
                    Name = "Sports Car",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car07"}
                },
                new VehicleClass()
                {
                    Name = "Pickup",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car08"}
                },
                new VehicleClass()
                {
                    Name = "SUV",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car09"}
                },
                new VehicleClass()
                {
                    Name = "Muscle Car",
                    VanillaProbability = 100,
                    Prefabs = new[] {"MuscleCar01", "MuscleCar02", "MuscleCar03", "MuscleCar04", "MuscleCar05"},
                },
                new VehicleClass()
                {
                    Name = "Van",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Van01"}
                }
            };
            _vehicleClasses = new Dictionary<string, VehicleClass>();
            foreach (var vehicleClass in vehicleClasses)
            {
                _vehicleClasses.Add(vehicleClass.Name, vehicleClass);
            }*/
            var path = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "vehicleClasses.json");
            LoadFromFile(path);
        }

        private static void LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var x = JsonConvert.DeserializeObject<VehicleClass[]>(json) ?? throw new InvalidDataException($"Failed to deserialize vehicle classes from {path}");
            _vehicleClasses = new Dictionary<string, VehicleClass>();
            foreach (var vehicleClass in x)
            {
                _vehicleClasses.Add(vehicleClass.Name, vehicleClass);
            }
        }
        
        /// <summary>
        /// Returns the names of all available vehicle classes.
        /// </summary>
        public static string[] GetNames()
        {
            return _vehicleClasses.Keys.ToArray();
        }

        /// <summary>
        /// Updates the vanilla probability value for an existing vehicle class.
        /// </summary>
        public static void SetVanillaProbability(string className, int vanillaProbability)
        {
            if (_vehicleClasses.TryGetValue(className, out var vehicleClass))
            {
                vehicleClass.VanillaProbability = vanillaProbability;
            }
        }

        /// <summary>
        /// Finds the class definition for the specified prefab.
        /// </summary>
        public static VehicleClass GetVehicleClass(string prefabName)
        {
            foreach (var vehicleClass in _vehicleClasses.Values)
            {
                if (vehicleClass.Prefabs.Contains(prefabName))
                {
                    return vehicleClass;
                }
            }

            Mod.log.Info($"Vehicle class for vehicle {prefabName} not found, using Sedan class as default");
            return _vehicleClasses["Sedan"];
        }

        /// <summary>
        /// Helper for settings UI to produce sanitized class names.
        /// </summary>
        public static (string, string)[] GetSettingClassNames()
        {
            // Replace spaces with empty string
            return _vehicleClasses.Keys.Select(name => (name.Replace(" ", ""), name)).ToArray();
        }

        /// <summary>
        /// Returns all classes that contain the specified prefab.
        /// </summary>
        public static List<string> GetClassesForPrefab(string name)
        {
            var classes = new List<string>();
            foreach (var vehicleClass in _vehicleClasses.Values)
            {
                if (vehicleClass.Prefabs.Contains(name))
                {
                    classes.Add(vehicleClass.Name);
                }
            }
            return classes;
        }
    }
}