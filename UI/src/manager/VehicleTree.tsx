// Middle column: category → class → vehicle tree, plus the filtering that feeds it.

import { FOCUS_DISABLED } from "cs2/input";
// PanelFoldout/PanelSectionRow are the runtime-safe *alias* exports of the game's
// InfoSectionFoldout/InfoRow — importing the originals from cs2/ui fails at runtime.
import { Icon, PanelFoldout, PanelSectionRow } from "cs2/ui";
import React, { useEffect, useRef } from "react";

import { playClick } from "./api";
import { CategoryNode, ClassNode, PrefabNode } from "./data";
import { ACCENT, DIM } from "./theme";

export const classHasSelected = (cls: ClassNode, ids: Set<string>): boolean =>
  cls.prefabs.some(p => ids.has(p.id));

/** A vehicle is "in this pack" if the editing pack changes it or its class is a pack class. */
const isInPack = (p: PrefabNode, customClasses: Set<string>): boolean =>
  !!p.edited || customClasses.has(p.className);

/**
 * Filters the tree by search text and the "in this pack" scope. Returns the very same array when
 * nothing is filtered, so foldout state survives re-renders.
 */
export function filterTree(
  tree: CategoryNode[],
  search: string,
  inPackOnly: boolean,
  customClasses: Set<string>
): CategoryNode[] {
  const q = search.trim().toLowerCase();
  if (!q && !inPackOnly) return tree;

  const matches = (p: PrefabNode) => {
    if (
      q &&
      !p.name.toLowerCase().includes(q) &&
      !p.id.toLowerCase().includes(q) &&
      !p.className.toLowerCase().includes(q)
    )
      return false;
    return !(inPackOnly && !isInPack(p, customClasses));
  };

  return tree
    .map(c => ({
      ...c,
      classes: c.classes
        .map(cls => ({ ...cls, prefabs: cls.prefabs.filter(matches) }))
        .filter(cls => cls.prefabs.length > 0),
    }))
    .filter(c => c.classes.length > 0);
}

/** Finds a class by name across all categories. */
export function findClass(tree: CategoryNode[], name: string | null): ClassNode | null {
  if (!name) return null;
  for (const c of tree) for (const cls of c.classes) if (cls.name === name) return cls;
  return null;
}

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

  // Scroll into view when selected from elsewhere (e.g. deep-linked from a vehicle's info panel).
  useEffect(() => {
    if (selected && ref.current) {
      try {
        ref.current.scrollIntoView({ block: "nearest" });
      } catch (e) {
        // scrollIntoView is not always available in Gameface; harmless if missing
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
        display: "flex",
        alignItems: "center",
        padding: "5rem 8rem 5rem 26rem",
        cursor: "pointer",
        color: selected ? ACCENT : undefined,
        backgroundColor: selected ? "rgba(140, 190, 255, 0.15)" : undefined,
        borderLeft: selected ? "3rem solid " + ACCENT : "3rem solid transparent",
      }}
    >
      {/* A dot marks vehicles the editing pack changes, as opposed to merely classifying. */}
      <span
        style={{
          flexShrink: 0,
          width: "5rem",
          height: "5rem",
          marginRight: "6rem",
          borderRadius: "3rem",
          backgroundColor: prefab.edited ? ACCENT : "transparent",
        }}
      />
      <span style={{ flex: 1, minWidth: 0, fontSize: "13rem" }}>
        {showInternal ? prefab.id : prefab.name}
      </span>
    </div>
  );
};

export const VehicleTree = ({
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
  /** Changes with the filter so foldouts remount and auto-expand onto matches. */
  filterKey: string;
  showInternal: boolean;
  onSelectPrefab: (id: string, additive: boolean, range: boolean) => void;
  onSelectClass: (name: string) => void;
}) => (
  <>
    {tree.map(category => (
      <PanelFoldout
        key={category.key + filterKey}
        initialExpanded={expandAll || category.classes.some(cls => classHasSelected(cls, selectedIds))}
        expandFromContent={false}
        focusKey={FOCUS_DISABLED}
        header={
          <PanelSectionRow
            disableFocus
            left={
              <div style={{ display: "flex", alignItems: "center" }}>
                {category.icon ? <Icon src={category.icon} tinted /> : null}
                <span style={{ marginLeft: "6rem" }}>{category.name}</span>
              </div>
            }
          />
        }
      >
        {category.classes.map(cls => (
          <PanelFoldout
            key={cls.name + filterKey}
            initialExpanded={expandAll || classHasSelected(cls, selectedIds)}
            expandFromContent={false}
            focusKey={FOCUS_DISABLED}
            header={
              <PanelSectionRow
                disableFocus
                subRow
                left={
                  <span style={{ color: cls.name === selectedClassName ? ACCENT : undefined }}>{cls.name}</span>
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
                        color: cls.name === selectedClassName ? ACCENT : DIM,
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
