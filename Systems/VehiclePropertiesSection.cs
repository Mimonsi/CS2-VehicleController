using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Colossal;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VehicleController.Data;
using VehicleController.Components;

namespace VehicleController.Systems
{
    /// <summary>
    /// Info UI section that allows service vehicle prefabs to be swapped at runtime.
    /// </summary>
    public partial class VehiclePropertiesSection : InfoSectionBase
    {
        //public ILog log = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(ChangeVehicleSection)}")
        //    .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Critical);

        protected override string group => $"{nameof(VehicleController)}.{nameof(Systems)}.{nameof(VehiclePropertiesSection)}";
        private new static ILog log;
        public static VehiclePropertiesSection Instance;
        private EndFrameBarrier m_Barrier;
        private SelectedInfoUISystem _selectedInfoUISystem;
        private ValueBinding<bool> m_Minimized;
        
        // <inheritdoc/>
        /// <summary>
        /// Initializes UI bindings and entity queries for the info section.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;
            
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            _selectedInfoUISystem.eventSelectionChanged =
                (Action<Entity, Entity, float3>)Delegate.Combine(
                    _selectedInfoUISystem.eventSelectionChanged,
                    (Action<Entity, Entity, float3>)SelectedEntityChanged);
            
            //m_InfoUISystem.AddMiddleSection(this); //
            AddMiddleSectionCustom();
            //m_InfoUISystem.AddBottomSection(this);
            Enabled = true;

            // UI -> C#
            AddBinding(new TriggerBinding<int>(group, "ApplyProbabilityClicked", ApplyProbabilityClicked));
            AddBinding(new TriggerBinding(group, "ApplyPropertiesClicked", ApplyPropertiesClicked));
            
            // C# -> UI
            m_Minimized = new ValueBinding<bool>(group, "Minimized", false);
            AddBinding(m_Minimized);
            m_Minimized.Update(false);
            AddBinding(new TriggerBinding(group, "Minimize", () =>
            {
                m_Minimized.Update(!m_Minimized.value);
            }));

            //GameManager.instance.RegisterUpdater(PopulateAvailableVehicles);
            log.Info($"VehiclePropertiesSection created with group {group}");
        }
        private void ApplyProbabilityClicked(int probability)
        {
            log.Info($"ApplyProbabilityClicked: {probability}");
            if (selectedEntity == Entity.Null) return;
            if (!EntityManager.TryGetComponent<PrefabRef>(selectedEntity, out var prefabRef)) return;

            Entity prefabEntity = prefabRef.m_Prefab;
            string prefabName = m_PrefabSystem.GetPrefabName(prefabEntity);

            // Persist in the current probability pack (saves to disk)
            VehicleProbabilitySystem.Instance.SetPrefabProbability(prefabName, probability);

            // Apply immediately to the prefab entity
            if (EntityManager.TryGetComponent<PersonalCarData>(prefabEntity, out var carData))
            {
                carData.m_Probability = (byte)Math.Clamp(probability, 0, 255);
                EntityManager.SetComponentData(prefabEntity, carData);
                EntityManager.AddComponent<BatchesUpdated>(prefabEntity);
                log.Info($"Set probability of {prefabName} to {probability}");
            }
            TriggerUpdate();
        }
        
        private void ApplyPropertiesClicked()
        {
            log.Trace("ApplyPropertiesClicked");
        }

