import { useLocalization    } from "cs2/l10n";

import { ModuleResolver     } from "moduleResolver";
import   styles               from "vehicleLabel.module.scss";

// Props for ResourceIconLabel.
interface VehicleLabelProps
{
    prefabName: string,
}

// Custom component to combine the icon and label for a resource
// so the game does not split the icon from the label when wrapping in the company selector.
export const VehicleLabel = ({ prefabName }: VehicleLabelProps) =>
{
    // Get game's icon for the vehicle.
    //const resourceIcon: string = "Media/Game/Resources/" + resource + ".svg";
    const defaultVehicleIcon: string = "Media/Game/Icons/Taxi.svg";

    // Get the game's translated text for the resource.
    // The game uses "+" for the concatenator character for all languages.
    const { translate } = useLocalization();
    //const resourceText: string = (translate("Resources.TITLE[" + resource + "]") || resource);
    const vehicleText: string = prefabName;
    //const vehicleText: string = (translate("Assets.NAME[" + prefabName + "]") || prefabName);

    // Return the resource icon and label.
    return (
        <div className={styles.resourceIconLabel}>
            <img className={ModuleResolver.instance.CompanySectionClasses.icon} src={defaultVehicleIcon} />
            <div className={styles.resourceLabel}>{vehicleText}</div>
        </div>
    );
}