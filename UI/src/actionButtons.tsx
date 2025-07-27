import React from "react";
import { trigger } from "cs2/api";
import { ModuleResolver } from "moduleResolver";
import mod from "../mod.json";

// This components create buttons allowing the player to copy and paste the vehicle selection
// Options include copying to other buildings of the exact same prefab, copying to all buildings of the same type, copying to clipboard. Also differentiate between district-wide copying and
// city-wide copying (global)
export const ActionButtons = () => {
  const InfoButton = ModuleResolver.instance.InfoButton;

  const buttons = [
    { label: "Copy Selection", event: "CopySelectionClicked" },
    { label: "Paste Same Prefab", event: "PasteSamePrefabClicked" },
    { label: "Paste Service Type", event: "PasteServiceTypeClicked" },
    { label: "Paste District", event: "PasteDistrictClicked" },
    { label: "Paste City", event: "PasteCityClicked" },
    { label: "Export", event: "ExportClipboardClicked" },
    { label: "Import", event: "ImportClipboardClicked" },
  ];

  return (
    <>
      {InfoButton ? (
        buttons.map((b) => (
          <InfoButton
            key={b.event}
            label={b.label}
            icon={"Media/Game/Icons/GenericVehicle.svg"}
            selected={false}
            onSelect={() => trigger(mod.id, b.event)}
          />
        ))
      ) : (
        <div>InfoButton not found</div>
      )}
    </>
  );
};
