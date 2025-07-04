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
            if (!Enabled)
                return;
            _currentProbabilityPack = pack;
            UpdateProbabilities();
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

                        var probability = _currentProbabilityPack.GetProbability(prefabName);
                        personalCarData.m_Probability = probability;
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
                Logger.Debug("Failed to update probabilities. No PersonalCarData found.");
                return false;
            }
            
            Logger.Info($"Updated properties for {count}/{entities.Length} car entities.");
            return true;
        }

        protected override void OnUpdate()
        {

        }

        public static void SaveValueChanges()
        {
            Instance.UpdateProbabilities();
        }
    }

}