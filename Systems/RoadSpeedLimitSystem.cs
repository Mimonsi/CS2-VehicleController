using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using VehicleController.Data;
using CarLane = Game.Net.CarLane;
using SubLane = Game.Net.SubLane;
using TrackLane = Game.Net.TrackLane;
using VehicleController.Components;

namespace VehicleController.Systems
{
    /// <summary>
    /// System for editing road speed limits
    /// </summary>
    public partial class RoadSpeedLimitSystem : GameSystemBase
    {
        private static ILog log;
        private EntityQuery _uneditedLaneEntityQuery;
        private EntityQuery _checkedLanesQuery;

        private PrefabSystem prefabSystem;
        public static RoadSpeedLimitSystem? Instance { get; private set; }
        private GameMode currentGameMode = GameMode.None;
        
        /// <summary>
        /// Creates entity queries and prepares the system.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;
            //log = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(RoadSpeedLimitSystem)}")
            //    .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Error);
            Enabled = true;
            
            _uneditedLaneEntityQuery = SystemAPI
                .QueryBuilder()
                .WithAny<CarLane, TrackLane>()
                .WithNone<LaneSpeedLimitChecked, Temp>() // Temp is untested, because the to be placed buildings are also recognized
                .Build();

            _checkedLanesQuery = SystemAPI.QueryBuilder().WithAny<LaneSpeedLimitChecked>().Build();
            
            //RequireForUpdate(_uneditedRoadPrefabQuery);
            RequireForUpdate(_uneditedLaneEntityQuery);
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            log.Info("RoadSpeedLimitSystem created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            currentGameMode = mode;
        }


        private DateTime _lastUpdateTime = DateTime.MinValue;
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            // Only update every 5 seconds
            /*if ((DateTime.Now - _lastUpdateTime).TotalSeconds < 5)
            {
                return;
            }*/
            if (currentGameMode != GameMode.Game || Setting.Instance!.DisableSpeedLimitUpdate)
                return;
            log.Debug("Updating road speed limits for unedited lanes.");
            UpdateSpeedForLanes(_uneditedLaneEntityQuery.ToEntityArray(Allocator.Temp), Setting.GetSpeedLimitModifier());
            _lastUpdateTime = DateTime.Now;
            
        }

        public static void UnmarkAllLanes(bool removeOriginalLimit = false)
        {
            if (Instance == null)
            {
                Mod.log.Warn("RoadSpeedLimitSystem instance is null, cannot trigger update.");
                return;
            }
            log.Debug("Marking all checked lanes as unchecked");
            foreach(var entity in Instance._checkedLanesQuery.ToEntityArray(Allocator.Temp))
            {
                Instance.EntityManager.RemoveComponent<LaneSpeedLimitChecked>(entity);
                if (removeOriginalLimit && Instance.EntityManager.HasComponent<OriginalLaneSpeedLimit>(entity))
                    Instance.EntityManager.RemoveComponent<OriginalLaneSpeedLimit>(entity);
            }
        }

        /// <summary>
        /// Updates Speed limits according to settings:
        /// If realistic speed limits enabled, halves the speed limit.
        /// If disabled, resets to vanilla speed limit if previously modified (unmodified roads still have original speed limit).
        /// </summary>
        /// <param name="roadEntities"></param>
        /// <param name="modifier"></param>
        private void UpdateSpeedForLanes(NativeArray<Entity> roadEntities, float modifier)
        {
            log.Trace("Updating speed limits for road lanes.");
            int entitiesUpdated = 0;
            
            Dictionary<float, int> entityAmountBySpeedLimit = new Dictionary<float, int>();
            foreach (var entity in roadEntities)
            {
                if (EntityManager.TryGetComponent(entity, out CarLane carLane))
                {
                    var speed = SetLaneSpeed(entity, modifier);
                    entitiesUpdated++;
                    if (speed < 0)
                        continue;
                    entityAmountBySpeedLimit.TryAdd(speed, 0);
                    entityAmountBySpeedLimit[speed]++;
                }
                if (EntityManager.TryGetComponent(entity, out TrackLane trackLane))
                {
                    var speed = SetLaneSpeed(entity, modifier);
                    entitiesUpdated++;
                    if (speed < 0)
                        continue;
                    entityAmountBySpeedLimit.TryAdd(speed, 0);
                    entityAmountBySpeedLimit[speed]++;
                }
            }
            if (entitiesUpdated > 0)
                log.Info($"Updated speed limits for {entitiesUpdated} road/track entities.");
            
            foreach (var pair in entityAmountBySpeedLimit)
            {
                log.Debug($"Limit for {pair.Value} CarLane/TrackLane entities set to {FormatSpeedLimit(pair.Key)}.");
            }
            
            
        }

