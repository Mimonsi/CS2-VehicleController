import { trigger } from "cs2/api";
import { Dropdown, DropdownToggle } from "cs2/ui";

import { SelectableVehiclePrefab } from "./SIPVehicleSelectorComponent";
import mod from "../mod.json";
import { ModuleResolver } from "./ModuleResolver";
import { VehicleLabel } from "./VehicleLabel";

// Define props for vehicle selector dropdown.
type VehicleSelectorProps = {
    vehicleTypes: SelectableVehiclePrefab[];
}

export const VehicleSelector = (props: VehicleSelectorProps) => {

    // Empty array as fallback if vehicleTypes is not provided
    const vehicleTypes = props.vehicleTypes ?? [];

    // Create a dropdown item for each selectable prefab and get content of the selected item.
    const vehicleDropdownItems: JSX.Element[] = vehicleTypes.map((vehiclePrefab, index): JSX.Element => {
        const prefabName = vehiclePrefab.prefabName;
        const selected = vehiclePrefab.selected ?? false;
        const imageUrl = vehiclePrefab.imageUrl;

        // Construct dropdown item content.
        const dropdownItemContent = (
          <VehicleLabel prefabName={prefabName} image={imageUrl}/>
        );

        return (
          <ModuleResolver.instance.DropdownFlagItem
            theme={{
              ...ModuleResolver.instance.DropdownFlagItemTheme,
              ...ModuleResolver.instance.SelectVehiclesDropdownItem,
            }}
            value={vehiclePrefab.prefabName}
            checked={selected}
            onChange={() => trigger(mod.id, "SelectedVehicleChanged", prefabName)}
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
            <div className={ModuleResolver.instance.SIPDropdownClasses.dropdownLabel}>Select models</div>
        </DropdownToggle>
      
    </Dropdown>
    );
};
