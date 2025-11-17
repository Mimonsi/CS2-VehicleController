using System;
using System.Collections.Generic;
using System.Linq;

namespace VehicleController.Data
{

    /// <summary>
    /// Defines default properties and probabilities for a logical vehicle class.
    /// </summary>
    public class VehicleClass
    {
        public string Name;
        public int VanillaProbability;
        public string[] Prefabs;

        private static Dictionary<string, VehicleClass> VehicleClasses;

        /// <summary>
        /// Initializes the static list of vehicle classes with built in values.
        /// </summary>
        static VehicleClass()
        {
            var vehicleClasses = new[]
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
            VehicleClasses = new Dictionary<string, VehicleClass>();
            foreach (var vehicleClass in vehicleClasses)
            {
                VehicleClasses.Add(vehicleClass.Name, vehicleClass);
            }
        }
        
        /// <summary>
        /// Returns the names of all available vehicle classes.
        /// </summary>
        public static string[] GetNames()
        {
            return VehicleClasses.Keys.ToArray();
        }

        /// <summary>
        /// Updates the vanilla probability value for an existing vehicle class.
        /// </summary>
        public static void SetVanillaProbability(string className, int vanillaProbability)
        {
            if (VehicleClasses.TryGetValue(className, out var vehicleClass))
            {
                vehicleClass.VanillaProbability = vanillaProbability;
            }
        }

        /// <summary>
        /// Finds the class definition for the specified prefab.
        /// </summary>
        public static VehicleClass GetVehicleClass(string prefabName)
        {
            foreach (var vehicleClass in VehicleClasses.Values)
            {
                if (vehicleClass.Prefabs.Contains(prefabName))
                {
                    return vehicleClass;
                }
            }

            Mod.log.Info($"Vehicle class for vehicle {prefabName} not found, using Sedan class as default");
            return VehicleClasses["Sedan"];
        }

        /// <summary>
        /// Helper for settings UI to produce sanitized class names.
        /// </summary>
        public static (string, string)[] GetSettingClassNames()
        {
            // Replace spaces with empty string
            return VehicleClasses.Keys.Select(name => (name.Replace(" ", ""), name)).ToArray();
        }

        /// <summary>
        /// Returns all classes that contain the specified prefab.
        /// </summary>
        public static List<string> GetClassesForPrefab(string name)
        {
            var classes = new List<string>();
            foreach (var vehicleClass in VehicleClasses.Values)
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