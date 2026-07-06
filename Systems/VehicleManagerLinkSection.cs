using System;
using Colossal;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Prefabs;
using Game.UI.InGame;
using Unity.Entities;
using Unity.Mathematics;

namespace VehicleController.Systems
{
    /// <summary>
    /// A small Selected-Info-Panel section shown for a selected vehicle. It exposes the vehicle's
    /// prefab name so the UI can offer a button that opens the Vehicle Manager focused on it.
    /// </summary>
    public partial class VehicleManagerLinkSection : InfoSectionBase
    {
        protected override string group =>
            $"{nameof(VehicleController)}.{nameof(Systems)}.{nameof(VehicleManagerLinkSection)}";

        private new static ILog log;
        public static VehicleManagerLinkSection Instance;
        private SelectedInfoUISystem _selectedInfoUISystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            _selectedInfoUISystem.eventSelectionChanged =
                (Action<Entity, Entity, float3>)Delegate.Combine(
                    _selectedInfoUISystem.eventSelectionChanged,
                    (Action<Entity, Entity, float3>)SelectedEntityChanged);

            m_InfoUISystem.AddMiddleSection(this);
            Enabled = true;
            log.Info($"VehicleManagerLinkSection created with group {group}");
        }

        private void SelectedEntityChanged(Entity entity, Entity prefab, float3 position)
        {
            visible = Visible();
        }

        private bool Visible()
        {
            if (selectedEntity == Entity.Null)
                return false;
            if (EntityManager.TryGetComponent<PrefabRef>(selectedEntity, out var prefabRef))
                return EntityManager.HasComponent<CarData>(prefabRef.m_Prefab)
                       || EntityManager.HasComponent<TrainData>(prefabRef.m_Prefab);
            return false;
        }

        protected override void Reset() { }
        protected override void OnProcess() { }

        protected override void OnUpdate() { }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            string prefabName = "";
            if (selectedEntity != Entity.Null &&
                EntityManager.TryGetComponent<PrefabRef>(selectedEntity, out var prefabRef))
            {
                prefabName = m_PrefabSystem.GetPrefabName(prefabRef.m_Prefab);
            }

            writer.PropertyName("prefabName");
            writer.Write(prefabName);
        }
    }
}
