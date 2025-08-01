import { trigger } from "cs2/api";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";

import { SelectableVehiclePrefab } from "./SIPVehicleSelectorComponent";
import styles from "vehicleSelector.module.scss";
import mod from "../mod.json";
import { ModuleResolver } from "./ModuleResolver";
import { VehicleLabel } from "./vehicleLabel";
import {getModule} from "cs2/modding";
import {prefab} from "cs2/bindings";
const dropdownStyle = getModule("game-ui/game/components/selected-info-panel/selected-info-sections/route-sections/select-vehicles-section.module.scss", "classes");


// Define props for company selector dropdown.
type VehicleSelectorProps = {
    vehicleTypes: SelectableVehiclePrefab[];
}

export const VehicleSelector = (props: VehicleSelectorProps) => {

    //const selectedCompanyIndex: number = useValue(bindingSelectedCompanyIndex);

    // Empty array as fallback if vehicleTypes is not provided
    const vehicleTypes = props.vehicleTypes ?? [];

    // Create a dropdown item for each selectable prefab and get content of the selected item.
    const vehicleDropdownItems: JSX.Element[] = vehicleTypes.map((vehiclePrefab, index) => {
        const prefabName = vehiclePrefab.prefabName;
        const selected = vehiclePrefab.selected ?? false;
        const imageUrl = vehiclePrefab.imageUrl;
        const isDummyItem = prefabName.includes("Vehicles Selected");
        
        console.log("VehicleSelector", prefabName, selected, imageUrl, isDummyItem);

        // Construct dropdown item content.
        const dropdownItemContent = (
          <VehicleLabel prefabName={prefabName} selected={selected} image={imageUrl}/>
        );

        return (
          <DropdownItem
            key={vehiclePrefab.prefabName}
            theme={ModuleResolver.instance.DropdownClasses}
            value=""
            closeOnSelect={false}
            selected={isDummyItem}
            onChange={() => trigger(mod.id, "SelectedVehicleChanged", prefabName)}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
          >
              {dropdownItemContent}
          </DropdownItem>
        );
    });
    
    // First item is selected by default, it's the header item
    const selectedCompanyDropdownItemContent = vehicleTypes[0] ? (
      <VehicleLabel prefabName={vehicleTypes[0].prefabName} />
    ) : (
      <>Nothing here :/</>
    );

    return (
    <Dropdown
      theme={ModuleResolver.instance.DropdownClasses}
      content={vehicleDropdownItems}
      focusKey={ModuleResolver.instance.FOCUS_DISABLED}
    >
        <DropdownToggle className={styles.zWC}>
            {selectedCompanyDropdownItemContent}
        </DropdownToggle>
      
    </Dropdown>
    );
};
