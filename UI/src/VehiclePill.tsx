import { useLocalization    } from "cs2/l10n";
import { ModuleResolver     } from "./ModuleResolver";
import React from "react";
import {Icon} from "cs2/ui";

interface VehiclePillProps
{
    prefabName: string,
    selected?: boolean,
    image?: string,
}

// Custom component to combine the icon and label for a resource
// so the game does not split the icon from the label when wrapping in the company selector.
export const VehiclePill = ({ prefabName, image }: VehiclePillProps) =>
{
    const vehicleIcon: string = image ? image : "Media/Game/Icons/GenericVehicle.svg";
    const { translate } = useLocalization();
    const translated = translate("Assets.NAME[" + prefabName + "]");
    let vehicleText: string = (translated && translated !== "null") ? `${translated} (${prefabName})` : prefabName;

  // This would be better for the icon and more accurate, but the images are not square: <Icon className={ModuleResolver.instance.SIPDropdownClasses.thumb} src={vehicleIcon}></Icon>
    return (
      <div className={ModuleResolver.instance.SIPDropdownClasses.item + " " + ModuleResolver.instance.SIPDropdownClasses.pill}>
          <img className={ModuleResolver.instance.SIPDropdownClasses.thumb} src={vehicleIcon}></img>
        
        <div className={ModuleResolver.instance.SIPDropdownClasses.label}>{vehicleText}</div>
      </div>
    );
}