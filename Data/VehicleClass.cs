using System.Collections.Generic;
using System.Linq;

namespace VehicleController.Data
{

    public class VehicleClass
    {
        public string Name;
        public int VanillaProbability;
        public string[] Prefabs;
        public int MaxSpeed = 170; // Default is 250
        public int Acceleration = 8;
        public int Braking = 15;

        private static Dictionary<string, VehicleClass> VehicleClasses;

        static VehicleClass()
        {
            var vehicleClasses = new[]
            {
                new VehicleClass()
                {
                    Name = "Motorbike",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Motorbike01"},
                    MaxSpeed = 210,
                    Acceleration = 10,
                    Braking = 15
                },
                new VehicleClass()
                {
                    Name = "Scooter",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Scooter01"},
                    MaxSpeed = 60,
                    Acceleration = 6,
                    Braking = 10,
                },
                new VehicleClass()
                {
                    Name = "City Car",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car01"},
                    MaxSpeed = 100,
                    Acceleration = 12,
                    Braking = 10
                },
                new VehicleClass()
                {
                    Name = "Hatchback",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car02", "Car03"},
                    // No Overrides
                    MaxSpeed = 160,
                    Acceleration = 8,
                    Braking = 15
                },
                new VehicleClass()
                {
                    Name = "Minivan",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car04"},
                    MaxSpeed = 180,
                    Acceleration = 7,
                    Braking = 15
                },
                new VehicleClass()
                {
                    Name = "Sedan",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car05", "Car06"},
                    // No Overrides
                    MaxSpeed = 160,
                    Acceleration = 8,
                    Braking = 15
                },
                new VehicleClass()
                {
                    Name = "Sports Car",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car07"},
                    MaxSpeed = 260,
                    Acceleration = 11,
                    Braking = 18
                },
                new VehicleClass()
                {
                    Name = "Pickup",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car08"},
                    MaxSpeed = 140,
                    Acceleration = 6,
                    Braking = 10
                },
                new VehicleClass()
                {
                    Name = "SUV",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Car09"},
                    MaxSpeed = 160,
                    Acceleration = 6,
                    Braking = 10
                },
                new VehicleClass()
                {
                    Name = "Muscle Car",
                    VanillaProbability = 100,
                    Prefabs = new[] {"MuscleCar01", "MuscleCar02", "MuscleCar03", "MuscleCar04", "MuscleCar05"},
                    MaxSpeed = 240,
                    Acceleration = 10,
                    Braking = 16
                },
                new VehicleClass()
                {
                    Name = "Van",
                    VanillaProbability = 100,
                    Prefabs = new[] {"Van01"},
                    MaxSpeed = 140,
                    Acceleration = 5,
                    Braking = 11
                }
            };
            VehicleClasses = new Dictionary<string, VehicleClass>();
            foreach (var vehicleClass in vehicleClasses)
            {
                VehicleClasses.Add(vehicleClass.Name, vehicleClass);
            }
        }
        
        public static string[] GetNames()
        {
            return VehicleClasses.Keys.ToArray();
        }

        public static void SetVanillaProbability(string className, int vanillaProbability)
        {
            if (VehicleClasses.TryGetValue(className, out var vehicleClass))
            {
                vehicleClass.VanillaProbability = vanillaProbability;
            }
        }

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

        public static (int probability, int maxSpeed, int acceleration, int braking) GetValues(string prefabName)
        {
            var vehicleClass = GetVehicleClass(prefabName);
            switch (vehicleClass.Name)
            {
                case "Motorbike":
                    return (Setting.Instance.MotorbikeProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Scooter":
                    return (Setting.Instance.ScooterProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "City Car":
                    return (Setting.Instance.CityCarProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Hatchback":
                    return (Setting.Instance.HatchbackProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Minivan":
                    return (Setting.Instance.MinivanProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Sedan":
                    return (Setting.Instance.SedanProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Sports Car":
                    return (Setting.Instance.SportsCarProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Pickup":
                    return (Setting.Instance.PickupProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "SUV":
                    return (Setting.Instance.SUVProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Muscle Car":
                    return (Setting.Instance.MuscleCarProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                case "Van":
                    return (Setting.Instance.VanProbability, vehicleClass.MaxSpeed,
                        vehicleClass.Acceleration, vehicleClass.Braking);
                default:
                    Mod.log.Debug("Vehicle class not found, using default values");
                    return (100, 250, 8, 15);
            }

            //return prob;
            // TODO: Use setting as a multiplier instead of absolute value
            //return vehicleClass.VanillaProbability * (prob / 100);
        }

        public static (string, string)[] GetSettingClassNames()
        {
            // Replace spaces with empty string
            return VehicleClasses.Keys.Select(name => (name.Replace(" ", ""), name)).ToArray();
        }
    }
}