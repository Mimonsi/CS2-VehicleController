// Small shared controls: text button, chip, number cell and the vanilla-styled dropdown.

import { Dropdown, DropdownToggle } from "cs2/ui";
import * as CS2UI from "cs2/ui";
import React, { useEffect, useState } from "react";

import { ModuleResolver } from "../ModuleResolver";
import { playClick, playHover } from "./api";
import { ACCENT, DANGER, DIM, textInputStyle } from "./theme";

/**
 * Text button with hover highlight and the game's native click/hover sounds.
 * `fill` makes it stretch inside a flex row — used where wrapping must stay deterministic.
 */
export const TxtButton = ({
  children,
  onClick,
  danger,
  fill,
}: {
  children: React.ReactNode;
  onClick: () => void;
  danger?: boolean;
  fill?: boolean;
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
        flex: fill ? 1 : undefined,
        textAlign: fill ? "center" : undefined,
        marginRight: "5rem",
        padding: "3rem 8rem",
        borderRadius: "4rem",
        fontSize: "12rem",
        color: danger ? DANGER : "white",
        border: "1rem solid rgba(255, 255, 255, 0.25)",
        backgroundColor: hover ? "rgba(255, 255, 255, 0.14)" : "rgba(255, 255, 255, 0.04)",
      }}
    >
      {children}
    </div>
  );
};

/** Pill-shaped toggle used for the display filters. */
export const Chip = ({
  label,
  active,
  onClick,
}: {
  label: string;
  active?: boolean;
  onClick?: () => void;
}) => (
  <span
    onClick={
      onClick
        ? () => {
            playClick();
            onClick();
          }
        : undefined
    }
    style={{
      fontSize: "12rem",
      padding: "4rem 10rem",
      marginLeft: "6rem",
      borderRadius: "20rem",
      cursor: onClick ? "pointer" : undefined,
      color: active ? ACCENT : DIM,
      border: "1rem solid " + (active ? ACCENT : DIM),
    }}
  >
    {label}
  </span>
);

/**
 * One cell of the cascade table. `null` renders empty, meaning "not set at this level";
 * clearing the text clears the override. The winning cell is ringed so the value's origin
 * is visible rather than described.
 */
export const NumberCell = ({
  value,
  winning,
  disabled,
  onCommit,
  onClear,
}: {
  value: number | null;
  winning?: boolean;
  disabled?: boolean;
  onCommit: (v: number) => void;
  onClear: () => void;
}) => {
  const display = (v: number | null) => (v === null ? "" : String(v));
  const [text, setText] = useState(display(value));
  useEffect(() => setText(display(value)), [value]);

  const commit = () => {
    const t = text.trim();
    if (t === "") {
      if (value !== null) onClear();
      return;
    }
    const parsed = parseFloat(t);
    if (!isNaN(parsed) && isFinite(parsed)) onCommit(parsed);
    else setText(display(value));
  };

  return (
    <input
      type="text"
      value={text}
      disabled={disabled}
      placeholder={"–"}
      onChange={e => setText(e.currentTarget.value)}
      onBlur={commit}
      onKeyDown={e => {
        if (e.key === "Enter") e.currentTarget.blur();
      }}
      style={{
        width: "100%",
        textAlign: "center",
        background: disabled ? "rgba(0, 0, 0, 0.12)" : "rgba(0, 0, 0, 0.25)",
        color: disabled ? DIM : "white",
        border: winning ? "2rem solid " + ACCENT : "1rem solid rgba(255, 255, 255, 0.18)",
        borderRadius: "3rem",
        padding: "3rem 4rem",
        fontSize: "12rem",
      }}
    />
  );
};

/** Plain text input matching the game's styling. */
export const TextInput = ({
  value,
  placeholder,
  width,
  onChange,
  onSubmit,
  onCancel,
  onBlur,
}: {
  value: string;
  placeholder?: string;
  width?: string;
  onChange: (v: string) => void;
  onSubmit?: () => void;
  onCancel?: () => void;
  onBlur?: () => void;
}) => (
  <input
    type="text"
    value={value}
    placeholder={placeholder}
    onChange={e => onChange(e.currentTarget.value)}
    onBlur={onBlur}
    onKeyDown={e => {
      if (e.key === "Enter") {
        if (onSubmit) onSubmit();
        else e.currentTarget.blur();
      }
      if (e.key === "Escape" && onCancel) onCancel();
    }}
    style={{ ...textInputStyle, width: width ?? "100%", textAlign: "left" }}
  />
);

// `DropdownItem` is an interface in cs2/ui's types; the runtime component is only reachable
// through the module namespace, so grab it as a value.
const DropdownItemComp: any = (CS2UI as any).DropdownItem;

/** Dropdown built from the game's own Dropdown/DropdownToggle/DropdownItem and theme. */
export const VDropdown = ({
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
