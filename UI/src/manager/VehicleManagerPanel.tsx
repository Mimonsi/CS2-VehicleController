import { bindValue, trigger, useValue } from "cs2/api";
import { FOCUS_DISABLED } from "cs2/input";
// PanelSection/PanelSectionRow/PanelFoldout are the runtime-safe *alias* exports
// of the game's InfoSection/InfoRow/InfoSectionFoldout (see ui.d.ts). Importing
// InfoRow/InfoSection directly from cs2/ui fails at runtime, the aliases work.
import { Button, Icon, Panel, PanelFoldout, PanelSection, PanelSectionRow, Scrollable } from "cs2/ui";
import React, { useEffect, useMemo, useState } from "react";

import { ModuleResolver } from "../ModuleResolver";
import { AttrState, CategoryNode, ClassNode, MOCK_PACKS, PrefabNode, VEHICLE_TREE } from "./data";
// Icons must be imported so webpack emits them (to coui://ui-mods/images/…);
// a bare string path is never emitted and won't resolve.
import ambulanceIcon from "../images/ambulance.png";
import carIcon from "../images/car.png";
import trainIcon from "../images/train.png";

// C# → UI: the vehicle tree is streamed as a JSON string (see VehicleManagerUISystem).
const MANAGER_GROUP = "VehicleController.VehicleManager";
const treeJson$ = bindValue<string>(MANAGER_GROUP, "treeJson", "[]");
const globalJson$ = bindValue<string>(MANAGER_GROUP, "globalJson", "{}");
const classesJson$ = bindValue<string>(MANAGER_GROUP, "classesJson", "[]");

interface ClassInfo {
  name: string;
  custom: boolean;
}

interface GlobalFactors {
  probability: number;
  speed: number;
  acceleration: number;
  braking: number;
}
const DEFAULT_GLOBAL: GlobalFactors = { probability: 1, speed: 1, acceleration: 1, braking: 1 };

// UI → C#: one edit command carries op/level/key/field/value as JSON.
type EditLevel = "prefab" | "class" | "global";
const sendSet = (level: EditLevel, key: string, field: string, value: number) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "set", level, key, field, value }));
const sendReset = (level: EditLevel, key: string, field: string) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "reset", level, key, field }));

// UI → C#: class management (assign / rename / delete). Empty class name = unassign.
const sendAssign = (prefab: string, className: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "assign", prefab, class: className }));
const sendAssignMany = (prefabs: string[], className: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "assignMany", prefabs, class: className }));
const sendRenameClass = (className: string, newName: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "rename", class: className, newName }));
const sendDeleteClass = (className: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "delete", class: className }));

const MS_TO_KMH = 3.6;

// The backend tree has no icons; map category keys to icons on the client.
const CATEGORY_ICONS: Record<string, string> = {
  cars: carIcon,
  trains: trainIcon,
  service: ambulanceIcon,
};

const resetSrc = "coui://uil/Standard/Reset.svg";
const packSrc = "coui://uil/Standard/BoxIcon.svg";

const OVERRIDE_COLOR = "rgba(140, 190, 255, 1)";
const DIM_COLOR = "rgba(255, 255, 255, 0.5)";

type Selection = { kind: "prefab"; id: string } | { kind: "class"; name: string };

function findPrefab(tree: CategoryNode[], id: string | null): PrefabNode | null {
  if (!id) return null;
  for (const c of tree)
    for (const cls of c.classes)
      for (const p of cls.prefabs)
        if (p.id === id) return p;
  return null;
}

function findClass(tree: CategoryNode[], name: string | null): ClassNode | null {
  if (!name) return null;
  for (const c of tree)
    for (const cls of c.classes)
      if (cls.name === name) return cls;
  return null;
}

const Badge = ({ state, inheritedLabel = "Inherited" }: { state: AttrState; inheritedLabel?: string }) => (
  <span
    style={{
      fontSize: "11rem",
      marginLeft: "8rem",
      padding: "2rem 7rem",
      borderRadius: "4rem",
      color: state.overridden ? OVERRIDE_COLOR : DIM_COLOR,
      border: state.overridden ? "1rem solid " + OVERRIDE_COLOR : "1rem solid " + DIM_COLOR,
    }}
  >
    {state.overridden ? "Override" : inheritedLabel}
  </span>
);

