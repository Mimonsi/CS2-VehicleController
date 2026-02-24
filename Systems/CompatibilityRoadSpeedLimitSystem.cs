using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using CarLane = Game.Net.CarLane;
using TrackLane = Game.Net.TrackLane;
using VehicleController.Components;

namespace VehicleController.Systems
{
    /// <summary>
    /// System for editing road speed limits
    /// </summary>
    public partial class CompatibilityRoadSpeedLimitSystem : GameSystemBase
    {
        public static ILog log;
        private EntityQuery _laneQuery;
        
        public static CompatibilityRoadSpeedLimitSystem? Instance { get; private set; }
        
        public static bool StartReset = true;
        
        /// <summary>
        /// Creates entity queries and prepares the system.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            //log = Mod.log;
            log = LogManager.GetLogger($"{nameof(VehicleController)}.RoadSpeedLimitSystem")
                .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Error);
            log.effectivenessLevel = Level.Verbose;
            Enabled = true;
            
            _laneQuery = SystemAPI
                .QueryBuilder()
                .WithAny<CarLane, TrackLane>()
                .WithNone<Temp>() // Temp is untested, because the to be placed buildings are also recognized
                .Build();
            
            RequireForUpdate(_laneQuery);
            
            log.Info("CompatibilityRoadSpeedLimitSystem created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode == GameMode.Game)
            {
                if (StartReset)
                {
                    log.Warn("Removing Road Speed Limit Data");
                    ResetAllSpeedLimits();
                }
            }
        }

        public void CountAllSpeedLimits()
        {
            log.Info("Printing all road/track lane speed limits:");
            Dictionary<float, int> speedLimitCounts = new Dictionary<float, int>();
            foreach (var entity in Instance?._laneQuery.ToEntityArray(Allocator.Temp))
            {
                if (Instance.EntityManager.TryGetComponent(entity, out CarLane carLane))
                {
                    log.Verbose(
                        $"CarLane Entity {entity.ToString()}: Speed Limit = {FormatSpeedLimit(carLane.m_SpeedLimit)}");
                    int limit = (int)carLane.m_SpeedLimit;
                    if (!speedLimitCounts.ContainsKey(limit))
                        speedLimitCounts[limit] = 0;
                    speedLimitCounts[limit]++;
                }

                if (Instance.EntityManager.TryGetComponent(entity, out TrackLane trackLane))
                {
                    var limit = (int)trackLane.m_SpeedLimit;
                    if (!speedLimitCounts.ContainsKey(limit))
                        speedLimitCounts[limit] = 0;
                    speedLimitCounts[limit]++;
                    log.Verbose(
                        $"TrackLane Entity {entity.ToString()}: Speed Limit = {FormatSpeedLimit(limit)}");
                }
            }
            log.Info("Speed limit counts:");
            // Sort by count descending
            speedLimitCounts = new Dictionary<float, int>(speedLimitCounts.OrderByDescending(x => x.Value));
            foreach (var key in speedLimitCounts.Keys)
            {
                log.Info($"Speed Limit {FormatSpeedLimit(key)}: {speedLimitCounts[key]} lanes");
            }
        }

        private static string FormatSpeedLimit(float value)
        {
            // return first value with 2 decimal places, second value in km/h with no decimal places
            return $"{value:0.##} m/s ({value * 3.6:0} km/h)";
        }

        public void ResetAllSpeedLimits()
        {
            //log.Debug($"Updated road prefab {prefabSystem.GetPrefabName(entity)} speed limit to {FormatSpeedLimit(carLane.m_SpeedLimit)}).");
            Dictionary<float, int> entityAmountBySpeedLimit = new Dictionary<float, int>();
            log.Info("Debug Option: Reset all road speed limits to vanilla values.");
            
            // Get all entities that have either OriginalLaneSpeedLimit or LaneSpeedLimitChecked
            var entities = _laneQuery.ToEntityArray(Allocator.Temp);
            log.Debug($"Found {entities.Length} lane entities to reset.");
            foreach (var entity in entities)
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
                /*if (EntityManager.HasComponent<LaneSpeedLimitChecked>(entity))
                    EntityManager.RemoveComponent<LaneSpeedLimitChecked>(entity);
                if (EntityManager.HasComponent<OriginalLaneSpeedLimit>(entity))
                    EntityManager.RemoveComponent<OriginalLaneSpeedLimit>(entity);*/
            }
            
            foreach (var pair in entityAmountBySpeedLimit)
            {
                log.Debug($"Limit for {pair.Value} CarLane entities reset to {FormatSpeedLimit(pair.Key)}.");
            }
        }

        protected override void OnUpdate()
        {
            
        }
    }
}