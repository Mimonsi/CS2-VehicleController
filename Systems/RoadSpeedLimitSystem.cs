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
        private EntityQuery _carLaneEntityQuery;
        private EntityQuery _uneditedCarLaneEntityQuery;

        private PrefabSystem prefabSystem;
        public static RoadSpeedLimitSystem? Instance { get; private set; }
        
        /// <summary>
        /// Creates entity queries and prepares the system.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            //log = Mod.log;
            log = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(RoadSpeedLimitSystem)}")
                .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Error);
            log.SetEffectiveness(Level.Trace);
            Enabled = true;
            
            _uneditedCarLaneEntityQuery = SystemAPI
                .QueryBuilder()
                .WithAll<CarLane>()
                .WithNone<LaneSpeedLimitModified>()
                .Build();
            
            _carLaneEntityQuery = SystemAPI
                .QueryBuilder()
                .WithAll<CarLane>()
                .Build();
            
            //RequireForUpdate(_uneditedRoadPrefabQuery);
            RequireForUpdate(_uneditedCarLaneEntityQuery);
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            log.Info("RoadSpeedLimitSystem created.");
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            //UpdateSpeedForLane(_uneditedCarLaneEntityQuery.ToEntityArray(Allocator.Temp), Setting.GetSpeedLimitModifier());
        }

        public static void TriggerSpeedLimitUpdate()
        {
            if (Instance == null)
            {
                Mod.log.Warn("RoadSpeedLimitSystem instance is null, cannot trigger update.");
                return;
            }
            log.Debug("Triggering road speed limit update.");
            Instance.UpdateSpeedForLanes(Instance._carLaneEntityQuery.ToEntityArray(Allocator.Temp), Setting.GetSpeedLimitModifier());
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
            }
            if (entitiesUpdated > 0)
                log.Info($"Updated speed limits for {entitiesUpdated} road entities.");
            
            foreach (var pair in entityAmountBySpeedLimit)
            {
                log.Debug($"Limit for {pair.Value} CarLane entities set to {FormatSpeedLimit(pair.Key)}.");
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
            var ignoreFlags = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection;
            
            if (EntityManager.TryGetComponent(laneEntity, out CarLane carLane) && (carLane.m_Flags & ignoreFlags) != ignoreFlags)
            {
                if (EntityManager.TryGetComponent(laneEntity, out LaneSpeedLimitModified speedLimitModified))
                {
                    //log.Trace($"LaneSpeedLimit already exists, using {speedLimitModified.VanillaSpeedLimit} as start value");
                }
                else
                {
                    speedLimitModified = new LaneSpeedLimitModified
                    {
                        VanillaSpeedLimit = carLane.m_DefaultSpeedLimit
                    };
                    EntityManager.AddComponentData(laneEntity, speedLimitModified); // Save old speed
                    //log.Trace($"Added LaneSpeedLimitModified component to lane., using {FormatSpeedLimit(speedLimitModified.VanillaSpeedLimit)} as start value");
                }
                //carLane.m_DefaultSpeedLimit = speed;
                carLane.m_SpeedLimit = speedLimitModified.VanillaSpeedLimit * modifier;
                carLane.m_DefaultSpeedLimit = speedLimitModified.VanillaSpeedLimit * modifier;
                //log.Trace($"After modification by {modifier}, speed limit is " + FormatSpeedLimit(carLane.m_SpeedLimit));
                EntityManager.SetComponentData(laneEntity, carLane);

                return speedLimitModified.VanillaSpeedLimit;

            }

            return -1;

            /*if (EntityManager.TryGetComponent(carLane.m_SubLane, out TrackLane trackLane))
            {
                var speedLimitModified = new RoadSpeedLimitModified()
                {
                    VanillaSpeedLimit = speed
                };
                trackLane.m_SpeedLimit = speed * speedMultiplier;
                EntityManager.SetComponentData(carLane.m_SubLane, trackLane);
                EntityManager.AddComponentData(carLane.m_SubLane, speedLimitModified); // Save old speed
            }*/
        }
        
        public void RemoveSpeedLimitModified()
        {
            var query = SystemAPI.QueryBuilder().WithAny<RoadSpeedLimitModified, LaneSpeedLimitModified>().Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                if (EntityManager.HasComponent<RoadSpeedLimitModified>(entity))
                    EntityManager.RemoveComponent<RoadSpeedLimitModified>(entity);
                if (EntityManager.HasComponent<LaneSpeedLimitModified>(entity))
                    EntityManager.RemoveComponent<LaneSpeedLimitModified>(entity);
                count++;
            }
            log.Info($"Removed SpeedLimitModified component from {count} entities.");
        }

        public void ResetAllSpeedLimits()
        {
            //log.Debug($"Updated road prefab {prefabSystem.GetPrefabName(entity)} speed limit to {FormatSpeedLimit(carLane.m_SpeedLimit)}).");
            Dictionary<float, int> entityAmountBySpeedLimit = new Dictionary<float, int>();
            foreach (var entity in _carLaneEntityQuery.ToEntityArray(Allocator.Temp))
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
            }
            
            foreach (var pair in entityAmountBySpeedLimit)
            {
                log.Debug($"Limit for {pair.Value} CarLane entities reset to {FormatSpeedLimit(pair.Key)}.");
            }
        }
    }
}