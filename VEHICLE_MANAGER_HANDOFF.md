# Vehicle Controller — Vehicle Manager redesign · Handoff

Status document for continuing the redesign of the **Vehicle Controller** Cities: Skylines II mod.
Written 2026-07-05. Read this top-to-bottom before touching code.

---

## 1. What this is

Vehicle Controller lets players tune how vehicles spawn and drive. It is being **rebuilt** around
two ideas:

1. **A cascade value model** shared by spawn probability *and* driving properties:
   `prefab override → class override → vanilla baseline`, then `× global multiplier`.
   Sparse: only what the user changed is stored; everything else falls through to vanilla.
2. **A floating in-game "Vehicle Manager" window** (vanilla-styled, like the game's own panels)
   for per-prefab/per-class precision — replacing the old "edit JSON by hand" workflow. Configs
   are **packs** (shareable, sparse).

The old split systems (probability + property, JSON-only) have been **removed** (see §6 Cutover).

### Mental model / personas
- **Easy:** turn a global multiplier (all cars ×1.5 speed) — the "grob" knob.
- **Medium:** set a whole class (all Sedans → 200 km/h).
- **Precise:** override a single prefab (this one bus → 60 km/h).
- Personas “only trains” / “only cars” / “everything precise” are served by the same window via
  category filtering + the cascade depth they choose. Nothing is forced on them.

---

## 2. Current state — what works (verified in-game unless noted)

| Area | Status |
|---|---|
| Floating Vehicle Manager window (vanilla look, top-left toggle button w/ VC logo) | ✅ in-game |
| C# cascade model + resolver (`VehiclePack`) | ✅ |
| Engine: capture vanilla once, resolve, apply to ECS (`VehicleConfigSystem`) | ✅ in-game |
| Window ↔ engine: real tree (category→class→prefab) streamed to UI | ✅ in-game |
| Editing **prefab** values (probability %, speed km/h, accel/braking m/s²) + reset-to-inherited | ✅ in-game |
| Editing **global** multipliers (speed/accel/braking/prob) | ✅ in-game |
| Editing **class** values (select class → class detail) | ✅ (built, test) |
| **Classes per pack**: assign prefab to class (existing/new/unassign), rename/delete custom classes | ✅ (built, test) |
| Category icons (Cars/Trains/Service) | ✅ (imported PNGs) |
| Cutover: legacy probability/property systems deleted, Settings cleaned | ✅ builds |
| Persistence: edits auto-save to `packs/vehicle/Default.json`, reloaded on game start | ✅ |

Everything type-checks: `cd UI && npx tsc --noEmit` → 0 errors. C# builds on the user's machine.

---

## 3. Architecture & key files

### C# (sim + UI backend) — `.cs`
- **`Mod.cs`** — entry point. Registers systems: `PrefabCacheSystem`, `VehicleConfigSystem`
  (MainLoop), `VehicleManagerUISystem` (UIUpdate), `VehicleCounterSystem`,
  `CompatibilityRoadSpeedLimitSystem`, `VehicleStiffnessSystem`. Copies embedded resources.
- **`Data/VehiclePack.cs`** — the unified **sparse cascade model**. Contains `VehicleOverride`
  (nullable fields = inherit), `GlobalMultipliers`, `VanillaBaseline`, `ResolvedVehicle`, and
  `VehiclePack` with:
  - `Resolve(prefab, vanilla)` — the cascade.
  - `GetEffectiveClasses(prefab)` — **pack membership overrides built-in** `VehicleClass` membership.
  - `SetPrefabOverride`/`SetClassOverride`, `AssignPrefabToClass`, `CreateCustomClass`,
    `RenameCustomClass`, `DeleteCustomClass`, `Merge`, `Duplicate`.
  - Persistence in `ModsData/VehicleController/packs/vehicle/<name>.json`.
  - Per-pack: `CustomClasses` (names) + `ClassMembership` (class → prefab names).
- **`Systems/VehicleConfigSystem.cs`** — **the engine** (`GameSystemBase`, MainLoop). Captures the
  vanilla baseline of every vehicle prefab once per game load (retries until data ready), resolves
  the active pack, writes `CarData`/`TrainData` (speed/accel/braking) + `PersonalCarData`
  (probability) + `BatchesUpdated`. Active pack defaults to `"Default"`, auto-loaded/saved from
  disk. Public API: `Edit(level,key,field,reset,value)`, `AssignClass/RenameClass/DeleteClass`,
  `SetActivePack/ApplyPackByName/ResetToVanilla`, `ActivePack`, `Vanilla`, `VanillaReady`, static
  `IsIngame`.
- **`Systems/VehicleManagerUISystem.cs`** — **UI backend** (`UISystemBase`, UIUpdate, binding group
  `"VehicleController.VehicleManager"`). Streams three JSON strings via `ValueBinding<string>`:
  `treeJson` (category→class→prefab with resolved values + override provenance), `globalJson`
  (multipliers), `classesJson` (assignable class names). Triggers: `refresh`, `edit` (JSON cmd),
  `classCmd` (assign/rename/delete). Categorization: `TrainData`→trains, `PersonalCarData`→cars,
  else service.
- **`Data/VehicleClass.cs`** — built-in classes from shipped `vehicleClasses.json` (read-only
  defaults). Custom classes live in the pack, not here.
- **`Systems/VehicleCounterSystem.cs` / `VehicleStiffnessSystem.cs` /
  `CompatibilityRoadSpeedLimitSystem.cs` / `PrefabCacheSystem.cs`** — existing utility systems, kept.
- **`Systems/VehiclePropertiesSection.cs`** — the OLD per-vehicle Selected-Info-Panel (SIP) section.
  **Unregistered & unfinished**; its legacy probability calls are stubbed with `TODO(M1)`. This is
  the thing M1 will rebuild against `VehicleConfigSystem`.
- **`Systems/VehicleSelectionSection.cs`** — experimental "choose which vehicles a service building
  uses" feature. Disabled by default, out of scope for this redesign.
- **`Data/ProbabilityPack.cs` / `PropertyPack.cs`** — **LEGACY, now unwired.** Kept only so the user
  can later write a migration script. Do not build on them.

### UI (React/TSX, webpack → coui) — `UI/`
- **`UI/src/index.tsx`** — registers SIP sections and `moduleRegistry.append("GameTopLeft",
  VehicleManager)`.
- **`UI/src/manager/VehicleManager.tsx`** — floating toggle button (`FloatingButton`, `VC.png`
  logo) that Portal-hosts the panel.
- **`UI/src/manager/VehicleManagerPanel.tsx`** — the whole window. Bindings + parsing, the tree,
  prefab detail (editable fields + `ClassPicker`), class detail (`ClassAdminRow` rename/delete for
  custom classes), `GlobalBar`, pack bar (visual only), chips (visual only). `ClassDropdownButton`
  is the shared assign dropdown.
- **`UI/src/manager/data.ts`** — TS types (`AttrState`, `PrefabNode`, `ClassNode`, `CategoryNode`,
  `PackInfo`, `ClassInfo`) + mock data (fallback when the backend sends nothing).
- **`UI/src/images/`** — `VC.*`, `car.*`, `train.*`, `ambulance.*` (png + svg). **Images must be
  `import`ed** to be emitted by webpack (`coui://ui-mods/images/…`); a bare string path never works.

### Data flow
- **Read:** engine applies pack → `RequestTreeUpdate()` → updates `treeJson`/`globalJson`/
  `classesJson` bindings → TS `useValue` re-renders. TS selects by **id/name** and re-derives the
  detail from the fresh tree, so edits reflect immediately.
- **Write:** UI `trigger(group, "edit"|"classCmd", JSON)` → C# parses → mutates active pack →
  `Persist()` (save) → `ApplyAll()` (write ECS) → `RequestTreeUpdate()`.

---

## 4. Key design decisions (and why)

- **Units:** stored internally in **m/s** (matches `CarData`); UI displays **km/h** (`×3.6`).
  Probability is a **percent of vanilla** (100 = vanilla), clamped 0–255.
- **Global vs per-vehicle:** the per-prefab/-class values shown are the *authored* values (pre-global).
  The global multiplier is a separate top layer applied on apply. Keeps the two concepts unmuddled.
- **Classes live in the pack** (portable — sharing a pack brings its classes + assignments). Built-in
  classes come from `vehicleClasses.json` (untouched). **Pack membership overrides built-in**, so any
  prefab (incl. custom/unclassified assets) can be moved into a class. `"Unclassified"` is a UI-only
  bucket, not an editable class.
- **One active pack** (`"Default"`) auto-saved/loaded for now. The **pack bar is not built yet.**
- **Bindings via JSON strings** (`ValueBinding<string>` + Newtonsoft, parsed on TS) and **triggers via
  JSON command payloads** — deliberately chosen over hand-writing nested `IJsonWriter`, because the C#
  side can't be compiled/tested in-agent, so the low-risk path matters.
- **cs2/ui import gotchas** (see also the memory `reference_cs2_ui_runtime_exports.md`):
  - Runtime-safe from `cs2/ui`: `Button, Panel, Portal, Scrollable, Icon, FloatingButton, Dropdown,
    DropdownToggle, Tooltip, FormattedParagraphs, …` (the `export export const` ones).
  - Do **not** import `InfoRow`/`InfoSection` from `cs2/ui` — use the alias names
    `PanelSectionRow`/`PanelSection`/`PanelFoldout` (or `getModule`/`ModuleResolver`).
  - `FOCUS_DISABLED` from `cs2/input`; button tooltip prop is `tooltipLabel` (not `tooltip`).
  - `moduleRegistry.append(target, Component)` targets: `Menu|Editor|Game|GameTopLeft|GameTopRight|
    GameBottomRight`.
  - Inline style unit is `rem` (e.g. `"640rem"`).

---

## 5. Roadmap & next steps

| Milestone | Status |
|---|---|
| M2 — floating window shell | ✅ |
| M0 — cascade model + engine | ✅ |
| Window ↔ engine (read + edit prefab/global/class) | ✅ |
| Editable per-pack classes (assign/create/rename/delete) | ✅ |
| Cutover (retire legacy systems) | ✅ |
| **Pack bar** (multiple packs) | ⬜ **next** |
| SIP deep-link | ⬜ |
| M1 — per-vehicle SIP panel | ⬜ |
| Settings redesign / remove empty tabs | ⬜ |
| Polish | ⬜ |

### Next (recommended order)
1. **Pack bar** — currently everything auto-saves to a single `"Default"` pack. Build: list packs,
   switch active, new/duplicate/rename/delete, **import = merge** (conflict handling), **export**
   (clipboard string + file), shipped **read-only** templates that duplicate-to-edit. The engine
   already has `Merge`/`Duplicate`/`LoadFromFile`/`GetPackNames`; needs C# triggers + a `packsJson`
   binding + UI to make the (currently visual-only) pack bar real. Savegame should remember the active
   pack name.
2. **SIP deep-link** (user-requested): a button in a vehicle's Selected-Info panel that opens the
   Vehicle Manager scrolled/selected to that prefab, ready to edit.
3. **M1** — rebuild `VehiclePropertiesSection` (the per-vehicle SIP) against `VehicleConfigSystem`.
4. **Settings redesign** — remove the now-empty "Spawning Behavior" tab and "Vehicle Property Pack"
   group (their consts/`[SettingsUI…]` attributes are still present but the members are gone); add an
   "easy mode" (presets + global multipliers + class sliders) if desired.

### Polish backlog
- Localize prefab names (currently internal names like `Car05`) via `Assets.NAME[<prefab>]`.
- Category icons for all types; replace the pack-bar `BoxIcon` placeholder.
- Real custom-asset detection (currently `custom` is always `false`).
- Verify the km/h `×3.6` calibration against the game's own speed display; adjust the factor if off.
- Vanilla-style number inputs (current inputs are plain dark boxes).
- Make the search box + category/scope chips functional (currently visual only).
- Optional: revert `ClassPicker` to the older inline dropdown (currently uses `ClassDropdownButton`,
  same UX).

---

## 6. Cutover notes (already done)

- **Deleted:** `Systems/VehicleProbabilitySystem.cs`, `Systems/VehiclePropertySystem.cs`.
  `VehicleConfigSystem` is now the single authority (fixes the old double-write that polluted the
  vanilla baseline, and the hardcoded train accel/braking).
- **`Setting.cs`** stripped of all probability sliders + property-pack dropdowns/factor/export; kept
  Stiffness, Road-speed reset, About, Debug (incl. **temporary** vehicle-pack test buttons: Create/
  Apply/Reset example vehicle pack). `IsIngame` repointed to `VehicleConfigSystem`. Empty tabs remain
  (see settings-redesign task).
- **No migration** (user's decision — they’ll script pack migration later). Old savegame system data
  is simply ignored on load. Legacy `ProbabilityPack`/`PropertyPack` classes remain but are unwired.

---

## 7. Working with this repo (important!)

- **Do NOT build/run the CS2 mod in-agent** — the user builds and tests. For the UI you *can* and
  *should* run `cd UI && npx tsc --noEmit` after TS changes (catches most breakage). There is **no
  in-agent C# compile**, so write C# carefully and cross-check references.
- Build (user): `cd UI && npm run build` (needs `CSII_USERDATAPATH` from the modding toolchain) or
  `npm run dev` (watch). C# via the .csproj (SDK-style glob — deleting a `.cs` is safe).
- **Gameface (cohtml) quirk:** raw `<input type="checkbox">` renders as an ugly white box — avoid;
  build custom controls. (This caused a "weird text boxes" bug that was reverted.)
- **Decompiled CS2 source** for engine lookups: see memory `reference_cs2_decompiled_source.md`
  (a full source tree at `…/CS2-source/1.6.0f1 (419.d6c6) …`). Use it instead of guessing/decompiling.
- **Memory files** (persist across sessions) under
  `.claude/projects/…/memory/`: `MEMORY.md` (index), `project_vehicle_manager_redesign.md` (this
  project's rolling state), `reference_cs2_ui_runtime_exports.md`, `reference_cs2_decompiled_source.md`,
  `feedback_no_build_cs2_mods.md`. Keep `project_vehicle_manager_redesign.md` updated as you go.

---

## 8. Known caveats / rough edges

- Pack bar, chips, search are **visual only**; the active pack is always `"Default"`.
- The class "edit" affordance in the tree uses `stopPropagation` to avoid toggling the foldout —
  minor reliability dependency.
- Debug "example vehicle pack" buttons in Settings are temporary test hooks.
- `VehiclePropertiesSection` (SIP) is unregistered/stubbed; the registered SIP TS component
  (`SIPVehicleProperties`) talks to a group whose C# isn't active — pre-existing, handled in M1.
- Two empty settings tabs (Spawning Behavior / Vehicle Property Pack group) until the settings redesign.
