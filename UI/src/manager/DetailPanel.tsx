// Right column: the value editor.
//
// Each property is one row of the cascade — the editing pack's value for this vehicle, its value
// for the class, and vanilla. The ringed cell is the one that currently wins, so provenance is
// read off the table instead of being described in a label.
//
// Layout is flexbox with fixed cell widths: Gameface does not support CSS grid.

import { PanelSectionRow } from "cs2/ui";
import React, { useEffect, useState } from "react";
import { useValue } from "cs2/api";

import {
  assignClass,
  clearClassValue,
  clearPrefabValue,
  deleteClass,
  jumpToInstance,
  renameClass,
  requestCount,
  selectedCount$,
  setClassValue,
  setPrefabValue,
} from "./api";
import { NumberCell, TextInput, TxtButton, VDropdown } from "./controls";
import { ClassInfo, ClassNode, FieldState, PrefabNode } from "./data";
import { ACCENT, CELL_WIDTH, DIM, HAIRLINE, MS_TO_KMH, VANILLA_WIDTH } from "./theme";

/** Definition of one editable property, shared by the row renderer and the merge logic. */
interface PropertySpec {
  label: string;
  unit: string;
  /** Field name understood by the C# side. */
  key: string;
  decimals: number;
  /** Converts the displayed value into the internal unit the engine stores. */
  toInternal: (displayed: number) => number;
  get: (p: PrefabNode) => FieldState;
}

const PROPERTIES: PropertySpec[] = [
  { label: "Spawn chance", unit: "%", key: "probability", decimals: 0, toInternal: v => v, get: p => p.probability },
  { label: "Max speed", unit: "km/h", key: "maxSpeed", decimals: 0, toInternal: v => v / MS_TO_KMH, get: p => p.maxSpeed },
  { label: "Acceleration", unit: "m/s²", key: "acceleration", decimals: 1, toInternal: v => v, get: p => p.acceleration },
  { label: "Braking", unit: "m/s²", key: "braking", decimals: 1, toInternal: v => v, get: p => p.braking },
];

/** A property merged across the current selection; disagreeing cells render empty. */
interface MergedProperty {
  spec: PropertySpec;
  field: FieldState;
  mixedOwn: boolean;
  mixedCls: boolean;
}

function mergeProperty(spec: PropertySpec, prefabs: PrefabNode[]): MergedProperty {
  const states = prefabs.map(spec.get);
  const first = states[0];
  const sameOwn = states.every(s => s.own === first.own);
  const sameCls = states.every(s => s.cls === first.cls);
  const sameWinner = states.every(s => s.winner === first.winner);
  return {
    spec,
    field: {
      own: sameOwn ? first.own : null,
      cls: sameCls ? first.cls : null,
      vanilla: first.vanilla,
      effective: first.effective,
      // With disagreeing winners nothing is ringed: "pack" rings neither editable cell.
      winner: sameWinner ? first.winner : "pack",
      source: first.source,
    },
    mixedOwn: !sameOwn,
    mixedCls: !sameCls,
  };
}

/** Shared column geometry so header and rows line up. */
const cellStyle: React.CSSProperties = { flexShrink: 0, width: CELL_WIDTH, marginLeft: "6rem" };
const vanillaStyle: React.CSSProperties = {
  flexShrink: 0,
  width: VANILLA_WIDTH,
  marginLeft: "6rem",
  textAlign: "right",
  fontSize: "12rem",
  color: DIM,
};

const CascadeRow = ({
  merged,
  ids,
  className,
  classEditable,
}: {
  merged: MergedProperty;
  ids: string[];
  className: string | null;
  classEditable: boolean;
}) => {
  const { spec, field } = merged;
  const round = (v: number | null) => (v === null ? null : Number(v.toFixed(spec.decimals)));

  return (
    <div style={{ display: "flex", alignItems: "center", padding: "4rem 0" }}>
      <span style={{ flex: 1, minWidth: 0, fontSize: "13rem" }}>
        {spec.label}
        <span style={{ color: DIM, fontSize: "11rem", marginLeft: "4rem" }}>{spec.unit}</span>
      </span>
      <div style={cellStyle}>
        <NumberCell
          value={merged.mixedOwn ? null : round(field.own)}
          winning={field.winner === "own"}
          onCommit={v => setPrefabValue(ids, spec.key, spec.toInternal(v))}
          onClear={() => clearPrefabValue(ids, spec.key)}
        />
      </div>
      <div style={cellStyle}>
        <NumberCell
          value={merged.mixedCls ? null : round(field.cls)}
          winning={field.winner === "class"}
          disabled={!classEditable || !className}
          onCommit={v => className && setClassValue(className, spec.key, spec.toInternal(v))}
          onClear={() => className && clearClassValue(className, spec.key)}
        />
      </div>
      <span style={vanillaStyle}>{Number(field.vanilla.toFixed(spec.decimals))}</span>
    </div>
  );
};

