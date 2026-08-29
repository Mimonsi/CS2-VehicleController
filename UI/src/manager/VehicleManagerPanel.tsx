// The Vehicle Manager window: pack library | vehicle tree | value editor, all visible at once.
// This file only wires state and composes the three columns; the panels themselves live next to it.

import { useValue } from "cs2/api";
import { Panel, PanelSection, PanelSectionRow, Scrollable } from "cs2/ui";
import React, { useEffect, useMemo, useRef, useState } from "react";

import { classesJson$, packsJson$, refreshTree, treeJson$ } from "./api";
import { Chip, TextInput } from "./controls";
import { ClassDetail, DetailPanel } from "./DetailPanel";
import { CategoryNode, ClassInfo, PacksInfo, PrefabNode, VEHICLE_TREE } from "./data";
import { PackLibrary } from "./PackLibrary";
import { BODY_MAX_HEIGHT, DIM, HAIRLINE, PACK_COLUMN_WIDTH, TREE_COLUMN_WIDTH, WINDOW_WIDTH } from "./theme";
import { filterTree, findClass, VehicleTree } from "./VehicleTree";
// Icons must be imported so webpack emits them to coui://ui-mods/images/;
// a bare string path is never emitted and won't resolve.
import ambulanceIcon from "../images/ambulance.png";
import carIcon from "../images/car.png";
import trainIcon from "../images/train.png";

const CATEGORY_ICONS: Record<string, string> = {
  cars: carIcon,
  trains: trainIcon,
  service: ambulanceIcon,
};

const EMPTY_PACKS: PacksInfo = {
  packs: [{ name: "Default", description: "", readOnly: false, active: true }],
  editTarget: "Default",
};

/** Parses a JSON binding, falling back to a default when it is empty or malformed. */
function parseJson<T>(raw: string, fallback: T, accept: (parsed: any) => boolean): T {
  try {
    const parsed = JSON.parse(raw);
    if (accept(parsed)) return parsed as T;
  } catch (e) {
    // fall through to the fallback
  }
  return fallback;
}

