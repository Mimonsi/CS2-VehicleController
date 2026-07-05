using System;
using System.Collections.Generic;
using Colossal.Core;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using VehicleController.Data;

namespace VehicleController.Systems
{
    /// <summary>
    /// Central authority for the cascade model (M0): captures the vanilla baseline of every
    /// vehicle prefab once, then resolves and applies the active <see cref="VehiclePack"/>
    /// (prefab override → class override → vanilla, then global multipliers).
    ///
    /// Additive for now: this runs alongside the legacy probability/property systems and does
    /// not touch them. By default vehicle packs are unselected, so those legacy systems don't
    /// modify vehicles and the baseline capture stays clean. The full cutover (retiring the
    /// legacy systems + wiring the real UI/savegame) is a later step.
    /// </summary>
    public partial class VehicleConfigSystem : GameSystemBase
    {
        private static ILog log;

        private EntityQuery _vehicleQuery;
        private PrefabSystem _prefabSystem;

        private readonly Dictionary<string, VanillaBaseline> _vanilla = new Dictionary<string, VanillaBaseline>();
        private VehiclePack _active = new VehiclePack("Default");

        private bool _isIngame;
        private bool _vanillaReady;
        private bool _captureScheduled;

        public static VehicleConfigSystem Instance { get; private set; }
        public static bool IsIngame { get; private set; }
        public VehiclePack ActivePack => _active;
        public IReadOnlyDictionary<string, VanillaBaseline> Vanilla => _vanilla;
        public bool VanillaReady => _vanillaReady;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            Enabled = true;
            log = Mod.log;

            _vehicleQuery = SystemAPI.QueryBuilder().WithAny<CarData, TrainData>().Build();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            log.Info("VehicleConfigSystem created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            IsIngame = mode == GameMode.Game;
            if (mode == GameMode.Game)
            {
                _isIngame = true;
                // Re-capture on every savegame load so the baseline matches the loaded prefabs.
                _vanilla.Clear();
                _vanillaReady = false;
                if (!_captureScheduled)
                {
                    _captureScheduled = true;
                    MainThreadDispatcher.RegisterUpdater(CaptureAndApply);
                }
            }
            else
            {
                _isIngame = false;
            }
        }

        /// <summary>
        /// Runs on the main thread until the prefab data is initialized, then captures the vanilla
        /// baseline and applies the active pack once. Returns true to unregister itself.
        /// </summary>
        private bool CaptureAndApply()
        {
            if (!_isIngame)
            {
                _captureScheduled = false;
                return true;
            }
            if (!CaptureVanilla())
                return false; // data not ready yet, retry next frame

            _vanillaReady = true;
            TryLoadActivePackFromDisk();
            ApplyAll();
            _captureScheduled = false;
            return true;
        }

        /// <summary>
        /// Reads the vanilla probability/speed/acceleration/braking of every vehicle prefab.
        /// Returns false while the data is still uninitialized (speed == 0) so the caller retries.
        /// </summary>
        private bool CaptureVanilla()
        {
            var entities = _vehicleQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return false;

            var captured = new Dictionary<string, VanillaBaseline>();
            foreach (var entity in entities)
            {
                float maxSpeed, acceleration, braking;
                if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                {
                    if (carData.m_MaxSpeed == 0)
                        return false; // not initialized yet
                    maxSpeed = carData.m_MaxSpeed;
                    acceleration = carData.m_Acceleration;
                    braking = carData.m_Braking;
                }
                else if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                {
                    if (trainData.m_MaxSpeed == 0)
                        return false; // not initialized yet
                    maxSpeed = trainData.m_MaxSpeed;
                    acceleration = trainData.m_Acceleration;
                    braking = trainData.m_Braking;
                }
                else
                {
                    continue;
                }

                var prefabName = _prefabSystem.GetPrefabName(entity);
                if (captured.ContainsKey(prefabName))
                    continue;

                int probability = 0;
                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                    probability = personalCarData.m_Probability;

                captured[prefabName] = new VanillaBaseline
                {
                    Probability = probability,
                    MaxSpeed = maxSpeed,
                    Acceleration = acceleration,
                    Braking = braking,
                };
            }

            if (captured.Count == 0)
                return false;

            foreach (var pair in captured)
                _vanilla[pair.Key] = pair.Value;

            log.Info($"Captured vanilla baseline for {_vanilla.Count} vehicle prefabs.");
            return true;
        }

        /// <summary>Resolves and writes the active pack's values to every vehicle prefab.</summary>
        private void ApplyAll()
        {
            if (!_vanillaReady)
                return;

            var entities = _vehicleQuery.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
            {
                var prefabName = _prefabSystem.GetPrefabName(entity);
                if (!_vanilla.TryGetValue(prefabName, out var baseline))
                    continue;

                var resolved = _active.Resolve(prefabName, baseline);

                if (EntityManager.TryGetComponent<CarData>(entity, out var carData))
                {
                    carData.m_MaxSpeed = resolved.MaxSpeed;
                    carData.m_Acceleration = resolved.Acceleration;
                    carData.m_Braking = resolved.Braking;
                    EntityManager.SetComponentData(entity, carData);
                }
                else if (EntityManager.TryGetComponent<TrainData>(entity, out var trainData))
                {
                    trainData.m_MaxSpeed = resolved.MaxSpeed;
                    trainData.m_Acceleration = resolved.Acceleration;
                    trainData.m_Braking = resolved.Braking;
                    EntityManager.SetComponentData(entity, trainData);
                }

                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    personalCarData.m_Probability = resolved.Probability;
                    EntityManager.SetComponentData(entity, personalCarData);
                }

                EntityManager.AddComponent<BatchesUpdated>(entity);
                count++;
            }

