﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI.InGame;
using Game.Vehicles;
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
    
        private EntityQuery m_ExistingServiceVehicleQuery;
        private EntityQuery m_CreatedServiceVehicleQuery;
        private EndFrameBarrier m_Barrier;
        private Dictionary<ServiceVehicleType, List<SelectableVehiclePrefab>> _availableVehiclePrefabs = new();
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
            AddBinding(new TriggerBinding("VehicleController", "ClearBufferClicked", ClearBufferClicked));
            AddBinding(new TriggerBinding("VehicleController", "Debug2Clicked", Debug2Clicked));
            
            m_CreatedServiceVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Game.Common.Owner>(),
                    ComponentType.ReadOnly<Car>(), // Don't affect aircraft
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
            
            m_ExistingServiceVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Game.Common.Owner>(),
                    ComponentType.ReadOnly<Car>()  // Don't affect aircraft
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

        private void Debug2Clicked()
        {
            
        }

        private void ClearBufferClicked()
        {
            if (EntityManager.HasBuffer<AllowedVehiclePrefab>(selectedEntity))
            {
                EntityManager.RemoveComponent<AllowedVehiclePrefab>(selectedEntity);
                Logger.Info("Buffer has been cleared for entity: " + selectedEntity);
            }
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
            // Don't save dummy element
            if (prefabName.Contains("Vehicles Selected"))
                return;
            // Save selected company index.
            //_selectedCompanyIndex = selectedCompanyIndex;
            Logger.Info("SelectedVehicleChanged: " + prefabName);
            // Send selected company index back to the UI so the correct dropdown entry is highlighted.
            //_bindingSelectedCompanyIndex.Update(_selectedCompanyIndex);
            AddAllowedVehicle(prefabName);
        }

        private void AddAllowedVehicle(string prefabName)
        {
            if (!EntityManager.HasBuffer<AllowedVehiclePrefab>(selectedEntity))
            {
                EntityManager.AddBuffer<AllowedVehiclePrefab>(selectedEntity);
            }
            var buffer = EntityManager.GetBuffer<AllowedVehiclePrefab>(selectedEntity);
            var prefab = new AllowedVehiclePrefab() { PrefabName = prefabName };
            int length = buffer.Length;
            var successfulAdd = CollectionUtils.TryAddUniqueValue(buffer, prefab);
            int length2 = buffer.Length;
            if (successfulAdd)
            {
                Logger.Info("Added allowed vehicle prefab: " + prefabName);
            }
            else
            { 
                Logger.Info($"Vehicle prefab {prefabName} already exists in allowed vehicles, therefore it is being removed");

                CollectionUtils.RemoveValue(buffer, prefab);
            }
            int length3 = buffer.Length;
            Logger.Debug("Buffer lengths: " + length + " -> " + length2 + " -> " + length3);
        }
        
        /// <summary>
        /// Handle click on the Change Now button. Applied changes to all existing vehicles, not just new ones
        /// </summary>
        private void ChangeNowClicked()
        {
            Logger.Debug("ChangeNow clicked");
            // Send the change company data to the ChangeCompanySystem.
            //Entity newCompanyPrefab = _sectionPropertyCompanyInfos[_selectedCompanyIndex].CompanyPrefab;
            //_changeCompanySystem.ChangeCompany(newCompanyPrefab, selectedEntity, selectedPrefab, _sectionPropertyPropertyType);
            
            NativeArray<Entity> entities = m_ExistingServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            foreach (Entity entity in entities)
            {
                // Has Owner
                if (EntityManager.TryGetComponent(entity, out Owner owner) && owner.m_Owner != Entity.Null)
                {
                    // Owner has custom buffer DISABLED FOR NOW, ALWAYS TRUE
                    if (EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<AllowedVehiclePrefab> allowedVehicles))
                    {
                        if (EntityManager.TryGetComponent<PrefabRef>(entity, out PrefabRef prefabRef))
                        {
                            Logger.Info("Detected prefab ref: " + prefabRef.m_Prefab);
                            var newPrefab = ChangePrefab(entity, prefabRef, allowedVehicles);
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

        /// <summary>
        /// Collect all vehicle prefabs and populate the dictionary. Each service now has a list of all available vehicle prefabs
        /// </summary>
        private void PopulateAvailableVehicles()
        {
            _availableVehiclePrefabs.Clear();
            
            var policeCars = SystemAPI.QueryBuilder().WithAll<PoliceCarData>().WithAll<CarData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.PoliceCar, GetPrefabsForType(ServiceVehicleType.PoliceCar, policeCars));
            //Logger.Info("Available Police Vehicles: " + _availableVehiclePrefabs[ServiceVehicleType.PoliceCar].Count);
            
            var ambulances = SystemAPI.QueryBuilder().WithAll<AmbulanceData>().WithAll<CarData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.Ambulance, GetPrefabsForType(ServiceVehicleType.Ambulance, ambulances));
            
            var fireEngines = SystemAPI.QueryBuilder().WithAll<FireEngineData>().WithAll<CarData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.FireEngine, GetPrefabsForType(ServiceVehicleType.FireEngine, fireEngines));
            
            var garbageTrucks = SystemAPI.QueryBuilder().WithAll<GarbageTruckData>().WithAll<CarData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.GarbageTruck, GetPrefabsForType(ServiceVehicleType.GarbageTruck, garbageTrucks));
            
            var hearses = SystemAPI.QueryBuilder().WithAll<HearseData>().WithAll<CarData>().Build().ToEntityArray(Allocator.Temp);
            _availableVehiclePrefabs.Add(ServiceVehicleType.Hearse, GetPrefabsForType(ServiceVehicleType.Hearse, hearses));
            
            var postVans = SystemAPI.QueryBuilder().WithAll<PostVanData>().WithAll<CarData>().Build().ToEntityArray(Allocator.Temp);
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

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            VehicleCreated();
        }
        
        private Entity? ChangePrefab(Entity entity, PrefabRef prefabRef, DynamicBuffer<AllowedVehiclePrefab> allowedPrefabs)
        {
            List<string> allowedVehicleNames = new List<string>();
            
            foreach (var allowedVehiclePrefab in allowedPrefabs)
            {
                if (!string.IsNullOrEmpty(allowedVehiclePrefab.PrefabName.ToString()))
                {
                    allowedVehicleNames.Add(allowedVehiclePrefab.PrefabName.ToString());
                }
            }
            
            while (allowedVehicleNames.Contains("")) // Temporary fix for empty prefab names TODO: Fix
            {
                allowedVehicleNames.Remove("");
            }

            if (m_PrefabSystem.TryGetPrefab(prefabRef, out VehiclePrefab prefab))
            {
                var currentPrefabName = m_PrefabSystem.GetPrefab<VehiclePrefab>(prefabRef);
                if (Setting.Instance.DeleteVehicleInstances)
                {
                    if (!allowedVehicleNames.Contains(currentPrefabName.name))
                    {
                        EntityManager.AddComponent<Deleted>(entity);
                        return null;
                    }
                }
                /*if (allowedVehicleNames.Count == 0 || allowedVehicleNames.Contains(currentPrefabName.name))
                {
                    Logger.Debug($"Not changing prefab {currentPrefabName}, as it's allowed.");
                    return null; // No change needed, prefab is already allowed
                }*/
            
                // If the prefab is not allowed, we need to change it
                // Select random allowed prefab
                int index = UnityEngine.Random.Range(0, allowedVehicleNames.Count);
                var newPrefabName = allowedVehicleNames[index];
            
                Logger.Info($"Changing Prefab to {newPrefabName}");
                if (m_PrefabSystem.TryGetPrefab(
                        new PrefabID("CarPrefab", newPrefabName),
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
                    Logger.Warn("Could not get prefab for name: " + newPrefabName);
                }
            }
            else
            {
                Logger.Error("Could not get prefab for prefabRef: " + prefabRef.m_Prefab);
            }
            

            return null;
        }

        private void VehicleCreated()
        {
            // TODO: Add code to handle behaviour after vehicle has been created
            NativeArray<Entity> entities = m_CreatedServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            //Logger.Debug("New vehicle created");
            foreach (Entity entity in entities)
            {
                // Has Owner
                if (EntityManager.TryGetComponent(entity, out Owner owner) && owner.m_Owner != Entity.Null)
                {
                    // Owner has custom buffer DISABLED FOR NOW, ALWAYS TRUE
                    if (EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<AllowedVehiclePrefab> allowedVehicles))
                    {
                        if (EntityManager.TryGetComponent<PrefabRef>(entity, out PrefabRef prefabRef))
                        {
                            Logger.Info("Detected prefab ref: " + prefabRef.m_Prefab);
                            var newPrefab = ChangePrefab(entity, prefabRef, allowedVehicles);
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
        
        private PrefabBase? GetPrefabBaseForName(string prefabName)
        {
            if (m_PrefabSystem.TryGetPrefab(
                    new PrefabID("CarPrefab", prefabName),
                    out PrefabBase prefab))
            {
                return prefab;
            }
            return null;
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

        private int markSelectedVehicles(List<SelectableVehiclePrefab> vehiclePrefabs)
        {
            var count = 0;
            if (EntityManager.HasBuffer<AllowedVehiclePrefab>(selectedEntity))
            {
                var allowedVehicles = EntityManager.GetBuffer<AllowedVehiclePrefab>(selectedEntity);
                foreach (var prefab in vehiclePrefabs)
                {
                    foreach( var allowedVehicle in allowedVehicles)
                    {
                        if (allowedVehicle.PrefabName == prefab.prefabName)
                        {
                            prefab.selected = true; // Mark the vehicle as selected
                            count++;
                            //Logger.Info("Marked vehicle as selected: " + prefab.prefabName);
                        }
                    }
                }
            }

            return count;
        }
        
        /// <inheritdoc/>
        public override void OnWriteProperties(IJsonWriter writer)
        {
            // TODO: Use ImageSystem.GetThumbnail(PrefabBase) to get UI
            var types = GetServiceVehicleTypes();
            var prefabs = new List<SelectableVehiclePrefab>(); // Collect all available vehicle prefabs for the selected building
            PopulateAvailableVehicles();
            int selectedVehicleCount = 0; // Count of selected vehicles
            foreach (var type in types)
            {
                if (_availableVehiclePrefabs.TryGetValue(type, out var vehiclePrefabs))
                {
                    selectedVehicleCount += markSelectedVehicles(vehiclePrefabs);
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
            writer.PropertyName("vehiclesSelected");
            writer.Write(selectedVehicleCount);
            //writer.PropertyName("serviceType");
            //writer.Write("");
        }

        protected override string group => nameof(ChangeVehicleSection);
    }
}