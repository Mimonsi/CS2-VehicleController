using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using VehicleController.Components;
using VehicleController.Data;
using PoliceCar = Game.Vehicles.PoliceCar;

namespace VehicleController.Systems
{
    /// <summary>
    /// Enforces a building's allowed-vehicle selection on newly spawned service vehicles by
    /// swapping their <see cref="PrefabRef"/> while they still carry the <see cref="Created"/> tag.
    ///
    /// The swap runs at Modification1 — before Game.Vehicles.InitializeSystem (Modification4),
    /// the search tree (Modification5), effects/colors (PreCulling) and render batches consume
    /// the PrefabRef — so every piece of derived instance state is built from the NEW prefab.
    /// No stale effects, no Updated/EffectsUpdated reconciliation, no cleanup. This replaces the
    /// old UIUpdate-phase + EndFrameBarrier approach in VehicleSelectionSection, which wrote the
    /// swap after vanilla had already initialized the vehicle from the old prefab (the source of
    /// the orphaned-EnabledEffect crashes). Approach modeled after VehicleControlFramework —
    /// see docs/VCF_ANALYSIS.md.
    /// </summary>
    public abstract partial class VehicleSpawnEnforcerBase : GameSystemBase
    {
        private static ILog log;

        private EntityQuery _createdVehicleQuery;
        private PrefabSystem _prefabSystem;

        /// <summary>Short name used in the log so the early and parked pass are distinguishable.</summary>
        protected abstract string Label { get; }

