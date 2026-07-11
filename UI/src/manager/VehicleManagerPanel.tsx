import { bindValue, trigger, useValue } from "cs2/api";
import { FOCUS_DISABLED } from "cs2/input";
// PanelSection/PanelSectionRow/PanelFoldout are the runtime-safe *alias* exports
// of the game's InfoSection/InfoRow/InfoSectionFoldout (see ui.d.ts). Importing
// InfoRow/InfoSection directly from cs2/ui fails at runtime, the aliases work.
import { Dropdown, DropdownToggle, Icon, Panel, PanelFoldout, PanelSection, PanelSectionRow, Scrollable } from "cs2/ui";
import * as CS2UI from "cs2/ui";
import React, { useEffect, useMemo, useRef, useState } from "react";

import { ModuleResolver } from "../ModuleResolver";
import { AttrState, CategoryNode, ClassNode, PrefabNode, VEHICLE_TREE } from "./data";
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
const packsJson$ = bindValue<string>(MANAGER_GROUP, "packsJson", "{}");

interface ClassInfo {
  name: string;
  custom: boolean;
}

interface PacksInfo {
  active: string;
  packs: string[];
}

interface GlobalFactors {
  probability: number;
  speed: number;
  acceleration: number;
  braking: number;
}
const DEFAULT_GLOBAL: GlobalFactors = { probability: 1, speed: 1, acceleration: 1, braking: 1 };

// UI → C#: edit commands. Class/global use `key`; prefab edits carry a `prefabs` array so one
// command edits the whole selection at once.
type EditLevel = "class" | "global";
const sendSet = (level: EditLevel, key: string, field: string, value: number) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "set", level, key, field, value }));
const sendReset = (level: EditLevel, key: string, field: string) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "reset", level, key, field }));
const sendEditMany = (prefabs: string[], field: string, value: number) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "set", level: "prefab", prefabs, field, value }));
const sendResetMany = (prefabs: string[], field: string) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "reset", level: "prefab", prefabs, field }));

// UI → C#: class management. Empty class name = unassign.
const sendAssignMany = (prefabs: string[], className: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "assignMany", prefabs, class: className }));
const sendRenameClass = (className: string, newName: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "rename", class: className, newName }));
const sendDeleteClass = (className: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "delete", class: className }));

// UI → C#: pack bar (switch / new / duplicate / rename / delete / export / import).
const sendPackCmd = (op: string, name: string) =>
  trigger(MANAGER_GROUP, "packCmd", JSON.stringify({ op, name }));

// UI → C#: move the camera to follow a live instance of this prefab.
const sendJumpTo = (prefab: string) => trigger(MANAGER_GROUP, "jumpTo", prefab);

// Play the game's standard UI sounds (so our controls feel like native ones).
const playClick = () => trigger("audio", "playSound", "select-item", 1);
const playHover = () => trigger("audio", "playSound", "hover-item", 1);

const MS_TO_KMH = 3.6;

const CATEGORY_ICONS: Record<string, string> = {
  cars: carIcon,
  trains: trainIcon,
  service: ambulanceIcon,
};

const resetSrc = "coui://uil/Standard/Reset.svg";

const OVERRIDE_COLOR = "rgba(140, 190, 255, 1)";
const DIM_COLOR = "rgba(255, 255, 255, 0.5)";

function findClass(tree: CategoryNode[], name: string | null): ClassNode | null {
  if (!name) return null;
  for (const c of tree)
    for (const cls of c.classes)
      if (cls.name === name) return cls;
  return null;
}

const classHasSelected = (cls: ClassNode, ids: Set<string>): boolean =>
  cls.prefabs.some(p => ids.has(p.id));
const categoryHasSelected = (cat: CategoryNode, ids: Set<string>): boolean =>
  cat.classes.some(cls => classHasSelected(cls, ids));

// A prefab is "in this pack" if it has a prefab-level override or belongs to a custom class.
const isInPack = (p: PrefabNode, customClasses: Set<string>): boolean =>
  p.probability.overridden ||
  p.maxSpeed.overridden ||
  p.acceleration.overridden ||
  p.braking.overridden ||
  customClasses.has(p.className);

