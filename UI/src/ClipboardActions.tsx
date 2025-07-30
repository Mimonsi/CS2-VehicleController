import React, { useMemo } from "react";
import { bindValue, useValue, trigger } from "cs2/api";
import { ModuleResolver } from "./ModuleResolver";
import mod from "../mod.json";
import { getModule } from "cs2/modding";
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

  const clipboardData$ = bindValue<string>(mod.id, "ClipboardData", "");
  const clipboardData = useValue(clipboardData$);

  function getClipboardEntryLength(text: string): number {
    // See how many comma seperated entries are in the clipboard text
    if (!text) return 0;
    // Split the text by commas and filter out empty entries
    const entries = text.split(",").map(entry => entry.trim()).filter(entry => entry.length > 0);
    return entries.length;
  }

  console.log("Clipboard data: ", clipboardData);
  //const clipboardLength = useMemo(() => getClipboardEntryLength(clipboardData), [clipboardData]);
  const clipboardLength = getClipboardEntryLength(clipboardData);
  console.log("Clipboard length: ", clipboardLength);
  
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
            tooltip = {"Export Clipboard to Windows-Clipboard (Click this button, then paste with Ctrl+V)"}
            className = {ModuleResolver.instance.toolButtonTheme.button}
            onSelect={() => handleClick("ExportClipboardClicked")}
          />
          <ModuleResolver.instance.ToolButton
            src = {"coui://uil/Standard/Folder.svg"}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
            selected={false}
            tooltip = {"Import Clipboard from Windows-Clipboard (Copy with Ctrl+C, then click this button)"}
            className = {ModuleResolver.instance.toolButtonTheme.button}
            onSelect={() => handleClick("ImportClipboardClicked")}
          />
        </>
        }
        disableFocus={true}
      />

      { /* Paste buttons */ }
      { clipboardLength>0 && (
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
                disabled = {clipboardLength==0 }
              />
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/RectanglePaste.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {formatTooltipText("Paste selection to all " + serviceName + " buildings in district " + districtName)}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSameServiceTypeDistrictClicked")}
                disabled = {clipboardLength==0 }
              />
            </>
        }
        disableFocus={true}
      />
    )}
    </>
  );
};
