import { trigger } from "cs2/api";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";

import { SelectableVehiclePrefab } from "./SIPVehicleSelectorComponent";
import styles from "vehicleSelector.module.scss";
import mod from "../mod.json";
import { ModuleResolver } from "./ModuleResolver";
import { VehicleLabel } from "./VehicleLabel";
import {VehiclePill} from "./VehiclePill";

// Define props for vehicle selector dropdown.
type WrapboxProps = {
    vehicleTypes: SelectableVehiclePrefab[];
}

export const Wrapbox = (props: WrapboxProps) => {

    // Empty array as fallback if vehicleTypes is not provided
    let vehicleTypes = props.vehicleTypes ?? [];

    // Create a dropdown item for each selectable prefab and get content of the selected item.
    vehicleTypes = vehicleTypes.filter(value => value.selected);
    const vehiclePills: JSX.Element[] = vehicleTypes.map((vehiclePrefab, index) => {
        const prefabName = vehiclePrefab.prefabName;
        const imageUrl = vehiclePrefab.imageUrl;
        const isDummyItem = prefabName.includes("Vehicles Selected");
        
        return (
          <VehiclePill prefabName={prefabName} image={imageUrl}/>
        );
    });
    
    console.log(ModuleResolver.instance.InfoWrapBox);
    
    /*
          <ModuleResolver.instance.InfoWrapBox>
        {vehiclePills}
      </ModuleResolver.instance.InfoWrapBox>
     */
    
    return (
      <div className={ModuleResolver.instance.InfoSectionClasses.infoWrapBox + " " + ModuleResolver.instance.SIPDropdownClasses.wrapbox}>
        {vehiclePills}
      </div>
    );
};
