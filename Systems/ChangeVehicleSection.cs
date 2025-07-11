using System;
using System.Collections.Generic;
using System.Reflection;
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
using VehicleController.Data;

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
        private Dictionary<Entity, VehiclePrefabPool> _vehiclePrefabPools = new();
        private SelectedInfoUISystem _selectedInfoUISystem;
        
        // <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            _selectedInfoUISystem.eventSelectionChanged =
                (Action<Entity, Entity, float3>)Delegate.Combine(
                    _selectedInfoUISystem.eventSelectionChanged,
                    (Action<Entity, Entity, float3>)SelectedEntityChanged);
            
            //m_InfoUISystem.AddMiddleSection(this); //
            AddMiddleSectionCustom();
            Enabled = true;

            // UI -> C#
            AddBinding(new TriggerBinding<string>("VehicleController", "SelectedVehicleChanged", SelectedVehicleChanged));
            AddBinding(new TriggerBinding("VehicleController", "ChangeNowClicked", ChangeNowClicked));
            
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
            
            RequireForUpdate(m_CreatedServiceVehicleQuery);

            //GameManager.instance.RegisterUpdater(PopulateAvailableVehicles);
            Logger.Info("ChangeVehicleSection created.");
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
                    BindingFlags.Instance | BindingFlags.NonPublic);
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
        /// Handle change to selected vehicle in the dropdown list.
        /// </summary>
        private void SelectedVehicleChanged(string prefabName)
        {
            // Save selected company index.
            //_selectedCompanyIndex = selectedCompanyIndex;
            Logger.Info("SelectedVehicleChanged: " + prefabName);
            // Send selected company index back to the UI so the correct dropdown entry is highlighted.
            //_bindingSelectedCompanyIndex.Update(_selectedCompanyIndex);
        }
        
        /// <summary>
        /// Handle click on the Change Now button.
        /// </summary>
        private void ChangeNowClicked()
        {
            Logger.Info("ChangeNow clicked");
            // Send the change company data to the ChangeCompanySystem.
            //Entity newCompanyPrefab = _sectionPropertyCompanyInfos[_selectedCompanyIndex].CompanyPrefab;
            //_changeCompanySystem.ChangeCompany(newCompanyPrefab, selectedEntity, selectedPrefab, _sectionPropertyPropertyType);
        }

        /// <summary>
        /// Collect all vehicle prefabs and populate the dictionary. Each service now has a list of all available vehicle prefabs
        /// </summary>
        private void PopulateAvailableVehicles()
        {
            _availableVehiclePrefabs.Clear();
            
            var policeCars = SystemAPI.QueryBuilder().WithAll<PoliceCarData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.PoliceCar, GetPrefabsForType(ServiceVehicleType.PoliceCar, policeCars));
            //Logger.Info("Available Police Vehicles: " + _availableVehiclePrefabs[ServiceVehicleType.PoliceCar].Count);
            
            var ambulances = SystemAPI.QueryBuilder().WithAll<AmbulanceData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.Ambulance, GetPrefabsForType(ServiceVehicleType.Ambulance, ambulances));
            
            var fireEngines = SystemAPI.QueryBuilder().WithAll<FireEngineData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.FireEngine, GetPrefabsForType(ServiceVehicleType.FireEngine, fireEngines));
            
            var garbageTrucks = SystemAPI.QueryBuilder().WithAll<GarbageTruckData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.GarbageTruck, GetPrefabsForType(ServiceVehicleType.GarbageTruck, garbageTrucks));
            
            var hearses = SystemAPI.QueryBuilder().WithAll<HearseData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.Hearse, GetPrefabsForType(ServiceVehicleType.Hearse, hearses));
            
            var postVans = SystemAPI.QueryBuilder().WithAll<PostVanData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.PostVan, GetPrefabsForType(ServiceVehicleType.PostVan, postVans));
            
            // TODO: Add support for road/park maintenance vehicles
            // TODO: Add support for transport vehicles
            
        }

        private List<SelectableVehiclePrefab> GetPrefabsForType(ServiceVehicleType type, NativeArray<Entity> entities)
        {
            List<SelectableVehiclePrefab> vehiclePrefabs = new List<SelectableVehiclePrefab>();
            foreach (var entity in entities)
            {
                //Logger.Info("Checking Car: " + entity);
                if (m_PrefabSystem.TryGetPrefab(entity, out PrefabBase prefab))
                {
                    //Logger.Info("Found Prefab: " + prefab.name);
                    if (prefab is VehiclePrefab vehiclePrefab)
                    {
                        //Logger.Info("Found Police Vehicle Prefab: " + vehiclePrefab.name);
                        vehiclePrefabs.Add(new SelectableVehiclePrefab()
                        {
                            prefabName = vehiclePrefab.name
                        });
                    }
                }
            }
            return vehiclePrefabs;
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
            //writer.PropertyName("serviceType");
            //writer.Write("");
        }

        protected override string group => nameof(ChangeVehicleSection);
    }
}