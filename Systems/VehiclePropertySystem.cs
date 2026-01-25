using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VehicleController.Components;
using VehicleController.Data;
using PersonalCar = Game.Vehicles.PersonalCar;

namespace VehicleController.Systems
{
    /// <summary>
    /// Modifies speed and handling parameters of vehicles based on selected packs.
    /// </summary>
    public partial class VehiclePropertySystem : GameSystemBase, IDefaultSerializable
    {
        private static ILog log;
        /// <summary> Whether the player is in a savegame</summary>
        public static bool IsIngame;

        private EntityQuery carQuery;
        private EntityQuery vehicleQuery;
        private EntityQuery trainQuery;
        private EntityQuery instanceQuery;

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
            
            carQuery = SystemAPI.QueryBuilder().WithAll<PersonalCarData>().Build();
            trainQuery = SystemAPI.QueryBuilder().WithAll<TrainData>().Build();
            vehicleQuery = SystemAPI.QueryBuilder().WithAny<CarData, TrainData>().Build();
            
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            //GameManager.instance.RegisterUpdater(SaveVanillaPack);
            //GameManager.instance.RegisterUpdater(UpdateProperties);
            log.Info("VehiclePropertySystem created.");
        }
        

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            var _settingPackName = Setting.Instance.SavegamePropertyPackDropdown;
            log.Debug($"Serializing (saving) VehiclePropertySystem with settings:\nName: {_settingPackName}\nFactor: {Setting.Instance.SavegamePropertyPackFactor}");
            if (_settingPackName == "Default")
                _settingPackName = "";
            writer.Write(DataMigrationVersion.InitialVersion); // Write version first
            writer.Write(_settingPackName);
            writer.Write(Setting.Instance.SavegamePropertyPackFactor);
        }
        
        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            log.Debug("Trying to deserialize VehiclePropertySystem savegame data.");
            try
            {
                string packName = "Default";
                float factor = 1.0f;
                // Read version first
                reader.Read(out int version);
                
                if (version == DataMigrationVersion.InitialVersion)
                {
                    // Versioning code
                    reader.Read(out packName);
                    reader.Read(out factor);
                    log.Debug($"Deserialized (loaded) VehiclePropertySystem with settings:\nName: {packName}\nFactor: {factor}");
                }
                else
                {
                    log.Warn("VehiclePropertySystem savegame data version mismatch. Data may have been lost.");
                }
                
                if (factor == 0)
                    factor = 1.0f;
                Setting.Instance.SavegamePropertyPackDropdown = packName;
                Setting.Instance.SavegamePropertyPackFactor = factor;
            }
            catch (Exception x)
            {
                log.Debug("Exception during Vehicle Property Savegame Data loading: " + x.Message);
                log.Warn("Error loading Vehicle Property Savegame Data. This can happen when loading an older savegame, and is safe to ignore. Exception: " + x.Message);
            }
        }

        public void SetDefaults(Context context)
        {
            // No implementation necessary, default pack will be used automatically
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
        /// Activates the given property pack and immediately updates entities.
        /// </summary>
        public void ActivatePropertyPack(PropertyPack pack)
        {
            if (!Enabled)
                return;
            _currentPropertyPack = pack;
            log.Info($"Property pack {pack.Name} activated with {pack.Entries?.Count ?? 0} entries.");
            var success = UpdateProperties();
            log.Trace("UpdateProperties returned " + success);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            log.Debug("OnGameLoadingComplete called with mode " + mode);
            base.OnGameLoadingComplete(purpose, mode);

            if (mode == GameMode.MainMenu) // TODO: Remove when migration period is over
            {
                var filePath = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController),
                    "migration_done.txt");
                if (!File.Exists(filePath))
                {
                    Mod.ShowMessageDialog("Vehicle Controller",
                            "You might see 2 error messages about 'Data size mismatch when deserializing component/system'. These errors will not happen again once you saved your game.",
                            "Ok");
                    File.Create(filePath).Dispose();
                }
            }
            
            IsIngame = mode == GameMode.Game;

            if (IsIngame)
            {
                log.Info($"Original Speed Limits deserialized: {Mod.OriginalSpeedLimitDeserialized}/{Mod.OriginalSpeedLimitCount}");
                SaveVanillaPack();
                LoadSelectedPropertyPack();
            }
        }

        private string? GetSavegamePack()
        {
            var savegameSetting = Setting.Instance.SavegamePropertyPackDropdown;
            var defaultSetting = Setting.Instance.DefaultPropertyPackDropdown;
            if (string.IsNullOrEmpty(savegameSetting) || savegameSetting == "Default")
            {
                return defaultSetting;
            }
            return savegameSetting;
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
                ActivatePropertyPack(pack);
                
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
                Instance.ActivatePropertyPack(pack);
            }
        }
        
        public static void SavegamePackSettingChanged()
        {
            //Mod.log.Verbose("SavegamePackSettingChanged called");
            if (!IsIngame)
                return;
            // When savegame pack is changed, only update when not using default
            if (Setting.Instance.SavegamePropertyPackDropdown == "Default")
            {
                var pack = PropertyPack.LoadFromFile(Setting.Instance.DefaultPropertyPackDropdown);
                Instance.ActivatePropertyPack(pack);
            }
            else
            {
                var pack = PropertyPack.LoadFromFile(Setting.Instance.SavegamePropertyPackDropdown);
                Instance.ActivatePropertyPack(pack);
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
        Vehicle drives 500 meters in 20 seconds, so is actually traveling 20 m/s.
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
                try
                {
                    log.Trace("Processing entity " + entity);
                    var prefabName = prefabSystem.GetPrefabName(entity);
                    log.Trace("Prefab name: " + prefabName);
                    var entry = _currentPropertyPack.GetEntry(prefabName);
                    log.Trace("Entry found: " + (entry != null));
                    if (entry != null)
                    {
                        if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                        {
                            log.Trace("Has CarData component");
                            carData.m_MaxSpeed = entry.MaxSpeed * Setting.Instance!.SavegamePropertyPackFactor;
                            carData.m_Acceleration = entry.Acceleration * Setting.Instance!.SavegamePropertyPackFactor;
                            carData.m_Braking = entry.Braking * Setting.Instance!.SavegamePropertyPackFactor;
                            EntityManager.SetComponentData(entity, carData);
                            count++;
                        }
                        else if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                        {
                            log.Trace("Has TrainData component");
                            trainData.m_MaxSpeed = entry.MaxSpeed * Setting.Instance!.SavegamePropertyPackFactor;
                            trainData.m_Acceleration = entry.Acceleration * Setting.Instance!.SavegamePropertyPackFactor;
                            trainData.m_Braking = entry.Braking * Setting.Instance!.SavegamePropertyPackFactor;
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
                catch (Exception e)
                {
                    log.Error($"Error updating vehicle properties for entity {entity}: " + e.Message);
                    throw;
                }
            }
            
            if (count == 0)
            {
                log.Debug("No vehicles were updated");
                return false;
            }
            
            //log.Info($"Updated properties for {count}/{entities.Length} car entities."); TODO: Re-enable
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