        protected override void OnCreate()
        {
            base.OnCreate();
            log = Mod.log;
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // Newly created service vehicles that belong to a building. Cars only (no helicopters),
            // matching what the selection UI offers.
            _createdVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Car>(),
                    ComponentType.ReadOnly<Owner>(),
                    ComponentType.ReadWrite<PrefabRef>(),
                },
                Any = ServiceCatalog.VehicleComponentTypes,
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });
            RequireForUpdate(_createdVehicleQuery);
        }

        protected override void OnUpdate()
        {
            if (!(Setting.Instance?.EnableChangeVehicles ?? false))
                return;

            EntityManager.CompleteDependencyBeforeRW<PrefabRef>();
            var vehicles = _createdVehicleQuery.ToEntityArray(Allocator.Temp);
            foreach (var vehicle in vehicles)
                Enforce(vehicle);
            vehicles.Dispose();
        }

        private void Enforce(Entity vehicle)
        {
            var owner = EntityManager.GetComponentData<Owner>(vehicle).m_Owner;
            if (owner == Entity.Null ||
                !EntityManager.TryGetBuffer(owner, isReadOnly: true, out DynamicBuffer<AllowedVehiclePrefab> allowed) ||
                allowed.Length == 0)
                return;

            var prefabRef = EntityManager.GetComponentData<PrefabRef>(vehicle);
            var currentName = _prefabSystem.GetPrefabName(prefabRef.m_Prefab);

            // One consolidated report per vehicle: what spawned, every allowed entry with its
            // verdict, and the outcome. Written as a single multi-line Debug entry so the whole
            // decision is readable at a glance instead of scattered across interleaved lines.
            var report = new System.Text.StringBuilder();
            report.Append($"[{Label}] spawn {vehicle.Index}: vanilla picked '{currentName}'");
            AppendPurpose(report, vehicle, prefabRef.m_Prefab);
            report.Append($", building has {allowed.Length} allowed vehicle(s)");

            var candidates = new List<Entity>(allowed.Length);
            foreach (var entry in allowed)
            {
                var name = entry.PrefabName.ToString();
                if (string.IsNullOrEmpty(name))
                    continue;

                if (name == currentName)
                {
                    // Whitelist semantics: vanilla already picked an allowed prefab, keep its choice.
                    report.Append($"\n    '{name}': ALREADY THIS ONE -> keeping vanilla pick");
                    log.Debug(report.ToString());
                    return;
                }

                if (!TryResolvePrefabEntity(name, out var candidate))
                {
                    report.Append($"\n    '{name}': UNRESOLVED (no prefab with this name is loaded)");
                    continue;
                }

                var rejection = RejectionReason(vehicle, prefabRef.m_Prefab, candidate);
                if (rejection != null)
                {
                    report.Append($"\n    '{name}': REJECTED - {rejection}");
                    continue;
                }

                report.Append($"\n    '{name}': usable");
                AppendPurpose(report, Entity.Null, candidate);
                candidates.Add(candidate);
            }

            if (candidates.Count == 0)
            {
                report.Append("\n  => no usable replacement, keeping vanilla pick");
                log.Debug(report.ToString());
                return;
            }

            // Deterministic pick per vehicle entity, so the early and parked pass agree.
            var seed = math.hash(new uint2((uint)(vehicle.Index + 1), (uint)(vehicle.Version + 1)));
            var replacement = candidates[(int)(seed % (uint)candidates.Count)];
            if (replacement == prefabRef.m_Prefab)
            {
                report.Append("\n  => replacement equals current prefab, nothing to do");
                log.Debug(report.ToString());
                return;
            }

            PrepareRendering(vehicle, replacement);
            EntityManager.SetComponentData(vehicle, new PrefabRef(replacement));
            report.Append($"\n  => SWAPPED to '{_prefabSystem.GetPrefabName(replacement)}' (chose 1 of {candidates.Count})");
            log.Debug(report.ToString());
        }

        /// <summary>
        /// Appends the police purpose mask, which explains WHICH vehicle vanilla asked for
        /// (Patrol / Emergency / Intelligence). Candidates are filtered on it in
        /// <see cref="RejectionReason"/>; logging it here makes the vanilla spawn choice and the
        /// resulting verdicts explainable.
        /// Pass <see cref="Entity.Null"/> as the vehicle to report a prefab's offered purposes.
        /// </summary>
        private void AppendPurpose(System.Text.StringBuilder report, Entity vehicle, Entity prefab)
        {
            if (vehicle != Entity.Null && EntityManager.TryGetComponent<PoliceCar>(vehicle, out var policeCar))
            {
                report.Append($" [dispatched for: {policeCar.m_PurposeMask}]");
                return;
            }
            if (vehicle == Entity.Null && EntityManager.TryGetComponent<PoliceCarData>(prefab, out var policeData))
                report.Append($" [offers: {policeData.m_PurposeMask}]");
        }

        /// <summary>
        /// Components that only drive rendering. They may differ between the current and the
        /// candidate prefab: <see cref="PrepareRendering"/> adds the ones the candidate needs
        /// before the swap, so a purely visual difference must not disqualify a candidate.
        /// Same list as VehicleControlFramework's ServiceVehicleRendering.RenderingTypes.
        /// </summary>
        private static readonly HashSet<ComponentType> RenderingTypes = new()
        {
            ComponentType.ReadWrite<MeshColor>(),
            ComponentType.ReadWrite<CustomMeshColor>(),
            ComponentType.ReadWrite<Skeleton>(),
            ComponentType.ReadWrite<Bone>(),
            ComponentType.ReadWrite<BoneHistory>(),
            ComponentType.ReadWrite<Momentum>(),
            ComponentType.ReadWrite<PlaybackLayer>(),
            ComponentType.ReadWrite<Emissive>(),
            ComponentType.ReadWrite<LightState>(),
        };

        /// <summary>
        /// Returns null when the candidate may replace the spawned vehicle, otherwise a human
        /// readable reason why not.
        ///
        /// Simulation components must match EXACTLY, in both directions. The instance was created
        /// from the current prefab's archetype and that archetype can no longer change, so a
        /// component the candidate expects but the instance lacks would leave its system reading
        /// nothing — and a surplus component the candidate doesn't know keeps running against
        /// prefab data the new prefab doesn't carry. Rendering components are exempt because we
        /// add the missing ones ourselves (see <see cref="PrepareRendering"/>).
        ///
        /// On top of that the candidate has to be able to do the job vanilla dispatched this
        /// vehicle for — police purposes and maintenance types. Modeled after VCF 0.3.1's
        /// VehicleProfileSelector.GetRejection, see docs/VCF_ANALYSIS.md.
        /// </summary>
        private string RejectionReason(Entity vehicle, Entity currentPrefab, Entity candidate)
        {
            if (candidate == Entity.Null || !EntityManager.Exists(candidate))
                return "candidate prefab does not exist";

            if (!EntityManager.TryGetComponent<ObjectData>(currentPrefab, out var currentObject) ||
                !EntityManager.TryGetComponent<ObjectData>(candidate, out var candidateObject))
                return "ObjectData missing on one side";

            var difference = SimulationDifference(currentObject.m_Archetype, candidateObject.m_Archetype);
            if (difference != null)
                return $"archetype differs [{difference}]";

            // A vehicle spawned parked was created from the stopped archetype, so that one has to
            // match as well. For a moving spawn it is irrelevant which stopped form the candidate
            // would use, because the instance never had it.
            if (EntityManager.HasComponent<ParkedCar>(vehicle) &&
                EntityManager.TryGetComponent<MovingObjectData>(currentPrefab, out var currentMoving) &&
                EntityManager.TryGetComponent<MovingObjectData>(candidate, out var candidateMoving))
            {
                difference = SimulationDifference(currentMoving.m_StoppedArchetype, candidateMoving.m_StoppedArchetype);
                if (difference != null)
                    return $"parked archetype differs [{difference}]";
            }

            // Vanilla dispatches a police car for a specific purpose (Patrol / Emergency /
            // Intelligence). A replacement that doesn't offer all of them would be sent on a job
            // it can't do, so the candidate's mask has to cover the requested one.
            if (EntityManager.TryGetComponent<PoliceCar>(vehicle, out var policeCar))
            {
                if (!EntityManager.TryGetComponent<PoliceCarData>(candidate, out var policeData))
                    return "no PoliceCarData, can't serve a police purpose";

                // A purpose of 0 means nothing is dispatched yet (a car parked at the station),
                // so there is nothing to preserve. VCF rejects that case outright, which would
                // stop every swap on idle police cars.
                if (policeCar.m_PurposeMask != 0 &&
                    (policeData.m_PurposeMask & policeCar.m_PurposeMask) != policeCar.m_PurposeMask)
                    return $"offers {policeData.m_PurposeMask}, dispatched for {policeCar.m_PurposeMask}";
            }

            // Same idea for maintenance: the depot picked a vehicle by the work it can do, so the
            // candidate has to cover at least the current prefab's maintenance types.
            if (EntityManager.TryGetComponent<MaintenanceVehicleData>(currentPrefab, out var currentMaintenance))
            {
                if (!EntityManager.TryGetComponent<MaintenanceVehicleData>(candidate, out var candidateMaintenance))
                    return "no MaintenanceVehicleData, can't do maintenance work";

                if (currentMaintenance.m_MaintenanceType != 0 &&
                    (candidateMaintenance.m_MaintenanceType & currentMaintenance.m_MaintenanceType) !=
                    currentMaintenance.m_MaintenanceType)
                    return $"does {candidateMaintenance.m_MaintenanceType}, needs {currentMaintenance.m_MaintenanceType}";

                if (candidateMaintenance.m_MaintenanceCapacity <= 0 || candidateMaintenance.m_MaintenanceRate <= 0)
                    return "maintenance capacity or rate is zero";
            }

            return null;
        }

        /// <summary>
        /// Null when both archetypes hold the same simulation components (rendering components are
        /// ignored), otherwise a +/- listing of what differs.
        /// </summary>
        private static string SimulationDifference(EntityArchetype current, EntityArchetype candidate)
        {
            if (!current.Valid || !candidate.Valid)
                return "invalid archetype";

            if (current == candidate)
                return null;

            var currentTypes = current.GetComponentTypes(Allocator.Temp);
            var candidateTypes = candidate.GetComponentTypes(Allocator.Temp);

            var currentSet = new HashSet<ComponentType>();
            foreach (var t in currentTypes)
                if (!RenderingTypes.Contains(t))
                    currentSet.Add(t);

            var candidateSet = new HashSet<ComponentType>();
            foreach (var t in candidateTypes)
                if (!RenderingTypes.Contains(t))
                    candidateSet.Add(t);

            currentTypes.Dispose();
            candidateTypes.Dispose();

            if (currentSet.SetEquals(candidateSet))
                return null;

            var differences = new List<string>();
            foreach (var t in currentSet)
                if (!candidateSet.Contains(t))
                    differences.Add("-" + DescribeType(t));
            foreach (var t in candidateSet)
                if (!currentSet.Contains(t))
                    differences.Add("+" + DescribeType(t));

            return string.Join(", ", differences);
        }

        /// <summary>
        /// Gives the instance the rendering components the new prefab expects. Only additions are
        /// needed here: the vehicle still carries the <see cref="Created"/> tag, so every rendering
        /// buffer is still empty and nothing derived from the old prefab has been built yet. A
        /// surplus buffer the new prefab doesn't use simply stays empty.
        ///
        /// Once we also rebind already running vehicles (VCF's ParkedDeparture window), this has to
        /// grow into VCF's full ServiceVehicleRendering.Prepare: release the Skeleton/Emissive heap
        /// allocations via ProceduralSkeletonSystem/ProceduralEmissiveSystem, clear the buffers and
        /// tag the vehicle Updated + BatchesUpdated.
        /// </summary>
        private void PrepareRendering(Entity vehicle, Entity candidate)
        {
            var parked = EntityManager.HasComponent<ParkedCar>(vehicle);
            var archetype = parked && EntityManager.TryGetComponent<MovingObjectData>(candidate, out var moving)
                ? moving.m_StoppedArchetype
                : EntityManager.GetComponentData<ObjectData>(candidate).m_Archetype;

            if (!archetype.Valid)
                return;

            var types = archetype.GetComponentTypes(Allocator.Temp);
            foreach (var type in types)
                if (RenderingTypes.Contains(type) && !EntityManager.HasComponent(vehicle, type))
                    EntityManager.AddComponent(vehicle, type);
            types.Dispose();
        }

        // ToString() prints "null" for component types whose managed Type the release build's
        // TypeManager can't hand back (seen on asset packs carrying a foreign component);
        // the stable hash still identifies them in the log.
        private static string DescribeType(ComponentType t)
        {
            var info = TypeManager.GetTypeInfo(t.TypeIndex);
            if (info.Type != null)
                return t.ToString();
            return $"unknown type (stableHash=0x{info.StableTypeHash:X16}){(t.IsBuffer ? " [Buffer]" : "")}";
        }

        /// <summary>Resolves a prefab name to its entity: built-in CarPrefab id first, then the modded-asset cache.</summary>
        private bool TryResolvePrefabEntity(string prefabName, out Entity entity)
        {
            if (_prefabSystem.TryGetPrefab(new PrefabID("CarPrefab", prefabName), out PrefabBase prefab) &&
                _prefabSystem.TryGetEntity(prefab, out entity))
                return true;

            var cachedId = PrefabCacheSystem.GetPrefabIDByName(prefabName);
            if (cachedId != null &&
                _prefabSystem.TryGetPrefab(cachedId.Value, out prefab) &&
                _prefabSystem.TryGetEntity(prefab, out entity))
                return true;

            entity = Entity.Null;
            return false;
        }
    }

    /// <summary>Catches vehicles spawned by simulation (played back at the previous frame's end barrier).</summary>
    public sealed partial class VehicleSpawnEnforcerEarlySystem : VehicleSpawnEnforcerBase
    {
        protected override string Label => "early";
    }

    /// <summary>
    /// Catches vehicles created mid-frame (e.g. parked service vehicles spawned with a building),
    /// which don't exist yet when the early system runs. Still before object init/search/effects.
    /// </summary>
    public sealed partial class VehicleSpawnEnforcerParkedSystem : VehicleSpawnEnforcerBase
    {
        protected override string Label => "parked";
    }
}