        private static string FormatSpeedLimit(float value)
        {
            // return first value with 2 decimal places, second value in km/h with no decimal places
            return $"{value:0.##} m/s ({value * 3.6:0} km/h)";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="laneEntity"></param>
        /// <param name="modifier"></param>
        /// <returns>Speed the lane was set to</returns>
        private float SetLaneSpeed(Entity laneEntity, float modifier)
        {
            var ignoreCarFlags = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection;

            try
            {
                // Car Lane is used for Roads, Waterways
                if (EntityManager.TryGetComponent(laneEntity, out CarLane carLane) &&
                    (carLane.m_Flags & ignoreCarFlags) != ignoreCarFlags)
                {
                    if (EntityManager.TryGetComponent(laneEntity, out OriginalLaneSpeedLimit speedLimitModified))
                    {
                        log.Trace(
                            $"LaneSpeedLimit already exists, using {speedLimitModified.VanillaSpeedLimit} as start value");
                    }
                    else
                    {
                        speedLimitModified = new OriginalLaneSpeedLimit
                        {
                            VanillaSpeedLimit = carLane.m_DefaultSpeedLimit
                        };
                        EntityManager.AddComponentData(laneEntity, speedLimitModified); // Save old speed
                        log.Trace(
                            $"Added LaneSpeedLimitModified component to lane., using {FormatSpeedLimit(speedLimitModified.VanillaSpeedLimit)} as start value");
                    }

                    //carLane.m_DefaultSpeedLimit = speed;
                    carLane.m_SpeedLimit = speedLimitModified.VanillaSpeedLimit * modifier;
                    carLane.m_DefaultSpeedLimit = speedLimitModified.VanillaSpeedLimit * modifier;
                    log.Trace($"Entity {laneEntity.ToString()}: After modification by {modifier}, speed limit is {FormatSpeedLimit(carLane.m_SpeedLimit)}"
                              );
                    EntityManager.SetComponentData(laneEntity, carLane);

                    return carLane.m_DefaultSpeedLimit;

                }

                if (EntityManager.TryGetComponent(laneEntity, out TrackLane trackLane))
                {
                    if (EntityManager.TryGetComponent(laneEntity, out OriginalLaneSpeedLimit speedLimitModified))
                    {
                        //log.Trace($"LaneSpeedLimit already exists, using {speedLimitModified.VanillaSpeedLimit} as start value");
                    }
                    else
                    {
                        speedLimitModified = new OriginalLaneSpeedLimit
                        {
                            VanillaSpeedLimit = trackLane.m_SpeedLimit
                        };
                        EntityManager.AddComponentData(laneEntity, speedLimitModified); // Save old speed
                    }

                    trackLane.m_SpeedLimit = speedLimitModified.VanillaSpeedLimit * modifier;
                    EntityManager.SetComponentData(laneEntity, trackLane);
                    
                    return trackLane.m_SpeedLimit;

                }
            }
            finally // Regardless of case, add LaneSpeedLimitChecked to avoid rechecking
            {
                EntityManager.AddComponent<LaneSpeedLimitChecked>(laneEntity);
            }

            return -1;
            
        }
        
        public void RemoveSpeedLimitComponents()
        {
            var query = SystemAPI.QueryBuilder().WithAny<OriginalLaneSpeedLimit, LaneSpeedLimitChecked>().Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.HasComponent<OriginalLaneSpeedLimit>(entity))
                    EntityManager.RemoveComponent<OriginalLaneSpeedLimit>(entity);
                if (EntityManager.HasComponent<LaneSpeedLimitChecked>(entity))
                    EntityManager.RemoveComponent<LaneSpeedLimitChecked>(entity);
                count++;
            }
            log.Info($"Removed Speed Limit Components from {count} entities.");
        }

        public void ResetAllSpeedLimits()
        {
            //log.Debug($"Updated road prefab {prefabSystem.GetPrefabName(entity)} speed limit to {FormatSpeedLimit(carLane.m_SpeedLimit)}).");
            Dictionary<float, int> entityAmountBySpeedLimit = new Dictionary<float, int>();
            log.Info("Debug Option: Reset all road speed limits to vanilla values.");
            UnmarkAllLanes(true);
            foreach (var entity in _uneditedLaneEntityQuery.ToEntityArray(Allocator.Temp))
            {
                if (EntityManager.TryGetComponent<CarLane>(entity, out var carLane))
                {
                    if (EntityManager.TryGetComponent<Owner>(entity, out var owner))
                    {
                        if (EntityManager.TryGetComponent<PrefabRef>(owner.m_Owner, out var prefabRef))
                        {
                            if (EntityManager.TryGetComponent<RoadData>(prefabRef.m_Prefab, out var roadData))
                            {
                                carLane.m_SpeedLimit = roadData.m_SpeedLimit;
                                carLane.m_DefaultSpeedLimit = roadData.m_SpeedLimit;
                                EntityManager.SetComponentData(entity, carLane);
                                
                                entityAmountBySpeedLimit.TryAdd(carLane.m_SpeedLimit, 0);
                                entityAmountBySpeedLimit[carLane.m_SpeedLimit]++;
                            }
                        }
                    }
                }
                if (EntityManager.TryGetComponent<TrackLane>(entity, out var trackLane))
                {
                    if (EntityManager.TryGetComponent<Owner>(entity, out var owner))
                    {
                        if (EntityManager.TryGetComponent<PrefabRef>(owner.m_Owner, out var prefabRef))
                        {
                            if (EntityManager.TryGetComponent<TrackData>(prefabRef.m_Prefab, out var trackData))
                            {
                                trackLane.m_SpeedLimit = trackData.m_SpeedLimit;
                                EntityManager.SetComponentData(entity, trackLane);
                                
                                entityAmountBySpeedLimit.TryAdd(trackLane.m_SpeedLimit, 0);
                                entityAmountBySpeedLimit[trackLane.m_SpeedLimit]++;
                            }
                        }
                    }
                }
            }
            
            foreach (var pair in entityAmountBySpeedLimit)
            {
                log.Debug($"Limit for {pair.Value} CarLane entities reset to {FormatSpeedLimit(pair.Key)}.");
            }
        }
    }
}