/** Assigns the selection to a class (existing, new, or none). */
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
  const [adding, setAdding] = useState(false);
  const [name, setName] = useState("");

  const items = [
    { value: "", label: "Unassign" },
    ...classes.filter(c => c.custom).map(c => ({ value: c.name, label: c.name + " · custom" })),
    ...classes.filter(c => !c.custom).map(c => ({ value: c.name, label: c.name })),
  ];

  const commit = () => {
    const t = name.trim();
    if (t) {
      assignClass(ids, t);
      setName("");
      setAdding(false);
    }
  };

  return (
    <div style={{ display: "flex", alignItems: "center" }}>
      <VDropdown value={current} toggleLabel={toggleLabel} items={items} onSelect={n => assignClass(ids, n)} />
      <div style={{ marginLeft: "6rem" }}>
        <TxtButton onClick={() => setAdding(o => !o)}>+ New</TxtButton>
      </div>
      {adding && (
        <div style={{ marginLeft: "2rem", width: "110rem" }}>
          <TextInput
            value={name}
            placeholder={"New class"}
            onChange={setName}
            onSubmit={commit}
            onCancel={() => setAdding(false)}
          />
        </div>
      )}
    </div>
  );
};

const SelectionHeader = ({
  prefabs,
  showInternal,
}: {
  prefabs: PrefabNode[];
  showInternal: boolean;
}) => {
  const liveCount = useValue(selectedCount$);
  const single = prefabs.length === 1 ? prefabs[0] : null;

  useEffect(() => {
    if (single) requestCount(single.id);
  }, [single ? single.id : null]);

  if (!single) {
    return (
      <div style={{ marginBottom: "10rem" }}>
        <div style={{ fontSize: "15rem", marginBottom: "6rem" }}>{prefabs.length} vehicles selected</div>
        <div style={{ display: "flex", flexWrap: "wrap" }}>
          {prefabs.slice(0, 14).map(p =>
            p.thumbnail ? (
              <img
                key={p.id}
                src={p.thumbnail}
                style={{ width: "32rem", height: "32rem", marginRight: "4rem", marginBottom: "4rem", objectFit: "contain" }}
              />
            ) : null
          )}
        </div>
      </div>
    );
  }

  return (
    <div style={{ display: "flex", alignItems: "center", marginBottom: "10rem" }}>
      {single.thumbnail ? (
        <img
          src={single.thumbnail}
          style={{ flexShrink: 0, width: "64rem", height: "64rem", marginRight: "10rem", objectFit: "contain" }}
        />
      ) : null}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: "15rem" }}>{showInternal ? single.id : single.name}</div>
        <div style={{ fontSize: "12rem", color: DIM, marginBottom: "4rem" }}>
          {liveCount === 1 ? "1 in the city" : `${liveCount} in the city`}
        </div>
        {liveCount > 0 && (
          <div style={{ display: "flex" }}>
            <TxtButton onClick={() => jumpToInstance(single.id)}>Jump to one</TxtButton>
          </div>
        )}
      </div>
    </div>
  );
};

