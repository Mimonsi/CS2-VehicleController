// Left column: the pack library.
//
// Ticking a pack activates it. Active packs are numbered in priority order — on overlap the
// LOWEST one wins (like Minecraft resource packs). Clicking a writable pack's name makes it the
// editing pack, so "where do my edits go" is answered right next to the editor.

import { Scrollable } from "cs2/ui";
import React, { useState } from "react";

import { packCommand, playClick } from "./api";
import { TextInput, TxtButton } from "./controls";
import { PackEntry } from "./data";
import { ACCENT, BODY_MAX_HEIGHT, DIM, HAIRLINE } from "./theme";

type PromptOp = "new" | "duplicate" | "rename";

/** One row of the library: activation tick, priority number, name, shipped marker. */
const PackRow = ({
  pack,
  order,
  isTarget,
  canReorder,
  isFirst,
  isLast,
}: {
  pack: PackEntry;
  order: number | null;
  isTarget: boolean;
  canReorder: boolean;
  isFirst: boolean;
  isLast: boolean;
}) => {
  const selectable = pack.active && !pack.readOnly;
  return (
    <div style={{ marginBottom: "2rem" }}>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          padding: "3rem 4rem",
          borderRadius: "3rem",
          backgroundColor: isTarget ? "rgba(140, 190, 255, 0.15)" : undefined,
          borderLeft: isTarget ? "2rem solid " + ACCENT : "2rem solid transparent",
        }}
      >
        <span
          onClick={() => {
            playClick();
            packCommand("toggle", pack.name);
          }}
          style={{
            cursor: "pointer",
            flexShrink: 0,
            width: "12rem",
            height: "12rem",
            marginRight: "6rem",
            borderRadius: "2rem",
            border: "1rem solid " + (pack.active ? ACCENT : DIM),
            backgroundColor: pack.active ? ACCENT : "transparent",
          }}
        />
        <span style={{ flexShrink: 0, width: "11rem", fontSize: "10rem", color: DIM }}>
          {order !== null ? order : ""}
        </span>
        <span
          onClick={selectable ? () => { playClick(); packCommand("setTarget", pack.name); } : undefined}
          style={{
            flex: 1,
            minWidth: 0,
            fontSize: "12rem",
            cursor: selectable ? "pointer" : undefined,
            color: !pack.active ? DIM : isTarget ? ACCENT : "white",
          }}
        >
          {pack.name}
        </span>
        {pack.readOnly && (
          <span style={{ flexShrink: 0, fontSize: "10rem", color: DIM, marginLeft: "4rem" }}>shipped</span>
        )}
      </div>

      {pack.description ? (
        <div style={{ fontSize: "10rem", color: DIM, padding: "0 4rem 2rem 33rem", lineHeight: 1.35 }}>
          {pack.description}
        </div>
      ) : null}

      {canReorder && (
        <div style={{ display: "flex", padding: "1rem 0 2rem 31rem" }}>
          {!isFirst && <TxtButton onClick={() => packCommand("moveUp", pack.name)}>Up</TxtButton>}
          {!isLast && <TxtButton onClick={() => packCommand("moveDown", pack.name)}>Down</TxtButton>}
        </div>
      )}
    </div>
  );
};

export const PackLibrary = ({
  packs,
  editTarget,
  width,
}: {
  packs: PackEntry[];
  editTarget: string;
  width: string;
}) => {
  const [prompt, setPrompt] = useState<{ op: PromptOp; value: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const active = packs.filter(p => p.active);
  const inactive = packs.filter(p => !p.active);
  const takenNames = packs.map(p => p.name);

  const startPrompt = (op: PromptOp, value: string) => {
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
    const name = prompt.value.trim();
    if (!name) return;
    if (prompt.op === "rename" && name === editTarget) {
      cancelPrompt();
      return;
    }
    if (name.startsWith("_")) {
      setError("Names can't start with \"_\".");
      return;
    }
    if (takenNames.includes(name)) {
      setError(`"${name}" already exists.`);
      return;
    }
    packCommand(prompt.op, name);
    cancelPrompt();
  };

  return (
    // The action buttons deliberately sit OUTSIDE the Scrollable: inside it, a scrollbar
    // appearing would rewrap them, changing the height, which removes the scrollbar again —
    // an endless reflow loop that shows up as flicker.
    <div style={{ width, flexShrink: 0, display: "flex", flexDirection: "column", borderRight: HAIRLINE }}>
      <div style={{ padding: "8rem 8rem 4rem", fontSize: "11rem", color: DIM }}>
        Packs · lowest active wins
      </div>

      <Scrollable vertical trackVisibility={"scrollable"} style={{ flex: 1, maxHeight: BODY_MAX_HEIGHT, padding: "0 8rem" }}>
        {active.map((p, i) => (
          <PackRow
            key={p.name}
            pack={p}
            order={i + 1}
            isTarget={p.name === editTarget}
            canReorder={active.length > 1}
            isFirst={i === 0}
            isLast={i === active.length - 1}
          />
        ))}

        {inactive.length > 0 && (
          <div style={{ fontSize: "10rem", color: DIM, margin: "8rem 0 3rem 4rem" }}>Not active</div>
        )}
        {inactive.map(p => (
          <PackRow key={p.name} pack={p} order={null} isTarget={false} canReorder={false} isFirst isLast />
        ))}
      </Scrollable>

      <div style={{ borderTop: HAIRLINE, padding: "6rem 8rem 8rem" }}>
        {/* Fixed two-per-row layout — no flexWrap, so the width can never make it reflow. */}
        <div style={{ display: "flex", marginBottom: "4rem" }}>
          <TxtButton fill onClick={() => startPrompt("new", "")}>New</TxtButton>
          <TxtButton fill onClick={() => startPrompt("duplicate", editTarget + " copy")}>Copy</TxtButton>
        </div>
        <div style={{ display: "flex", marginBottom: "4rem" }}>
          <TxtButton fill onClick={() => startPrompt("rename", editTarget)}>Rename</TxtButton>
          <TxtButton fill danger onClick={() => { cancelPrompt(); setConfirmDelete(true); }}>Delete</TxtButton>
        </div>
        <div style={{ display: "flex" }}>
          <TxtButton fill onClick={() => packCommand("export", "")}>Export</TxtButton>
          <TxtButton fill onClick={() => packCommand("import", "")}>Import</TxtButton>
        </div>

        {prompt && (
          <div style={{ marginTop: "6rem" }}>
            <TextInput
              value={prompt.value}
              placeholder={"Pack name"}
              onChange={v => {
                setPrompt({ ...prompt, value: v });
                setError(null);
              }}
              onSubmit={confirmPrompt}
              onCancel={cancelPrompt}
            />
            <div style={{ display: "flex", marginTop: "4rem" }}>
              <TxtButton fill onClick={confirmPrompt}>OK</TxtButton>
              <TxtButton fill onClick={cancelPrompt}>Cancel</TxtButton>
            </div>
            {error && <div style={{ marginTop: "3rem", fontSize: "11rem", color: "rgba(255,140,140,1)" }}>{error}</div>}
          </div>
        )}

        {confirmDelete && (
          <div style={{ marginTop: "6rem" }}>
            <div style={{ fontSize: "11rem" }}>Delete "{editTarget}"?</div>
            <div style={{ display: "flex", marginTop: "4rem" }}>
              <TxtButton
                fill
                danger
                onClick={() => {
                  packCommand("delete", editTarget);
                  setConfirmDelete(false);
                }}
              >
                Delete
              </TxtButton>
              <TxtButton fill onClick={() => setConfirmDelete(false)}>Cancel</TxtButton>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
