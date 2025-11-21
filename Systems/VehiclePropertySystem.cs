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
        /// <summary> Whether the player is in a savegame</summary>
        public static bool IsIngame;

        private EntityQuery carQuery;
        private EntityQuery vehicleQuery;
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
            
            vehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any =
                    new []{
                        ComponentType.ReadOnly<TrainData>(),
                        ComponentType.ReadOnly<CarData>(),
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
            //GameManager.instance.RegisterUpdater(SaveVanillaPack);
            //GameManager.instance.RegisterUpdater(UpdateProperties);
            log.Info("VehiclePropertySystem created and updater registered.");
        }

        /// <summary>
        /// Placeholder for saving vanilla property values.
        /// </summary>
        public bool SaveVanillaPack(string name="Vanilla")
        {
            // Read values from game
            var entities = vehicleQuery.ToEntityArray(Allocator.Temp);
            var entries = new Dictionary<string, PropertyPackEntry>();
            foreach (var entity in entities)
            {
                var prefabName = prefabSystem.GetPrefabName(entity);
                if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                {
                    var entry = new PropertyPackEntry()
                    {
                        PrefabName = prefabName,
                        MaxSpeed = carData.m_MaxSpeed,
                        Acceleration = carData.m_Acceleration,
                        Braking = carData.m_Braking
                    };
                    entries.Add(prefabName, entry);
                }
                if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                {
                    var entry = new PropertyPackEntry()
                    {
                        PrefabName = prefabName,
                        MaxSpeed = trainData.m_MaxSpeed,
                        Acceleration = trainData.m_Acceleration,
                        Braking = trainData.m_Braking
                    };
                    entries.Add(prefabName, entry);
                }
            }
            // Sort entries
            entries = entries.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value);
            log.Info($"Saving vanilla property pack with {entries.Count} entries under name {name}.");
            PropertyPack.SaveEntriesToFile(entries, name, 1);
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
            log.Info($"Property pack {pack.Name} loaded with {pack.Entries?.Count ?? 0} entries.");
            Instance.UpdateSavegameComponent();
            var success = UpdateProperties();
            log.Trace("UpdateProperties returned " + success);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            log.Debug("OnGameLoadingComplete called with mode " + mode);
            base.OnGameLoadingComplete(purpose, mode);
            IsIngame = mode == GameMode.Game;

            if (IsIngame)
            {
                SaveVanillaPack();
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
                    // Set the setting to the loaded pack for persistence
                    Setting.Instance.SavegamePropertyPackDropdown = savegamePack.PackName.ToString();
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

        /// <summary>
        /// Loads the property pack selected in settings, if no savegame pack is provided, falls back to default.
        /// </summary>
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

        public static void DefaultPackSettingChanged()
        {
            if (!IsIngame)
                return;
            if (Setting.Instance.SavegamePropertyPackDropdown == "Default")
            {
                var pack = PropertyPack.LoadFromFile(Setting.Instance.DefaultPropertyPackDropdown);
                Instance.LoadPropertyPack(pack);
            }
        }
        
        public static void SavegamePackSettingChanged()
        {
            if (!IsIngame)
                return;
            // When savegame pack is changed, only update when not using default
            if (Setting.Instance.SavegamePropertyPackDropdown == "Default")
            {
                var pack = PropertyPack.LoadFromFile(Setting.Instance.DefaultPropertyPackDropdown);
                Instance.LoadPropertyPack(pack);
            }
            else
            {
                var pack = PropertyPack.LoadFromFile(Setting.Instance.SavegamePropertyPackDropdown);
                Instance.LoadPropertyPack(pack);
            }
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
            if (UpdateVehicleProperties() && UpdateTrainProperties())
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
            return true;
            log.Debug("Updating Train Properties");
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

        
        /*
        Findings:
        Police car has max speed of 20 (m/s), and speed is displayed as 36 km/h, which is 10 m/s actually.
        Vehicle drives 500 meters in 20 seconds, so is actually travelling 20 m/s.
        */
        
        /// <summary>
        /// Applies property changes to all personal car entities.
        /// </summary>
        private bool UpdateVehicleProperties()
        {
            log.Debug("Updating Vehicle Properties");
            var entities = vehicleQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                var prefabName = prefabSystem.GetPrefabName(entity);
                log.Trace("Prefab name: " + prefabName);
                var entry = _currentPropertyPack.GetEntry(prefabName);
                log.Trace("Entry found: " + (entry != null));
                if (entry != null)
                {
                    if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                    {
                        log.Trace("Has CarData component");
                        carData.m_MaxSpeed = entry.MaxSpeed;
                        carData.m_Acceleration = entry.Acceleration;
                        carData.m_Braking = entry.Braking;
                        EntityManager.SetComponentData(entity, carData);
                        count++;
                    }
                    else if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                    {
                        log.Trace("Has TrainData component");
                        trainData.m_MaxSpeed = entry.MaxSpeed;
                        trainData.m_Acceleration = entry.Acceleration;
                        trainData.m_Braking = entry.Braking;
                        EntityManager.SetComponentData(entity, trainData);
                        count++;
                    }
                    else
                    {
                        // Disabled for now to reduce log spam
                        //log.Error("CarData component not found on entity " + prefabName);
                    }
                }
            }
            
            if (count == 0)
            {
                log.Debug("No vehicles were updated");
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
            UpdateVehicleProperties();
        }
    }

}