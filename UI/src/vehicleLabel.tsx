import { useLocalization    } from "cs2/l10n";

import { ModuleResolver     } from "moduleResolver";
import React from "react";
import {getModule} from "cs2/modding";
const styles = getModule("game-ui/game/components/selected-info-panel/selected-info-sections/route-sections/select-vehicles-section.module.scss", "classes");
// Props for ResourceIconLabel.
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
    console.log("vehicleLabel", prefabName, selected);
    // Get game's icon for the vehicle.
    //const resourceIcon: string = "Media/Game/Resources/" + resource + ".svg";
    const vehicleIcon: string = image ? image : "Media/Game/Icons/Taxi.svg";
    const checkmarkIcon: string = "Media/Glyphs/Checkmark.svg";
    //selected = true;

    // Get the game's translated text for the resource.
    // The game uses "+" for the concatenator character for all languages.
    const { translate } = useLocalization();
    //const resourceText: string = (translate("Resources.TITLE[" + resource + "]") || resource);
    const translated = translate("Assets.NAME[" + prefabName + "]");
    let vehicleText: string = (translated && translated !== "null") ? `${translated} (${prefabName})` : prefabName;
    if (selected)
    {
        vehicleText = `[✓] ${vehicleText}`;
    }
    else
    {
        vehicleText = `[ ] ${vehicleText}`;
    }
    
    // If prefabName contains "Vehicles selected", change the vehicleText to "Vehicles selected".
    if (prefabName === "0 Vehicles Selected")
    {
        vehicleText = "No custom selection"
    }
    else if (prefabName.includes("Vehicles Selected"))
    {
        vehicleText = prefabName;
    }


    //{/*<div style={{ "--checkmark-url": `url(${checkmarkIcon})` } as React.CSSProperties} className={styles.checkmark}></div>*/} 
  /*
<div>}
{selected && (
  <div>x</div>
)}
</div>
   */
    // Return the resource icon and label.
  
  //        <div className={styles.dropdownRow}>
  //           {/* Insert checkbox here */}
  //           <img className={ModuleResolver.instance.CompanySectionClasses.icon} src={vehicleIcon} />
  //           <div className={styles.resourceLabel}>{vehicleText}</div>
  //         </div>
  //
    return (
      <div className={styles.item}>
        <img className={styles.thumb} src={vehicleIcon} />
        <div className={styles.label}>{vehicleText}</div>
      </div>
    );
}