import { useLocalization    } from "cs2/l10n";
import { ModuleResolver     } from "./ModuleResolver";
import React from "react";

interface VehicleLabelProps
{
    prefabName: string,
    image?: string,
    displayPrefabName: boolean // TODO: Implement properly, shouldn't be passed to every vehicle label
}

// Custom component to combine the icon and label for a resource
// so the game does not split the icon from the label when wrapping in the company selector.
export const VehicleLabel = ({ prefabName, image, displayPrefabName }: VehicleLabelProps) =>
{
    //console.log("vehicleLabel", prefabName, image);
    // Get game's icon for the vehicle.
    //const resourceIcon: string = "Media/Game/Resources/" + resource + ".svg";
    const vehicleIcon: string = image ? image : "Media/Game/Icons/GenericVehicle.svg";
    //const vehicleIcon: string = image ? image : "assetdb://Global/292d20d2f0c5403c5d3bc3452649fde9";

    // Get the game's translated text for the resource.
    // The game uses "+" for the concatenator character for all languages.
    const { translate } = useLocalization();
    //const resourceText: string = (translate("Resources.TITLE[" + resource + "]") || resource);
    const translated = translate("Assets.NAME[" + prefabName + "]");
    let vehicleText: string = (translated && translated !== "null") ? `${translated} (${prefabName})` : prefabName;
    if (!displayPrefabName)
    {
        vehicleText = (translated && translated !== "null") ? translated : prefabName;
    }

    return (
      <div className={ModuleResolver.instance.SIPDropdownClasses.item}>
        <img className={ModuleResolver.instance.SIPDropdownClasses.thumb} src={vehicleIcon} data-src={vehicleIcon}/>
        <div className={ModuleResolver.instance.SIPDropdownClasses.label}>{vehicleText}</div>
      </div>
    );
}