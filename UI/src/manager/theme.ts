// Shared colours and input styling for the Vehicle Manager.
//
// Layout note: Cohtml/Gameface does NOT support CSS grid — every layout in this UI must be
// flexbox with explicit widths (`flexShrink: 0`) for fixed columns.

import React from "react";

export const ACCENT = "rgba(140, 190, 255, 1)";
export const DIM = "rgba(255, 255, 255, 0.5)";
export const DANGER = "rgba(255, 140, 140, 1)";
export const HAIRLINE = "1rem solid rgba(255, 255, 255, 0.12)";

export const MS_TO_KMH = 3.6;

export const textInputStyle: React.CSSProperties = {
  background: "rgba(0, 0, 0, 0.25)",
  color: "white",
  border: "1rem solid rgba(255, 255, 255, 0.2)",
  borderRadius: "3rem",
  padding: "2rem 6rem",
  fontSize: "13rem",
};

// Column widths of the three-pane layout. The window width is the sum plus borders.
export const PACK_COLUMN_WIDTH = "210rem";
export const TREE_COLUMN_WIDTH = "240rem";
export const BODY_MAX_HEIGHT = "460rem";
export const WINDOW_WIDTH = "900rem";

// Cascade table columns (detail panel).
export const CELL_WIDTH = "68rem";
export const VANILLA_WIDTH = "48rem";
