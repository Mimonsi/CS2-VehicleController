// Every binding and command that crosses the C# ↔ UI boundary lives here, so the components
// stay free of protocol details. See Systems/VehicleManagerUISystem.cs for the other side.

import { bindValue, trigger } from "cs2/api";

export const MANAGER_GROUP = "VehicleController.VehicleManager";

// ---- C# → UI -------------------------------------------------------------

/** Category → class → prefab tree with each property's cascade state (JSON string). */
export const treeJson$ = bindValue<string>(MANAGER_GROUP, "treeJson", "[]");
/** All assignable class names (built-in + the editing pack's custom ones). */
export const classesJson$ = bindValue<string>(MANAGER_GROUP, "classesJson", "[]");
/** The pack library: active packs in priority order, then inactive ones. */
export const packsJson$ = bindValue<string>(MANAGER_GROUP, "packsJson", "{}");
/** Live instance count of the focused prefab, refreshed per selection. */
export const selectedCount$ = bindValue<number>(MANAGER_GROUP, "selectedCount", 0);
/** `{prefab, nonce}` — bumped when a vehicle's info panel asks to open the manager on it. */
export const openRequest$ = bindValue<string>(MANAGER_GROUP, "openRequest", "{}");

/** Called from a vehicle's Selected-Info panel to open the manager focused on that prefab. */
export const openManagerFor = (prefabName: string) => trigger(MANAGER_GROUP, "openManager", prefabName);

// ---- UI → C#: values -----------------------------------------------------

/** Sets a class-level value in the editing pack. */
export const setClassValue = (className: string, field: string, value: number) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "set", level: "class", key: className, field, value }));

/** Clears a class-level value in the editing pack. */
export const clearClassValue = (className: string, field: string) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "reset", level: "class", key: className, field }));

/** Sets a per-vehicle value for the whole selection in one command. */
export const setPrefabValue = (prefabs: string[], field: string, value: number) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "set", level: "prefab", prefabs, field, value }));

/** Clears a per-vehicle value for the whole selection. */
export const clearPrefabValue = (prefabs: string[], field: string) =>
  trigger(MANAGER_GROUP, "edit", JSON.stringify({ op: "reset", level: "prefab", prefabs, field }));

// ---- UI → C#: classes ----------------------------------------------------

/** Assigns every selected vehicle to a class; an empty name unassigns. */
export const assignClass = (prefabs: string[], className: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "assignMany", prefabs, class: className }));

export const renameClass = (className: string, newName: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "rename", class: className, newName }));

export const deleteClass = (className: string) =>
  trigger(MANAGER_GROUP, "classCmd", JSON.stringify({ op: "delete", class: className }));

// ---- UI → C#: packs ------------------------------------------------------

export type PackOp =
  | "toggle"
  | "setTarget"
  | "moveUp"
  | "moveDown"
  | "new"
  | "duplicate"
  | "rename"
  | "delete"
  | "export"
  | "import";

export const packCommand = (op: PackOp, name: string) =>
  trigger(MANAGER_GROUP, "packCmd", JSON.stringify({ op, name }));

// ---- UI → C#: misc -------------------------------------------------------

/** Rebuilds the tree; call when the window opens. */
export const refreshTree = () => trigger(MANAGER_GROUP, "refresh");

/** Moves the camera to follow a random live instance of this prefab. */
export const jumpToInstance = (prefab: string) => trigger(MANAGER_GROUP, "jumpTo", prefab);

/** Asks for a fresh instance count; the answer arrives on selectedCount$. */
export const requestCount = (prefab: string) => trigger(MANAGER_GROUP, "requestCount", prefab);

// ---- Native UI sounds ----------------------------------------------------

export const playClick = () => trigger("audio", "playSound", "select-item", 1);
export const playHover = () => trigger("audio", "playSound", "hover-item", 1);
