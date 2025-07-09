using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace VehicleController.Systems
{
    enum ServiceVehicleType
    {
        None,
        Ambulance,
        FireEngine,
        PoliceCar,
        GarbageTruck,
        Hearse,
        PostVan,
        TransportVehicle, // Taxi, Bus
        RoadMaintenanceVehicle,
        ParkMaintenanceVehicle
    }
    
    public partial class ChangeVehicleSection : InfoSectionBase
    {
        private ILog Logger = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(ChangeVehicleSection)}")
            .SetShowsErrorsInUI(false);
        
                private EntityQuery m_CreatedServiceVehicleQuery;
        private EndFrameBarrier m_Barrier;
        private PrefabSystem m_PrefabSystem;
        private Dictionary<ServiceVehicleType, List<SelectableVehiclePrefab>> _availableVehiclePrefabs = new();
        
        // <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            
            m_InfoUISystem.AddMiddleSection(this);
            Enabled = true;
            
            m_CreatedServiceVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Game.Common.Owner>(),
                    ComponentType.ReadOnly<Created>(),
                },
                Any = new[]
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
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });
            
            
            SelectedInfoUISystem selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            selectedInfoUISystem.eventSelectionChanged =
                (Action<Entity, Entity, float3>)Delegate.Combine(
                    selectedInfoUISystem.eventSelectionChanged,
                    (Action<Entity, Entity, float3>)SelectedEntityChanged);
            RequireForUpdate(m_CreatedServiceVehicleQuery);
            
            /*availableVehicles = new List<string>()
            {
                "NA_PoliceVehicle01",
                "NA_PoliceVehicle02",
                "EU_PoliceVehicle01",
                "EU_PoliceVehicle02",
            };*/

            //GameManager.instance.RegisterUpdater(PopulateAvailableVehicles);
            Logger.Info("ChangeVehicleSection created.");
        }

        /// <summary>
        /// Collect all vehicle prefabs and populate the dictionary. Each service now has a list of all available vehicle prefabs
        /// </summary>
        private bool PopulateAvailableVehicles()
        {
            _availableVehiclePrefabs.Clear();
            var policeCars = SystemAPI.QueryBuilder().WithAll<PoliceCarData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.PoliceCar, new List<SelectableVehiclePrefab>());
            if (policeCars.Length == 0)
                return false;
            foreach (var policeCar in policeCars)
            {
                //Logger.Info("Checking Police Car: " + policeCar);
                if (m_PrefabSystem.TryGetPrefab(policeCar, out PrefabBase prefab))
                {
                    //Logger.Info("Found Prefab: " + prefab.name);
                    if (prefab is VehiclePrefab vehiclePrefab)
                    {
                        //Logger.Info("Found Police Vehicle Prefab: " + vehiclePrefab.name);
                        _availableVehiclePrefabs[ServiceVehicleType.PoliceCar].Add(new SelectableVehiclePrefab()
                        {
                            prefabName = vehiclePrefab.name
                        });
                    }
                }
            }
            
            if (_availableVehiclePrefabs[ServiceVehicleType.PoliceCar].Count == 0)
            {
                //Logger.Info("No police vehicles found.");
                return false;
            }
            Logger.Info("Available Police Vehicles: " + _availableVehiclePrefabs[ServiceVehicleType.PoliceCar].Count);
            return true;
            
            //var ambulances = SystemAPI.QueryBuilder().WithAll<AmbulanceData>() .WithNone<Deleted>() .Build().ToEntityArray(Allocator.Temp);
            
            /*_availableVehiclePrefabs.Add(ServiceVehicleType.Ambulance, new List<SelectableVehiclePrefab>());
            foreach (var ambulance in ambulances)
            {
                if (EntityManager.TryGetComponent(ambulance, out PrefabRef prefabRef) &&
                    m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefab))
                {
                    if (prefab is VehiclePrefab vehiclePrefab)
                    {
                        _availableVehiclePrefabs[ServiceVehicleType.Ambulance].Add(new SelectableVehiclePrefab()
                        {
                            prefabName = vehiclePrefab.name
                        });
                    }
                }
            }
                
            
            var fireEngines = SystemAPI.QueryBuilder().WithAll<FireEngineData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.FireEngine, new List<SelectableVehiclePrefab>());
            foreach (var fireEngine in fireEngines)
            {
                if (EntityManager.TryGetComponent(fireEngine, out PrefabRef prefabRef) &&
                    m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefab))
                {
                    if (prefab is VehiclePrefab vehiclePrefab)
                    {
                        _availableVehiclePrefabs[ServiceVehicleType.FireEngine].Add(new SelectableVehiclePrefab()
                        {
                            prefabName = vehiclePrefab.name
                        });
                    }
                }
            }*/
        }

        private void SelectedEntityChanged(Entity entity, Entity prefab, float3 position)
        {
            visible = Visible();
        }
        
        protected override void Reset()
        {
            
        }

        protected override void OnUpdate()
        {
            
        }

        private void VehicleCreated()
        {
            // TODO: Add code to handle behaviour after vehicle has been created
        }

        private List<ServiceVehicleType> GetServiceVehicleTypes()
        {
            List<ServiceVehicleType> types = new List<ServiceVehicleType>();
            if (EntityManager.HasComponent<Game.Buildings.Hospital>(selectedEntity))
                types.Add(ServiceVehicleType.Ambulance);
            if (EntityManager.HasComponent<Game.Buildings.FireStation>(selectedEntity))
                types.Add(ServiceVehicleType.FireEngine);
            if (EntityManager.HasComponent<Game.Buildings.PoliceStation>(selectedEntity))
                types.Add(ServiceVehicleType.PoliceCar);
            if (EntityManager.HasComponent<Game.Buildings.GarbageFacility>(selectedEntity))
                types.Add(ServiceVehicleType.GarbageTruck);
            if (EntityManager.HasComponent<Game.Buildings.DeathcareFacility>(selectedEntity))
                types.Add(ServiceVehicleType.Hearse);
            if (EntityManager.HasComponent<Game.Buildings.PostFacility>(selectedEntity))
                types.Add(ServiceVehicleType.PostVan);
            if (EntityManager.TryGetComponent<Game.Buildings.MaintenanceDepot>(selectedEntity, out var maintenanceDepot))
            {
                // TODO: Differentiate between road and park maintenance vehicles
                types.Add(ServiceVehicleType.RoadMaintenanceVehicle);
            }
            if (EntityManager.HasComponent<Game.Buildings.TransportDepot>(selectedEntity))
                types.Add(ServiceVehicleType.TransportVehicle); // Taxi, Bus
            

            return types;
        }

        private bool Visible()
        {
            if (selectedEntity == Entity.Null)
            {
                return false;
            }
            var types = GetServiceVehicleTypes();
            if (types.Count == 0)
            {
                Logger.Info("No service vehicle types available for selected entity.");
                return false;
            }
            Logger.Info("Types: " + string.Join(", ", types));
            // TODO: Add toggle to disable in settings
            return true;
        }

        protected override void OnProcess()
        {
            
        }

        /// <inheritdoc/>
        public override void OnWriteProperties(IJsonWriter writer)
        {
            var types = GetServiceVehicleTypes();
            var prefabs = new List<SelectableVehiclePrefab>(); // Collect all available vehicle prefabs for the selected building
            PopulateAvailableVehicles();
            foreach (var type in types)
            {
                if (_availableVehiclePrefabs.TryGetValue(type, out var vehiclePrefabs))
                {
                    prefabs.AddRange(vehiclePrefabs);
                }
            }
            
            writer.PropertyName("availableVehicles");
            writer.ArrayBegin(prefabs.Count);
            //new SelectableVehiclePrefab(){ prefabName = "NA_PoliceVehicle01" }.Write(writer);
            foreach (var prefab in prefabs) // Write all available vehicle prefabs
            {
                prefab.Write(writer);
            }
            writer.ArrayEnd();
            writer.PropertyName("serviceType");
            writer.Write("Anything");
        }

        protected override string group => nameof(ChangeVehicleSection);
    }
}