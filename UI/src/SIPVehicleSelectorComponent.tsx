import {bindValue, trigger, useValue} from "cs2/api";
import { SelectedInfoSectionBase    } from "cs2/bindings";
import { useLocalization            } from "cs2/l10n";
import {FormattedParagraphs, } from "cs2/ui";
import { VehicleSelector            } from "./VehicleSelector";
import { ClipboardActions } from "./ClipboardActions";
import { ModuleResolver             } from "./ModuleResolver";
import   mod                          from "../mod.json";
import React from "react";
import {Wrapbox} from "./Wrapbox";

export type SelectableVehiclePrefab =
  {
    prefabName: string,
    imageUrl?: string, // Optional property for image URL
    selected?: boolean, // Optional property to indicate if this vehicle is selected
  }

const uilStandard =                          "coui://uil/Standard/";
const uilColored =                           "coui://uil/Colored/";
const minimizeSrc =                     uilStandard + "ArrowsMinimize.svg";
const expandSrc =                       uilStandard + "ArrowsExpand.svg";

const group = "VehicleController.Systems.VehicleSelectionSection";
const Minimized$ = bindValue<boolean>(group, "Minimized");
// The component for the change vehicle section
export const SIPVehicleSelectorComponent = (componentList: any): any =>
{
  // Define service vehicle types
  // Matches Query in ChangeVehicleSection.cs
  enum ServiceType
  {
    None,
    Healthcare,
    Fire,
    Police,
    Garbage,
    Deathcare,
    Postal,
    Transport,
    RoadMaintenance,
    ParkMaintenance
  }

  // Define props for change vehicle section.
  // Adapted from bindings.d.ts for the game's sections.
  interface ChangeVehicleSection extends SelectedInfoSectionBase
  {
    prefabName: string,
    serviceType: ServiceType,
    serviceName: string,
    districtName: string | null,
    availableVehicles: SelectableVehiclePrefab[],
    vehiclesSelected: number,
    availableHelicopters: SelectableVehiclePrefab[],
    helicoptersSelected: number,
    displayPrefabNames: boolean
  }

  // Add ChangeVehicleSection to the component list.
  // Make sure section name is unique by including the mod id.
  componentList["VehicleController.Systems.VehicleSelectionSection"] = (props: ChangeVehicleSection) =>
  {
    const Minimized = useValue(Minimized$);
    // Get the mod's translated text for the section heading and button.
    const { translate } = useLocalization();
    const sectionHeading: string = translate(mod.id + ".VehicleController") || "Vehicle Controller";
    const changeNowLabel: string = translate(mod.id + ".ChangeNow"    ) || "Apply to existing vehicles";
    const clearBufferLabel: string = translate(mod.id + ".ClearBuffer"    ) || "Clear allowed Vehicles";
    const DeleteOwnedVehiclesLabel: string = translate(mod.id + ".DeleteOwnedVehicles"    ) || "Delete owned vehicles";

    const tooltipText = "Select all **vehicle models** that this building is allowed to use. When multiple vehicles are selected, a **random** vehicle from the selection will be chosen.";
    const helicopterTooltipText = "Select all **helicopter models** that this building is allowed to use. When multiple helicopters are selected, a **random** helicopter from the selection will be chosen.";

    const modifiedVehicleList: SelectableVehiclePrefab[] = [
      ...props.availableVehicles
    ];

    const modifiedHelicopterList: SelectableVehiclePrefab[] = [
      ...(props.availableHelicopters ?? [])
    ];

    const hasHelicopters = modifiedHelicopterList.length > 0;

    // Handle click on Change Now button
    function onChangeNowClicked()
    {
      trigger(group, "ChangeNowClicked");
    }

    // Handle click
    function onClearBufferClicked()
    {
      trigger(group, "ClearBufferClicked");
    }

    // Handle click
    function onDeleteOwnedClicked()
    {
      trigger(group, "DeleteOwnedVehiclesClicked");
    }

    function minimizeClick()
    {
      trigger(group, "Minimize");
    }

    // Construct the change company section.
    // Info row 1 has section heading and Change Now button.
    // Info row 2 has the vehicle dropdown
    // Info row 3 (conditional) has the helicopter dropdown
    // Further rows are for action and clipboard buttons
    return (
      <ModuleResolver.instance.InfoSection>
        <ModuleResolver.instance.InfoRow
          left={sectionHeading}
          uppercase={true}
          right={
            <ModuleResolver.instance.ToolButton
              src={Minimized? expandSrc : minimizeSrc}
              focusKey={ModuleResolver.instance.FOCUS_DISABLED}
              tooltip = {Minimized? "Expand" : "Minimize"}
              className = {ModuleResolver.instance.toolButtonTheme.button}
              onSelect={() => minimizeClick()}
            />
          }
          disableFocus={true}
        />
        {!Minimized && (
          <>
            <ModuleResolver.instance.InfoRow
              left={<VehicleSelector
                vehicleTypes={modifiedVehicleList}
                displayPrefabNames={props.displayPrefabNames}
                label="Select vehicles"
                triggerName="SelectedVehicleChanged"
              />}
              tooltip={<FormattedParagraphs>{tooltipText}</FormattedParagraphs>}
              disableFocus={true}
            />

            <Wrapbox vehicleTypes={modifiedVehicleList}/>

            {hasHelicopters && (
              <>
                <ModuleResolver.instance.InfoRow
                  left={<VehicleSelector
                    vehicleTypes={modifiedHelicopterList}
                    displayPrefabNames={props.displayPrefabNames}
                    label="Select helicopters"
                    triggerName="SelectedHelicopterChanged"
                  />}
                  tooltip={<FormattedParagraphs>{helicopterTooltipText}</FormattedParagraphs>}
                  disableFocus={true}
                />

                <Wrapbox vehicleTypes={modifiedHelicopterList}/>
              </>
            )}

            <ModuleResolver.instance.InfoRow
              left={"Actions"}
              uppercase={false}
              right={
                <>
                  <ModuleResolver.instance.ToolButton
                    src = {"coui://uil/Standard/Checkmark.svg"}
                    focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                    selected={false}
                    tooltip = {"Apply to existing vehicles"}
                    className = {ModuleResolver.instance.toolButtonTheme.button}
                    onSelect={() => onChangeNowClicked()}
                  />
                  <ModuleResolver.instance.ToolButton
                    src = {"coui://uil/Standard/Reset.svg"}
                    focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                    selected={false}
                    tooltip = {"Clear Vehicle Buffer (Reset allowed vehicles to default)"}
                    className = {ModuleResolver.instance.toolButtonTheme.button}
                    onSelect={() => onClearBufferClicked()}
                  />
                  <ModuleResolver.instance.ToolButton
                    src = {"coui://uil/Standard/XClose.svg"}
                    focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                    selected={false}
                    tooltip = {"Delete owned vehicles of the building (will respawn)"}
                    className = {ModuleResolver.instance.toolButtonTheme.button}
                    onSelect={() => onDeleteOwnedClicked()}
                  />
                </>
              }
              disableFocus={false}
            />

            <ClipboardActions
                prefabName={props.prefabName}
                serviceName={props.serviceName}
                districtName={props.districtName}
            />
          </>
        )}
      </ModuleResolver.instance.InfoSection>
    );
  }

  // Return the updated component list.
  return componentList as any;
}
