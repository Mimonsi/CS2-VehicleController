using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using VehicleController.Data;
using CarLane = Game.Net.CarLane;
using SubLane = Game.Net.SubLane;
using TrackLane = Game.Net.TrackLane;

namespace VehicleController.Systems
{
    /// <summary>
    /// System for editing road speed limits
    /// </summary>
    public partial class RoadSpeedLimitSystem : GameSystemBase
    {
        private static ILog log;
        private EntityQuery _roadPrefabQuery;
        private EntityQuery _uneditedRoadPrefabQuery;
        private EntityQuery _roadEntityQuery;

        private PrefabSystem prefabSystem;
        public static RoadSpeedLimitSystem Instance { get; private set; }
        
        /// <summary>
        /// Creates entity queries and prepares the system.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;
            Enabled = true;
            
            _uneditedRoadPrefabQuery = SystemAPI
                .QueryBuilder()
                .WithAll<RoadData>()
                .WithNone<SpeedLimitModified>()
                .Build();
            
            _roadPrefabQuery = SystemAPI
                .QueryBuilder()
                .WithAll<RoadData>()
                .Build();
            
            _roadEntityQuery = SystemAPI
                .QueryBuilder()
                .WithAll<Road>()
                .Build();
            
            RequireForUpdate(_uneditedRoadPrefabQuery);
            RequireForUpdate(_roadEntityQuery);
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            log.Info("RoadSpeedLimitSystem created.");
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            UpdateSpeedForRoadPrefabs(_uneditedRoadPrefabQuery.ToEntityArray(Allocator.Temp));
        }

        public static void TriggerSpeedLimitUpdate()
        {
            Mod.log.Debug("Triggering road speed limit update.");
            if (Instance == null)
            {
                Mod.log.Warn("RoadSpeedLimitSystem instance is null, cannot trigger update.");
                return;
            }
            Instance.UpdateSpeedForRoadPrefabs(Instance._roadPrefabQuery.ToEntityArray(Allocator.Temp));
        }
        
        /// <summary>
        /// Updates Speed limits according to settings:
        /// If realistic speed limits enabled, halves the speed limit.
        /// If disabled, resets to vanilla speed limit if previously modified (unmodified roads still have original speed limit).
        /// </summary>
        /// <param name="roadPrefabs"></param>
        private void UpdateSpeedForRoadPrefabs(NativeArray<Entity> roadPrefabs)
        {
            int prefabsUpdated = 0;
            foreach (var entity in roadPrefabs)
            {
                //log.Debug("Running Update for " + entities.Length + " road prefabs.");
                if (EntityManager.TryGetComponent(entity, out RoadData roadData))
                {
                    // Reset speed limit to vanilla if previously modified
                    if (!EntityManager.TryGetComponent(entity, out SpeedLimitModified speedLimitModified))
                    {
                        speedLimitModified = new SpeedLimitModified()
                        {
                            VanillaSpeedLimit = roadData.m_SpeedLimit
                        };
                    }
                    //log.Debug($"Speed Limit Before: FormatSpeedLimit(roadData.m_SpeedLimit) (Component: {FormatSpeedLimit(speedLimitModified.VanillaSpeedLimit)}");
                    // Default to vanilla speed limit, apply modification if applicable
                    roadData.m_SpeedLimit = speedLimitModified.VanillaSpeedLimit;

                    if (Setting.Instance != null && Setting.Instance.SpeedLimitOverride == SpeedLimitOverride.Half)
                        roadData.m_SpeedLimit /= 2;
                    else if (Setting.Instance != null &&
                             Setting.Instance.SpeedLimitOverride == SpeedLimitOverride.Double)
                        roadData.m_SpeedLimit *= 2;
                    else if (Setting.Instance != null &&
                             Setting.Instance.SpeedLimitOverride == SpeedLimitOverride.Speed)
                        roadData.m_SpeedLimit *= 10;

                    EntityManager.SetComponentData(entity, roadData);
                    EntityManager.AddComponentData(entity, speedLimitModified);
                    EntityManager.AddComponent<Updated>(entity);
                    log.Debug(
                        $"Updated road prefab {prefabSystem.GetPrefabName(entity)} speed limit to {FormatSpeedLimit(roadData.m_SpeedLimit)}).");
                    prefabsUpdated++;
                }
            }
            if (prefabsUpdated > 0)
                log.Info($"Updated speed limits for {prefabsUpdated} road prefabs.");
            
            SetSpeedForLanes(_roadEntityQuery.ToEntityArray(Allocator.Temp));
        }

        private static string FormatSpeedLimit(float value)
        {
            // return first value with 2 decimal places, second value in km/h with no decimal places
            return $"{value:0.##} m/s ({value * 3.6:0} km/h)";
        }

        private void SetSpeedForLanes(NativeArray<Entity> roadEntities)
        {
            int entitiesUpdates = 0;
            foreach (var entity in roadEntities)
            {
                if (EntityManager.TryGetComponent(entity, out Road road))
                {
                    if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                    {
                        if (EntityManager.TryGetComponent(prefabRef, out RoadData roadData))
                        {
                            if (SetSpeed(entity, roadData.m_SpeedLimit))
                            {
                                log.Trace($"Updated road entity speed limit to {FormatSpeedLimit(roadData.m_SpeedLimit)} km/h).");
                                entitiesUpdates++;
                            }
                        }

                    }

                }
            }
            if (entitiesUpdates > 0)
                log.Info($"Updated speed limits for {entitiesUpdates} road entities.");
        }
        
        private bool SetSpeed(Entity entity, float speed)
        {
            // TODO: Make job like in https://github.com/Aberro/SpeedLimitMod/blob/master/Systems/SetCustomSpeedLimitsSystem.cs
            DynamicBuffer<SubLane> subLanes;
            if (EntityManager.TryGetBuffer(entity, false, out subLanes))
            {
                //log.Debug("Setting lane limits for sublanes");
                for(int i = 0; i < subLanes.Length; i++)
                {
                    var subLane = subLanes[i];
                    SetSpeedSubLane(ref subLane, speed);
                    subLanes[i] = subLane;
                }

                return true;
            }
            else
            {
                log.Warn("No sublane buffer");
                return false;
            }
        }

        private void SetSpeedSubLane(ref SubLane subLane, float speed)
        {
            var ignoreFlags = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection;
            if (EntityManager.TryGetComponent(subLane.m_SubLane, out CarLane carLane) && ((carLane.m_Flags & ignoreFlags) != ignoreFlags))
            {
                carLane.m_DefaultSpeedLimit = speed;
                carLane.m_SpeedLimit = speed;
                EntityManager.SetComponentData(subLane.m_SubLane, carLane);
                log.Trace("Limit for CarLane set to " + speed);
            }
            if (EntityManager.TryGetComponent(subLane.m_SubLane, out TrackLane trackLane))
            {
                trackLane.m_SpeedLimit = speed;
                EntityManager.SetComponentData(subLane.m_SubLane, trackLane);
            }
        }
    }
}