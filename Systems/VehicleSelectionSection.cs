using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Game.Effects;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
    public partial class VehicleSelectionSection : InfoSectionBase
    {
        //public ILog log = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(ChangeVehicleSection)}")
        //    .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Critical);

        protected override string group => $"{nameof(VehicleController)}.{nameof(Systems)}.{nameof(VehicleSelectionSection)}";
        private new static ILog log;
        public static VehicleSelectionSection Instance;
        private EntityQuery _existingServiceVehicleQuery;
        private EntityQuery _createdServiceVehicleQuery;
        private EntityQuery _serviceBuildingQuery;
        private static readonly VehicleClipboard Clipboard = new();
        private EndFrameBarrier _endFrameBarrier;
        private VFXSystem _vfxSystem;
        private EffectControlSystem _effectControlSystem;
        private Dictionary<ServiceType, List<SelectableVehiclePrefab>> _availableVehiclePrefabs = new();
        private SelectedInfoUISystem _selectedInfoUISystem;
        private ValueBinding<bool> _minimized;
        private ValueBinding<string> _clipboardData;

        private string _serviceName;
        private string? _districtName;
        private string _prefabName;

        private static readonly IReadOnlyList<ServiceDescriptor> ServiceDescriptors = ServiceCatalog.Descriptors;

        private static readonly ComponentType[] ServiceVehicleComponentTypes = ServiceCatalog.VehicleComponentTypes;

        private static readonly ComponentType[] ServiceBuildingComponentTypes = ServiceCatalog.BuildingComponentTypes;

        private static readonly ComponentType[] ServiceVehicleExcludedComponents =
        {
            ComponentType.ReadOnly<Deleted>(),
            ComponentType.ReadOnly<Game.Tools.Temp>()
        };

        private static readonly ComponentType CarDataComponent = ComponentType.ReadOnly<CarData>();
        
        
        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;

            InitializeSystems();
            RegisterBindings();
            InitializeQueries();

            RequireForUpdate(_createdServiceVehicleQuery);
            log.Info($"ChangeVehicleSection created with group {group}");
        }

        private void InitializeSystems()
        {
            _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            _vfxSystem = World.GetOrCreateSystemManaged<VFXSystem>();
            _effectControlSystem = World.GetOrCreateSystemManaged<EffectControlSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            _selectedInfoUISystem.eventSelectionChanged =
                (Action<Entity, Entity, float3>)Delegate.Combine(
                    _selectedInfoUISystem.eventSelectionChanged,
                    (Action<Entity, Entity, float3>)SelectedEntityChanged);

            AddMiddleSectionCustom();
            Enabled = true;
        }

        private void RegisterBindings()
        {
            // UI -> C# triggers
            AddBinding(new TriggerBinding<string>(group, "SelectedVehicleChanged", SelectedVehicleChanged));
            AddBinding(new TriggerBinding(group, "ChangeNowClicked", ChangeNowClicked));
            AddBinding(new TriggerBinding(group, "ClearBufferClicked", ClearBufferClicked));
            AddBinding(new TriggerBinding(group, "DeleteOwnedVehiclesClicked", DeleteOwnedVehiclesClicked));

            AddBinding(new TriggerBinding(group, "CopySelectionClicked", CopySelectionClicked));
            AddBinding(new TriggerBinding(group, "ImportClipboardClicked", ImportClipboardClicked));
            AddBinding(new TriggerBinding(group, "ExportClipboardClicked", ExportClipboardClicked));

            AddBinding(new TriggerBinding(group, "PasteSelectionClicked", PasteSelectionClicked));

            AddBinding(new TriggerBinding(group, "PasteSamePrefabClicked", PasteSamePrefabClicked));
            AddBinding(new TriggerBinding(group, "PasteSamePrefabDistrictClicked", PasteSamePrefabDistrictClicked));
            AddBinding(new TriggerBinding(group, "PasteSameServiceTypeClicked", PasteSameServiceTypeClicked));
            AddBinding(new TriggerBinding(group, "PasteSameServiceTypeDistrictClicked", PasteSameServiceTypeDistrictClicked));

            // C# -> UI value bindings
            _minimized = new ValueBinding<bool>(group, "Minimized", false);
            AddBinding(_minimized);
            _minimized.Update(false);
            AddBinding(new TriggerBinding(group, "Minimize", () =>
            {
                _minimized.Update(!_minimized.value);
            }));

            _clipboardData = new ValueBinding<string>(group, "ClipboardData", string.Empty);
            AddBinding(_clipboardData);
            _clipboardData.Update(string.Empty);
        }

        private void InitializeQueries()
        {
            _createdServiceVehicleQuery = CreateServiceVehicleQuery(requireCreatedComponent: true);
            _existingServiceVehicleQuery = CreateServiceVehicleQuery(requireCreatedComponent: false);
            _serviceBuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>()
                },
                Any = ServiceBuildingComponentTypes
            });
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
                Any = ServiceVehicleComponentTypes,
                None = ServiceVehicleExcludedComponents
            });
        }

        private void DeleteOwnedVehiclesClicked()
        {
            log.Trace("DeleteVehicles clicked");
            NativeArray<Entity> existingServiceVehicleEntities = _existingServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            
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
            TriggerUpdate();
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
                Clipboard.CopyFrom(EntityManager, selectedEntity);
                _clipboardData.Update(Clipboard.Serialize());
            }
            catch(Exception x)
            {
                log.Error($"Error copying vehicles: {x.Message}");
            }
        }
        
        private void PasteSelectionClicked()
        {
            log.Trace("PasteSelectionClicked");
            if (selectedEntity == Entity.Null)
            {
                log.Error("Selected entity is null, cannot paste vehicles");
                return;
            }
            if (Clipboard.IsEmpty)
            {
                log.Warn("Clipboard is empty, nothing to paste");
                return;
            }
            Clipboard.ApplyTo(EntityManager, selectedEntity);
            log.Info($"Pasted {Clipboard.Count} vehicles to entity: {selectedEntity}");
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
            if (Clipboard.IsEmpty)
                return;
            if (!EntityManager.TryGetComponent(selectedEntity, out PrefabRef selectedPrefab))
                return;
            PasteToMatchingBuildings(
                entity => EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) && prefabRef.m_Prefab == selectedPrefab.m_Prefab,
                district);
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
            if (Clipboard.IsEmpty)
                return;
            var selectedTypes = new HashSet<ServiceType>(GetServiceTypes());
            if (selectedTypes.Count == 0)
                return;
            PasteToMatchingBuildings(
                entity => TryGetServiceTypeForBuilding(entity, out ServiceType serviceType) && selectedTypes.Contains(serviceType),
                district);
        }

        private void PasteSameServiceTypeDistrictClicked()
        {
            log.Info("PasteSameServiceTypeDistrictClicked");
            if (EntityManager.TryGetComponent<CurrentDistrict>(selectedEntity, out var buildingDistrict))
            {
                PasteSameServiceWithinDistrict(buildingDistrict.m_District);
            }
        }

        /// <summary>
        /// Applies the clipboard to all service buildings that match the predicate,
        /// optionally filtering by district.
        /// </summary>
        private void PasteToMatchingBuildings(Func<Entity, bool> matches, Entity? district = null)
        {
            NativeArray<Entity> entities = _serviceBuildingQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (!matches(entity))
                        continue;
                    if (EntityManager.TryGetComponent<CurrentDistrict>(entity, out var currentDistrict))
                    {
                        if (district != null && currentDistrict.m_District != district)
                            continue;
                        Clipboard.ApplyTo(EntityManager, entity);
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private void ExportClipboardClicked()
        {
            log.Verbose("ExportClipboardClicked");
            Clipboard.ExportToSystem();
        }

        private void ImportClipboardClicked()
        {
            log.Verbose("ImportClipboardClicked");
            Clipboard.ImportFromSystem();
            _clipboardData.Update(Clipboard.Serialize());
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
            log.Info("SelectedVehicleChanged: " + prefabName);
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
                log.Debug("Added allowed vehicle prefab: " + prefabName);
            }
            else
            { 
                log.Debug($"Vehicle prefab {prefabName} already exists in allowed vehicles, therefore it is being removed");
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
            NativeArray<Entity> existingServiceVehicleEntities = _existingServiceVehicleQuery.ToEntityArray(Allocator.Temp);
            
            // Filter by owner = selectedEntity
            var entities = existingServiceVehicleEntities
                .Where(e => EntityManager.HasComponent<Game.Common.Owner>(e) &&
                            EntityManager.GetComponentData<Game.Common.Owner>(e).m_Owner == selectedEntity)
                .ToArray();
            
            NativeArray<Entity> entitiesArray = new NativeArray<Entity>(entities, Allocator.Temp);
            log.Debug($"Changing vehicle prefabs for {entitiesArray.Length} existing service vehicles.");
            ChangeVehiclePrefabs(entitiesArray);
            TriggerUpdate();
        }

        /// <summary>
        /// Scans the world for service vehicle prefabs and stores them by service type.
        /// </summary>
        private void PopulateAvailableVehicles()
        {
            _availableVehiclePrefabs.Clear();

            foreach (var descriptor in ServiceDescriptors)
            {
                if (descriptor.VehiclePrefabComponents.Length == 0)
                {
                    continue;
                }

                List<ComponentType> prefabComponents = new List<ComponentType>(descriptor.VehiclePrefabComponents.Length + 1)
                {
                    CarDataComponent
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
        
        /// <summary>
        /// See Game.Effects.EffectControlSystem.EnabledActionJob.Disable()
        /// Removes all visual effects from the vehicle entity before swapping its prefab to prevent crashes.
        /// </summary>
        private unsafe void CleanupEffects(Entity effectsOwner)
        {
            if (!EntityManager.TryGetBuffer(effectsOwner, true, out DynamicBuffer<EnabledEffect> dynamicBuffer))
            {
                return;
            }
            var vfxUpdateQueue = _vfxSystem.GetSourceUpdateData();
            NativeList<EnabledEffectData> enabledData = _effectControlSystem.GetEnabledData(false, out _);
            for (int i = 0; i < dynamicBuffer.Length; i++)
            {
                ref EnabledEffect reference = ref dynamicBuffer.ElementAt(i);
                if (reference.m_EnabledIndex >= enabledData.Length)
                {
                    break;
                }
                ref EnabledEffectData enabledEffect = ref UnsafeUtility.ArrayElementAsRef<EnabledEffectData>(enabledData.GetUnsafePtr(), reference.m_EnabledIndex);
                if ((enabledEffect.m_Flags & EnabledEffectFlags.IsEnabled) != 0)
                {
                    enabledEffect.m_Flags &= ~EnabledEffectFlags.IsEnabled;
                    enabledEffect.m_Flags |= EnabledEffectFlags.EnabledUpdated;
                    if ((enabledEffect.m_Flags & EnabledEffectFlags.IsVFX) != 0)
                    {
                        vfxUpdateQueue.Enqueue(new VFXUpdateInfo
                        {
                            m_Type = VFXUpdateType.Remove,
                            m_EnabledIndex = reference.m_EnabledIndex
                        });
                    }
                }
                enabledEffect.m_Flags |= EnabledEffectFlags.Deleted;
            }
        }

        private void ChangePrefabToRandomAllowedPrefab(Entity vehicleEntity, PrefabRef prefabRef, DynamicBuffer<AllowedVehiclePrefab> allowedPrefabs)
        {
            // Collect all non-empty allowed vehicle prefab names
            // (foreach instead of LINQ because DynamicBuffer doesn't implement IEnumerable)
            var allowedVehicleNames = new List<string>();
            foreach (var allowedPrefab in allowedPrefabs)
            {
                var name = allowedPrefab.PrefabName.ToString();
                if (!string.IsNullOrEmpty(name))
                    allowedVehicleNames.Add(name);
            }

            if (allowedVehicleNames.Count == 0)
            {
                log.Warn("No allowed vehicle prefabs found");
                return;
            }

            // Select random allowed prefab
            int index = UnityEngine.Random.Range(0, allowedVehicleNames.Count);
            var newPrefabName = allowedVehicleNames[index];

            if (m_PrefabSystem.TryGetPrefab(prefabRef, out VehiclePrefab currentPrefab))
                log.Debug($"Changing {currentPrefab} Prefab to {newPrefabName}");
            else
                log.Debug($"Changing UNKNOWN Prefab to {newPrefabName}");

            if (!TryResolvePrefab(newPrefabName, out PrefabBase newPrefab))
            {
                log.Warn($"Could not resolve prefab for name: {newPrefabName}. Aborting change.");
                return;
            }

            if (!m_PrefabSystem.TryGetEntity(newPrefab, out Entity prefabEntity))
            {
                log.Warn("Could not find entity for new prefab: " + newPrefab.name);
                return;
            }

            log.Debug("New Prefab: " + newPrefab.name);
            prefabRef.m_Prefab = prefabEntity;
            if (!EntityManager.Exists(vehicleEntity))
            {
                log.Warn("Potential Crash #2: Entity destroyed in meantime");
                return;
            }

            log.Verbose("Setting prefabRef on vehicle entity: " + vehicleEntity + " to " + prefabRef.m_Prefab);
            CleanupEffects(vehicleEntity);
            EntityCommandBuffer commandBuffer = _endFrameBarrier.CreateCommandBuffer();
            commandBuffer.RemoveComponent<EnabledEffect>(vehicleEntity);
            commandBuffer.SetComponent(vehicleEntity, prefabRef);
            commandBuffer.AddComponent<Updated>(vehicleEntity);
            log.Verbose("Changed vehicle prefab to: " + newPrefab.name);
        }

        /// <summary>
        /// Called each frame to handle newly created service vehicles.
        /// </summary>
        private void VehicleCreated()
        {
            NativeArray<Entity> entities = _createdServiceVehicleQuery.ToEntityArray(Allocator.Temp);
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
            if (entities.Length == 0) // Performance skip code if no results
                return;
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
            foreach (var descriptor in ServiceDescriptors)
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
            foreach (var descriptor in ServiceDescriptors)
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
            _serviceName = GetServiceTypeNameForBuilding(selectedEntity);
            _prefabName = GetLocalizedPrefabName(selectedEntity);
            _districtName = null;
            log.Debug($"Service name for selected entity: {_serviceName}, prefab: {_prefabName}");
            if (EntityManager.TryGetComponent<CurrentDistrict>(selectedEntity, out var district))
            {
                if (district.m_District != Entity.Null)
                {
                    _districtName = m_NameSystem.GetRenderedLabelName(district.m_District);
                }
            }
            
            if (types.Count == 0)
            {
                log.Debug($"No service vehicle types available for selected entity of type: {_serviceName}");
                return false;
            }
            
            if (!types.Any(t => SupportedServiceTypes.Contains(t)))
            {
                log.Debug($"Service vehicle types for entity type {_serviceName} are not supported: " + string.Join(", ", types));
                return false;
            }
            
            log.Debug($"Service vehicle types for entity type {_serviceName}: " + string.Join(", ", types));
            return true;
        }

        protected override void OnProcess()
        {
        }
        
        /// <summary>
        /// Resolves a prefab by name, checking both built-in CarPrefab IDs
        /// and the PrefabCacheSystem for modded/PDXMods assets.
        /// </summary>
        private bool TryResolvePrefab(string prefabName, out PrefabBase prefab)
        {
            // Try built-in CarPrefab first
            if (m_PrefabSystem.TryGetPrefab(new PrefabID("CarPrefab", prefabName), out prefab))
                return true;

            // Fall back to PrefabCacheSystem for modded assets (since 1.5.3)
            var cachedId = PrefabCacheSystem.GetPrefabIDByName(prefabName);
            if (cachedId != null && m_PrefabSystem.TryGetPrefab(cachedId.Value, out prefab))
                return true;

            prefab = null;
            return false;
        }

        private Entity? GetEntityForName(string prefabName)
        {
            if (TryResolvePrefab(prefabName, out var prefab) && m_PrefabSystem.TryGetEntity(prefab, out Entity entity))
                return entity;
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
                    if (TryResolvePrefab(prefab.prefabName, out var prefabBase))
                        prefab.imageUrl = ImageSystem.GetThumbnail(prefabBase);
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
            writer.Write(_serviceName); // TODO: Get actual service name based on building type
            writer.PropertyName("prefabName");
            writer.Write(_prefabName);
            writer.PropertyName("districtName");
            writer.Write(_districtName);
            writer.PropertyName("displayPrefabNames");
            writer.Write(Setting.Instance!.DisplayVehiclePrefabNames);
            
            
            //Logger.Debug("ChangeVehicleSection properties written");
        }

        public void RemoveAllowedVehiclePrefabs()
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