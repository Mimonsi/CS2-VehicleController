import { trigger } from "cs2/api";
import { Dropdown, DropdownToggle } from "cs2/ui";

import { SelectableVehiclePrefab } from "./SIPVehicleSelectorComponent";
import mod from "../mod.json";
import { ModuleResolver } from "./ModuleResolver";
import { VehicleLabel } from "./VehicleLabel";

// Define props for vehicle selector dropdown.
type VehicleSelectorProps = {
    vehicleTypes: SelectableVehiclePrefab[];
    displayPrefabNames: boolean;
    label?: string;
    triggerName?: string;
}

export const VehicleSelector = (props: VehicleSelectorProps) => {

    // Empty array as fallback if vehicleTypes is not provided
    const vehicleTypes = props.vehicleTypes ?? [];
    const label = props.label ?? "Select models";
    const triggerName = props.triggerName ?? "SelectedVehicleChanged";

    // Create a dropdown item for each selectable prefab and get content of the selected item.
    const vehicleDropdownItems: JSX.Element[] = vehicleTypes.map((vehiclePrefab, index): JSX.Element => {
        const prefabName = vehiclePrefab.prefabName;
        const selected = vehiclePrefab.selected ?? false;
        const imageUrl = vehiclePrefab.imageUrl;

        // Construct dropdown item content.
        const dropdownItemContent = (
          <VehicleLabel prefabName={prefabName} image={imageUrl} displayPrefabName={props.displayPrefabNames}/>
        );

        return (
          <ModuleResolver.instance.DropdownFlagItem
            theme={{
              ...ModuleResolver.instance.DropdownFlagItemTheme,
              ...ModuleResolver.instance.SelectVehiclesDropdownItem,
            }}
            value={vehiclePrefab.prefabName}
            checked={selected}
            onChange={() => trigger("VehicleController.Systems.VehicleSelectionSection", triggerName, prefabName)}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
          >
              {dropdownItemContent}
          </ModuleResolver.instance.DropdownFlagItem>
        );
    });

    return (
    <Dropdown
      theme={ModuleResolver.instance.DropdownClasses}
      content={vehicleDropdownItems}
      focusKey={ModuleResolver.instance.FOCUS_DISABLED}
    >
        <DropdownToggle className={ModuleResolver.instance.SIPDropdownClasses.dropdown}>
            <div className={ModuleResolver.instance.SIPDropdownClasses.dropdownLabel}>{label}</div>
        </DropdownToggle>

    </Dropdown>
    );
};