// Filter the tree by search text and "in this pack" scope. Returns the full tree (same reference)
// when nothing is filtered so foldout state is preserved. Categories stay as the tree's grouping.
function filterTree(
  tree: CategoryNode[],
  search: string,
  inPackOnly: boolean,
  customClasses: Set<string>
): CategoryNode[] {
  const q = search.trim().toLowerCase();
  if (!q && !inPackOnly) return tree;
  const matchPrefab = (p: PrefabNode) => {
    if (
      q &&
      !p.name.toLowerCase().includes(q) &&
      !p.id.toLowerCase().includes(q) &&
      !p.className.toLowerCase().includes(q)
    )
      return false;
    if (inPackOnly && !isInPack(p, customClasses)) return false;
    return true;
  };
  return tree
    .map(c => ({
      ...c,
      classes: c.classes
        .map(cls => ({ ...cls, prefabs: cls.prefabs.filter(matchPrefab) }))
        .filter(cls => cls.prefabs.length > 0),
    }))
    .filter(c => c.classes.length > 0);
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
// A non-finite value renders as an empty field (e.g. when a multi-selection has mixed values).
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
  canReset,
}: {
  label: string;
  state: AttrState;
  editValue: number;
  unit: string;
  onCommit: (displayValue: number) => void;
  onReset: () => void;
  inheritedLabel?: string;
  canReset?: boolean;
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
            disabled={!(canReset ?? state.overridden)}
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

const Chip = ({ label, active, onClick }: { label: string; active?: boolean; onClick?: () => void }) => (
  <span
    onClick={onClick ? () => { playClick(); onClick(); } : undefined}
    style={{
      fontSize: "12rem",
      padding: "4rem 10rem",
      marginLeft: "6rem",
      borderRadius: "20rem",
      cursor: onClick ? "pointer" : undefined,
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
  showInternal,
  onSelect,
}: {
  prefab: PrefabNode;
  selected: boolean;
  showInternal: boolean;
  onSelect: (additive: boolean, range: boolean) => void;
}) => {
  const ref = useRef<HTMLDivElement>(null);
  // Scroll the selected row into view (e.g. after a deep-link from a vehicle's info panel).
  useEffect(() => {
    if (selected && ref.current) {
      try {
        ref.current.scrollIntoView({ block: "nearest" });
      } catch (e) {
        // scrollIntoView may be unsupported; ignore
      }
    }
  }, [selected]);
  return (
    <div
      ref={ref}
      onClick={e => {
        playClick();
        onSelect(e.ctrlKey || e.metaKey, e.shiftKey);
      }}
      style={{
        padding: "6rem 10rem 6rem 34rem",
        cursor: "pointer",
        color: selected ? OVERRIDE_COLOR : undefined,
        backgroundColor: selected ? "rgba(140, 190, 255, 0.15)" : undefined,
        borderLeft: selected ? "3rem solid " + OVERRIDE_COLOR : "3rem solid transparent",
      }}
    >
      {prefab.custom ? "◆ " : ""}
      {showInternal ? prefab.id : prefab.name}
    </div>
  );
};

const textInputStyle: React.CSSProperties = {
  background: "rgba(0, 0, 0, 0.25)",
  color: "white",
  border: "1rem solid rgba(255, 255, 255, 0.2)",
  borderRadius: "3rem",
  padding: "2rem 6rem",
  fontSize: "13rem",
};

// Small text button with hover highlight + native click/hover sounds.
const TxtButton = ({
  children,
  onClick,
  danger,
}: {
  children: React.ReactNode;
  onClick: () => void;
  danger?: boolean;
}) => {
  const [hover, setHover] = useState(false);
  return (
    <div
      onClick={() => {
        playClick();
        onClick();
      }}
      onMouseEnter={() => {
        setHover(true);
        playHover();
      }}
      onMouseLeave={() => setHover(false)}
      style={{
        cursor: "pointer",
        marginLeft: "6rem",
        padding: "3rem 9rem",
        borderRadius: "4rem",
        fontSize: "12rem",
        color: danger ? "rgba(255, 140, 140, 1)" : "white",
        border: "1rem solid rgba(255, 255, 255, 0.25)",
        backgroundColor: hover ? "rgba(255, 255, 255, 0.14)" : "rgba(255, 255, 255, 0.04)",
      }}
    >
      {children}
    </div>
  );
};

// The `DropdownItem` name in cs2/ui's types is an interface; the runtime component is only
// reachable via the module namespace, so grab it as a value here.
const DropdownItemComp: any = (CS2UI as any).DropdownItem;

// Class items for the assign dropdowns: Unassign, then custom classes first, then built-in.
const classItems = (classes: ClassInfo[]) => [
  { value: "", label: "Unassign" },
  ...classes.filter(c => c.custom).map(c => ({ value: c.name, label: c.name + " · custom" })),
  ...classes.filter(c => !c.custom).map(c => ({ value: c.name, label: c.name })),
];

// Reusable vanilla dropdown built from the game's Dropdown/DropdownToggle/DropdownItem + theme.
const VDropdown = ({
  value,
  items,
  onSelect,
  toggleLabel,
}: {
  value: string;
  items: { value: string; label: string }[];
  onSelect: (v: string) => void;
  toggleLabel?: string;
}) => {
  const theme = ModuleResolver.instance.DropdownClasses;
  return (
    <Dropdown
      focusKey={ModuleResolver.instance.FOCUS_DISABLED}
      theme={theme}
      content={items.map(it => (
        <DropdownItemComp
          key={it.value}
          theme={theme}
          value={it.value}
          selected={it.value === value}
          closeOnSelect
          onChange={() => onSelect(it.value)}
        >
          {it.label}
        </DropdownItemComp>
      ))}
    >
      <DropdownToggle theme={theme}>
        {toggleLabel ?? items.find(i => i.value === value)?.label ?? value}
      </DropdownToggle>
    </Dropdown>
  );
};

// Assign the selected vehicle(s) to a class (existing / new / unassign).
const ClassPicker = ({
  ids,
  current,
  classes,
  toggleLabel,
}: {
  ids: string[];
  current: string;
  classes: ClassInfo[];
  toggleLabel?: string;
}) => {
  const [newOpen, setNewOpen] = useState(false);
  const [newName, setNewName] = useState("");
  const addNew = () => {
    const t = newName.trim();
    if (t) {
      sendAssignMany(ids, t);
      setNewName("");
      setNewOpen(false);
    }
  };
  return (
    <div style={{ display: "flex", alignItems: "center" }}>
      <VDropdown value={current} toggleLabel={toggleLabel} items={classItems(classes)} onSelect={name => sendAssignMany(ids, name)} />
      <TxtButton onClick={() => setNewOpen(o => !o)}>+ New</TxtButton>
      {newOpen && (
        <input
          type="text"
          value={newName}
          placeholder={"New class"}
          onChange={e => setNewName(e.currentTarget.value)}
          onKeyDown={e => {
            if (e.key === "Enter") addNew();
            if (e.key === "Escape") setNewOpen(false);
          }}
          style={{ ...textInputStyle, marginLeft: "6rem", width: "120rem", textAlign: "left" }}
        />
      )}
    </div>
  );
};

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
      <TxtButton onClick={doRename}>Rename</TxtButton>
      <TxtButton onClick={() => sendDeleteClass(name)} danger>
        Delete
      </TxtButton>
    </div>
  );
};

