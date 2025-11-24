using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using VehicleController.Data;

namespace VehicleController.Systems
{
    /// <summary>
    /// Utility system that counts vehicle prefabs currently present in the world.
    /// </summary>
    public partial class VehicleCounterSystem : GameSystemBase
    {
        //public static ILog log = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(VehicleCounterSystem)}")
        //    .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Critical);

        private static ILog log;
        private EntityQuery carQuery;
        private EntityQuery instanceQuery;

        private PrefabSystem prefabSystem;
        private ProbabilityPack _currentProbabilityPack;
        public static VehicleCounterSystem Instance { get; private set; }

        /// <summary>
        /// Creates entity queries and prepares the system.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;
            Enabled = true;
            
            carQuery = SystemAPI.QueryBuilder().WithAll<PersonalCarData>().Build();
            instanceQuery = SystemAPI.QueryBuilder().WithAny<Game.Vehicles.PersonalCar>().Build();
            
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            log.Info("VehicleCounterSystem created.");
        }
        
        /// <summary>
        /// Counts current instances of each prefab and logs the results.
        /// </summary>
        public void CountPrefabInstances()
        {
            Dictionary<string, int> prefabCounts = new Dictionary<string, int>();
            int totalInstances = 0;
            var entities = instanceQuery.ToEntityArray(Allocator.Temp);
            log.Info("Entities found: " + entities.Length);
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                {
                    if (prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefab))
                    {
                        if (prefab is VehiclePrefab vehiclePrefab)
                        {
                            if (!string.IsNullOrEmpty(vehiclePrefab.name))
                            {
                                totalInstances++;
                                if (prefabCounts.ContainsKey(vehiclePrefab.name))
                                {
                                    prefabCounts[vehiclePrefab.name]++;
                                }
                                else
                                {
                                    prefabCounts[vehiclePrefab.name] = 1;
                                }
                            }
                        }
                    }
                }
            }

            string output = "Prefab Instance Counts:\n";
            foreach (var kvp in prefabCounts)
            {
                var percentage = (float)kvp.Value / totalInstances * 100;
                output += $"{kvp.Key}: {kvp.Value} instances, {percentage}%\n";
            }
            log.Info(output);
            SortByClasses(prefabCounts, totalInstances);
        }

        /// <summary>
        /// Aggregates the raw prefab counts by vehicle class.
        /// </summary>
        private void SortByClasses(Dictionary<string, int> counts, int totalInstances)
        {
            Dictionary<string, int> classCounts = new Dictionary<string, int>();
            foreach (var kvp in counts)
            {
                string className = VehicleClass.GetVehicleClass(kvp.Key).Name;
                if (classCounts.ContainsKey(className))
                {
                    classCounts[className] += kvp.Value;
                }
                else
                {
                    classCounts[className] = kvp.Value;
                }
            }

            string output = "Class Instance Counts:\n";
            foreach (var kvp in classCounts)
            {
                var percentage = (float)kvp.Value / totalInstances * 100;
                output += $"{kvp.Key}: {percentage} ({kvp.Value} instances)\n";
            }
            log.Info(output);
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
        }
    }
}