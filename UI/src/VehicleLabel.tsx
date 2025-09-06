import { useLocalization    } from "cs2/l10n";
import { ModuleResolver     } from "./ModuleResolver";
import React from "react";
import {Icon} from "cs2/ui";

interface VehicleLabelProps
{
    prefabName: string,
    selected?: boolean,
    image?: string,
}

// Custom component to combine the icon and label for a resource
// so the game does not split the icon from the label when wrapping in the company selector.
export const VehicleLabel = ({ prefabName, selected, image }: VehicleLabelProps) =>
{
    //console.log("vehicleLabel", prefabName, selected, image);
    // Get game's icon for the vehicle.
    //const resourceIcon: string = "Media/Game/Resources/" + resource + ".svg";
    const vehicleIcon: string = image ? image : "Media/Game/Icons/GenericVehicle.svg";
    //const vehicleIcon: string = image ? image : "assetdb://Global/292d20d2f0c5403c5d3bc3452649fde9";
    const checkmarkIcon: string = "Media/Glyphs/Checkmark.svg";
    //selected = true;

    // Get the game's translated text for the resource.
    // The game uses "+" for the concatenator character for all languages.
    const { translate } = useLocalization();
    //const resourceText: string = (translate("Resources.TITLE[" + resource + "]") || resource);
    const translated = translate("Assets.NAME[" + prefabName + "]");
    let vehicleText: string = (translated && translated !== "null") ? `${translated} (${prefabName})` : prefabName;
    
    // If prefabName contains "Vehicles selected", change the vehicleText to "Vehicles selected".
    if (prefabName === "0 Vehicles Selected")
    {
        vehicleText = "No custom selection"
    }
    else if (prefabName.includes("Vehicles Selected"))
    {
        vehicleText = prefabName;
    }

    //        <img className={styles.thumb} src={vehicleIcon} />
    
    //{/*<div style={{ "--checkmark-url": `url(${checkmarkIcon})` } as React.CSSProperties} className={styles.checkmark}></div>*/} 
  /*
<div>}
{selected && (
  <div>x</div>
)}
</div>
   */

  // This would be better for the icon and more accurate, but the images are not square: <Icon className={ModuleResolver.instance.SIPDropdownClasses.thumb} src={vehicleIcon}></Icon>
    return (
      <div className={ModuleResolver.instance.SIPDropdownClasses.item}>
          <Icon className={ModuleResolver.instance.SIPDropdownClasses.thumb} src={vehicleIcon}></Icon>
        
        <div className={ModuleResolver.instance.SIPDropdownClasses.label}>{vehicleText}</div>
      </div>
    );
}