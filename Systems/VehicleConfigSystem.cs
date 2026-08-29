using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Core;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

        // The layered active stack (index 0 = top of the list; the BOTTOM pack wins on conflict,
        // like Minecraft resource packs). Edits target _stack[_editIndex].
        private readonly List<VehiclePack> _stack = new List<VehiclePack>();
        private int _editIndex;

        private bool _isIngame;
        private bool _vanillaReady;
        private bool _captureScheduled;

        public static VehicleConfigSystem Instance { get; private set; }
        public static bool IsIngame { get; private set; }
        public IReadOnlyList<VehiclePack> ActiveStack => _stack;
        public VehiclePack EditTarget => (_editIndex >= 0 && _editIndex < _stack.Count) ? _stack[_editIndex] : null;
        /// <summary>The pack edits are written to (edit target). Null only if the stack is somehow empty.</summary>
        public VehiclePack ActivePack => EditTarget;
        public IReadOnlyDictionary<string, VanillaBaseline> Vanilla => _vanilla;
        public bool VanillaReady => _vanillaReady;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            Enabled = true;
            log = Mod.log;

            _vehicleQuery = SystemAPI.QueryBuilder().WithAny<CarData, TrainData, WatercraftData, AircraftData>().Build();
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
            LoadStack();
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
                else if (EntityManager.TryGetComponent<WatercraftData>(entity, out var watercraftData))
                {
                    if (watercraftData.m_MaxSpeed == 0)
                        return false;
                    maxSpeed = watercraftData.m_MaxSpeed;
                    acceleration = watercraftData.m_Acceleration;
                    braking = watercraftData.m_Braking;
                }
                else if (EntityManager.TryGetComponent<HelicopterData>(entity, out var helicopterData))
                {
                    if (helicopterData.m_FlyingMaxSpeed == 0)
                        return false;
                    maxSpeed = helicopterData.m_FlyingMaxSpeed;
                    acceleration = helicopterData.m_FlyingAcceleration;
                    braking = 0f; // helicopters have no braking parameter
                }
                else if (EntityManager.TryGetComponent<AirplaneData>(entity, out var airplaneData))
                {
                    if (airplaneData.m_FlyingSpeed.y == 0)
                        return false;
                    maxSpeed = airplaneData.m_FlyingSpeed.y;
                    acceleration = airplaneData.m_FlyingAcceleration;
                    braking = airplaneData.m_FlyingBraking;
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

        // ---- Layered resolution -------------------------------------------------
        // Priority runs bottom-up (like Minecraft resource packs): the LOWEST pack in the list that
        // defines a value wins on conflict. We walk the stack top→bottom without breaking, so each
        // later (lower) pack overwrites — the last hit is the bottom-most one.

        /// <summary>Effective classes for a prefab: the bottom-most active pack that assigns it wins; else built-in.</summary>
        public List<string> GetEffectiveClasses(string prefabName)
        {
            List<string> assigned = null;
            foreach (var pack in _stack)
            {
                var a = pack.AssignedClasses(prefabName);
                if (a.Count > 0)
                    assigned = a; // keep going; the lowest assigning pack wins
            }
            return assigned ?? new List<string>(VehicleClass.GetClassesForPrefab(prefabName));
        }

        /// <summary>
        /// Resolves a prefab across the whole stack: for each field, the bottom-most pack that defines
        /// it (prefab override → class override) wins; else vanilla. Then × the global multipliers.
        /// </summary>
        public ResolvedVehicle ResolveStacked(string prefabName, VanillaBaseline vanilla)
        {
            var classes = GetEffectiveClasses(prefabName);

            int probPercent = 100;
            foreach (var pack in _stack) { var v = pack.ProbabilityPercentFor(prefabName, classes); if (v != null) probPercent = v.Value; }

            float maxSpeed = vanilla.MaxSpeed;
            foreach (var pack in _stack) { var v = pack.MaxSpeedFor(prefabName, classes); if (v != null) maxSpeed = v.Value; }

            float acceleration = vanilla.Acceleration;
            foreach (var pack in _stack) { var v = pack.AccelerationFor(prefabName, classes); if (v != null) acceleration = v.Value; }

            float braking = vanilla.Braking;
            foreach (var pack in _stack) { var v = pack.BrakingFor(prefabName, classes); if (v != null) braking = v.Value; }

            var g = CombinedGlobals();
            int probability = (int)Math.Round(vanilla.Probability * (probPercent / 100f) * g.ProbabilityFactor);
            probability = Math.Max(0, Math.Min(255, probability));

            return new ResolvedVehicle
            {
                Probability = probability,
                MaxSpeed = maxSpeed * g.SpeedFactor,
                Acceleration = acceleration * g.AccelerationFactor,
                Braking = braking * g.BrakingFactor,
            };
        }

        /// <summary>
        /// Multiplies the (UI-less) global multipliers of every active pack together. Untouched packs
        /// are all 1.0, so this is a no-op unless a hand-written or shipped pack sets one.
        /// </summary>
        public GlobalMultipliers CombinedGlobals()
        {
            var g = new GlobalMultipliers();
            foreach (var pack in _stack)
            {
                if (pack.Global == null) continue;
                g.ProbabilityFactor *= pack.Global.ProbabilityFactor;
                g.SpeedFactor *= pack.Global.SpeedFactor;
                g.AccelerationFactor *= pack.Global.AccelerationFactor;
                g.BrakingFactor *= pack.Global.BrakingFactor;
            }
            return g;
        }

        /// <summary>Resolves and writes the whole active stack's values to every vehicle prefab.</summary>
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

                var resolved = ResolveStacked(prefabName, baseline);

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
                else if (EntityManager.TryGetComponent<WatercraftData>(entity, out var watercraftData))
                {
                    watercraftData.m_MaxSpeed = resolved.MaxSpeed;
                    watercraftData.m_Acceleration = resolved.Acceleration;
                    watercraftData.m_Braking = resolved.Braking;
                    EntityManager.SetComponentData(entity, watercraftData);
                }
                else if (EntityManager.TryGetComponent<HelicopterData>(entity, out var helicopterData))
                {
                    helicopterData.m_FlyingMaxSpeed = resolved.MaxSpeed;
                    helicopterData.m_FlyingAcceleration = resolved.Acceleration;
                    EntityManager.SetComponentData(entity, helicopterData);
                }
                else if (EntityManager.TryGetComponent<AirplaneData>(entity, out var airplaneData))
                {
                    airplaneData.m_FlyingSpeed = new float2(Math.Min(airplaneData.m_FlyingSpeed.x, resolved.MaxSpeed), resolved.MaxSpeed);
                    airplaneData.m_FlyingAcceleration = resolved.Acceleration;
                    airplaneData.m_FlyingBraking = resolved.Braking;
                    EntityManager.SetComponentData(entity, airplaneData);
                }

                if (EntityManager.TryGetComponent<PersonalCarData>(entity, out var personalCarData))
                {
                    personalCarData.m_Probability = resolved.Probability;
                    EntityManager.SetComponentData(entity, personalCarData);
                }

                EntityManager.AddComponent<BatchesUpdated>(entity);
                count++;
            }

            log.Info($"Applied vehicle stack [{string.Join(", ", _stack.Select(p => p.Name))}] to {count} prefabs.");

            // Keep the Vehicle Manager window (if present) in sync with the applied values.
            VehicleManagerUISystem.Instance?.RequestTreeUpdate();
        }

        // ---- Public API ---------------------------------------------------------

        public void Reapply() => ApplyAll();

        public void ResetToVanilla()
        {
            _stack.Clear();
            _stack.Add(new VehiclePack("Default"));
            _editIndex = 0;
            PersistEditTarget();
            PersistStack();
            ApplyAll();
        }

        /// <summary>
        /// Applies a single prefab/class edit from the UI to the current edit-target pack, then
        /// persists and re-applies. <paramref name="value"/> is in internal units (m/s for speed,
        /// percent for probability). Global multipliers have no UI and are not edited here.
        /// </summary>
        public void Edit(string level, string key, string field, bool reset, float value)
        {
            var target = EnsureEditTarget();
            switch (level)
            {
                case "prefab":
                {
                    target.PrefabOverrides.TryGetValue(key, out var over);
                    over ??= new VehicleOverride();
                    SetOverrideField(over, field, reset, value);
                    target.SetPrefabOverride(key, over);
                    break;
                }
                case "class":
                {
                    target.ClassOverrides.TryGetValue(key, out var over);
                    over ??= new VehicleOverride();
                    SetOverrideField(over, field, reset, value);
                    target.SetClassOverride(key, over);
                    break;
                }
            }

            PersistEditTarget();
            ApplyAll();
        }

        public void AssignClass(string prefabName, string className)
        {
            EnsureEditTarget().AssignPrefabToClass(prefabName, className);
            PersistEditTarget();
            ApplyAll();
        }

        public void AssignClassMany(string[] prefabNames, string className)
        {
            if (prefabNames == null)
                return;
            var target = EnsureEditTarget();
            foreach (var prefabName in prefabNames)
                target.AssignPrefabToClass(prefabName, className);
            PersistEditTarget();
            ApplyAll();
        }

        public void RenameClass(string oldName, string newName)
        {
            EnsureEditTarget().RenameCustomClass(oldName, newName);
            PersistEditTarget();
            ApplyAll();
        }

        public void DeleteClass(string name)
        {
            EnsureEditTarget().DeleteCustomClass(name);
            PersistEditTarget();
            ApplyAll();
        }

        /// <summary>Applies the same field edit to many prefabs at once (persist + apply once).</summary>
        public void EditManyPrefabs(string[] prefabs, string field, bool reset, float value)
        {
            if (prefabs == null)
                return;
            var target = EnsureEditTarget();
            foreach (var prefabName in prefabs)
            {
                target.PrefabOverrides.TryGetValue(prefabName, out var over);
                over ??= new VehicleOverride();
                SetOverrideField(over, field, reset, value);
                target.SetPrefabOverride(prefabName, over);
            }
            PersistEditTarget();
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

        // ---- Stack state helpers ------------------------------------------------

        /// <summary>
        /// Returns the pack edits are written to, guaranteeing it is writable. If only shipped
        /// (read-only) packs are active, a personal pack is created so the first edit has a home.
        /// </summary>
        private VehiclePack EnsureEditTarget()
        {
            if (_editIndex >= 0 && _editIndex < _stack.Count && !_stack[_editIndex].ReadOnly)
                return _stack[_editIndex];

            var writable = _stack.FindIndex(p => !p.ReadOnly);
            bool created = writable < 0;
            if (created)
            {
                _stack.Add(new VehiclePack("My changes"));
                writable = _stack.Count - 1;
            }
            _editIndex = writable;
            if (created)
                PersistStack(); // the new pack must survive a restart
            return _stack[_editIndex];
        }

        private int IndexOfPack(string name) => _stack.FindIndex(p => p.Name == name);

        // "_" is reserved for internal state files (_active.json), so packs may not start with it.
        private static bool IsValidPackName(string name) =>
            !string.IsNullOrWhiteSpace(name) && !name.StartsWith("_");

        private static VehiclePack LoadOrNew(string name)
        {
            try
            {
                if (VehiclePack.GetPackNames().Contains(name))
                    return VehiclePack.LoadFromFile(name);
            }
            catch (Exception x) { Mod.log.Warn($"Could not load pack '{name}': {x.Message}"); }
            return new VehiclePack(name);
        }

        // Saves the edit-target pack file (the only pack the user can be mutating).
        private void PersistEditTarget()
        {
            var target = EditTarget;
            if (target == null) return;
            try
            {
                if (!target.ReadOnly)
                    target.SaveToFile();
            }
            catch (Exception x)
            {
                log.Warn($"Could not save vehicle pack '{target.Name}': {x.Message}");
            }
        }

        // Saves the layered config (active pack order, edit target, global multipliers).
        private void PersistStack()
        {
            try
            {
                VehiclePack.SaveStackConfig(new StackConfig
                {
                    Packs = _stack.Select(p => p.Name).ToList(),
                    EditTarget = EditTarget?.Name,
                });
            }
            catch (Exception x) { log.Warn($"Could not save stack config: {x.Message}"); }
        }

        private void LoadStack()
        {
            try
            {
                _stack.Clear();
                var cfg = VehiclePack.LoadStackConfig();
                if (cfg != null && cfg.Packs != null && cfg.Packs.Count > 0)
                {
                    foreach (var name in cfg.Packs)
                        _stack.Add(LoadOrNew(name));
                    _editIndex = Math.Max(0, _stack.FindIndex(p => p.Name == cfg.EditTarget));
                }
                else
                {
                    // Backward compat: single active-name marker from before layering.
                    var legacy = VehiclePack.LoadActiveName();
                    _stack.Add(LoadOrNew(string.IsNullOrEmpty(legacy) ? "Default" : legacy));
                    _editIndex = 0;
                }
            }
            catch (Exception x)
            {
                log.Warn($"Could not load vehicle stack: {x.Message}");
            }
            EnsureEditTarget();
        }

        // Replaces the whole stack with a single pack (used by the Debug helper button).
        public void ApplyPackByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _stack.Clear();
            _stack.Add(LoadOrNew(name));
            _editIndex = 0;
            PersistStack();
            ApplyAll();
        }

        // ---- Pack management ----------------------------------------------------

        /// <summary>
        /// Adds an existing (or new) pack to the bottom of the stack (highest priority). It also becomes
        /// the edit target unless it is read-only — edits to a shipped pack would be silently dropped.
        /// </summary>
        public void AddToStack(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (IndexOfPack(name) >= 0) { SetEditTarget(name); return; }

            var editName = EditTarget?.Name;
            var pack = LoadOrNew(name);
            _stack.Add(pack);
            if (pack.ReadOnly)
                _editIndex = Math.Max(0, _stack.FindIndex(p => p.Name == editName));
            else
                _editIndex = _stack.Count - 1;
            PersistStack();
            ApplyAll();
        }

        /// <summary>Activates an inactive pack, or deactivates an active one (the file is kept either way).</summary>
        public void TogglePack(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (IndexOfPack(name) >= 0)
                RemoveFromStack(name);
            else
                AddToStack(name);
        }

        /// <summary>Removes a pack from the active stack (its file is kept).</summary>
        public void RemoveFromStack(string name)
        {
            var i = IndexOfPack(name);
            if (i < 0) return;
            var editName = EditTarget?.Name;
            _stack.RemoveAt(i);
            EnsureEditTarget();
            if (editName != null && editName != name)
                _editIndex = Math.Max(0, _stack.FindIndex(p => p.Name == editName));
            PersistStack();
            ApplyAll();
        }

        /// <summary>Moves a pack up (-1) or down (+1) in the priority order.</summary>
        public void MovePack(string name, int delta)
        {
            var i = IndexOfPack(name);
            if (i < 0) return;
            int j = i + delta;
            if (j < 0 || j >= _stack.Count) return;
            var editName = EditTarget?.Name;
            var p = _stack[i];
            _stack.RemoveAt(i);
            _stack.Insert(j, p);
            _editIndex = Math.Max(0, _stack.FindIndex(x => x.Name == editName));
            PersistStack();
            ApplyAll();
        }

        /// <summary>Selects which active pack subsequent edits are written to. Read-only packs can't be targeted.</summary>
        public void SetEditTarget(string name)
        {
            var i = IndexOfPack(name);
            if (i < 0 || _stack[i].ReadOnly) return;
            _editIndex = i;
            PersistStack();
            // Values are unchanged; just refresh the UI's edit-provenance/reset state.
            VehicleManagerUISystem.Instance?.RequestTreeUpdate();
        }

        public void NewPack(string name)
        {
            if (!IsValidPackName(name)) return;
            if (IndexOfPack(name) >= 0) { SetEditTarget(name); return; }
            _stack.Add(new VehiclePack(name));
            _editIndex = _stack.Count - 1;
            PersistEditTarget();
            PersistStack();
            ApplyAll();
        }

        public void DuplicatePack(string newName)
        {
            if (!IsValidPackName(newName)) return;
            var copy = EnsureEditTarget().Duplicate(newName);
            _stack.Add(copy);
            _editIndex = _stack.Count - 1;
            PersistEditTarget();
            PersistStack();
            ApplyAll();
        }

        public void RenamePack(string newName)
        {
            var target = EditTarget;
            if (target == null || !IsValidPackName(newName) || newName == target.Name) return;
            var old = target.Name;
            target.Name = newName;
            PersistEditTarget();
            VehiclePack.DeleteFile(old);
            PersistStack();
            ApplyAll();
        }

        public void DeletePack(string name)
        {
            VehiclePack.DeleteFile(name);
            if (IndexOfPack(name) >= 0)
                RemoveFromStack(name); // persists + re-applies
            else
                VehicleManagerUISystem.Instance?.RequestTreeUpdate(); // refresh available list
        }

        public void ExportActiveToClipboard()
        {
            var target = EditTarget;
            if (target == null) return;
            try
            {
                UnityEngine.GUIUtility.systemCopyBuffer = target.ToJson();
                log.Info($"Exported vehicle pack '{target.Name}' to clipboard.");
            }
            catch (Exception x) { log.Warn($"Could not export pack: {x.Message}"); }
        }

        public void ImportFromClipboard()
        {
            try
            {
                var incoming = VehiclePack.FromJson(UnityEngine.GUIUtility.systemCopyBuffer);
                if (incoming == null) { log.Warn("Clipboard does not contain a valid vehicle pack."); return; }
                var target = EnsureEditTarget();
                target.Merge(incoming, MergeConflictPolicy.TakeTheirs);
                PersistEditTarget();
                ApplyAll();
                log.Info($"Merged clipboard pack into '{target.Name}'.");
            }
            catch (Exception x) { log.Warn($"Could not import pack: {x.Message}"); }
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
            pack.SetClassOverride("Sedan", new VehicleOverride { MaxSpeed = 60f, ProbabilityPercent = 150 });
            pack.SetPrefabOverride("Car01", new VehicleOverride { MaxSpeed = 80f });
            pack.SaveToFile();
        }

        protected override void OnUpdate()
        {
        }
    }
}