export const DetailPanel = ({
  prefabs,
  classes,
  showInternal,
  editTarget,
}: {
  prefabs: PrefabNode[];
  classes: ClassInfo[];
  showInternal: boolean;
  editTarget: string;
}) => {
  if (prefabs.length === 0) {
    return (
      <div style={{ padding: "20rem", color: DIM, fontSize: "13rem" }}>
        Pick a vehicle in the middle column. Ctrl-click adds to the selection, Shift-click picks a range.
      </div>
    );
  }

  const ids = prefabs.map(p => p.id);
  const multi = prefabs.length > 1;

  const sameClass = prefabs.every(p => p.className === prefabs[0].className);
  const className = sameClass ? prefabs[0].className : null;
  // "Unclassified" is a display bucket, not a class values can attach to.
  const classEditable = sameClass && prefabs[0].className !== "Unclassified";

  // Spawn chance only exists for naturally spawning vehicles (personal cars).
  const specs = PROPERTIES.filter(s => s.key !== "probability" || prefabs.every(p => p.spawns));
  const merged = specs.map(s => mergeProperty(s, prefabs));

  // Only worth spelling out when another pack wins, or a pack multiplier moved the real value.
  const notes: string[] = [];
  for (const m of merged) {
    const f = m.field;
    if (f.winner === "pack" && f.source) notes.push(`${m.spec.label} comes from pack "${f.source}".`);
    const shown = f.winner === "own" ? f.own : f.winner === "class" ? f.cls : f.vanilla;
    if (shown != null && Math.abs(f.effective - shown) > 0.05)
      notes.push(`${m.spec.label} in game: ${Number(f.effective.toFixed(m.spec.decimals))} ${m.spec.unit}.`);
  }

  return (
    <div style={{ padding: "10rem 14rem" }}>
      <SelectionHeader prefabs={prefabs} showInternal={showInternal} />

      <PanelSectionRow
        disableFocus
        left={"Class"}
        right={
          <ClassPicker
            ids={ids}
            current={sameClass ? prefabs[0].className : " "}
            toggleLabel={sameClass ? undefined : "Mixed"}
            classes={classes}
          />
        }
      />

      <div style={{ display: "flex", alignItems: "center", marginTop: "10rem", paddingBottom: "3rem", borderBottom: HAIRLINE }}>
        <span style={{ flex: 1, minWidth: 0 }} />
        <span style={{ ...cellStyle, fontSize: "11rem", color: DIM, textAlign: "center" }}>
          {multi ? "Selected" : "Vehicle"}
        </span>
        <span style={{ ...cellStyle, fontSize: "11rem", color: DIM, textAlign: "center" }}>
          {classEditable ? className : "Class"}
        </span>
        <span style={{ ...vanillaStyle, fontSize: "11rem" }}>Vanilla</span>
      </div>

      {merged.map(m => (
        <CascadeRow key={m.spec.key} merged={m} ids={ids} className={className} classEditable={classEditable} />
      ))}

      <div
        style={{
          marginTop: "10rem",
          padding: "7rem 9rem",
          borderRadius: "4rem",
          backgroundColor: "rgba(255, 255, 255, 0.06)",
          color: DIM,
          fontSize: "11rem",
        }}
      >
        <div>
          Empty cell = not set here. Writing to <span style={{ color: ACCENT }}>{editTarget}</span>.
          {!classEditable && sameClass ? " Assign a class to use the class column." : ""}
        </div>
        {notes.map((n, i) => (
          <div key={i} style={{ marginTop: "3rem" }}>
            {n}
          </div>
        ))}
      </div>
    </div>
  );
};

/**
 * Shown when a class row is selected. Class *values* are edited through the class column of any
 * member vehicle, so this only covers renaming and deleting a pack's own classes.
 */
export const ClassDetail = ({ cls }: { cls: ClassNode | null }) => {
  const [text, setText] = useState(cls ? cls.name : "");
  useEffect(() => setText(cls ? cls.name : ""), [cls ? cls.name : null]);

  if (!cls) {
    return (
      <div style={{ padding: "20rem", color: DIM, fontSize: "13rem" }}>
        Pick a vehicle to edit values. Selecting a class lets you rename or delete it.
      </div>
    );
  }

  const commitRename = () => {
    const t = text.trim();
    if (t && t !== cls.name) renameClass(cls.name, t);
  };

  return (
    <div style={{ padding: "10rem 14rem" }}>
      <div style={{ fontSize: "15rem", marginBottom: "2rem" }}>{cls.name}</div>
      <div style={{ fontSize: "12rem", color: DIM, marginBottom: "12rem" }}>{cls.prefabs.length} vehicles</div>

      {cls.custom ? (
        <div style={{ display: "flex", alignItems: "center", marginBottom: "10rem" }}>
          <div style={{ width: "140rem", marginRight: "6rem" }}>
            <TextInput value={text} onChange={setText} onBlur={commitRename} />
          </div>
          <TxtButton onClick={commitRename}>Rename</TxtButton>
          <TxtButton danger onClick={() => deleteClass(cls.name)}>
            Delete
          </TxtButton>
        </div>
      ) : (
        <div style={{ fontSize: "12rem", color: DIM }}>Built-in class — can't be renamed or deleted.</div>
      )}

      <div
        style={{
          marginTop: "12rem",
          padding: "8rem 10rem",
          borderRadius: "4rem",
          backgroundColor: "rgba(255, 255, 255, 0.06)",
          color: DIM,
          fontSize: "11rem",
        }}
      >
        To change what every {cls.name} does, pick one of its vehicles and edit the class column.
      </div>
    </div>
  );
};
