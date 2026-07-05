import { FOCUS_DISABLED } from "cs2/input";
// PanelSection/PanelSectionRow/PanelFoldout are the runtime-safe *alias* exports
// of the game's InfoSection/InfoRow/InfoSectionFoldout (see ui.d.ts). Importing
// InfoRow/InfoSection directly from cs2/ui fails at runtime, the aliases work.
import { Button, Icon, Panel, PanelFoldout, PanelSection, PanelSectionRow, Scrollable } from "cs2/ui";
import React, { useMemo, useState } from "react";

import { ModuleResolver } from "../ModuleResolver";
import { AttrState, CategoryNode, MOCK_PACKS, PrefabNode, VEHICLE_TREE } from "./data";

const resetSrc = "coui://uil/Standard/Reset.svg";
const packSrc = "coui://uil/Standard/BoxIcon.svg";
const lockSrc = "coui://uil/Standard/Lock.svg";

const OVERRIDE_COLOR = "rgba(140, 190, 255, 1)";
const DIM_COLOR = "rgba(255, 255, 255, 0.5)";

// Small provenance tag next to a value: "Override" (this prefab has its own
// value) vs "Inherited" (value comes from a parent class / global).
const Badge = ({ state }: { state: AttrState }) => (
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
    {state.overridden ? "Override" : "Inherited"}
  </span>
);

// One editable-looking property line in the detail panel. The reset button is
// only enabled when this value is an override (prototype: not wired yet).
const DetailRow = ({
  label,
  state,
  format,
}: {
  label: string;
  state: AttrState;
  format: (v: number) => string;
}) => (
  <PanelSectionRow
    disableFocus
    subRow
    left={label}
    right={
      <div style={{ display: "flex", alignItems: "center" }}>
        <span style={{ color: state.overridden ? OVERRIDE_COLOR : DIM_COLOR }}>
          {format(state.value)}
        </span>
        <Badge state={state} />
        <div style={{ marginLeft: "8rem" }}>
          <ModuleResolver.instance.ToolButton
            src={resetSrc}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
            disabled={!state.overridden}
            tooltip={"Reset to inherited"}
            className={ModuleResolver.instance.toolButtonTheme.button}
            onSelect={() => {}}
          />
        </div>
      </div>
    }
  />
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
  onSelect,
}: {
  prefab: PrefabNode;
  selected: boolean;
  onSelect: () => void;
}) => (
  <div
    onClick={onSelect}
    style={{
      padding: "6rem 10rem 6rem 34rem",
      cursor: "pointer",
      color: selected ? OVERRIDE_COLOR : undefined,
      backgroundColor: selected ? "rgba(140, 190, 255, 0.15)" : undefined,
      borderLeft: selected ? "3rem solid " + OVERRIDE_COLOR : "3rem solid transparent",
    }}
  >
    {prefab.custom ? "◆ " : ""}
    {prefab.name}
  </div>
);

const DetailPanel = ({ prefab }: { prefab: PrefabNode | null }) => {
  if (!prefab) {
    return (
      <div style={{ padding: "20rem", color: DIM_COLOR }}>
        Select a vehicle on the left to edit its spawn probability and properties.
      </div>
    );
  }
  return (
    <div style={{ padding: "10rem 14rem" }}>
      <div style={{ fontSize: "15rem", marginBottom: "2rem" }}>{prefab.name}</div>
      <div style={{ fontSize: "12rem", color: DIM_COLOR, marginBottom: "12rem" }}>
        {prefab.className}
        {prefab.custom ? " · custom asset" : ""}
      </div>

      <PanelSectionRow
        disableFocus
        left={"Spawn probability"}
        right={
          <div style={{ display: "flex", alignItems: "center" }}>
            <span style={{ color: prefab.probability.overridden ? OVERRIDE_COLOR : DIM_COLOR }}>
              {prefab.probability.value}%
            </span>
            <Badge state={prefab.probability} />
          </div>
        }
      />

      <div style={{ color: DIM_COLOR, fontSize: "12rem", margin: "10rem 0 2rem" }}>
        Properties
      </div>
      <DetailRow label={"Max speed"} state={prefab.maxSpeed} format={v => `${v} km/h`} />
      <DetailRow
        label={"Acceleration"}
        state={prefab.acceleration}
        format={v => `${v.toFixed(1)} m/s²`}
      />
      <DetailRow label={"Braking"} state={prefab.braking} format={v => `${v.toFixed(1)} m/s²`} />

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

const Tree = ({
  tree,
  selectedId,
  onSelect,
}: {
  tree: CategoryNode[];
  selectedId: string | null;
  onSelect: (p: PrefabNode) => void;
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
              <PanelSectionRow disableFocus subRow left={cls.name} />
            }
          >
            {cls.prefabs.map(prefab => (
              <PrefabRow
                key={prefab.id}
                prefab={prefab}
                selected={prefab.id === selectedId}
                onSelect={() => onSelect(prefab)}
              />
            ))}
          </PanelFoldout>
        ))}
      </PanelFoldout>
    ))}
  </>
);

export const VehicleManagerPanel = ({ onClose }: { onClose: () => void }) => {
  const [selected, setSelected] = useState<PrefabNode | null>(null);

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
      {/* Pack bar (visual only in the prototype) */}
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

      {/* Body: tree (scrollable) + detail */}
      <div style={{ display: "flex" }}>
        <Scrollable
          vertical
          trackVisibility={"scrollable"}
          style={{ width: "260rem", maxHeight: "440rem", borderRight: "1rem solid rgba(255,255,255,0.1)" }}
        >
          <Tree tree={VEHICLE_TREE} selectedId={selected?.id ?? null} onSelect={setSelected} />
        </Scrollable>
        <div style={{ flex: 1, minWidth: "0" }}>
          <DetailPanel prefab={selected} />
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
