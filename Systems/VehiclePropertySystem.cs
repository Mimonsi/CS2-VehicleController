using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// Modifies speed and handling parameters of vehicles based on selected packs.
    /// </summary>
    public partial class VehiclePropertySystem : GameSystemBase
    {
        private static ILog log;
        /// <summary> Whether or not the player is in a savegame</summary>
        public static bool IsIngame;

        private EntityQuery carQuery;
        private EntityQuery trainQuery;
        private EntityQuery instanceQuery;
        private EntityQuery savegamePackQuery;
        private EntityQuery savegameHostEntityQuery;

        private PrefabSystem prefabSystem;
        private PropertyPack _currentPropertyPack;
        public static VehiclePropertySystem Instance { get; private set; }

        /// <summary>
        /// Initializes queries and registers the vanilla export updater.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            Enabled = true;
            log = Mod.log;
            
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
            
            savegamePackQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                    new []{
                        ComponentType.ReadOnly<SavegamePropertyPack>(),
                    }
            });
            
            savegameHostEntityQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                    new []{
                        ComponentType.ReadOnly<EconomyParameterData>(),
                    }
            });

            
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            GameManager.instance.RegisterUpdater(SaveVanillaPack);
            //GameManager.instance.RegisterUpdater(UpdateProperties);
            log.Info("VehiclePropertySystem created and updater registered.");
        }

        /// <summary>
        /// Placeholder for saving vanilla property values.
        /// </summary>
        private bool SaveVanillaPack()
        {
            return true;
        }

        /// <summary>
        /// Loads the given property pack and immediately updates entities.
        /// </summary>
        public void LoadPropertyPack(PropertyPack pack)
        {
            if (!Enabled)
                return;
            _currentPropertyPack = pack;
            UpdateProperties();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            log.Debug("OnGameLoadingComplete called with mode " + mode);
            base.OnGameLoadingComplete(purpose, mode);
            IsIngame = mode == GameMode.Game;
            // TODO: Load property pack from the save, or default

            if (IsIngame)
            {
                LoadSelectedPropertyPack();
            }
        }

        private string? GetSavegamePack()
        {
            var entities = savegamePackQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return null;
            if (entities.Length == 1)
            {
                if (EntityManager.TryGetComponent<SavegamePropertyPack>(entities[0], out var savegamePack))
                {
                    return savegamePack.PackName.ToString();
                }
                return null;
            }
            log.Error("Multiple SavegamePropertyPack entities found! Using default pack instead.");
            return null;
        }

        private string GetDefaultPack()
        {
            return Setting.Instance.DefaultPropertyPackDropdown;
        }

        private void LoadSelectedPropertyPack()
        {
            string? packName = GetSavegamePack();
            if (packName == null)
            {
                log.Debug("No savegame pack found, loading default pack.");
                packName = GetDefaultPack();
            }

            try
            {
                var pack = PropertyPack.LoadFromFile(packName);
                LoadPropertyPack(pack);
            }
            catch (Exception x)
            {
                log.Warn("Could not load property pack from file: " + x.Message);
            }
        }
        
        public static void UpdateSavegamePropertyPack()
        {
            Instance.LoadSelectedPropertyPack();
            Instance.UpdateSavegameComponent();   
        }

        /// <summary>
        /// Adds or updates the SavegamePropertyPack component in the savegame to reflect the current pack.
        /// </summary>
        private void UpdateSavegameComponent()
        {
            var entities = savegamePackQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
            {
                var hostEntities = savegameHostEntityQuery.ToEntityArray(Allocator.Temp);
                if (hostEntities.Length == 0)
                {
                    log.Error("No host entity found to add SavegamePropertyPack component to.");
                    return;
                }
                if (hostEntities.Length == 1)
                {
                    var savegamePackName = Setting.Instance.SavegamePropertyPackDropdown;
                    var hostEntity = hostEntities.First();
                    EntityManager.AddComponent<SavegamePropertyPack>(hostEntity);
                    if (EntityManager.TryGetComponent<SavegamePropertyPack>(hostEntity, out var savegamePack))
                    {
                        savegamePack.PackName = Setting.Instance.SavegamePropertyPackDropdown;
                        EntityManager.SetComponentData(hostEntity, savegamePack);
                    }
                    log.Info("SavegamePropertyPack component created with pack " + savegamePackName);
                }
                else
                {
                    log.Error("Too many host entities found to add SavegamePropertyPack component to. (" + hostEntities.Length + ")");
                }
            }
            else if (entities.Length == 1)
            {
                if (EntityManager.TryGetComponent<SavegamePropertyPack>(entities.First(), out var savegamePack))
                {
                    savegamePack.PackName = Setting.Instance.SavegamePropertyPackDropdown;
                    EntityManager.SetComponentData(entities.First(), savegamePack);
                    log.Info("SavegamePropertyPack component updated to " + savegamePack.PackName);
                }
            }
            else
            {
                log.Error("Multiple SavegamePropertyPack entities found! This should not happen.");
            }
        }

        /// <summary>
        /// Updates all car and train properties according to the loaded pack and settings.
        /// </summary>
        private bool UpdateProperties()
        {
            if (UpdateCarProperties() && UpdateTrainProperties())
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Applies property changes to all train entities if enabled.
        /// </summary>
        private bool UpdateTrainProperties()
        {
            /*if (!Setting.Instance.EnableImprovedTrainBehavior) // TODO: Track original settings to not require restart
            {
                log.Info("Not Updating Train Properties, Improved Car Behavior is disabled.");
                return true;
            }*/
            log.Info("Updating Train Properties");
            var entities = trainQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                {
                    if (trainData.m_TrackType == TrackTypes.None)
                    {
                        log.Debug("TrainData not initialized, retrying later");
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
                log.Debug("Failed to update train parameters, no TrainData found.");
                return false;
            }

            log.Info($"Updated parameters for {count}/{entities.Length} train entities.");
            return true;
        }

        /// <summary>
        /// Applies property changes to all personal car entities.
        /// </summary>
        private bool UpdateCarProperties()
        {
            log.Info("Updating Vehicle Properties");
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
                            log.Info("CarData not initialized, retrying later");
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
                        log.Error("CarData component not found on entity " + prefabName);
                    }
                    EntityManager.SetComponentData(entity, personalCarData);
                    EntityManager.AddComponent<BatchesUpdated>(entity);
                    count++;
                }
            }
            
            if (count == 0)
            {
                log.Debug("Failed to update properties. No PersonalCarData found.");
                return false;
            }
            
            log.Info($"Updated properties for {count}/{entities.Length} car entities.");
            return true;
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {

        }

        /// <summary>
        /// Invoked from the settings UI to reapply car properties.
        /// </summary>
        public void ApplySettings()
        {
            UpdateCarProperties();
        }
    }

}