import React, { useMemo } from "react";
import { bindValue, useValue, trigger } from "cs2/api";
import { ModuleResolver } from "./ModuleResolver";
import mod from "../mod.json";
import {FormattedParagraphs} from "cs2/ui";


interface ClipboardActionsProps
{
  prefabName: string,
  serviceName: string,
  districtName: string | null,
}

// UI-Idea:
// Display a text:
// "Paste selection to all *FR_PoliceStation* in the *city/district*" -> Paste to prefab type
// "Paste selection to all *Police Buildings* in the *city/district*" -> Paste to service type
export const ClipboardActions = (props : ClipboardActionsProps) => {
  
  function handleClick(eventName : string)
  {
    //trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1); // Sound already played by ToolButton
    //trigger("audio", "playSound", ModuleResolver.instance.UISound.xpEvent, 1);
    trigger(mod.id, eventName);
  }
  
  const prefabName = `**${props.prefabName}**`
  const serviceName = `**${props.serviceName}**`;
  const districtName = `**${props.districtName}**`;

  const clipboardData$ = bindValue<string>(mod.id, "ClipboardData", "");
  const clipboardData = useValue(clipboardData$);

  function getClipboardEntryLength(text: string): number {
    try {
      // See how many comma seperated entries are in the clipboard text
      if (!text) return 0;
      // Split the text by commas and filter out empty entries
      const entries = text.split(",").map(entry => entry.trim()).filter(entry => entry.length > 0);
      return entries.length;
    }
    catch(error) {
      console.error("Error getting clipboard entry length: ", error);
      return 0;
    }
  }

  console.log("Clipboard data: ", clipboardData);
  const clipboardLength = useMemo(() => getClipboardEntryLength(clipboardData), [clipboardData]);
  console.log("Clipboard length: ", clipboardLength);
  const showPasteButtons = clipboardLength > 0;
  console.log("Show paste buttons: ", showPasteButtons);
  
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
      { (showPasteButtons) && (
      <ModuleResolver.instance.InfoRow
        left={"Import Config"}
        uppercase={false}
        right={
            <>
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/RectanglePaste.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {<FormattedParagraphs>{"Paste Selection to this building"}</FormattedParagraphs>}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSelectionClicked")}
              />
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Standard/SingleRhombus.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {<FormattedParagraphs>{`Paste selection to all ${prefabName}`}</FormattedParagraphs>}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSamePrefabClicked")}
              />
              <ModuleResolver.instance.ToolButton
                  src = {"coui://uil/Standard/SameRhombus.svg"}
                  focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                  selected={false}
                  tooltip = {<FormattedParagraphs>{`Paste selection to all ${serviceName} buildings`}</FormattedParagraphs>}
                  className = {ModuleResolver.instance.toolButtonTheme.button}
                  onSelect={() => handleClick("PasteSameServiceTypeClicked")}
              />
              {props.districtName && (<>
              <ModuleResolver.instance.ToolButton
                  src = {"coui://uil/Colored/SingleRhombus.svg"}
                  focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                  selected={false}
                  tooltip = {<FormattedParagraphs>{`Paste selection to all ${prefabName} in district ${districtName}`}</FormattedParagraphs>}
                  className = {ModuleResolver.instance.toolButtonTheme.button}
                  onSelect={() => handleClick("PasteSamePrefabDistrictClicked")}
              />
              <ModuleResolver.instance.ToolButton
                src = {"coui://uil/Colored/SameRhombus.svg"}
                focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                selected={false}
                tooltip = {<FormattedParagraphs>{`Paste selection to all ${serviceName} in district ${districtName}`}</FormattedParagraphs>}
                className = {ModuleResolver.instance.toolButtonTheme.button}
                onSelect={() => handleClick("PasteSameServiceTypeDistrictClicked")}
              />
              </>)}
            </>
        }
        disableFocus={true}
      />
    )}
    </>
  );
};
