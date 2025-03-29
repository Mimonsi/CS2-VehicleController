using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
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
                [
                    ComponentType.ReadOnly<PersonalCarData>(),
                ]
            });
            
            trainQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                [
                    ComponentType.ReadOnly<TrainData>(),
                ]
            });
            
            instanceQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                [
                    ComponentType.ReadOnly<PersonalCar>(),
                ],
            });

            
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            GameManager.instance.RegisterUpdater(SaveVanillaProbabilities);
            GameManager.instance.RegisterUpdater(UpdateComponents);
            GameManager.instance.RegisterUpdater(UpdateTrainParameters);
        }

        private void UpdateTrainParameters()
        {
            var entities = trainQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                {
                    if (trainData.m_TrackType == TrackTypes.Train)
                    {
                        if (Setting.Instance.EnableImprovedTrainBehaviour) // TODO: Track original settings to not require restart
                        {
                            trainData.m_Acceleration = 2;
                            trainData.m_Braking = 4;
                            EntityManager.SetComponentData(entity, trainData);
                            EntityManager.AddComponent<BatchesUpdated>(entity);
                        }
                    }
                }
            }
        }
        
        private void SaveVanillaProbabilities()
        {
            Logger.Info("Saving vanilla probabilities");
            var entities = carQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    VehicleClass.SetVanillaProbability(prefabSystem.GetPrefabName(entity), personalCarData.m_Probability);
                }
            }
        }

        private void UpdateComponents()
        {
            Logger.Info("Updating Probabilities");
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
                        if (Setting.Instance.EnableImprovedCarBehaviour) // TODO: Track original settings to not require restart
                        {
                            carData.m_Acceleration = vehicleClass.Acceleration;
                            carData.m_Braking = vehicleClass.Braking;
                            carData.m_MaxSpeed = vehicleClass.MaxSpeed;
                            EntityManager.SetComponentData(entity, carData);
                        }
                    }
                    EntityManager.SetComponentData(entity, personalCarData);
                    EntityManager.AddComponent<BatchesUpdated>(entity);
                }
            }
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