// Editable number field. Commits on blur / Enter; reverts to the incoming value on invalid input.
const EditableNumber = ({ value, onCommit }: { value: number; onCommit: (v: number) => void }) => {
  const display = (v: number) => (Number.isFinite(v) ? String(v) : "");
  const [text, setText] = useState(display(value));
  useEffect(() => setText(display(value)), [value]);

  const commit = () => {
    const parsed = parseFloat(text);
    if (!isNaN(parsed) && isFinite(parsed)) onCommit(parsed);
    else setText(display(value));
  };

  return (
    <input
      type="text"
      value={text}
      onChange={e => setText(e.currentTarget.value)}
      onBlur={commit}
      onKeyDown={e => {
        if (e.key === "Enter") e.currentTarget.blur();
      }}
      style={{
        width: "64rem",
        textAlign: "right",
        background: "rgba(0, 0, 0, 0.25)",
        color: "white",
        border: "1rem solid rgba(255, 255, 255, 0.2)",
        borderRadius: "3rem",
        padding: "2rem 6rem",
        fontSize: "13rem",
      }}
    />
  );
};

// One editable property line: input + unit + provenance badge + reset-to-inherited.
const EditRow = ({
  label,
  state,
  editValue,
  unit,
  onCommit,
  onReset,
  inheritedLabel,
}: {
  label: string;
  state: AttrState;
  editValue: number;
  unit: string;
  onCommit: (displayValue: number) => void;
  onReset: () => void;
  inheritedLabel?: string;
}) => (
  <PanelSectionRow
    disableFocus
    subRow
    left={label}
    right={
      <div style={{ display: "flex", alignItems: "center" }}>
        <EditableNumber value={editValue} onCommit={onCommit} />
        <span style={{ marginLeft: "5rem", color: DIM_COLOR, fontSize: "12rem" }}>{unit}</span>
        <Badge state={state} inheritedLabel={inheritedLabel} />
        <div style={{ marginLeft: "8rem" }}>
          <ModuleResolver.instance.ToolButton
            src={resetSrc}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
            disabled={!state.overridden}
            tooltip={"Reset to inherited"}
            className={ModuleResolver.instance.toolButtonTheme.button}
            onSelect={onReset}
          />
        </div>
      </div>
    }
  />
);

// Global cascade multipliers (applied on top of resolved per-vehicle values).
const GlobalInput = ({ label, value, field }: { label: string; value: number; field: string }) => (
  <div style={{ display: "flex", alignItems: "center", marginLeft: "10rem" }}>
    <span style={{ color: DIM_COLOR, fontSize: "12rem", marginRight: "4rem" }}>{label}</span>
    <EditableNumber value={Number(value.toFixed(2))} onCommit={v => sendSet("global", "", field, v)} />
    <span style={{ color: DIM_COLOR, fontSize: "12rem", marginLeft: "3rem" }}>×</span>
  </div>
);

const GlobalBar = ({ g }: { g: GlobalFactors }) => (
  <div
    style={{
      display: "flex",
      alignItems: "center",
      padding: "6rem 14rem",
      flexWrap: "wrap",
      borderTop: "1rem solid rgba(255, 255, 255, 0.08)",
    }}
  >
    <span style={{ fontSize: "12rem" }}>Global multipliers</span>
    <GlobalInput label={"Speed"} value={g.speed} field={"maxSpeed"} />
    <GlobalInput label={"Accel"} value={g.acceleration} field={"acceleration"} />
    <GlobalInput label={"Braking"} value={g.braking} field={"braking"} />
    <GlobalInput label={"Prob"} value={g.probability} field={"probability"} />
  </div>
);

