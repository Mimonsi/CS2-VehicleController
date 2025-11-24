using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using VehicleController.Data;

namespace VehicleController.Systems
{
    public partial class VehicleStiffnessSystem : GameSystemBase
    {
        private static ILog log;

        // Queries for entities with SwayingData
        private EntityQuery _uneditedEntities;
        private EntityQuery _allEntities;

        //private UIUpdateState uiUpdateState;
        private PrefabSystem _prefabSystem;
        public static VehicleStiffnessSystem Instance { get; private set; }
        private bool IsIngame = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = true;
            log = Mod.log;
            
            _uneditedEntities = SystemAPI.QueryBuilder().WithAll<SwayingData>().WithNone<StiffnessModified>().Build();
            _allEntities = SystemAPI.QueryBuilder().WithAll<SwayingData>().Build();

            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            RequireForUpdate(_uneditedEntities);
            Instance = this;
            log.Info("VehicleStiffnessSystem created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode == GameMode.Game)
            {
                log.Info("Game loaded in gamemode Game");
                IsIngame = true;

            }
        }

        protected override void OnUpdate()
        {
            if (!IsIngame)
                return;
            UpdateEntities(_uneditedEntities.ToEntityArray(Allocator.Temp));
        }

        public void SettingsUpdated()
        {
            if (!IsIngame)
                return;
            // Update all entities when settings change
            UpdateEntities(_allEntities.ToEntityArray(Allocator.Temp));
        }

        public void ResetSettingsToVanilla()
        {
            Setting.Instance.DampingModifier = 1;
            Setting.Instance.StiffnessModifier = 1;
        }

        public void ResetSettingsToDefault()
        {
            Setting.Instance.DampingModifier = 2;
            Setting.Instance.StiffnessModifier = 3;
        }
        
        public void UpdateEntities(NativeArray<Entity> entities)
        {
            log.Info($"Updating {entities.Length} Entities for Vehicle Stiffness System.");
            foreach (var entity in entities)
            {
                try
                {
                    if (EntityManager.TryGetComponent(entity, out SwayingData swayingData))
                    {
                        log.Trace("Original Swaying Data: " +
                                  $"MaxPosition={swayingData.m_MaxPosition}, " +
                                  $"DampingFactors={swayingData.m_DampingFactors}");
                        var prefabName = _prefabSystem.GetPrefabName(entity);
                        if (!EntityManager.TryGetComponent(entity, out StiffnessModified stiffnessModified))
                        {
                            stiffnessModified = new StiffnessModified
                            {
                                VanillaData = new SwayingData()
                                {
                                    m_VelocityFactors = swayingData.m_VelocityFactors,
                                    m_SpringFactors = swayingData.m_SpringFactors,
                                    m_DampingFactors = swayingData.m_DampingFactors,
                                    m_MaxPosition = swayingData.m_MaxPosition
                                }
                            };
                            // Store vanilla data (not serializable, prefab will reset on restart anyway)
                            EntityManager.AddComponentData(entity, stiffnessModified);
                        }
                        swayingData.m_MaxPosition = stiffnessModified.VanillaData.m_MaxPosition / Setting.Instance!.StiffnessModifier;
                        swayingData.m_DampingFactors = stiffnessModified.VanillaData.m_DampingFactors / Setting.Instance!.DampingModifier;
                        EntityManager.SetComponentData(entity, swayingData);
                        log.Trace($"Updated swaying data for {prefabName}: MaxPosition={swayingData.m_MaxPosition}, DampingFactors={swayingData.m_DampingFactors}");

                    }
                }
                catch (Exception x)
                {
                    log.Error("Error updating stiffness: " + x.Message);
                }
            }
        }
    }

}