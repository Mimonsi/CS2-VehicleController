import {bindValue, trigger, useValue} from "cs2/api";
import { SelectedInfoSectionBase    } from "cs2/bindings";
import { useLocalization            } from "cs2/l10n";
import { FormattedParagraphsProps   } from "cs2/ui";

import   styles                       from "SIPVehicleSelectorComponent.module.scss";
import { VehicleSelector            } from "./vehicleSelector";
import { ClipboardActions } from "./ClipboardActions";
import { ModuleResolver             } from "./ModuleResolver";
import   mod                          from "../mod.json";
import {getModule} from "cs2/modding";
import React from "react";
const styleSelectVehicle = getModule("game-ui/game/components/selected-info-panel/selected-info-sections/route-sections/select-vehicles-section.module.scss", "classes");
const dropdownToggleStyle = getModule("game-ui/game/themes/game-dropdown.module.scss", "classes");

// Resource data for a company.
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

const Minimized$ = bindValue<boolean>(mod.id, "Minimized");
// The component for the change company section.
export const SIPVehicleSelectorComponent = (componentList: any): any =>
{
  // Define service vehicle types
  // Matches Query in CreatedServiceVehicleModifierSystem.cs
  enum ServiceVehicleType
  {
    None,
    Ambulance,
    FireEngine,
    PoliceCar,
    GarbageTruck,
    Hearse,
    PostVan,
    TransportVehicle, // Taxi, Bus
    RoadMaintenanceVehicle,
    ParkMaintenanceVehicle
  }

  // Define props for change company section.
  // Adapted from bindings.d.ts for the game's sections.
  interface ChangeVehicleSection extends SelectedInfoSectionBase
  {
    serviceType: ServiceVehicleType,
    availableVehicles: SelectableVehiclePrefab[],
    vehiclesSelected: number
  }

  // Add ChangeVehicleSection to the component list.
  // Make sure section name is unique by including the mod id.
  componentList["VehicleController.Systems.ChangeVehicleSection"] = (props: ChangeVehicleSection) =>
  {
    const Minimized = useValue(Minimized$);
    // Get the mod's translated text for the section heading and button.
    const { translate } = useLocalization();
    const sectionHeading: string = translate(mod.id + ".VehicleController") || "Vehicle Controller";
    const changeNowLabel: string = translate(mod.id + ".ChangeNow"    ) || "Apply to existing vehicles";
    const clearBufferLabel: string = translate(mod.id + ".ClearBuffer"    ) || "Clear allowed Vehicles";
    const debug2Label: string = translate(mod.id + ".Debug2"    ) || "Delete owned vehicles";

    // Get the mod's translated formatted tooltip text based on property type.
    //const tooltipText: string = translate(mod.id + ".SectionTooltip" + PropertyType[props.propertyType]) ||
    //    "Select a company from the dropdown and click Change Now.";
    const tooltipText = "Select all **vehicle models** that this building is allowed to use. When multiple vehicles are selected, a **random** vehicle from the selection will be chosen.";
    const formattedParagraphsProps: FormattedParagraphsProps = { children: tooltipText };
    const formattedTooltip: JSX.Element = ModuleResolver.instance.FormattedParagraphs(formattedParagraphsProps);

    const dummyVehicle: SelectableVehiclePrefab = { prefabName: props.vehiclesSelected + " Vehicles Selected" };
    // Add dummy vehicle on first index
    const modifiedVehicleList: SelectableVehiclePrefab[] = [
      dummyVehicle,
      ...props.availableVehicles
    ];

    // Handle click on Change Now button
    function onChangeNowClicked()
    {
      trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
      trigger(mod.id, "ChangeNowClicked");
    }

    // Handle click
    function onClearBufferClicked()
    {
      trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
      trigger(mod.id, "ClearBufferClicked");
    }

    // Handle click
    function onDeleteOwnedClicked()
    {
      trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
      trigger(mod.id, "Debug2Clicked");
    }

    function minimizeClick()
    {
      trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
      trigger(mod.id, "Minimize");
    }

    // Construct the change company section.
    // Info row 1 has section heading and Change Now button.
    // Info row 2 has the actual dropdown
    // Info row 3 has two additional buttons
    // All further rows are for actions buttons, e.g. copy, paste, export, etc.
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
              left={<VehicleSelector vehicleTypes={modifiedVehicleList}/>}
              tooltip={formattedTooltip}
              disableFocus={true}
            />

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

            <ClipboardActions/>
          </>
        )}
      </ModuleResolver.instance.InfoSection>
    );
  }

  // Return the updated component list.
  return componentList as any;
}