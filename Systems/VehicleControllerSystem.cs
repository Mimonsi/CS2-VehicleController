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
    public partial class VehicleControllerSystem : GameSystemBase
    {
        private static ILog Logger;

        private EntityQuery carQuery;
        private EntityQuery trainQuery;
        private EntityQuery instanceQuery;

        private PrefabSystem prefabSystem;
        public static VehicleControllerSystem Instance { get; private set; }

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
            
            trainQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                    new []{
                        ComponentType.ReadOnly<TrainData>(),
                    }
            });
            
            instanceQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                    new[] {
                        ComponentType.ReadOnly<PersonalCar>(),
                    },
            });

            
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            GameManager.instance.RegisterUpdater(Initialize);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

        }

        public bool Initialize()
        {
            PrintDebug();
            if (UpdateComponents())
            {
                PrintDebug();
                return true;
            }
            PrintDebug();
            return false;
        }
        
        public void PrintDebug()
        {
            Logger.Info("Printing Debug");
            var entities = carQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    var prefabName = prefabSystem.GetPrefabName(entity);
                    personalCarData.m_Probability = VehicleClass.GetProbability(prefabName);
                    if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                    {
                        var vehicleClass = VehicleClass.GetVehicleClass(prefabName);
                        if (Setting.Instance.EnableImprovedCarBehavior)
                        {
                            Logger.Info("CarData for entity " + prefabName + ": " +
                                        $"Acceleration: {carData.m_Acceleration}, " +
                                        $"Braking: {carData.m_Braking}, " +
                                        $"MaxSpeed: {carData.m_MaxSpeed}");
                        }
                    }
                    else
                    {
                        Logger.Error("CarData component not found on entity " + prefabName);
                    }
                }
            }
        }

        private bool UpdateTrainParameters()
        {
            Logger.Debug("Updating train parameters");
            var entities = trainQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                {
                    if (trainData.m_TrackType == TrackTypes.Train)
                    {
                        if (Setting.Instance.EnableImprovedTrainBehavior) // TODO: Track original settings to not require restart
                        {
                            if (trainData.m_Acceleration == 0 && trainData.m_Braking == 0)
                            {
                                Logger.Debug("TrainData not initialized, retrying later");
                                return false;
                            }
                            trainData.m_Acceleration = 2;
                            trainData.m_Braking = 4;
                            EntityManager.SetComponentData(entity, trainData);
                            EntityManager.AddComponent<BatchesUpdated>(entity);
                            Logger.Debug("Updated train parameters for entity: " + entity.Index);
                        }
                    }

                    count++;
                }
            }
            
            if (count == 0)
            {
                Logger.Debug("Failed to update train parameters, no TrainData found.");
                return false;
            }

            Logger.Info($"Updated parameters for {count}/{entities.Length} train entities.");
            return true;
        }
        
        private bool SaveVanillaProbabilities()
        {
            Logger.Info("Saving vanilla probabilities");
            var entities = carQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    count++;
                    VehicleClass.SetVanillaProbability(prefabSystem.GetPrefabName(entity), personalCarData.m_Probability);
                }
            }

            if (count == 0)
            {
                Logger.Debug("Failed to save vanilla probabilities, no PersonalCarData found.");
                return false;
            }

            Logger.Info($"Saved vanilla probabilities for {count}/{entities.Length} car entities.");
            return true;
        }

        private bool UpdateComponents()
        {
            if (!Setting.Instance.EnableImprovedCarBehavior) // TODO: Track original settings to not require restart
            {
                Logger.Info("Not Updating Car Properties, Improved Car Behavior is disabled.");
                return true;
            }
            Logger.Info("Updating Car Properties");

            var entities = carQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    var prefabName = prefabSystem.GetPrefabName(entity);
                    personalCarData.m_Probability = VehicleClass.GetProbability(prefabName);
                    if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                    {
                        var vehicleClass = VehicleClass.GetVehicleClass(prefabName);
                        if (carData.m_Acceleration == 0 && carData.m_Braking == 0 && carData.m_MaxSpeed == 0)
                        {
                            Logger.Info("CarData not initialized, retrying later");
                            return false;
                        }
                        carData.m_Acceleration = vehicleClass.Acceleration;
                        carData.m_Braking = vehicleClass.Braking;
                        carData.m_MaxSpeed = vehicleClass.MaxSpeed;
                        EntityManager.SetComponentData(entity, carData);
                        /*Logger.Debug("Updated CarData for entity: " + prefabName +
                                     $" with Acceleration: {carData.m_Acceleration}, " +
                                     $"Braking: {carData.m_Braking}, " +
                                     $"MaxSpeed: {carData.m_MaxSpeed}");*/
                        count++;
                    }
                    else
                    {
                        Logger.Error("CarData component not found on entity " + prefabName);
                    }
                    EntityManager.SetComponentData(entity, personalCarData);
                    EntityManager.AddComponent<BatchesUpdated>(entity);
                }
                EntityManager.SetComponentData(entity, personalCarData);
                EntityManager.AddComponent<BatchesUpdated>(entity);
            }
            
            if (count == 0)
            {
                Logger.Debug("Failed to update properties. No PersonalCarData found.");
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
            UpdateComponents();
        }

        public void DeleteInstances()
        {
            Logger.Info("Deleting instances");
            double motorcycleDeletionChance = 1 - (Setting.Instance.MotorbikeProbability / 100.0);
            double scooterDeletionChance = 1 - (Setting.Instance.ScooterProbability / 100.0);
            Logger.Info($"Motorcycle deletion chance: {motorcycleDeletionChance}, Scooter deletion chance: {scooterDeletionChance}");
            List<Entity> motorbikes = new List<Entity>();
            List<Entity> scooters = new List<Entity>();

            var entities = instanceQuery.ToEntityArray(Allocator.Temp);

            /*foreach (var entity in entities)
            {
                string prefabName = prefabSystem.GetPrefabName(entity);
                if (prefabName.Contains("Motorbike"))
                {
                    motorbikes.Add(entity);
                }

                if (prefabName.Contains("Scooter"))
                {
                    motorbikes.Add(entity);
                }
            }*/



            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                {
                    if (prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefab))
                    {
                        if (prefab is VehiclePrefab vehiclePrefab)
                        {
                            if (vehiclePrefab.name.Contains("Motorbike"))
                            {
                                motorbikes.Add(entity);
                            }
                            if (vehiclePrefab.name.Contains("Scooter"))
                            {
                                scooters.Add(entity);
                            }
                        }
                    }
                }
            }

            Logger.Info($"Found {motorbikes.Count} motorcycles and {scooters.Count} scooters");
            int motorcyclesDeleted = 0, scootersDeleted = 0;

            // Delete percentage of motorcycles and scooters
            foreach (var entity in motorbikes)
            {
                if (Random.value < motorcycleDeletionChance)
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    motorcyclesDeleted++;
                }
            }

            foreach (var entity in scooters)
            {
                if (Random.value < scooterDeletionChance)
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    scootersDeleted++;
                }
            }

            Logger.Info($"Deleted {motorcyclesDeleted} motorcycles and {scootersDeleted} scooters");
        }
    }

}