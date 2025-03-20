using System.Collections.Generic;
using System.Linq;

namespace VehicleController;

public class VehicleClass
{
    public string Name;
    public int VanillaProbability;
    public string[] Prefabs;
    public int MaxSpeed = 170; // Default is 250
    public int Acceleration = 8;
    public int Braking = 15;
    

    private static readonly Dictionary<string, VehicleClass> VehicleClasses;

    static VehicleClass()
    {
        var vehicleClasses = new[]
        {
            new VehicleClass()
            {
                Name = "Motorbike",
                VanillaProbability = 100,
                Prefabs = ["Motorbike01"],
                MaxSpeed = 210,
                Acceleration = 10,
                Braking = 15
            },
            new VehicleClass()
            {
                Name = "Scooter",
                VanillaProbability = 100,
                Prefabs = ["Scooter01"],
                MaxSpeed = 60,
                Acceleration = 6,
                Braking = 10,
            },
            new VehicleClass()
            {
                Name = "City Car",
                VanillaProbability = 100,
                Prefabs = ["Car01"],
                Acceleration = 12,
                Braking = 10,
                MaxSpeed = 120
            },
            new VehicleClass()
            {
                Name = "Hatchback",
                VanillaProbability = 100,
                Prefabs = ["Car02", "Car03"],
                // No Overrides
            },
            new VehicleClass()
            {
                Name = "Minivan",
                VanillaProbability = 100,
                Prefabs = ["Car04"],
                Acceleration = 7,
                Braking = 15,
                MaxSpeed = 180
            },
            new VehicleClass()
            {
                Name = "Sedan",
                VanillaProbability = 100,
                Prefabs = ["Car05", "Car06"],
                // No Overrides
            },
            new VehicleClass()
            {
                Name = "Sports Car",
                VanillaProbability = 100,
                Prefabs = ["Car07"],
                Acceleration = 11,
                Braking = 18,
                MaxSpeed = 260
            },
            new VehicleClass()
            {
                Name = "Pickup",
                VanillaProbability = 100,
                Prefabs = ["Car08"],
                Acceleration = 6,
                Braking = 12,
                MaxSpeed = 140
            },
            new VehicleClass()
            {
                Name = "SUV",
                VanillaProbability = 100,
                Prefabs = ["Car09"],
                Acceleration = 6,
                Braking = 10,
                MaxSpeed = 160
            },
            new VehicleClass()
            {
                Name = "Muscle Car",
                VanillaProbability = 100,
                Prefabs = ["MuscleCar01", "MuscleCar02", "MuscleCar03", "MuscleCar04", "MuscleCar05"],
                Acceleration = 10,
                Braking = 16,
                MaxSpeed = 270
            },
            new VehicleClass()
            {
                Name = "Van",
                VanillaProbability = 100,
                Prefabs = ["Van01"],
                Acceleration = 5,
                Braking = 11,
                MaxSpeed = 140
            }
        };
        VehicleClasses = new Dictionary<string, VehicleClass>();
        foreach (var vehicleClass in vehicleClasses)
        {
            VehicleClasses.Add(vehicleClass.Name, vehicleClass);
        }
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
    
    public static int GetProbability(string prefabName)
    {
        var vehicleClass = GetVehicleClass(prefabName);

        var prob = 100;
        switch (vehicleClass.Name)
        {
            case "Motorbike":
                prob = Setting.Instance.MotorbikeProbability;
                break;
            case "Scooter":
                prob = Setting.Instance.ScooterProbability;
                break;
            case "City Car":
                prob = Setting.Instance.CityCarProbability;
                break;
            case "Hatchback":
                prob = Setting.Instance.HatchbackProbability;
                break;
            case "Minivan":
                prob = Setting.Instance.MinivanProbability;
                break;
            case "Sedan":
                prob = Setting.Instance.SedanProbability;
                break;
            case "Sports Car":
                prob = Setting.Instance.SportsCarProbability;
                break;
            case "Pickup":
                prob = Setting.Instance.PickupProbability;
                break;
            case "SUV":
                prob = Setting.Instance.SUVProbability;
                break;
            case "Muscle Car":
                prob = Setting.Instance.MuscleCarProbability;
                break;
            case "Van":
                prob = Setting.Instance.VanProbability;
                break;
        }

        return prob;
        // TODO: Use setting as a multiplier instead of absolute value
        //return vehicleClass.VanillaProbability * (prob / 100);
    }

    public static (string, string)[] GetSettingClassNames()
    {
        // Replace spaces with empty string
        return VehicleClasses.Keys.Select(name => (name.Replace(" ", ""), name)).ToArray();
    }
}