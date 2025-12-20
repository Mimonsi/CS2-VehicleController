using System;
using System.Collections.Generic;
using System.Diagnostics;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Game.Prefabs.Water;
using Unity.Collections;
using Unity.Entities;

namespace VehicleController.Systems
{
    public struct CachedPrefab
    {
        public string PrefabName;
        public string PrefabTypeName;
        public PrefabBase PrefabBase;
        public PrefabID PrefabID;
    }
    
    
    public partial class PrefabCacheSystem : GameSystemBase
    {
        private ILog log;
        private PrefabSystem _prefabSystem;
        private List<CachedPrefab> _cachedPrefabs = new();
        public static PrefabCacheSystem Instance;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log = Mod.log;
            
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override void OnUpdate()
        {
            
        }
        
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game)
                CacheAssets();
        }
        
        public static PrefabID? GetPrefabIDByName(string prefabName)
        {
            foreach (var cachedPrefab in Instance._cachedPrefabs)
            {
                if (cachedPrefab.PrefabName.Equals(prefabName))
                {
                    return cachedPrefab.PrefabID;
                }
            }

            return null;
        }

        private void CacheAssets()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string prefabName = string.Empty;
            try
            {
                _cachedPrefabs.Clear();
                var prefabRefQuery = SystemAPI.QueryBuilder().WithAll<PrefabRef>().Build();

                var prefabRefEntities = prefabRefQuery.ToEntityArray(Allocator.Temp);
                if (prefabRefEntities.Length <= 0)
                    return;

                var prefabAssets = AssetDatabase.global.GetAssets<PrefabAsset>();
                foreach (var item in prefabAssets)
                {
                    PrefabBase? prefabBase = item.GetInstance<PrefabBase>();
                    if (
                        prefabBase == null
                        || prefabBase is AssetPackPrefab
                        || prefabBase is ContentPrefab
                        || prefabBase is EffectPrefab
                        || prefabBase is ProcessingRequirementPrefab
                        || prefabBase is RenderPrefab
                        || prefabBase is StrictObjectBuiltRequirementPrefab
                        || prefabBase is UIAssetCategoryPrefab
                        || prefabBase is UIAssetMenuPrefab
                        || prefabBase is WaterRenderSettingsPrefab
                    )
                        continue;

                    prefabName = prefabBase.name;
                    string typeName = $"{prefabBase.GetType().Name}:{prefabBase.GetPrefabID().GetName()}";
                    _cachedPrefabs.Add(new CachedPrefab
                    {
                        PrefabName = prefabName,
                        PrefabTypeName = typeName,
                        PrefabBase = prefabBase,
                        PrefabID = prefabBase.GetPrefabID()
                    });
                }
            }
            catch (Exception x)
            {
                log.Error($"Prefab Caching process failed at prefab '{prefabName}': {x.Message}");
            }
            finally
            {
                stopwatch.Stop();

                log.Info($"Prefab Caching process completed in {stopwatch.Elapsed.Duration()}s");
            }
        }
    }
}