const Chip = ({ label, active }: { label: string; active?: boolean }) => (
  <span
    style={{
      fontSize: "12rem",
      padding: "4rem 10rem",
      marginLeft: "6rem",
      borderRadius: "20rem",
      color: active ? OVERRIDE_COLOR : DIM_COLOR,
      border: "1rem solid " + (active ? OVERRIDE_COLOR : DIM_COLOR),
    }}
  >
    {label}
  </span>
);

const PrefabRow = ({
  prefab,
  selected,
  checked,
  onSelect,
  onToggleCheck,
}: {
  prefab: PrefabNode;
  selected: boolean;
  checked: boolean;
  onSelect: () => void;
  onToggleCheck: () => void;
}) => (
  <div
    style={{
      display: "flex",
      alignItems: "center",
      paddingLeft: "20rem",
      backgroundColor: selected ? "rgba(140, 190, 255, 0.15)" : undefined,
      borderLeft: selected ? "3rem solid " + OVERRIDE_COLOR : "3rem solid transparent",
    }}
  >
    <input
      type="checkbox"
      checked={checked}
      onChange={onToggleCheck}
      onClick={e => e.stopPropagation()}
      style={{ marginRight: "8rem" }}
    />
    <div
      onClick={onSelect}
      style={{ flex: 1, padding: "6rem 10rem", cursor: "pointer", color: selected ? OVERRIDE_COLOR : undefined }}
    >
      {prefab.custom ? "◆ " : ""}
      {prefab.name}
    </div>
  </div>
);

const textInputStyle: React.CSSProperties = {
  background: "rgba(0, 0, 0, 0.25)",
  color: "white",
  border: "1rem solid rgba(255, 255, 255, 0.2)",
  borderRadius: "3rem",
  padding: "2rem 6rem",
  fontSize: "13rem",
};

const miniBtnStyle: React.CSSProperties = {
  cursor: "pointer",
  marginLeft: "8rem",
  padding: "2rem 8rem",
  borderRadius: "3rem",
  border: "1rem solid rgba(255, 255, 255, 0.2)",
  fontSize: "12rem",
};

