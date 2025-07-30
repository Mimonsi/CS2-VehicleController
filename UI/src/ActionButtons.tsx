import React from "react";
import { trigger } from "cs2/api";
import { ModuleResolver } from "./ModuleResolver";
import mod from "../mod.json";
import { getModule } from "cs2/modding";
import styles from "./actionButtons.module.scss";

const InfoButton = getModule(
  "game-ui/game/components/selected-info-panel/shared-components/info-button/info-button.tsx",
  "InfoButton"
);

// This components create buttons allowing the player to copy and paste the vehicle selection
// Options include copying to other buildings of the exact same prefab, copying to all buildings of the same type, copying to clipboard. Also differentiate between district-wide copying and
// city-wide copying (global)


// UI-Idea:
// Display a text:
// "Paste selection to all *FR_PoliceStation* in the *city/district*" -> Paste to prefab type
// "Paste selection to all *Police Buildings* in the *city/district*" -> Paste to service type
export const ActionButtons = () => {

  const groups = [
    {
      title: "Copy & Paste",
      buttons: [
        { label: "Copy Selection", icon: "coui://uil/Standard/RectangleCopy.svg", event: "CopySelectionClicked" },
        { label: "Paste Same Prefab", icon: "coui://uil/Standard/RectanglePaste.svg", event: "PasteSamePrefabClicked" },
        { label: "Paste Service Type", icon: "coui://uil/Standard/RectanglePaste.svg", event: "PasteServiceTypeClicked" },
        { label: "Paste District", icon: "coui://uil/Standard/RectanglePaste.svg", event: "PasteDistrictClicked" },
      ],
    },
    {
      title: "Import / Export",
      buttons: [
        { label: "Export", icon: "coui://uil/Standard/DiskSave.svg", event: "ExportClipboardClicked" },
        { label: "Import", icon: "coui://uil/Standard/DiskLoad.svg", event: "ImportClipboardClicked" },
      ],
    },
  ];
  
  function handleClick(eventName : string)
  {
    trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
    trigger(mod.id, eventName);
  }
  
  return (
    <>
      <ModuleResolver.instance.InfoRow
        left={"Clipboard"}
        uppercase={true}
        right=
        {
        <>
          <ModuleResolver.instance.ToolButton
            src = {"coui://uil/Standard/RectangleCopy.svg"}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
            selected={false}
            tooltip = {"Copy Selection to Clipboard"}
            className = {ModuleResolver.instance.toolButtonTheme.button}
            onSelect={() => handleClick("CopySelectionClicked")}
          />
          <ModuleResolver.instance.ToolButton
            src = {"coui://uil/Standard/DiskSave.svg"}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
            selected={false}
            tooltip = {"Export Clipboard to File"}
            className = {ModuleResolver.instance.toolButtonTheme.button}
            onSelect={() => handleClick("ExportClipboardClicked")}
          />
          <ModuleResolver.instance.ToolButton
            src = {"coui://uil/Standard/DiskLoad.svg"}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
            selected={false}
            tooltip = {"Import Clipboard from File"}
            className = {ModuleResolver.instance.toolButtonTheme.button}
            onSelect={() => handleClick("ImportClipboardClicked")}
          />
        </>
        }
        disableFocus={true}
      />
    </>
  );
};
