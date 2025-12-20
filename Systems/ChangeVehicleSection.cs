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
    public partial class ChangeVehicleSection : InfoSectionBase
    {
        //public ILog log = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(ChangeVehicleSection)}")
        //    .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Critical);

        private new static ILog log;
        public static ChangeVehicleSection Instance;
        private EntityQuery m_ExistingServiceVehicleQuery;
        private EntityQuery m_CreatedServiceVehicleQuery;
        private EntityQuery m_ServiceBuildingQuery;
        private static List<string> m_Clipboard = new();
        private EndFrameBarrier m_Barrier;
        private Dictionary<ServiceType, List<SelectableVehiclePrefab>> _availableVehiclePrefabs = new();
        private SelectedInfoUISystem _selectedInfoUISystem;
        private ValueBinding<bool> m_Minimized;
        private ValueBinding<string> m_ClipboardData;

        private string serviceName;
        private string? districtName;
        private string prefabName;

        private static readonly IReadOnlyList<ServiceDescriptor> s_ServiceDescriptors = ServiceCatalog.Descriptors;

        private static readonly ComponentType[] s_ServiceVehicleComponentTypes = ServiceCatalog.VehicleComponentTypes;

        private static readonly ComponentType[] s_ServiceBuildingComponentTypes = ServiceCatalog.BuildingComponentTypes;

        private static readonly ComponentType[] s_ServiceVehicleExcludedComponents =
        {
            ComponentType.ReadOnly<Deleted>(),
            ComponentType.ReadOnly<Game.Tools.Temp>()
        };

        private static readonly ComponentType s_CarDataComponent = ComponentType.ReadOnly<CarData>();
        
        
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
            Enabled = true;

            // UI -> C#
            AddBinding(new TriggerBinding<string>(Mod.Id, "SelectedVehicleChanged", SelectedVehicleChanged));
            AddBinding(new TriggerBinding(Mod.Id, "ChangeNowClicked", ChangeNowClicked));
            AddBinding(new TriggerBinding(Mod.Id, "ClearBufferClicked", ClearBufferClicked));
            AddBinding(new TriggerBinding(Mod.Id, "DeleteOwnedVehiclesClicked", DeleteOwnedVehiclesClicked));
            
            AddBinding(new TriggerBinding(Mod.Id, "CopySelectionClicked", CopySelectionClicked));            
            AddBinding(new TriggerBinding(Mod.Id, "ExportClipboardClicked", ExportClipboardClicked));
            AddBinding(new TriggerBinding(Mod.Id, "ImportClipboardClicked", ImportClipboardClicked));
            
            AddBinding(new TriggerBinding(Mod.Id, "PasteSelectionClicked", PasteSelectionClicked));
            
            AddBinding(new TriggerBinding(Mod.Id, "PasteSamePrefabClicked", PasteSamePrefabClicked));
            AddBinding(new TriggerBinding(Mod.Id, "PasteSamePrefabDistrictClicked", PasteSamePrefabDistrictClicked));
            AddBinding(new TriggerBinding(Mod.Id, "PasteSameServiceTypeClicked", PasteSameServiceTypeClicked));
            AddBinding(new TriggerBinding(Mod.Id, "PasteSameServiceTypeDistrictClicked", PasteSameServiceTypeDistrictClicked));
            
            // C# -> UI
            m_Minimized = new ValueBinding<bool>(Mod.Id, "Minimized", false);
            AddBinding(m_Minimized);
            m_Minimized.Update(false);
            AddBinding(new TriggerBinding(Mod.Id, "Minimize", () =>
            {
                m_Minimized.Update(!m_Minimized.value);
            }));

            m_ClipboardData = new ValueBinding<string>(Mod.Id, "ClipboardData", string.Empty);
            AddBinding(m_ClipboardData);
            m_ClipboardData.Update(string.Empty);
            
            m_CreatedServiceVehicleQuery = CreateServiceVehicleQuery(requireCreatedComponent: true);
            m_ExistingServiceVehicleQuery = CreateServiceVehicleQuery(requireCreatedComponent: false);
            m_ServiceBuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>()
                },
                Any = s_ServiceBuildingComponentTypes
            });
            
            RequireForUpdate(m_CreatedServiceVehicleQuery);

            //GameManager.instance.RegisterUpdater(PopulateAvailableVehicles);
            log.Info("ChangeVehicleSection created.");
        }

        private EntityQuery CreateServiceVehicleQuery(bool requireCreatedComponent)
        {
            List<ComponentType> allComponents = new List<ComponentType>
            {
                ComponentType.ReadOnly<Game.Common.Owner>(),
                ComponentType.ReadOnly<Car>()
            };

            if (requireCreatedComponent)
            {
                allComponents.Add(ComponentType.ReadOnly<Created>());
            }

            return GetEntityQuery(new EntityQueryDesc
            {
                All = allComponents.ToArray(),
                Any = s_ServiceVehicleComponentTypes,
                None = s_ServiceVehicleExcludedComponents
            });
        }

        private void DeleteOwnedVehiclesClicked()
        {
            log.Trace("DeleteVehicles clicked");
            NativeArray<Entity> existingServiceVehicleEntities = m_ExistingServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            
            // Filter by owner = selectedEntity
            var entities = existingServiceVehicleEntities
                .Where(e => EntityManager.HasComponent<Game.Common.Owner>(e) &&
                            EntityManager.GetComponentData<Game.Common.Owner>(e).m_Owner == selectedEntity)
                .ToArray();

            foreach (Entity entity in entities)
            {
                EntityManager.AddComponent<Deleted>(entity);
            }
            log.Info("Deleted " + entities.Length + " vehicles for entity: " + selectedEntity);
        }

        private void ClearBufferClicked()
        {
            log.Trace("ClearBufferClicked");
            if (EntityManager.HasBuffer<AllowedVehiclePrefab>(selectedEntity))
            {
                EntityManager.RemoveComponent<AllowedVehiclePrefab>(selectedEntity);
                log.Info("Buffer has been cleared for entity: " + selectedEntity);
                TriggerUpdate();
            }
        }

        private void CopySelectionClicked()
        {
            log.Trace("CopySelectionClicked");
            try
            {
                m_Clipboard.Clear();
                if (EntityManager.HasBuffer<AllowedVehiclePrefab>(selectedEntity))
                {
                    foreach (var allowed in EntityManager.GetBuffer<AllowedVehiclePrefab>(selectedEntity))
                    {
                        m_Clipboard.Add(allowed.PrefabName.ToString());
                    }
                }

                m_ClipboardData.Update(string.Join(",", m_Clipboard));
                log.Info($"Copied {m_Clipboard.Count} vehicles to clipboard");
            }
            catch(Exception x)
            {
                log.Error($"Error copying vehicles: {x.Message}");
            }
        }
        
        private void PasteSelectionClicked()
        {
            // TODO: Fix
            log.Trace("PasteSelectionClicked");
            if (selectedEntity == Entity.Null)
            {
                log.Error("Selected entity is null, cannot paste vehicles");
                return;
            }
            if (m_Clipboard.Count == 0)
            {
                log.Warn("Clipboard is empty, nothing to paste");
                return;
            }
            ApplyClipboardToEntity(selectedEntity);
            log.Info($"Pasted {m_Clipboard.Count} vehicles to entity: {selectedEntity}");
        }

        /// <summary>
        /// Load allowed vehicles from the clipboard into the entity
        /// </summary>
        /// <param name="entity"></param>
        private void ApplyClipboardToEntity(Entity entity)
        {
            log.Trace("Applying clipboard to entity: " + entity);
            if (EntityManager.HasBuffer<AllowedVehiclePrefab>(entity))
            {
                EntityManager.RemoveComponent<AllowedVehiclePrefab>(entity);
            }
            if (m_Clipboard.Count == 0)
                return;
            var buffer = EntityManager.AddBuffer<AllowedVehiclePrefab>(entity);
            foreach (var name in m_Clipboard)
            {
                buffer.Add(new AllowedVehiclePrefab { PrefabName = name });
            }
        }

        /// <summary>
        /// Paste clipboard to all service buildings with the same prefab as the selected entity
        /// </summary>
        private void PasteSamePrefabClicked()
        {
            log.Trace("PasteSamePrefabClicked");
            PasteSamePrefabWithinDistrict();
        }

        private void PasteSamePrefabWithinDistrict(Entity? district = null)
        {
            if (m_Clipboard.Count == 0)
                return;
            if (!EntityManager.TryGetComponent(selectedEntity, out PrefabRef selectedPrefab))
                return;
            NativeArray<Entity> entities = m_ServiceBuildingQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) && prefabRef.m_Prefab == selectedPrefab.m_Prefab)
                    {
                        if(EntityManager.TryGetComponent<CurrentDistrict>(entity, out var currentDistrict))
                        {
                            // Don't apply if district doesn't match
                            if (district != null && currentDistrict.m_District != district)
                            {
                                continue;
                            }
                            ApplyClipboardToEntity(entity);
                        }

                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        
        /// <summary>
        /// Paste clipboard to all service buildings with the same prefab as the selected entity
        /// </summary>
        private void PasteSamePrefabDistrictClicked()
        {
            log.Trace("PasteSamePrefabDistrictClicked");
            if (EntityManager.TryGetComponent<CurrentDistrict>(selectedEntity, out var buildingDistrict))
            {
                PasteSamePrefabWithinDistrict(buildingDistrict.m_District);
            }
            
        }

        private void PasteSameServiceTypeClicked()
        {
            log.Trace("PasteSameServiceTypeClicked");
            PasteSameServiceWithinDistrict();
        }

        private void PasteSameServiceWithinDistrict(Entity? district = null)
        {

            if (m_Clipboard.Count == 0)
                return;
            var selectedTypes = new HashSet<ServiceType>(GetServiceTypes());
            if (selectedTypes.Count == 0)
            {
                return;
            }

            NativeArray<Entity> entities = m_ServiceBuildingQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (TryGetServiceTypeForBuilding(entity, out ServiceType serviceType) && selectedTypes.Contains(serviceType))
                    {
                        if(EntityManager.TryGetComponent<CurrentDistrict>(entity, out var currentDistrict))
                        {
                            // Don't apply if district doesn't match
                            if (district != null && currentDistrict.m_District != district)
                            {
                                continue;
                            }
                            ApplyClipboardToEntity(entity);
                        }
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private void PasteSameServiceTypeDistrictClicked()
        {
            log.Info("PasteSameServiceTypeDistrictClicked");
            if (EntityManager.TryGetComponent<CurrentDistrict>(selectedEntity, out var buildingDistrict))
            {
                PasteSameServiceWithinDistrict(buildingDistrict.m_District);
            }
        }

        private void ExportClipboardClicked()
        {
            log.Verbose("ExportClipboardClicked");
            try
            {
                string clipboardText = string.Join(",", m_Clipboard);
                GUIUtility.systemCopyBuffer = clipboardText;
                log.Info("Clipboard exported to system clipboard");
            }
            catch (Exception ex)
            {
                log.Error($"Error exporting clipboard: {ex.Message}");
            }
        }

        private void ImportClipboardClicked()
        {
            log.Verbose("ImportClipboardClicked");
            try
            {
                string clipboardText = GUIUtility.systemCopyBuffer;
                log.Info($"Importing clipboard data: {clipboardText}");
                m_Clipboard.Clear();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    foreach (string entry in clipboardText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        m_Clipboard.Add(entry.Trim());
                    }
                }
                m_ClipboardData.Update(string.Join(",", m_Clipboard));
                log.Info($"Imported {m_Clipboard.Count} entries from system clipboard");
            }
            catch (Exception ex)
            {
                log.Error($"Error importing clipboard: {ex.Message}");
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
        /// Handle change to selected vehicle in the dropdown list.
        /// </summary>
        private void SelectedVehicleChanged(string prefabName)
        {
            log.Verbose("SelectedVehicleChanged: " + prefabName);
            // Don't save dummy element
            if (prefabName.Contains("Vehicles Selected"))
                return;
            // Save selected company index.
            //_selectedCompanyIndex = selectedCompanyIndex;
            log.Info("SelectedVehicleChanged: " + prefabName);
            // Send selected company index back to the UI so the correct dropdown entry is highlighted.
            //_bindingSelectedCompanyIndex.Update(_selectedCompanyIndex);
            AddAllowedVehicle(prefabName);
        }

        /// <summary>
        /// Toggles the presence of a prefab in the allowed vehicle list for the selected building.
        /// </summary>
        private void AddAllowedVehicle(string prefabName)
        {
            if (!EntityManager.HasBuffer<AllowedVehiclePrefab>(selectedEntity))
            {
                EntityManager.AddBuffer<AllowedVehiclePrefab>(selectedEntity);
            }
            var buffer = EntityManager.GetBuffer<AllowedVehiclePrefab>(selectedEntity);
            var prefab = new AllowedVehiclePrefab() { PrefabName = prefabName };
            if (CollectionUtils.TryAddUniqueValue(buffer, prefab))
            {
                log.Info("Added allowed vehicle prefab: " + prefabName);
            }
            else
            { 
                log.Info($"Vehicle prefab {prefabName} already exists in allowed vehicles, therefore it is being removed");
                CollectionUtils.RemoveValue(buffer, prefab);
            }
            TriggerUpdate();
        }
        
        /// <summary>
        /// Applies prefab changes immediately to all existing service vehicles owned by the selected building.
        /// </summary>
        private void ChangeNowClicked()
        {
            log.Verbose("ChangeNow clicked");
            NativeArray<Entity> existingServiceVehicleEntities = m_ExistingServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            
            // Filter by owner = selectedEntity
            var entities = existingServiceVehicleEntities
                .Where(e => EntityManager.HasComponent<Game.Common.Owner>(e) &&
                            EntityManager.GetComponentData<Game.Common.Owner>(e).m_Owner == selectedEntity)
                .ToArray();
            
            NativeArray<Entity> entitiesArray = new NativeArray<Entity>(entities, Allocator.Temp);
            log.Debug($"Changing vehicle prefabs for {entitiesArray.Length} existing service vehicles.");
            ChangeVehiclePrefabs(entitiesArray);
        }

        /// <summary>
        /// Scans the world for service vehicle prefabs and stores them by service type.
        /// </summary>
        private void PopulateAvailableVehicles()
        {
            _availableVehiclePrefabs.Clear();

            foreach (var descriptor in s_ServiceDescriptors)
            {
                if (descriptor.VehiclePrefabComponents.Length == 0)
                {
                    continue;
                }

                List<ComponentType> prefabComponents = new List<ComponentType>(descriptor.VehiclePrefabComponents.Length + 1)
                {
                    s_CarDataComponent
                };
                prefabComponents.AddRange(descriptor.VehiclePrefabComponents);

                EntityQuery query = GetEntityQuery(new EntityQueryDesc
                {
                    All = prefabComponents.ToArray()
                });

                NativeArray<Entity> prefabEntities = query.ToEntityArray(Allocator.Temp);
                try
                {
                    _availableVehiclePrefabs[descriptor.ServiceType] =
                        GetPrefabsForType(descriptor.ServiceType, prefabEntities);
                }
                finally
                {
                    prefabEntities.Dispose();
                }
            }

        }

        private List<SelectableVehiclePrefab> GetPrefabsForType(ServiceType type, NativeArray<Entity> entities)
        {
            List<SelectableVehiclePrefab> vehiclePrefabs = new List<SelectableVehiclePrefab>();
            foreach (var entity in entities)
            {
                if (m_PrefabSystem.TryGetPrefab(entity, out PrefabBase prefab))
                {
                    if (prefab is VehiclePrefab vehiclePrefab)
                    {
                        vehiclePrefabs.Add(new SelectableVehiclePrefab()
                        {
                            prefabName = vehiclePrefab.name
                        });
                    }
                }
            }
            return vehiclePrefabs;
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
            if (Setting.Instance.EnableChangeVehicles)
                VehicleCreated();
        }
        
        private void ChangePrefabToRandomAllowedPrefab(Entity vehicleEntity, PrefabRef prefabRef, DynamicBuffer<AllowedVehiclePrefab> allowedPrefabs)
        {
            List<string> allowedVehicleNames = new List<string>();
            
            // Collect all allowed vehicle prefab names
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

            if (allowedVehicleNames.Count == 0)
            {
                log.Warn("No allowed vehicle prefabs found");
                return; // No allowed vehicles, nothing to change
            }

            // Check if current prefab exists
            /*if (allowedVehicleNames.Count == 0 || allowedVehicleNames.Contains(currentPrefabName.name))
            {
                Logger.Debug($"Not changing prefab {currentPrefabName}, as it's allowed.");
                return null; // No change needed, prefab is already allowed
            }*/
        
            // If the prefab is not allowed, we need to change it
            // Select random allowed prefab
            int index = UnityEngine.Random.Range(0, allowedVehicleNames.Count);
            var newPrefabName = allowedVehicleNames[index];
        
            if (m_PrefabSystem.TryGetPrefab(prefabRef, out VehiclePrefab currentPrefab))
                log.Debug($"Changing {currentPrefab} Prefab to {newPrefabName}");
            else
                log.Debug($"Changing UNKNOWN Prefab to {newPrefabName}");
            // Since 1.5.3 we need to check internal assets separately from PDXMods assets (PrefabCacheSystem)
            PrefabBase newPrefab;
            if (!m_PrefabSystem.TryGetPrefab(
                    new PrefabID("CarPrefab", newPrefabName),
                    out newPrefab))
            {
                var prefabId = PrefabCacheSystem.GetPrefabIDByName(newPrefabName);
                if (prefabId != null)
                {
                    PrefabID id = prefabId.Value;
                    if (!m_PrefabSystem.TryGetPrefab(
                            id,
                            out newPrefab))
                    {
                        log.Warn($"Could not get prefab for name: {newPrefabName}. Aborting change.");
                        return;
                    }
                }
            }
            log.Debug("New Prefab: " + newPrefab!.name);
            if (m_PrefabSystem.TryGetEntity(newPrefab, out Entity prefabEntity)) // Get entity for prefab
            {
                prefabRef.m_Prefab = prefabEntity;
                log.Verbose("Setting prefabRef on vehicle entity: " + vehicleEntity + " to " + prefabRef.m_Prefab);
                EntityManager.SetComponentData(vehicleEntity, prefabRef);
                EntityManager.AddComponent<Updated>(vehicleEntity);
                log.Verbose("Changed vehicle prefab to: " + newPrefab.name);
                return;
            }
            log.Warn("Could not find entity for new prefab: " + newPrefab.name);
        }

        /// <summary>
        /// Called each frame to handle newly created service vehicles.
        /// </summary>
        private void VehicleCreated()
        {
            NativeArray<Entity> entities = m_CreatedServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            //EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            log.Verbose($"Calling Change Vehicle Prefabs for {entities.Length} created vehicles.");
            ChangeVehiclePrefabs(entities);
            log.Verbose("Finished Change Vehicle Prefabs");
        }

        /// <summary>
        /// Forces an update of the selected entity and refreshes this section.
        /// </summary>
        private void TriggerUpdate()
        {
            _selectedInfoUISystem.SetDirty();
        }

        /// <summary>
        /// Iterates over the provided vehicle entities and replaces their prefab if needed.
        /// </summary>
        private void ChangeVehiclePrefabs(NativeArray<Entity> entities)
        {
            // Loop through all vehicles that were just created (might be multiple in one frame)
            foreach (Entity entity in entities)
            {
                // Has Owner
                if (EntityManager.TryGetComponent(entity, out Owner owner) && owner.m_Owner != Entity.Null)
                {
                    // Get allowed vehicle list form owner (service building)
                    if (EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<AllowedVehiclePrefab> allowedVehicles))
                    {
                        if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                        {
                            ChangePrefabToRandomAllowedPrefab(entity, prefabRef, allowedVehicles);
                        }
                    }
                }
            }
        }

        private List<ServiceType> GetServiceTypes()
        {
            List<ServiceType> types = new List<ServiceType>();
            foreach (var descriptor in s_ServiceDescriptors)
            {
                if (descriptor.IsSupportedBuilding(EntityManager, selectedEntity))
                {
                    types.Add(descriptor.ServiceType);
                }
            }

            return types;
        }

        private bool TryGetServiceTypeForBuilding(Entity entity, out ServiceType serviceType)
        {
            foreach (var descriptor in s_ServiceDescriptors)
            {
                if (descriptor.IsSupportedBuilding(EntityManager, entity))
                {
                    serviceType = descriptor.ServiceType;
                    return true;
                }
            }

            serviceType = ServiceType.None;
            return false;
        }

        private string GetServiceTypeNameForBuilding(Entity entity)
        {
            if (TryGetServiceTypeForBuilding(entity, out ServiceType serviceType))
            {
                return serviceType.ToString();
            }
            return "Unknown ServiceType";
        }

        private bool TryGetServiceTypeForVehicle(Entity entity, out ServiceType serviceType)
        {
            foreach (var descriptor in s_ServiceDescriptors)
            {
                if (descriptor.IsSupportedVehicle(EntityManager, entity))
                {
                    serviceType = descriptor.ServiceType;
                    return true;
                }
            }

            serviceType = ServiceType.None;
            return false;
        }

        private bool TryGetServiceType(Entity entity, out ServiceType serviceType)
        {
            if (TryGetServiceTypeForBuilding(entity, out serviceType))
            {
                return true;
            }

            if (TryGetServiceTypeForVehicle(entity, out serviceType))
            {
                return true;
            }

            serviceType = ServiceType.None;
            return false;
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

        private static readonly ServiceType[] SupportedServiceTypes =
        {
            ServiceType.Healthcare,
            ServiceType.Fire,
            ServiceType.Police,
            ServiceType.Garbage,
            ServiceType.Deathcare,
            ServiceType.Postal,
        };

        private bool Visible()
        {
            if (selectedEntity == Entity.Null)
            {
                return false;
            }
            var types = GetServiceTypes();
            serviceName = GetServiceTypeNameForBuilding(selectedEntity);
            prefabName = GetLocalizedPrefabName(selectedEntity);
            districtName = null;
            log.Debug($"Service name for selected entity: {serviceName}, prefab: {prefabName}");
            if (EntityManager.TryGetComponent<CurrentDistrict>(selectedEntity, out var district))
            {
                if (district.m_District != Entity.Null)
                {
                    districtName = m_NameSystem.GetRenderedLabelName(district.m_District);
                }
            }
            
            if (types.Count == 0)
            {
                log.Debug($"No service vehicle types available for selected entity of type: {serviceName}");
                return false;
            }
            
            if (!types.Any(t => SupportedServiceTypes.Contains(t)))
            {
                log.Debug($"Service vehicle types for entity type {serviceName} are not supported: " + string.Join(", ", types));
                return false;
            }
            
            log.Debug($"Service vehicle types for entity type {serviceName}: " + string.Join(", ", types));
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

        /// <summary>
        /// Get thumbnails for all vehicle prefabs and mark selected vehicles
        /// </summary>
        /// <param name="vehiclePrefabs"></param>
        /// <returns>Count of selected vehicles</returns>
        private int ProcessVehicles(List<SelectableVehiclePrefab> vehiclePrefabs)
        {
            var count = 0;
            DynamicBuffer<AllowedVehiclePrefab> allowedVehicles = new DynamicBuffer<AllowedVehiclePrefab>();
            if (EntityManager.HasBuffer<AllowedVehiclePrefab>(selectedEntity))
            {
                allowedVehicles = EntityManager.GetBuffer<AllowedVehiclePrefab>(selectedEntity);
            }
            foreach (var prefab in vehiclePrefabs)
            {
                // Try getting the thumbnail
                try
                {
                    var prefabBase = GetPrefabBaseForName(prefab.prefabName);
                    var thumbnail = ImageSystem.GetThumbnail(prefabBase);
                    prefab.imageUrl = thumbnail;
                }
                catch (Exception x)
                {
                    log.Trace("No thumbnail found for prefab: " + prefab.prefabName + ", " + x.Message);
                }
                if (!allowedVehicles.IsEmpty)
                {
                    foreach (var allowedVehicle in allowedVehicles)
                    {
                        if (allowedVehicle.PrefabName == prefab.prefabName)
                        {
                            prefab.selected = true; // Mark the vehicle as selected
                            count++;
                        }
                    }
                }
            }
            return count;
        }
        
        /// <inheritdoc/>
        public override void OnWriteProperties(IJsonWriter writer)
        {
            if (selectedEntity == Entity.Null)
            {
                log.Error("Selected entity is null, THIS SHOULD NEVER HAPPEN!");
                return;
            }
            
            var types = GetServiceTypes();
            var prefabs = new List<SelectableVehiclePrefab>(); // Collect all available vehicle prefabs for the selected building
            PopulateAvailableVehicles();
            int selectedVehicleCount = 0; // Count of selected vehicles
            foreach (var type in types)
            {
                if (_availableVehiclePrefabs.TryGetValue(type, out var vehiclePrefabs))
                {
                    selectedVehicleCount += ProcessVehicles(vehiclePrefabs); // Add selected = true to all allowed vehicles
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
            
            writer.PropertyName("serviceName");
            writer.Write(serviceName); // TODO: Get actual service name based on building type
            writer.PropertyName("prefabName");
            writer.Write(prefabName);
            writer.PropertyName("districtName");
            writer.Write(districtName);
            writer.PropertyName("displayPrefabNames");
            writer.Write(Setting.Instance!.DisplayVehiclePrefabNames);
            
            
            //Logger.Debug("ChangeVehicleSection properties written");
        }

        protected override string group => nameof(ChangeVehicleSection);

        public static void RemoveAllowedVehiclePrefabs()
        {
            var componentQuery = Instance.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<AllowedVehiclePrefab>(),
                },
            });

            var entities = componentQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                Instance.EntityManager.RemoveComponent<AllowedVehiclePrefab>(entity);
            }
            log.Info("Removed AllowedVehiclePrefab component from " + entities.Length + " entities.");
        }
    }
}