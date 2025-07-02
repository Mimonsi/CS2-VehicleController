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
using VehicleController.Data;
using PersonalCar = Game.Vehicles.PersonalCar;

namespace VehicleController.Systems
{
    public partial class VehiclePropertySystem : GameSystemBase
    {
        private static ILog Logger;

        private EntityQuery carQuery;
        private EntityQuery trainQuery;
        private EntityQuery instanceQuery;

        private PrefabSystem prefabSystem;
        private ProbabilityPack _currentProbabilityPack;
        public static VehiclePropertySystem Instance { get; private set; }

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

            
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            GameManager.instance.RegisterUpdater(UpdateProperties);
            Logger.Info("VehiclePropertySystem created and updater registered.");
        }

        public void LoadPropertyPack(PropertyPack pack)
        {
            if (!Enabled)
                return;
            _currentProbabilityPack = pack;
            UpdateProperties();
        }

        private bool UpdateProperties()
        {
            if (UpdateCarProperties() && UpdateTrainProperties())
            {
                return true;
            }

            return false;
        }
        
        private bool UpdateTrainProperties()
        {
            if (!Setting.Instance.EnableImprovedTrainBehavior) // TODO: Track original settings to not require restart
            {
                Logger.Info("Not Updating Train Properties, Improved Car Behavior is disabled.");
                return true;
            }
            Logger.Info("Updating Train Properties");
            var entities = trainQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                {
                    if (trainData.m_TrackType == TrackTypes.None)
                    {
                        Logger.Debug("TrainData not initialized, retrying later");
                        return false;
                    }
                    if (trainData.m_TrackType == TrackTypes.Train)
                    {
                        trainData.m_Acceleration = 2;
                        trainData.m_Braking = 4;
                        EntityManager.SetComponentData(entity, trainData);
                        EntityManager.AddComponent<BatchesUpdated>(entity);
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

        private bool UpdateCarProperties()
        {
            Logger.Info("Updating Vehicle Properties");
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
                        var values = VehicleClass.GetValues(prefabName);
                        personalCarData.m_Probability = values.probability;
                        carData.m_MaxSpeed = values.maxSpeed;
                        carData.m_Acceleration = values.acceleration;
                        carData.m_Braking = values.braking;
                        EntityManager.SetComponentData(entity, carData);
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
            UpdateCarProperties();
        }
    }

}