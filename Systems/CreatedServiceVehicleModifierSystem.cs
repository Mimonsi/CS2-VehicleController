using Colossal.Entities;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using VehicleController.Data;

namespace VehicleController.Systems
{
    /// <summary>
    /// Adds custom mesh color components to created service vehicles who's owners have service vehicle color buffers.
    /// </summary>
    public partial class CreatedServiceVehicleModifierSystem : GameSystemBase
    {
        public static ILog Logger = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(CreatedServiceVehicleModifierSystem)}")
            .SetShowsErrorsInUI(false);
        private EntityQuery m_CreatedServiceVehicleQuery;
        private EndFrameBarrier m_Barrier;
        private PrefabSystem m_PrefabSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            Enabled = true;

            m_CreatedServiceVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<MeshColor>(),
                    ComponentType.ReadOnly<Game.Common.Owner>(),
                    ComponentType.ReadOnly<Created>(),
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<Game.Vehicles.Ambulance>(),
                    ComponentType.ReadOnly<Game.Vehicles.FireEngine>(),
                    ComponentType.ReadOnly<Game.Vehicles.PoliceCar>(),
                    ComponentType.ReadOnly<Game.Vehicles.GarbageTruck>(),
                    ComponentType.ReadOnly<Game.Vehicles.Hearse>(),
                    ComponentType.ReadOnly<Game.Vehicles.MaintenanceVehicle>(),
                    ComponentType.ReadOnly<Game.Vehicles.PostVan>(),
                    ComponentType.ReadOnly<Game.Vehicles.RoadMaintenanceVehicle>(),
                    ComponentType.ReadOnly<Game.Vehicles.Taxi>(),
                    ComponentType.ReadOnly<Game.Vehicles.ParkMaintenanceVehicle>(),
                    ComponentType.ReadOnly<Game.Vehicles.WorkVehicle>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });

            RequireForUpdate(m_CreatedServiceVehicleQuery);
            Logger.Info("CreatedServiceVehicleModifierSystem created.");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_CreatedServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            foreach (Entity entity in entities)
            {
                // Has Owner
                if (EntityManager.TryGetComponent(entity, out Owner owner) && owner.m_Owner != Entity.Null)
                {
                    // Owner has custom buffer DISABLED FOR NOW, ALWAYS TRUE
                    if (EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<ServiceVehicleSelection> serviceVehicleBuffer) || true)
                    {
                        if (EntityManager.TryGetComponent<PrefabRef>(entity, out PrefabRef prefabRef))
                        {
                            Logger.Info("Detected prefab ref: " + prefabRef.m_Prefab);
                            var newPrefab = ChangePolicePrefab(prefabRef);
                            if (newPrefab != null)
                            {
                                prefabRef.m_Prefab = newPrefab.Value;
                                EntityManager.SetComponentData(entity, prefabRef);
                                EntityManager.AddComponent<Updated>(entity);
                                Logger.Info("SUCCESS: Changed prefab ref for entity: " + entity);
                            }
                            // Change the prefab reference to the one in the service vehicle buffer
                            //ChangePrefabRef(serviceVehicleBuffer[0].m_PrefabRef, ref buffer, entity);
                        }
                        /*if (serviceVehicleBuffer.Length != 0)
                        {
                            if (EntityManager.HasBuffer<OwnedVehicle>(owner.m_Owner))
                            {
                                ChangePrefabRef(serviceVehicleBuffer[0].m_PrefabRef, ref buffer, entity);
                            }
                        }*/
                    }
                }
            }
        }

        private Entity? ChangePolicePrefab(PrefabRef prefabRef)
        {
            Logger.Info("Changing Prefab");
            // Check if prefab is Police1
            var prefab = m_PrefabSystem.GetPrefab<VehiclePrefab>(prefabRef);
            Logger.Info("Prefab: " + prefab?.name);
            
            if (prefab.name == "NA_PoliceVehicle01")
            {
                Logger.Info("Changing Police1 to Police2");
                if (m_PrefabSystem.TryGetPrefab(
                        new PrefabID("CarPrefab", "NA_PoliceVehicle03"),
                        out PrefabBase newPrefab))
                {
                    Logger.Info("New Prefab: " + newPrefab?.name);
                    // Change to Police2

                    if (m_PrefabSystem.TryGetEntity(newPrefab, out Entity prefabEntity))
                    {
                        return prefabEntity;
                    }
                    else
                    {
                        Logger.Warn("Could not find entity for new prefab: " + newPrefab.name);
                    }
                    
                }
                else
                {
                    Logger.Warn("Could not prefab for Police2");
                }
                
            }

            return null;
        }

        private void ChangePrefabRef(PrefabRef mPrefabRef, ref EntityCommandBuffer buffer, Entity entity)
        {
            Logger.Info("Changing PrefabRef to " + mPrefabRef.m_Prefab);
        }
    }
}