export const VehicleManagerPanel = ({
  onClose,
  focusPrefab,
}: {
  onClose: () => void;
  focusPrefab?: string | null;
}) => {
  // Selection: vehicles (multi) or a single class row — never both.
  const [selectedIds, setSelectedIds] = useState<Set<string>>(focusPrefab ? new Set([focusPrefab]) : new Set());
  const [selectedClassName, setSelectedClassName] = useState<string | null>(null);

  // Display filters.
  const [search, setSearch] = useState("");
  const [inPackOnly, setInPackOnly] = useState(false);
  const [showInternal, setShowInternal] = useState(false);

  // ---- Data from the backend ---------------------------------------------

  const treeJson = useValue(treeJson$);
  const classesJson = useValue(classesJson$);
  const packsJson = useValue(packsJson$);

  const tree = useMemo<CategoryNode[]>(() => {
    const parsed = parseJson<CategoryNode[]>(treeJson, [], p => Array.isArray(p) && p.length > 0);
    // Fall back to the mock tree before the backend has sent anything.
    if (parsed.length === 0) return VEHICLE_TREE;
    return parsed.map(c => ({ ...c, icon: CATEGORY_ICONS[c.key] ?? "" }));
  }, [treeJson]);

  const classes = useMemo<ClassInfo[]>(
    () => parseJson<ClassInfo[]>(classesJson, [], p => Array.isArray(p)),
    [classesJson]
  );

  const packInfo = useMemo<PacksInfo>(
    () => parseJson<PacksInfo>(packsJson, EMPTY_PACKS, p => p && Array.isArray(p.packs)),
    [packsJson]
  );

  // ---- Derived view state -------------------------------------------------

  const customClasses = useMemo(() => new Set(classes.filter(c => c.custom).map(c => c.name)), [classes]);
  const filteredTree = useMemo(
    () => filterTree(tree, search, inPackOnly, customClasses),
    [tree, search, inPackOnly, customClasses]
  );
  const filtering = search.trim() !== "" || inPackOnly;
  const filterKey = filtering ? `${search}|${inPackOnly}` : "";

  // Flat visible order, needed for shift-range selection.
  const orderedIds = useMemo(() => {
    const out: string[] = [];
    for (const c of filteredTree) for (const cls of c.classes) for (const p of cls.prefabs) out.push(p.id);
    return out;
  }, [filteredTree]);

  // Re-derive the selection from the current tree so it reflects edits after a refresh.
  const selectedPrefabs = useMemo<PrefabNode[]>(() => {
    const out: PrefabNode[] = [];
    for (const c of tree) for (const cls of c.classes) for (const p of cls.prefabs) if (selectedIds.has(p.id)) out.push(p);
    return out;
  }, [tree, selectedIds]);
  const selectedClass = useMemo(() => findClass(tree, selectedClassName), [tree, selectedClassName]);

  // ---- Selection handling -------------------------------------------------

  const anchorRef = useRef<string | null>(null);

  const selectPrefab = (id: string, additive: boolean, range: boolean) => {
    setSelectedClassName(null);

    if (range && anchorRef.current) {
      const from = orderedIds.indexOf(anchorRef.current);
      const to = orderedIds.indexOf(id);
      if (from !== -1 && to !== -1) {
        const [lo, hi] = from <= to ? [from, to] : [to, from];
        setSelectedIds(new Set(orderedIds.slice(lo, hi + 1)));
        return; // keep the anchor so further shift-clicks extend from it
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

  // Follow a deep-link from a vehicle's Selected-Info panel.
  useEffect(() => {
    if (focusPrefab) {
      setSelectedIds(new Set([focusPrefab]));
      setSelectedClassName(null);
    }
  }, [focusPrefab]);

  // Build the tree once the window is open.
  useEffect(() => {
    refreshTree();
  }, []);

  return (
    <Panel
      draggable
      header={<span>Vehicle manager</span>}
      onClose={onClose}
      initialPosition={{ x: 0.28, y: 0.14 }}
      style={{ width: WINDOW_WIDTH }}
    >
      <div style={{ display: "flex", alignItems: "center", padding: "8rem 14rem", flexWrap: "wrap" }}>
        <div style={{ width: "180rem" }}>
          <TextInput value={search} placeholder={"Search vehicles"} onChange={setSearch} />
        </div>
        <span style={{ marginLeft: "12rem", color: DIM, fontSize: "12rem" }}>Show</span>
        <Chip label={"In this pack"} active={inPackOnly} onClick={() => setInPackOnly(v => !v)} />
        <Chip label={"Internal names"} active={showInternal} onClick={() => setShowInternal(v => !v)} />
      </div>

      <div style={{ display: "flex", borderTop: HAIRLINE }}>
        <PackLibrary packs={packInfo.packs} editTarget={packInfo.editTarget} width={PACK_COLUMN_WIDTH} />

        <Scrollable
          vertical
          trackVisibility={"scrollable"}
          style={{ width: TREE_COLUMN_WIDTH, flexShrink: 0, maxHeight: BODY_MAX_HEIGHT, borderRight: HAIRLINE }}
        >
          <VehicleTree
            tree={filteredTree}
            selectedIds={selectedIds}
            selectedClassName={selectedClassName}
            expandAll={filtering}
            filterKey={filterKey}
            showInternal={showInternal}
            onSelectPrefab={selectPrefab}
            onSelectClass={selectClass}
          />
        </Scrollable>

        <Scrollable vertical trackVisibility={"scrollable"} style={{ flex: 1, minWidth: 0, maxHeight: BODY_MAX_HEIGHT }}>
          {selectedClassName ? (
            <ClassDetail cls={selectedClass} />
          ) : (
            <DetailPanel
              prefabs={selectedPrefabs}
              classes={classes}
              showInternal={showInternal}
              editTarget={packInfo.editTarget}
            />
          )}
        </Scrollable>
      </div>

      <PanelSection>
        <PanelSectionRow disableFocus left={`Changes save automatically to "${packInfo.editTarget}".`} />
      </PanelSection>
    </Panel>
  );
};