            log.Info($"Applied vehicle pack '{_active.Name}' to {count} prefabs.");

            // Keep the Vehicle Manager window (if present) in sync with the applied values.
            VehicleManagerUISystem.Instance?.RequestTreeUpdate();
        }

        // ---- Public API ---------------------------------------------------------

        public void SetActivePack(VehiclePack pack)
        {
            _active = pack ?? new VehiclePack("Default");
            log.Info($"Active vehicle pack set to '{_active.Name}'.");
            ApplyAll();
        }

        public void Reapply() => ApplyAll();

        public void ResetToVanilla()
        {
            _active = new VehiclePack("Default");
            Persist();
            ApplyAll();
        }

        /// <summary>
        /// Applies a single edit from the UI to the active pack, then persists and re-applies.
        /// <paramref name="value"/> is expected in the pack's internal units (m/s for speed,
        /// percent for probability); the UI converts before sending.
        /// </summary>
        public void Edit(string level, string key, string field, bool reset, float value)
        {
            switch (level)
            {
                case "global":
                    switch (field)
                    {
                        case "probability": _active.Global.ProbabilityFactor = value; break;
                        case "maxSpeed": _active.Global.SpeedFactor = value; break;
                        case "acceleration": _active.Global.AccelerationFactor = value; break;
                        case "braking": _active.Global.BrakingFactor = value; break;
                    }
                    break;
                case "prefab":
                {
                    _active.PrefabOverrides.TryGetValue(key, out var over);
                    over ??= new VehicleOverride();
                    SetOverrideField(over, field, reset, value);
                    _active.SetPrefabOverride(key, over);
                    break;
                }
                case "class":
                {
                    _active.ClassOverrides.TryGetValue(key, out var over);
                    over ??= new VehicleOverride();
                    SetOverrideField(over, field, reset, value);
                    _active.SetClassOverride(key, over);
                    break;
                }
            }

            Persist();
            ApplyAll();
        }

        public void AssignClass(string prefabName, string className)
        {
            _active.AssignPrefabToClass(prefabName, className);
            Persist();
            ApplyAll();
        }

        public void AssignClassMany(string[] prefabNames, string className)
        {
            if (prefabNames == null)
                return;
            foreach (var prefabName in prefabNames)
                _active.AssignPrefabToClass(prefabName, className);
            Persist();
            ApplyAll();
        }

        public void RenameClass(string oldName, string newName)
        {
            _active.RenameCustomClass(oldName, newName);
            Persist();
            ApplyAll();
        }

        public void DeleteClass(string name)
        {
            _active.DeleteCustomClass(name);
            Persist();
            ApplyAll();
        }

        private static void SetOverrideField(VehicleOverride over, string field, bool reset, float value)
        {
            switch (field)
            {
                case "probability": over.ProbabilityPercent = reset ? (int?)null : (int)Math.Round(value); break;
                case "maxSpeed": over.MaxSpeed = reset ? (float?)null : value; break;
                case "acceleration": over.Acceleration = reset ? (float?)null : value; break;
                case "braking": over.Braking = reset ? (float?)null : value; break;
            }
        }

        private void Persist()
        {
            try
            {
                if (!_active.ReadOnly)
                    _active.SaveToFile();
            }
            catch (Exception x)
            {
                log.Warn($"Could not save vehicle pack '{_active.Name}': {x.Message}");
            }
        }

        private void TryLoadActivePackFromDisk()
        {
            try
            {
                if (VehiclePack.GetPackNames().Contains(_active.Name))
                    _active = VehiclePack.LoadFromFile(_active.Name);
            }
            catch (Exception x)
            {
                log.Warn($"Could not load active vehicle pack '{_active.Name}': {x.Message}");
            }
        }

        public void ApplyPackByName(string name)
        {
            try
            {
                SetActivePack(VehiclePack.LoadFromFile(name));
            }
            catch (Exception x)
            {
                log.Warn($"Could not load vehicle pack '{name}': {x.Message}");
            }
        }

        // ---- Debug helper -------------------------------------------------------

        /// <summary>
        /// Creates a visible example pack (all cars 1.5x speed, Sedans 60 m/s, one prefab boosted)
        /// so the cascade can be tested from the Debug settings before the real UI exists.
        /// </summary>
        public static void CreateExamplePack()
        {
            var pack = new VehiclePack("Example")
            {
                Description = "Example cascade pack for testing",
            };
            pack.Global.SpeedFactor = 1.5f;
            pack.SetClassOverride("Sedan", new VehicleOverride { MaxSpeed = 60f, ProbabilityPercent = 150 });
            pack.SetPrefabOverride("Car01", new VehicleOverride { MaxSpeed = 80f });
            pack.SaveToFile();
        }

        protected override void OnUpdate()
        {
        }
    }
}
