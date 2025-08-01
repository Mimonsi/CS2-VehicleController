using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VehicleController.Data;
using PersonalCar = Game.Vehicles.PersonalCar;

namespace VehicleController.Systems
{
    public partial class VehicleProbabilitySystem : GameSystemBase
    {
        private static ILog Logger;

        private EntityQuery carQuery;

        private PrefabSystem prefabSystem;
        private ProbabilityPack _currentProbabilityPack;
        public static VehicleProbabilitySystem Instance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            Enabled = true;
            Logger = Mod.log;
            
            carQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                new []
                    {ComponentType.ReadOnly<PersonalCarData>(),
                }
            });
            
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            GameManager.instance.RegisterUpdater(SaveVanillaPack);
            //GameManager.instance.RegisterUpdater(UpdateProbabilities);
            Logger.Info("VehicleProbabilitySystem created and updater registered.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            Setting.CurrentProbabilityPackVersion++;
        }

        private bool SaveVanillaPack()
        {
            Logger.Info("Saving vanilla probabilities");
            ProbabilityPack pack = new ProbabilityPack("Vanilla");
            var entities = carQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    if (personalCarData.m_Probability == 0)
                    {
                        Logger.Info("PersonalCarData not initialized, retrying later");
                        return false;
                    }
                    var prefabName = prefabSystem.GetPrefabName(entity);
                    pack.AddEntry(prefabName, personalCarData.m_Probability);
                    VehicleClass.SetVanillaProbability(prefabName, personalCarData.m_Probability);
                    count++;
                }
            }

            if (count == 0)
            {
                Logger.Debug("Failed to save vanilla probabilities, no PersonalCarData found.");
                return false;
            }

            try
            {
                pack.SaveToFile();
                Logger.Info($"Saved vanilla probabilities for {count}/{entities.Length} car entities.");
                return true;
            }
            catch (Exception x)
            {
                Logger.Error($"Error saving vanilla probabilities: {x.Message}");
                return false;
            }
        }
        
        public void LoadProbabilityPack(ProbabilityPack pack)
        {
            Logger.Info("Loading Probability Pack: " + pack.Name);
            if (!Enabled)
                return;
            _currentProbabilityPack = pack;
            UpdateSliders();
            UpdateProbabilities();
        }

        private void UpdateSliders()
        {
            if (_currentProbabilityPack.TryGetClassEntry("Motorbike", out var motorbikeEntry))
            {
                Setting.Instance.MotorbikeProbability = motorbikeEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("Scooter", out var scooterEntry))
            {
                Setting.Instance.ScooterProbability = scooterEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("City Car", out var cityCarEntry))
            {
                Setting.Instance.CityCarProbability = cityCarEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("Hatchback", out var hatchbackEntry))
            {
                Setting.Instance.HatchbackProbability = hatchbackEntry.Probability;
            }   
            if (_currentProbabilityPack.TryGetClassEntry("Minivan", out var minivanEntry))
            {
                Setting.Instance.MinivanProbability = minivanEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("Sedan", out var sedanEntry))
            {
                Setting.Instance.SedanProbability = sedanEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("Sports Car", out var sportsCarEntry))
            {
                Setting.Instance.SportsCarProbability = sportsCarEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("Pickup", out var pickupEntry))
            {
                Setting.Instance.PickupProbability = pickupEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("SUV", out var suvEntry))
            {
                Setting.Instance.SUVProbability = suvEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("Muscle Car", out var muscleCarEntry))
            {
                Setting.Instance.MuscleCarProbability = muscleCarEntry.Probability;
            }
            if (_currentProbabilityPack.TryGetClassEntry("Van", out var vanEntry))
            {
                Setting.Instance.VanProbability = vanEntry.Probability;
            }
            Logger.Info("Updated sliders with current probability pack values.");
        }
        
        private bool UpdateProbabilities()
        {
            Logger.Info("Updating Vehicle Probabilities");
            Logger.Info("Loading probability pack: " + _currentProbabilityPack.Name);
            var entities = carQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    var prefabName = prefabSystem.GetPrefabName(entity);
                    if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                    {
                        if (carData is { m_Acceleration: 0, m_Braking: 0, m_MaxSpeed: 0 })
                        {
                            Logger.Info("CarData not initialized, retrying later");
                            return false;
                        }

                        if (_currentProbabilityPack.TryGetProbability(prefabName, out var probability))
                        {
                            personalCarData.m_Probability = probability;
                            EntityManager.SetComponentData(entity, carData);
                        }
                    }
                    else
                    {
                        Logger.Error("CarData component not found on entity " + prefabName);
                    }
                    EntityManager.SetComponentData(entity, personalCarData);
                    EntityManager.AddComponent<BatchesUpdated>(entity);
                    count++;
                }
            }
            
            if (count == 0)
            {
                Logger.Debug("Failed to update probabilities. No PersonalCarData found.");
                return false;
            }
            
            Logger.Info($"Updated properties for {count}/{entities.Length} car entities.");
            return true;
        }

        protected override void OnUpdate()
        {

        }
        
        public void ApplySettings()
        {
            throw new NotImplementedException();
        }

        public static void SaveValueChanges()
        {
            Instance.UpdateProbabilities();
        }
        
//         public void DeleteInstances()
//         {
//             Logger.Info("Deleting instances");
//             double motorcycleDeletionChance = 1 - (Setting.Instance.MotorbikeProbability / 100.0);
//             double scooterDeletionChance = 1 - (Setting.Instance.ScooterProbability / 100.0);
//             Logger.Info($"Motorcycle deletion chance: {motorcycleDeletionChance}, Scooter deletion chance: {scooterDeletionChance}");
//             List<Entity> motorbikes = new List<Entity>();
//             List<Entity> scooters = new List<Entity>();
//
//             var entities = instanceQuery.ToEntityArray(Allocator.Temp);
//
//             /*foreach (var entity in entities)
//             {
//                 string prefabName = prefabSystem.GetPrefabName(entity);
//                 if (prefabName.Contains("Motorbike"))
//                 {
//                     motorbikes.Add(entity);
//                 }
//
//                 if (prefabName.Contains("Scooter"))
//                 {
//                     motorbikes.Add(entity);
//                 }
//             }*/
//
//
//
//             foreach (var entity in entities)
//             {
//                 if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
//                 {
//                     if (prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefab))
//                     {
//                         if (prefab is VehiclePrefab vehiclePrefab)
//                         {
//                             if (vehiclePrefab.name.Contains("Motorbike"))
//                             {
//                                 motorbikes.Add(entity);
//                             }
//                             if (vehiclePrefab.name.Contains("Scooter"))
//                             {
//                                 scooters.Add(entity);
//                             }
//                         }
//                     }
//                 }
//             }
//
//             Logger.Info($"Found {motorbikes.Count} motorcycles and {scooters.Count} scooters");
//             int motorcyclesDeleted = 0, scootersDeleted = 0;
//
//             // Delete percentage of motorcycles and scooters
//             foreach (var entity in motorbikes)
//             {
//                 if (Random.value < motorcycleDeletionChance)
//                 {
//                     EntityManager.AddComponent<Deleted>(entity);
//                     motorcyclesDeleted++;
//                 }
//             }
//
//             foreach (var entity in scooters)
//             {
//                 if (Random.value < scooterDeletionChance)
//                 {
//                     EntityManager.AddComponent<Deleted>(entity);
//                     scootersDeleted++;
//                 }
//             }
//
//             Logger.Info($"Deleted {motorcyclesDeleted} motorcycles and {scootersDeleted} scooters");
//         }
    }

}