        /// <summary>
        /// Modified version of AddMiddleSection to customize the exact position
        /// </summary>
        private void AddMiddleSectionCustom()
        {
            // Use reflection to add this section to the list of middle sections from SelectedInfoUISystem.
            // By adding right after the game's CompanySection, this section will be displayed right after CompanySection.
            try
            {
                FieldInfo fieldInfoMiddleSections = typeof(SelectedInfoUISystem).GetField("m_MiddleSections",
                    BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException();
                List<ISectionSource> middleSections =
                    (List<ISectionSource>)fieldInfoMiddleSections.GetValue(_selectedInfoUISystem);
                bool found = false;
                for (int i = 0; i < middleSections.Count; i++)
                {
                    if (middleSections[i] is VehiclesSection)
                    {
                        middleSections.Insert(i, this); // Put this section BEFORE the VehicleSection. i + 1 would put it after.
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Mod.log.Error(
                        $"Change Company unable to find CompanySection in middle sections of SelectedInfoUISystem.");
                }
            }
            catch (Exception ex)
            {
                m_InfoUISystem.AddMiddleSection(this);
            }
        }
        
        /// <summary>
        /// Called when the selection changes in the info UI.
        /// Refreshes the visible state of this section.
        /// </summary>
        private void SelectedEntityChanged(Entity entity, Entity prefab, float3 position)
        {
            visible = Visible();
        }
        
        protected override void Reset()
        {
            
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            
        }

        /// <summary>
        /// Forces an update of the selected entity and refreshes this section.
        /// </summary>
        private void TriggerUpdate()
        {
            _selectedInfoUISystem.SetDirty();
        }
        
        private string GetLocalizedPrefabName(Entity entity)
        {
            if (EntityManager.TryGetComponent<PrefabRef>(entity, out var prefabRef))
            {
                if (m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefab))
                {
                    return LocaleHelper.Translate($"Assets.NAME[{prefab.name}]") ?? prefab.name;
                }
            }
            return "Unknown Prefab";
        }
        

        private bool Visible()
        {
            if (selectedEntity == Entity.Null) return false;
            if (EntityManager.TryGetComponent<PrefabRef>(selectedEntity, out var prefabRef))
            {
                return EntityManager.HasComponent<PersonalCarData>(prefabRef.m_Prefab);
            }
            return false;
        }

        protected override void OnProcess()
        {
        }
        
        private PrefabBase? GetPrefabBaseForName(string prefabName)
        {
            // TODO: Make this work for custom assets
            if (!m_PrefabSystem.TryGetPrefab(
                    new PrefabID("CarPrefab", prefabName),
                    out PrefabBase prefab))
            {
                var prefabId = PrefabCacheSystem.GetPrefabIDByName(prefabName);
                if (prefabId != null)
                {
                    PrefabID id = prefabId.Value;
                    if (!m_PrefabSystem.TryGetPrefab(
                            id,
                            out prefab))
                    {
                        log.Warn($"Could not get prefab for name: {prefabName}. Thumbnail not loaded.");
                        return null;
                    }
                }
            }
            return prefab;
        }

        private Entity? GetEntityForName(string prefabName)
        {
            var prefab = GetPrefabBaseForName(prefabName);
            if (prefab != null && m_PrefabSystem.TryGetEntity(prefab, out Entity entity))
            {
                return entity;
            }
            return null;
        }
        
        /// <inheritdoc/>
        public override void OnWriteProperties(IJsonWriter writer)
        {
            if (selectedEntity == Entity.Null)
            {
                log.Error("Selected entity is null, THIS SHOULD NEVER HAPPEN!");
                return;
            }

            string prefabName = "";
            int probability = 100;
            bool overrideProbability = false;

            if (EntityManager.TryGetComponent<PrefabRef>(selectedEntity, out var prefabRef))
            {
                Entity prefabEntity = prefabRef.m_Prefab;
                prefabName = m_PrefabSystem.GetPrefabName(prefabEntity);

                if (EntityManager.TryGetComponent<PersonalCarData>(prefabEntity, out var carData))
                {
                    probability = carData.m_Probability;
                }
                overrideProbability = VehicleProbabilitySystem.Instance?.HasPrefabOverride(prefabName) ?? false;
            }

            writer.PropertyName("prefabName");
            writer.Write(prefabName);
            writer.PropertyName("overrideProbability");
            writer.Write(overrideProbability);
            writer.PropertyName("probability");
            writer.Write(probability);
            writer.PropertyName("overrideProperties");
            writer.Write(false);
            writer.PropertyName("maxSpeed");
            writer.Write(0);
            writer.PropertyName("acceleration");
            writer.Write(0);
            writer.PropertyName("braking");
            writer.Write(0);
        }
    }
}