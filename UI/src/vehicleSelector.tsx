import { bindValue, useValue, trigger           } from "cs2/api";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";

import { VehiclePrefab                    } from "./vehicleSelectorComponent";
import   styles                                   from "vehicleSelector.module.scss";
import   mod                                      from "../mod.json";
import { ModuleResolver                         } from "moduleResolver";
import { VehicleLabel                      } from "./vehicleLabel";
import {prefab} from "cs2/bindings";

// Define binding.
const bindingSelectedCompanyIndex = bindValue<number>(mod.id, "SelectedCompanyIndex", 0);

// Define props for company selector dropdown.
type VehicleSelectorProps =
    {
        vehicleTypes: VehiclePrefab[]
    }

// Custom dropdown for selecting a company.
export const VehicleSelector = (props: VehicleSelectorProps) =>
{
    // Get the value from binding.
    //const selectedCompanyIndex: number = useValue(bindingSelectedCompanyIndex); // TODO: Implement selection logic

    // Function to join classes.
    function joinClasses(...classes: any) { return classes.join(" "); }

    // Create a dropdown item for each company and get content of the selected item.
    let selectedCompanyDropdownItemContent: JSX.Element = <></>;
    let vehiclePrefabCounter: number = 0;
    const companyDropdownItems: JSX.Element[] = props.vehicleTypes.map
        (
            (vehiclePrefabs: VehiclePrefab) =>
            {
                // Get company resources.
                const prefabName: string = vehiclePrefabs.prefabName;

                // Check if this company info is for the selected company.
                //const selected: boolean = (companyInfoCounter == selectedCompanyIndex);
                const selected: boolean = false; // TODO: Implement selection logic

                // Get company info index.
                // Cannot use companyInfoCounter directly because its value changes for each entry.
                // Using companyInfoCounter directly results in all dropdown entires having the same index value as the last one.
                const vehiclePrefabIndex: number = vehiclePrefabCounter;

                // Construct dropdown item content.
                // Left always has the output resource.
                // Right has nothing, input1, or input1+input2 resources.
                const dropdownItemContent: JSX.Element =
                    <div className={styles.companyDropdownRow}>
                        <div className={joinClasses(ModuleResolver.instance.InfoRowClasses.left, styles.companyDropdownItemLeft)}>
                            <VehicleLabel prefabName={prefabName} />
                        </div>
                    </div>

                // Save content for selected company.
                if (selected)
                {
                    selectedCompanyDropdownItemContent = dropdownItemContent;
                }

                // Build dropdown item.
                // Don't know what the value property is used for, but it is required and an empty string seems to work.
                const dropdownItem: JSX.Element =
                    <DropdownItem
                        theme={ModuleResolver.instance.DropdownClasses}
                        value=""
                        closeOnSelect={true}
                        selected={selected}
                        className={styles.companyDropdownItem}
                        //onChange={() => trigger(mod.id, "SelectedCompanyChanged", companyInfoIndex)} TODO: Implement change event
                        focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                    >
                        {dropdownItemContent}
                    </DropdownItem>

                // Increment company info counter for next one.
                vehiclePrefabCounter++;

                // Return the dropdown item.
                return (dropdownItem);
            }
        );

    // Create the dropdown of companies.
    // The DropdownToggle shows the current selection and is the thing the user clicks on to show the dropdown list.
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
}