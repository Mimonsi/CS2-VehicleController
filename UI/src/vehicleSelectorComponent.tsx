import { trigger                    } from "cs2/api";
import { SelectedInfoSectionBase    } from "cs2/bindings";
import { useLocalization            } from "cs2/l10n";
import { FormattedParagraphsProps   } from "cs2/ui";

import   styles                       from "vehicleSelectorComponent.module.scss";
import { VehicleSelector            } from "./vehicleSelector";
import { ModuleResolver             } from "moduleResolver";
import   mod                          from "../mod.json";

// Resource data for a company.
export type VehiclePrefab =
    {
        prefabName: string,
    }

// The component for the change company section.
export const VehicleSelectorComponent = (componentList: any): any =>
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
        MaintenanceVehicle,
        PostVan,
        RoadMaintenanceVehicle,
        Taxi,
        ParkMaintenanceVehicle,
        WorkVehicle
    }

    // Define props for change company section.
    // Adapted from bindings.d.ts for the game's sections.
    interface ChangeVehicleSection extends SelectedInfoSectionBase
        {
            serviceType:          ServiceVehicleType,
            availableVehicles:         VehiclePrefab[]
        }

    // Add ChangeVehicleSection to the component list.
    // Make sure section name is unique by including the mod id.
    componentList["VehicleController.Systems.ChangeVehicleSection"] = (props: ChangeVehicleSection) =>
    {
        console.log("Registering component with key: ", mod.id + ".Systems.ChangeVehicleSection");
        // Get the mod's translated text for the section heading and button.
        const { translate } = useLocalization();
        const sectionHeading: string = translate(mod.id + ".ChangeVehicles") || "Change Vehicles";
        const changeNowLabel: string = translate(mod.id + ".ChangeNow"    ) || "Change Now";

        // Get the game's translated text for left and right headings based on property type.
        let headingSuffixLeft:  string | null = null;
        let headingSuffixRight: string | null = null;
        // Display service vehicle type
        headingSuffixLeft = props.serviceType ? props.serviceType.toString() : "None";
        
        const headingLeft:  string = headingSuffixLeft  ? translate("SelectedInfoPanel.COMPANY_" + headingSuffixLeft ) || headingSuffixLeft  : "";
        const headingRight: string = headingSuffixRight ? translate("SelectedInfoPanel.COMPANY_" + headingSuffixRight) || headingSuffixRight : "";

        // Get the mod's translated formatted tooltip text based on property type.
        //const tooltipText: string = translate(mod.id + ".SectionTooltip" + PropertyType[props.propertyType]) ||
        //    "Select a company from the dropdown and click Change Now.";
        const tooltipText = "Hello tooltip text";
        const formattedParagraphsProps: FormattedParagraphsProps = { children: tooltipText };
        const formattedTooltip: JSX.Element = ModuleResolver.instance.FormattedParagraphs(formattedParagraphsProps);

        // Handle click on Change Now button
        function onChangeNowClicked()
        {
            trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
            trigger(mod.id, "ChangeNowClicked");
        }

        // Construct the change company section.
        // Info row 1 has section heading and Change Now button.
        // Info row 2 has left and right headings.
        // Info row 3 has dropdown list of companies to choose from.
        return (
            <ModuleResolver.instance.InfoSection tooltip={formattedTooltip}>
                <ModuleResolver.instance.InfoRow
                    left={sectionHeading}
                    uppercase={true}
                    right={<button className={styles.changeNowButton} onClick={() => onChangeNowClicked()}>{changeNowLabel}</button>}
                    disableFocus={true}
                />
                <ModuleResolver.instance.InfoRow
                    className={styles.headingRow}
                    left={headingLeft}
                    right={headingRight}
                    disableFocus={true}
                    subRow={true}
                />
                <ModuleResolver.instance.InfoRow
                    className={styles.dropdownRow}
                    left={<VehicleSelector vehicleTypes={props.availableVehicles} />}
                    disableFocus={true}
                    subRow={true}
                />
            </ModuleResolver.instance.InfoSection>
        );
    }

    // Return the updated component list.
    return componentList as any;
}