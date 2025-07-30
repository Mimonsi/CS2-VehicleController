import React from "react";
import { trigger } from "cs2/api";
import { ModuleResolver } from "./ModuleResolver";
import mod from "../mod.json";
import { getModule } from "cs2/modding";
import styles from "./actionButtons.module.scss";
import {FormattedParagraphsProps} from "cs2/ui";

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
export const ClipboardActions = () => {
  
  function handleClick(eventName : string)
  {
    trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
    trigger(mod.id, eventName);
  }
  
  function formatTooltipText(text: string) {
    const formattedParagraphsProps: FormattedParagraphsProps = { children: text };
    return ModuleResolver.instance.FormattedParagraphs(formattedParagraphsProps)
  }
  
  const prefabName = "**prefabName**"
  const serviceName = "**serviceName**";
  const districtName = "**districtName**";
  const clipboardEmpty = false;
  
  // TODO: Find other icon for Importing from file
  return (
    <>
      { /* Clipboard buttons */ }
      <ModuleResolver.instance.InfoRow
        left={"Clipboard"}
        uppercase={false}
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
            src = {"coui://uil/Standard/Folder.svg"}
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

      { /* Paste buttons */ }
      { !clipboardEmpty && (
      <ModuleResolver.instance.InfoRow
        left={"Import Config"}
        uppercase={false}
        right={
            <>
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/RectanglePaste.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {"Paste Selection to this building"}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSelectionClicked")}
              />
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/RectanglePaste.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {formatTooltipText("Paste selection to all " + prefabName + "")}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSamePrefabClicked")}
              />
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/RectanglePaste.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {formatTooltipText("Paste selection to all " + prefabName + " in district " + districtName)}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSamePrefabDistrictClicked")}
              />
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/RectanglePaste.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {formatTooltipText("Paste selection to all " + serviceName + " buildings")}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSameServiceTypeClicked")}
                disabled = {clipboardEmpty}
              />
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/RectanglePaste.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {formatTooltipText("Paste selection to all " + serviceName + " buildings in district " + districtName)}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSameServiceTypeDisstrictClicked")}
                disabled = {clipboardEmpty}
              />
            </>
        }
        disableFocus={true}
      />
    )}
    </>
  );
};
