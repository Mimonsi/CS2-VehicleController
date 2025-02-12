using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using PersonalCar = Game.Vehicles.PersonalCar;

namespace LessMotorcycles
{
    public partial class LessMotorcyclesSystem : GameSystemBase
    {
        private static ILog Logger;

        private EntityQuery prefabQuery;
        private EntityQuery instanceQuery;

        private PrefabSystem prefabSystem;
        public static LessMotorcyclesSystem Instance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            Enabled = true;
            Logger = Mod.log;

            EntityQueryDesc prefabQueryDesc = new EntityQueryDesc
            {
                Any =
                [
                    ComponentType.ReadOnly<PersonalCarData>(),
                ]
            };
            prefabQuery = GetEntityQuery(prefabQueryDesc);

            EntityQueryDesc instanceQueryDesc = new EntityQueryDesc
            {
                Any =
                [
                    ComponentType.ReadOnly<Game.Vehicles.PersonalCar>(),
                ],
            };
            instanceQuery = GetEntityQuery(instanceQueryDesc);


            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            GameManager.instance.RegisterUpdater(UpdateProbabilities);
        }

        private void UpdateProbabilities()
        {
            Logger.Info("Updating Probabilities");
            var entities = prefabQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    var prefabName = prefabSystem.GetPrefabName(entity);
                    if (prefabName.Contains("Motorbike"))
                    {
                        personalCarData.m_Probability = Setting.Instance.MotorbikeProbability;
                    }
                    if (prefabName.Contains("Scooter"))
                    {
                        personalCarData.m_Probability = Setting.Instance.ScooterProbability;
                    }
                    EntityManager.SetComponentData(entity, personalCarData);
                    EntityManager.AddComponent<BatchesUpdated>(entity);
                }
            }
        }

        protected override void OnUpdate()
        {

        }

        public void ApplySettings()
        {
            UpdateProbabilities();
        }

        public void DeleteInstances()
        {
            Logger.Info("Deleting instances");
            double motorcycleDeletionChance = 1 - (Setting.Instance.MotorbikeProbability / 100.0);
            double scooterDeletionChance = 1 - (Setting.Instance.ScooterProbability / 100.0);
            Logger.Info($"Motorcycle deletion chance: {motorcycleDeletionChance}, Scooter deletion chance: {scooterDeletionChance}");
            List<Entity> motorbikes = new List<Entity>();
            List<Entity> scooters = new List<Entity>();

            var entities = instanceQuery.ToEntityArray(Allocator.Temp);

            /*foreach (var entity in entities)
            {
                string prefabName = prefabSystem.GetPrefabName(entity);
                if (prefabName.Contains("Motorbike"))
                {
                    motorbikes.Add(entity);
                }

                if (prefabName.Contains("Scooter"))
                {
                    motorbikes.Add(entity);
                }
            }*/



            foreach (var entity in entities)
            {
                if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                {
                    if (prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefab))
                    {
                        if (prefab is VehiclePrefab vehiclePrefab)
                        {
                            if (vehiclePrefab.name.Contains("Motorbike"))
                            {
                                motorbikes.Add(entity);
                            }
                            if (vehiclePrefab.name.Contains("Scooter"))
                            {
                                scooters.Add(entity);
                            }
                        }
                    }
                }
            }

            Logger.Info($"Found {motorbikes.Count} motorcycles and {scooters.Count} scooters");
            int motorcyclesDeleted = 0, scootersDeleted = 0;

            // Delete percentage of motorcycles and scooters
            foreach (var entity in motorbikes)
            {
                if (Random.value < motorcycleDeletionChance)
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    motorcyclesDeleted++;
                }
            }

            foreach (var entity in scooters)
            {
                if (Random.value < scooterDeletionChance)
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    scootersDeleted++;
                }
            }

            Logger.Info($"Deleted {motorcyclesDeleted} motorcycles and {scootersDeleted} scooters");
        }
    }

}