// Popup to assign to an existing class, create a new one, or unassign.
// New-class input and Unassign stay pinned at the top; the class list scrolls below.
const ClassDropdownButton = ({
  label,
  classes,
  onPick,
}: {
  label: string;
  classes: ClassInfo[];
  onPick: (className: string) => void;
}) => {
  const [open, setOpen] = useState(false);
  const [newName, setNewName] = useState("");
  const pick = (name: string) => {
    onPick(name);
    setOpen(false);
    setNewName("");
  };
  const addNew = () => {
    const t = newName.trim();
    if (t) pick(t);
  };

  const itemStyle: React.CSSProperties = { cursor: "pointer", padding: "5rem 10rem", fontSize: "13rem" };

  return (
    <div style={{ position: "relative" }}>
      <div
        onClick={() => setOpen(o => !o)}
        style={{
          cursor: "pointer",
          display: "flex",
          alignItems: "center",
          padding: "3rem 8rem",
          borderRadius: "3rem",
          border: "1rem solid rgba(255, 255, 255, 0.25)",
          fontSize: "13rem",
        }}
      >
        {label}
        <span style={{ marginLeft: "6rem", color: DIM_COLOR }}>▾</span>
      </div>
      {open && (
        <div
          style={{
            position: "absolute",
            top: "100%",
            right: 0,
            marginTop: "4rem",
            minWidth: "200rem",
            background: "rgba(22, 27, 34, 0.98)",
            border: "1rem solid rgba(255, 255, 255, 0.2)",
            borderRadius: "4rem",
            zIndex: 50,
          }}
        >
          <div
            style={{
              display: "flex",
              alignItems: "center",
              padding: "6rem",
              borderBottom: "1rem solid rgba(255, 255, 255, 0.12)",
            }}
          >
            <input
              type="text"
              value={newName}
              placeholder={"New class…"}
              onChange={e => setNewName(e.currentTarget.value)}
              onKeyDown={e => {
                if (e.key === "Enter") addNew();
              }}
              style={{ ...textInputStyle, flex: 1, textAlign: "left" }}
            />
            <div onClick={addNew} style={miniBtnStyle}>
              Add
            </div>
          </div>
          <div
            onClick={() => pick("")}
            style={{ ...itemStyle, color: DIM_COLOR, borderBottom: "1rem solid rgba(255, 255, 255, 0.12)" }}
          >
            Unassign
          </div>
          <div style={{ maxHeight: "180rem", overflowY: "auto" }}>
            {classes.map(c => (
              <div key={c.name} onClick={() => pick(c.name)} style={itemStyle}>
                {c.name}
                {c.custom ? <span style={{ color: DIM_COLOR, fontSize: "11rem" }}> · custom</span> : null}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

// Assign a single selected prefab to a class.
const ClassPicker = ({
  prefabId,
  current,
  classes,
}: {
  prefabId: string;
  current: string;
  classes: ClassInfo[];
}) => (
  <ClassDropdownButton
    label={current || "Unclassified"}
    classes={classes}
    onPick={name => sendAssign(prefabId, name)}
  />
);

// Rename / delete controls for a custom (pack) class.
const ClassAdminRow = ({ name }: { name: string }) => {
  const [text, setText] = useState(name);
  useEffect(() => setText(name), [name]);
  const doRename = () => {
    const t = text.trim();
    if (t && t !== name) sendRenameClass(name, t);
  };
  return (
    <div style={{ display: "flex", alignItems: "center", marginBottom: "10rem" }}>
      <input
        type="text"
        value={text}
        onChange={e => setText(e.currentTarget.value)}
        onKeyDown={e => {
          if (e.key === "Enter") e.currentTarget.blur();
        }}
        onBlur={doRename}
        style={{ ...textInputStyle, width: "150rem" }}
      />
      <div onClick={doRename} style={miniBtnStyle}>
        Rename
      </div>
      <div onClick={() => sendDeleteClass(name)} style={{ ...miniBtnStyle, color: "rgba(255, 140, 140, 1)" }}>
        Delete
      </div>
    </div>
  );
};

const DetailPanel = ({ prefab, classes }: { prefab: PrefabNode | null; classes: ClassInfo[] }) => {
  if (!prefab) {
    return (
      <div style={{ padding: "20rem", color: DIM_COLOR }}>
        Select a vehicle on the left to edit its spawn probability and properties.
      </div>
    );
  }

  const id = prefab.id;
  return (
    <div style={{ padding: "10rem 14rem" }}>
      <div style={{ fontSize: "15rem", marginBottom: "2rem" }}>{prefab.name}</div>
      <div style={{ fontSize: "12rem", color: DIM_COLOR, marginBottom: "12rem" }}>
        {prefab.className}
      </div>

      <PanelSectionRow
        disableFocus
        left={"Class"}
        right={<ClassPicker prefabId={id} current={prefab.className} classes={classes} />}
      />

      <EditRow
        label={"Spawn probability"}
        state={prefab.probability}
        editValue={Math.round(prefab.probability.value)}
        unit={"%"}
        onCommit={v => sendSet("prefab", id, "probability", v)}
        onReset={() => sendReset("prefab", id, "probability")}
      />

      <div style={{ color: DIM_COLOR, fontSize: "12rem", margin: "10rem 0 2rem" }}>Properties</div>
      <EditRow
        label={"Max speed"}
        state={prefab.maxSpeed}
        editValue={Math.round(prefab.maxSpeed.value)}
        unit={"km/h"}
        onCommit={kmh => sendSet("prefab", id, "maxSpeed", kmh / MS_TO_KMH)}
        onReset={() => sendReset("prefab", id, "maxSpeed")}
      />
      <EditRow
        label={"Acceleration"}
        state={prefab.acceleration}
        editValue={Number(prefab.acceleration.value.toFixed(1))}
        unit={"m/s²"}
        onCommit={v => sendSet("prefab", id, "acceleration", v)}
        onReset={() => sendReset("prefab", id, "acceleration")}
      />
      <EditRow
        label={"Braking"}
        state={prefab.braking}
        editValue={Number(prefab.braking.value.toFixed(1))}
        unit={"m/s²"}
        onCommit={v => sendSet("prefab", id, "braking", v)}
        onReset={() => sendReset("prefab", id, "braking")}
      />

      <div
        style={{
          marginTop: "12rem",
          padding: "8rem 10rem",
          borderRadius: "4rem",
          backgroundColor: "rgba(255, 255, 255, 0.06)",
          color: DIM_COLOR,
          fontSize: "12rem",
        }}
      >
        Unset values inherit from class {prefab.className}, then the global default.
      </div>
    </div>
  );
};

const ClassDetail = ({ cls }: { cls: ClassNode | null }) => {
  if (!cls) {
    return (
      <div style={{ padding: "20rem", color: DIM_COLOR }}>
        Select a class on the left to set values shared by all its vehicles.
      </div>
    );
  }
  if (cls.editable === false) {
    return (
      <div style={{ padding: "20rem", color: DIM_COLOR }}>
        The "{cls.name}" group has no shared class values. Edit individual vehicles instead.
      </div>
    );
  }

  const name = cls.name;
  const attr = (a?: AttrState): AttrState => a ?? { value: NaN, overridden: false, source: "" };
  const editVal = (a: AttrState, round: (n: number) => number) =>
    a.overridden ? round(a.value) : NaN;
  const prob = attr(cls.probability);
  const spd = attr(cls.maxSpeed);
  const acc = attr(cls.acceleration);
  const brk = attr(cls.braking);

  return (
    <div style={{ padding: "10rem 14rem" }}>
      <div style={{ fontSize: "15rem", marginBottom: "2rem" }}>{name}</div>
      <div style={{ fontSize: "12rem", color: DIM_COLOR, marginBottom: "12rem" }}>
        Class · applies to all members
      </div>

      {cls.custom ? <ClassAdminRow name={name} /> : null}

      <EditRow
        label={"Spawn probability"}
        state={prob}
        editValue={editVal(prob, Math.round)}
        unit={"%"}
        inheritedLabel={"Not set"}
        onCommit={v => sendSet("class", name, "probability", v)}
        onReset={() => sendReset("class", name, "probability")}
      />

      <div style={{ color: DIM_COLOR, fontSize: "12rem", margin: "10rem 0 2rem" }}>Properties</div>
      <EditRow
        label={"Max speed"}
        state={spd}
        editValue={editVal(spd, Math.round)}
        unit={"km/h"}
        inheritedLabel={"Not set"}
        onCommit={kmh => sendSet("class", name, "maxSpeed", kmh / MS_TO_KMH)}
        onReset={() => sendReset("class", name, "maxSpeed")}
      />
      <EditRow
        label={"Acceleration"}
        state={acc}
        editValue={editVal(acc, n => Number(n.toFixed(1)))}
        unit={"m/s²"}
        inheritedLabel={"Not set"}
        onCommit={v => sendSet("class", name, "acceleration", v)}
        onReset={() => sendReset("class", name, "acceleration")}
      />
      <EditRow
        label={"Braking"}
        state={brk}
        editValue={editVal(brk, n => Number(n.toFixed(1)))}
        unit={"m/s²"}
        inheritedLabel={"Not set"}
        onCommit={v => sendSet("class", name, "braking", v)}
        onReset={() => sendReset("class", name, "braking")}
      />

      <div
        style={{
          marginTop: "12rem",
          padding: "8rem 10rem",
          borderRadius: "4rem",
          backgroundColor: "rgba(255, 255, 255, 0.06)",
          color: DIM_COLOR,
          fontSize: "12rem",
        }}
      >
        These values apply to every {name} unless an individual vehicle overrides them.
      </div>
    </div>
  );
};

const Tree = ({
  tree,
  selectedPrefabId,
  selectedClassName,
  checkedIds,
  onSelectPrefab,
  onSelectClass,
  onToggleCheck,
}: {
  tree: CategoryNode[];
  selectedPrefabId: string | null;
  selectedClassName: string | null;
  checkedIds: Set<string>;
  onSelectPrefab: (id: string) => void;
  onSelectClass: (name: string) => void;
  onToggleCheck: (id: string) => void;
}) => (
  <>
    {tree.map(category => (
      <PanelFoldout
        key={category.key}
        initialExpanded={category.key === "cars"}
        expandFromContent={false}
        focusKey={FOCUS_DISABLED}
        header={
          <PanelSectionRow
            disableFocus
            uppercase
            left={
              <div style={{ display: "flex", alignItems: "center" }}>
                <Icon src={category.icon} />
                <span style={{ marginLeft: "6rem" }}>{category.name}</span>
              </div>
            }
          />
        }
      >
        {category.classes.map(cls => (
          <PanelFoldout
            key={cls.name}
            initialExpanded={cls.name === "Sedan"}
            expandFromContent={false}
            focusKey={FOCUS_DISABLED}
            header={
              <PanelSectionRow
                disableFocus
                subRow
                left={
                  <span style={{ color: cls.name === selectedClassName ? OVERRIDE_COLOR : undefined }}>
                    {cls.name}
                  </span>
                }
                right={
                  cls.editable !== false ? (
                    <div
                      onClick={e => {
                        e.stopPropagation();
                        onSelectClass(cls.name);
                      }}
                      style={{
                        cursor: "pointer",
                        color: cls.name === selectedClassName ? OVERRIDE_COLOR : DIM_COLOR,
                        fontSize: "12rem",
                        padding: "0 6rem",
                      }}
                    >
                      edit
                    </div>
                  ) : undefined
                }
              />
            }
          >
            {cls.prefabs.map(prefab => (
              <PrefabRow
                key={prefab.id}
                prefab={prefab}
                selected={prefab.id === selectedPrefabId}
                checked={checkedIds.has(prefab.id)}
                onSelect={() => onSelectPrefab(prefab.id)}
                onToggleCheck={() => onToggleCheck(prefab.id)}
              />
            ))}
          </PanelFoldout>
        ))}
      </PanelFoldout>
    ))}
  </>
);

export const VehicleManagerPanel = ({ onClose }: { onClose: () => void }) => {
  const [selection, setSelection] = useState<Selection | null>(null);
  const selectedPrefabId = selection?.kind === "prefab" ? selection.id : null;
  const selectedClassName = selection?.kind === "class" ? selection.name : null;

  // Multi-select for bulk class assignment (independent of the single edit selection).
  const [checkedIds, setCheckedIds] = useState<Set<string>>(new Set());
  const toggleCheck = (id: string) =>
    setCheckedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  const clearChecked = () => setCheckedIds(new Set());

  // Real tree from the backend; falls back to mock data when empty or unparsable.
  const treeJson = useValue(treeJson$);
  const tree = useMemo<CategoryNode[]>(() => {
    try {
      const parsed = JSON.parse(treeJson) as CategoryNode[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed.map(c => ({ ...c, icon: CATEGORY_ICONS[c.key] ?? "" }));
      }
    } catch (e) {
      // fall through to mock
    }
    return VEHICLE_TREE;
  }, [treeJson]);

  // Derive the selection from the current tree so it reflects edits after a refresh.
  const selectedPrefab = useMemo(() => findPrefab(tree, selectedPrefabId), [tree, selectedPrefabId]);
  const selectedClass = useMemo(() => findClass(tree, selectedClassName), [tree, selectedClassName]);

  const globalJson = useValue(globalJson$);
  const global = useMemo<GlobalFactors>(() => {
    try {
      const p = JSON.parse(globalJson);
      if (p && typeof p === "object") return { ...DEFAULT_GLOBAL, ...p };
    } catch (e) {
      // fall through to defaults
    }
    return DEFAULT_GLOBAL;
  }, [globalJson]);

  const classesJson = useValue(classesJson$);
  const classes = useMemo<ClassInfo[]>(() => {
    try {
      const p = JSON.parse(classesJson);
      if (Array.isArray(p)) return p as ClassInfo[];
    } catch (e) {
      // fall through to empty
    }
    return [];
  }, [classesJson]);

  // Ask the backend to (re)build the tree whenever the window mounts.
  useEffect(() => {
    trigger(MANAGER_GROUP, "refresh");
  }, []);

  const allPacks = useMemo(() => [...MOCK_PACKS.yours, ...MOCK_PACKS.shared], []);
  const [activePack] = useState(allPacks[0].name);

  return (
    <Panel
      draggable
      header={
        <div style={{ display: "flex", alignItems: "center" }}>
          <Icon src={packSrc} />
          <span style={{ marginLeft: "8rem" }}>Vehicle manager</span>
        </div>
      }
      onClose={onClose}
      initialPosition={{ x: 0.35, y: 0.2 }}
      style={{ width: "640rem" }}
    >
      {/* Pack bar (visual only for now) */}
      <PanelSection>
        <PanelSectionRow
          disableFocus
          left={"Pack"}
          right={
            <div style={{ display: "flex", alignItems: "center" }}>
              <Icon src={packSrc} />
              <span style={{ margin: "0 8rem" }}>{activePack}</span>
              <Button variant="flat" focusKey={FOCUS_DISABLED} onSelect={() => {}}>
                Export
              </Button>
              <Button variant="flat" focusKey={FOCUS_DISABLED} onSelect={() => {}}>
                Import
              </Button>
            </div>
          }
        />
      </PanelSection>

      {/* Search + category / scope chips (visual only) */}
      <div style={{ display: "flex", alignItems: "center", padding: "8rem 14rem", flexWrap: "wrap" }}>
        <Chip label={"All"} active />
        <Chip label={"Cars"} />
        <Chip label={"Trains"} />
        <Chip label={"Service"} />
        <span style={{ marginLeft: "12rem", color: DIM_COLOR, fontSize: "12rem" }}>Show</span>
        <Chip label={"In this pack"} />
      </div>

      <GlobalBar g={global} />

      {checkedIds.size > 0 && (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            padding: "6rem 14rem",
            backgroundColor: "rgba(140, 190, 255, 0.12)",
            borderTop: "1rem solid rgba(255, 255, 255, 0.08)",
          }}
        >
          <span style={{ fontSize: "12rem", color: OVERRIDE_COLOR }}>{checkedIds.size} selected</span>
          <span style={{ marginLeft: "10rem", marginRight: "8rem", fontSize: "12rem", color: DIM_COLOR }}>
            Assign to
          </span>
          <ClassDropdownButton
            label={"class"}
            classes={classes}
            onPick={name => {
              sendAssignMany([...checkedIds], name);
              clearChecked();
            }}
          />
          <div onClick={clearChecked} style={miniBtnStyle}>
            Clear
          </div>
        </div>
      )}

      {/* Body: tree (scrollable) + detail */}
      <div style={{ display: "flex" }}>
        <Scrollable
          vertical
          trackVisibility={"scrollable"}
          style={{ width: "260rem", maxHeight: "440rem", borderRight: "1rem solid rgba(255,255,255,0.1)" }}
        >
          <Tree
            tree={tree}
            selectedPrefabId={selectedPrefabId}
            selectedClassName={selectedClassName}
            checkedIds={checkedIds}
            onSelectPrefab={id => setSelection({ kind: "prefab", id })}
            onSelectClass={name => setSelection({ kind: "class", name })}
            onToggleCheck={toggleCheck}
          />
        </Scrollable>
        <div style={{ flex: 1, minWidth: "0" }}>
          {selection?.kind === "class" ? (
            <ClassDetail cls={selectedClass} />
          ) : (
            <DetailPanel prefab={selectedPrefab} classes={classes} />
          )}
        </div>
      </div>

      {/* Status line */}
      <PanelSection>
        <PanelSectionRow
          disableFocus
          left={`Changes apply live and auto-save to "${activePack}".`}
        />
      </PanelSection>
    </Panel>
  );
};
