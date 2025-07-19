import { bindValue, useValue, trigger } from "cs2/api";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";

import { SelectableVehiclePrefab } from "./vehicleSelectorComponent";
import styles from "vehicleSelector.module.scss";
import mod from "../mod.json";
import { ModuleResolver } from "moduleResolver";
import { VehicleLabel } from "./vehicleLabel";
import {getModule} from "cs2/modding";
import {prefab} from "cs2/bindings";
const dropdownStyle = getModule("game-ui/game/components/selected-info-panel/selected-info-sections/route-sections/select-vehicles-section.module.scss", "classes");

// Binding für ausgewählten Fahrzeugindex
//const bindingSelectedCompanyIndex = bindValue<number>(mod.id, "SelectedCompanyIndex", 0);

// Define props for company selector dropdown.
type VehicleSelectorProps = {
    vehicleTypes: SelectableVehiclePrefab[];
}

export const VehicleSelector = (props: VehicleSelectorProps) => {

    //const selectedCompanyIndex: number = useValue(bindingSelectedCompanyIndex);

    // Empty array as fallback if vehicleTypes is not provided
    const vehicleTypes = props.vehicleTypes ?? [];

    // Create a dropdown item for each company and get content of the selected item.
    const companyDropdownItems: JSX.Element[] = vehicleTypes.map((vehiclePrefab, index) => {
        const prefabName = vehiclePrefab.prefabName;
        const selected = vehiclePrefab.selected ?? false;
        const imageUrl = vehiclePrefab.imageUrl;
        const isDummyItem = prefabName.includes("Vehicles Selected");
        // Check if this company info is for the selected company.
        //const selected = index === selectedCompanyIndex;

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
    
    const selectedCompanyDropdownItemContent = vehicleTypes[0] ? (
      <VehicleLabel prefabName={vehicleTypes[0].prefabName} />
    ) : (
      <>Nothing here :/</>
    );

    return (
    <Dropdown
      theme={ModuleResolver.instance.DropdownClasses}
      content={companyDropdownItems}
      focusKey={ModuleResolver.instance.FOCUS_DISABLED}
    >
        <DropdownToggle className={styles.zWC}>
            {selectedCompanyDropdownItemContent}
        </DropdownToggle>
      
    </Dropdown>
    );
};