// Detail panel for the current selection of one *or many* vehicles. Editing applies to all of them.
const DetailPanel = ({
  prefabs,
  classes,
  showInternal,
}: {
  prefabs: PrefabNode[];
  classes: ClassInfo[];
  showInternal: boolean;
}) => {
  if (prefabs.length === 0) {
    return (
      <div style={{ padding: "20rem", color: DIM_COLOR }}>
        Select a vehicle on the left. Hold Ctrl to select several and edit them together.
      </div>
    );
  }

  const ids = prefabs.map(p => p.id);
  const multi = prefabs.length > 1;

  // Combine a field across the selection: shared value or blank (mixed), and combined provenance.
  const combine = (get: (p: PrefabNode) => AttrState) => {
    const attrs = prefabs.map(get);
    const allSameVal = attrs.every(a => a.value === attrs[0].value);
    const allOver = attrs.every(a => a.overridden);
    const noneOver = attrs.every(a => !a.overridden);
    return {
      state: { value: allSameVal ? attrs[0].value : NaN, overridden: allOver, source: "" } as AttrState,
      inheritedLabel: noneOver ? "Inherited" : "Mixed",
      anyOver: attrs.some(a => a.overridden),
    };
  };

  const prob = combine(p => p.probability);
  const spd = combine(p => p.maxSpeed);
  const acc = combine(p => p.acceleration);
  const brk = combine(p => p.braking);

  const allSameClass = prefabs.every(p => p.className === prefabs[0].className);
  const currentClass = allSameClass ? prefabs[0].className : " ";
  const classToggleLabel = allSameClass ? undefined : "Mixed";
  const round0 = (a: AttrState) => (Number.isFinite(a.value) ? Math.round(a.value) : NaN);
  const round1 = (a: AttrState) => (Number.isFinite(a.value) ? Number(a.value.toFixed(1)) : NaN);

  return (
    <div style={{ padding: "10rem 14rem" }}>
      {multi ? (
        <div style={{ marginBottom: "12rem" }}>
          <div style={{ fontSize: "15rem", marginBottom: "6rem" }}>{prefabs.length} vehicles selected</div>
          <div style={{ display: "flex", flexWrap: "wrap" }}>
            {prefabs.slice(0, 16).map(p =>
              p.thumbnail ? (
                <img
                  key={p.id}
                  src={p.thumbnail}
                  style={{ width: "40rem", height: "40rem", marginRight: "4rem", marginBottom: "4rem", objectFit: "contain" }}
                />
              ) : null
            )}
          </div>
        </div>
      ) : (
        <div style={{ display: "flex", alignItems: "center", marginBottom: "12rem" }}>
          {prefabs[0].thumbnail ? (
            <img
              src={prefabs[0].thumbnail}
              style={{ width: "90rem", height: "90rem", marginRight: "12rem", objectFit: "contain" }}
            />
          ) : null}
          <div>
            <div style={{ fontSize: "15rem" }}>{showInternal ? prefabs[0].id : prefabs[0].name}</div>
            <div style={{ fontSize: "12rem", color: DIM_COLOR }}>{prefabs[0].className}</div>
          </div>
        </div>
      )}

      {!multi && (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            marginBottom: "10rem",
          }}
        >
          <span style={{ fontSize: "12rem", color: DIM_COLOR }}>
            {(prefabs[0].count ?? 0) === 1
              ? "1 vehicle active in city"
              : `${prefabs[0].count ?? 0} vehicles active in city`}
          </span>
          {(prefabs[0].count ?? 0) > 0 && (
            <TxtButton onClick={() => sendJumpTo(prefabs[0].id)}>Jump to instance</TxtButton>
          )}
        </div>
      )}

      <PanelSectionRow
        disableFocus
        left={"Class"}
        right={<ClassPicker ids={ids} current={currentClass} toggleLabel={classToggleLabel} classes={classes} />}
      />

      <EditRow
        label={"Spawn probability"}
        state={prob.state}
        editValue={round0(prob.state)}
        unit={"%"}
        inheritedLabel={prob.inheritedLabel}
        canReset={prob.anyOver}
        onCommit={v => sendEditMany(ids, "probability", v)}
        onReset={() => sendResetMany(ids, "probability")}
      />

      <div style={{ color: DIM_COLOR, fontSize: "12rem", margin: "10rem 0 2rem" }}>Properties</div>
      <EditRow
        label={"Max speed"}
        state={spd.state}
        editValue={round0(spd.state)}
        unit={"km/h"}
        inheritedLabel={spd.inheritedLabel}
        canReset={spd.anyOver}
        onCommit={kmh => sendEditMany(ids, "maxSpeed", kmh / MS_TO_KMH)}
        onReset={() => sendResetMany(ids, "maxSpeed")}
      />
      <EditRow
        label={"Acceleration"}
        state={acc.state}
        editValue={round1(acc.state)}
        unit={"m/s²"}
        inheritedLabel={acc.inheritedLabel}
        canReset={acc.anyOver}
        onCommit={v => sendEditMany(ids, "acceleration", v)}
        onReset={() => sendResetMany(ids, "acceleration")}
      />
      <EditRow
        label={"Braking"}
        state={brk.state}
        editValue={round1(brk.state)}
        unit={"m/s²"}
        inheritedLabel={brk.inheritedLabel}
        canReset={brk.anyOver}
        onCommit={v => sendEditMany(ids, "braking", v)}
        onReset={() => sendResetMany(ids, "braking")}
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
        {multi
          ? "Edits apply to all selected vehicles."
          : `Unset values inherit from class ${prefabs[0].className}, then the global default.`}
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
  const editVal = (a: AttrState, round: (n: number) => number) => (a.overridden ? round(a.value) : NaN);
  const prob = attr(cls.probability);
  const spd = attr(cls.maxSpeed);
  const acc = attr(cls.acceleration);
  const brk = attr(cls.braking);

  return (
    <div style={{ padding: "10rem 14rem" }}>
      <div style={{ fontSize: "15rem", marginBottom: "2rem" }}>{name}</div>
      <div style={{ fontSize: "12rem", color: DIM_COLOR, marginBottom: "12rem" }}>Class · applies to all members</div>

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
  selectedIds,
  selectedClassName,
  expandAll,
  filterKey,
  showInternal,
  onSelectPrefab,
  onSelectClass,
}: {
  tree: CategoryNode[];
  selectedIds: Set<string>;
  selectedClassName: string | null;
  expandAll: boolean;
  filterKey: string;
  showInternal: boolean;
  onSelectPrefab: (id: string, additive: boolean, range: boolean) => void;
  onSelectClass: (name: string) => void;
}) => (
  <>
    {tree.map(category => (
      <PanelFoldout
        key={category.key + filterKey}
        initialExpanded={expandAll || categoryHasSelected(category, selectedIds) || category.key === "cars"}
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
            key={cls.name + filterKey}
            initialExpanded={expandAll || classHasSelected(cls, selectedIds) || cls.name === "Sedan"}
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
                        playClick();
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
                selected={selectedIds.has(prefab.id)}
                showInternal={showInternal}
                onSelect={(additive, range) => onSelectPrefab(prefab.id, additive, range)}
              />
            ))}
          </PanelFoldout>
        ))}
      </PanelFoldout>
    ))}
  </>
);

