import { bindValue, useValue, trigger } from "cs2/api";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";

import { SelectableVehiclePrefab } from "./vehicleSelectorComponent";
import styles from "vehicleSelector.module.scss";
import mod from "../mod.json";
import { ModuleResolver } from "moduleResolver";
import { VehicleLabel } from "./vehicleLabel";

// Binding für ausgewählten Fahrzeugindex
const bindingSelectedCompanyIndex = bindValue<number>(mod.id, "SelectedCompanyIndex", 0);

// Define props for company selector dropdown.
type VehicleSelectorProps = {
    vehicleTypes: SelectableVehiclePrefab[];
}

export const VehicleSelector = (props: VehicleSelectorProps) => {
    // Aktuell ausgewählter Index aus Binding
    const selectedCompanyIndex: number = useValue(bindingSelectedCompanyIndex);

    // Empty array as fallback if vehicleTypes is not provided
    const vehicleTypes = props.vehicleTypes ?? [];

    // Function to join classes
    function joinClasses(...classes: any[]) {
        return classes.filter(Boolean).join(" ");
    }

    // Create a dropdown item for each company and get content of the selected item.
    const companyDropdownItems: JSX.Element[] = vehicleTypes.map((vehiclePrefabs, index) => {
        const prefabName = vehiclePrefabs.prefabName;

        // Check if this company info is for the selected company.
        const selected = index === selectedCompanyIndex;

        // Construct dropdown item content.
        const dropdownItemContent = (
          <div className={styles.companyDropdownRow}>
              <div className={joinClasses(ModuleResolver.instance.InfoRowClasses.left, styles.companyDropdownItemLeft)}>
                  <VehicleLabel prefabName={prefabName} />
              </div>
          </div>
        );

        return (
          <DropdownItem
            key={index}
            theme={ModuleResolver.instance.DropdownClasses}
            value=""
            closeOnSelect={true}
            selected={selected}
            className={styles.companyDropdownItem}
            onChange={() => trigger(mod.id, "SelectedCompanyChanged", index)}
            focusKey={ModuleResolver.instance.FOCUS_DISABLED}
          >
              {dropdownItemContent}
          </DropdownItem>
        );
    });

    // Inhalt des aktuell ausgewählten Dropdown-Toggles
    const selectedCompanyDropdownItemContent = vehicleTypes[selectedCompanyIndex] ? (
      <VehicleLabel prefabName={vehicleTypes[selectedCompanyIndex].prefabName} />
    ) : (
      <></>
    );

    return (
      <Dropdown
        theme={ModuleResolver.instance.DropdownClasses}
        content={companyDropdownItems}
        focusKey={ModuleResolver.instance.FOCUS_DISABLED}
      >
          <DropdownToggle>
              {selectedCompanyDropdownItemContent}
          </DropdownToggle>
      </Dropdown>
    );
};