// Pack bar: switch active pack, create/duplicate/rename/delete, export/import (clipboard).
const PackBar = ({ active, packs }: { active: string; packs: string[] }) => {
  const [prompt, setPrompt] = useState<{ op: "new" | "duplicate" | "rename"; value: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const startPrompt = (op: "new" | "duplicate" | "rename", value: string) => {
    setConfirmDelete(false);
    setError(null);
    setPrompt({ op, value });
  };
  const cancelPrompt = () => {
    setPrompt(null);
    setError(null);
  };
  const confirmPrompt = () => {
    if (!prompt) return;
    const v = prompt.value.trim();
    if (!v) return;
    // Renaming to the same name is a no-op.
    if (prompt.op === "rename" && v === active) {
      cancelPrompt();
      return;
    }
    // Guard against silently overwriting an existing pack.
    if (packs.includes(v)) {
      setError(`A pack named "${v}" already exists.`);
      return;
    }
    sendPackCmd(prompt.op, v);
    cancelPrompt();
  };
  const btn = (label: string, onClick: () => void, danger?: boolean) => (
    <TxtButton onClick={onClick} danger={danger}>
      {label}
    </TxtButton>
  );

  return (
    <PanelSection>
      <PanelSectionRow
        disableFocus
        left={"Pack"}
        right={
          <div style={{ display: "flex", alignItems: "center", flexWrap: "wrap" }}>
            <VDropdown
              value={active}
              items={packs.map(p => ({ value: p, label: p }))}
              onSelect={p => sendPackCmd("switch", p)}
            />
            {btn("New", () => startPrompt("new", ""))}
            {btn("Duplicate", () => startPrompt("duplicate", active + " copy"))}
            {btn("Rename", () => startPrompt("rename", active))}
            {btn("Delete", () => {
              cancelPrompt();
              setConfirmDelete(true);
            }, true)}
            {btn("Export", () => sendPackCmd("export", ""))}
            {btn("Import", () => sendPackCmd("import", ""))}
          </div>
        }
      />
      {prompt && (
        <div style={{ padding: "2rem 14rem 8rem" }}>
          <div style={{ display: "flex", alignItems: "center" }}>
            <input
              type="text"
              value={prompt.value}
              placeholder={"Pack name"}
              onChange={e => {
                setPrompt({ ...prompt, value: e.currentTarget.value });
                setError(null);
              }}
              onKeyDown={e => {
                if (e.key === "Enter") confirmPrompt();
                if (e.key === "Escape") cancelPrompt();
              }}
              style={{ ...textInputStyle, flex: 1, textAlign: "left" }}
            />
            {btn("OK", confirmPrompt)}
            {btn("Cancel", cancelPrompt)}
          </div>
          {error && (
            <div style={{ marginTop: "4rem", fontSize: "12rem", color: "rgba(255, 140, 140, 1)" }}>{error}</div>
          )}
        </div>
      )}
      {confirmDelete && (
        <div style={{ display: "flex", alignItems: "center", padding: "2rem 14rem 8rem" }}>
          <span style={{ fontSize: "13rem" }}>Delete pack "{active}"?</span>
          {btn("Delete", () => {
            sendPackCmd("delete", active);
            setConfirmDelete(false);
          }, true)}
          {btn("Cancel", () => setConfirmDelete(false))}
        </div>
      )}
    </PanelSection>
  );
};

export const VehicleManagerPanel = ({
  onClose,
  focusPrefab,
}: {
  onClose: () => void;
  focusPrefab?: string | null;
}) => {
  // Multi-select of prefabs (click = replace, Ctrl/Cmd-click = toggle). Class selection is separate.
  const [selectedIds, setSelectedIds] = useState<Set<string>>(focusPrefab ? new Set([focusPrefab]) : new Set());
  const [selectedClassName, setSelectedClassName] = useState<string | null>(null);

  // Tree filters.
  const [search, setSearch] = useState("");
  const [inPackOnly, setInPackOnly] = useState(false);
  // Show internal prefab ids instead of localized names (many vehicles share a display name).
  const [showInternal, setShowInternal] = useState(false);


  const anchorRef = useRef<string | null>(null);
  const selectPrefab = (id: string, additive: boolean, range: boolean) => {
    setSelectedClassName(null);
    // Shift: select the contiguous range from the anchor to this row (in the visible order).
    if (range && anchorRef.current) {
      const a = orderedIds.indexOf(anchorRef.current);
      const b = orderedIds.indexOf(id);
      if (a !== -1 && b !== -1) {
        const [lo, hi] = a <= b ? [a, b] : [b, a];
        setSelectedIds(new Set(orderedIds.slice(lo, hi + 1)));
        return; // keep the anchor for further shift-clicks
      }
    }
    if (additive) {
      setSelectedIds(prev => {
        const next = new Set(prev);
        if (next.has(id)) next.delete(id);
        else next.add(id);
        return next;
      });
      anchorRef.current = id;
      return;
    }
    setSelectedIds(new Set([id]));
    anchorRef.current = id;
  };
  const selectClass = (name: string) => {
    setSelectedIds(new Set());
    setSelectedClassName(name);
  };

  // Select the prefab requested from a vehicle's info panel (deep-link).
  useEffect(() => {
    if (focusPrefab) {
      setSelectedIds(new Set([focusPrefab]));
      setSelectedClassName(null);
    }
  }, [focusPrefab]);

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
  const selectedPrefabs = useMemo<PrefabNode[]>(() => {
    const out: PrefabNode[] = [];
    for (const c of tree) for (const cls of c.classes) for (const p of cls.prefabs) if (selectedIds.has(p.id)) out.push(p);
    return out;
  }, [tree, selectedIds]);
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

  const customClasses = useMemo(() => new Set(classes.filter(c => c.custom).map(c => c.name)), [classes]);
  const filteredTree = useMemo(
    () => filterTree(tree, search, inPackOnly, customClasses),
    [tree, search, inPackOnly, customClasses]
  );
  const filterActive = search.trim() !== "" || inPackOnly;
  const filterKey = filterActive ? `${search}|${inPackOnly}` : "";

  // Flat visible order of prefab ids, for shift-range selection.
  const orderedIds = useMemo(() => {
    const out: string[] = [];
    for (const c of filteredTree) for (const cls of c.classes) for (const p of cls.prefabs) out.push(p.id);
    return out;
  }, [filteredTree]);

  const packsJson = useValue(packsJson$);
  const packInfo = useMemo<PacksInfo>(() => {
    try {
      const p = JSON.parse(packsJson);
      if (p && typeof p === "object" && Array.isArray(p.packs))
        return { active: p.active ?? "Default", packs: p.packs };
    } catch (e) {
      // fall through to default
    }
    return { active: "Default", packs: ["Default"] };
  }, [packsJson]);

  // Ask the backend to (re)build the tree whenever the window mounts.
  useEffect(() => {
    trigger(MANAGER_GROUP, "refresh");
  }, []);

  return (
    <Panel
      draggable
      header={<span>Vehicle manager</span>}
      onClose={onClose}
      initialPosition={{ x: 0.3, y: 0.15 }}
      style={{ width: "760rem" }}
    >
      <PackBar active={packInfo.active} packs={packInfo.packs} />

      {/* Search + scope filter (categories are the tree's top-level grouping). */}
      <div style={{ display: "flex", alignItems: "center", padding: "8rem 14rem", flexWrap: "wrap" }}>
        <input
          type="text"
          value={search}
          placeholder={"Search vehicles"}
          onChange={e => setSearch(e.currentTarget.value)}
          style={{ ...textInputStyle, width: "180rem", textAlign: "left" }}
        />
        <span style={{ marginLeft: "12rem", color: DIM_COLOR, fontSize: "12rem" }}>Show</span>
        <Chip label={"In this pack"} active={inPackOnly} onClick={() => setInPackOnly(v => !v)} />
        <Chip label={"Internal names"} active={showInternal} onClick={() => setShowInternal(v => !v)} />
      </div>

      <GlobalBar g={global} />

      {/* Body: tree (scrollable) + detail (scrollable) */}
      <div style={{ display: "flex" }}>
        <Scrollable
          vertical
          trackVisibility={"scrollable"}
          style={{ width: "260rem", maxHeight: "480rem", borderRight: "1rem solid rgba(255,255,255,0.1)" }}
        >
          <Tree
            tree={filteredTree}
            selectedIds={selectedIds}
            selectedClassName={selectedClassName}
            expandAll={filterActive}
            filterKey={filterKey}
            showInternal={showInternal}
            onSelectPrefab={selectPrefab}
            onSelectClass={selectClass}
          />
        </Scrollable>
        <Scrollable
          vertical
          trackVisibility={"scrollable"}
          style={{ flex: 1, minWidth: "0", maxHeight: "480rem" }}
        >
          {selectedClassName ? (
            <ClassDetail cls={selectedClass} />
          ) : (
            <DetailPanel prefabs={selectedPrefabs} classes={classes} showInternal={showInternal} />
          )}
        </Scrollable>
      </div>

      {/* Status line */}
      <PanelSection>
        <PanelSectionRow
          disableFocus
          left={`Changes apply live and auto-save to "${packInfo.active}".`}
        />
      </PanelSection>
    </Panel>
  );
};
