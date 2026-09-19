import { ModRegistrar } from "cs2/modding";

import { SIPVehicleSelectorComponent } from "./SIPVehicleSelectorComponent"
import mod from "../mod.json";
import {SipVehicleProperties} from "./SIPVehicleProperties";
import { VehicleManager } from "./manager/VehicleManager";
import { SIPVehicleManagerLink } from "./manager/SIPVehicleManagerLink";

const register: ModRegistrar = (moduleRegistry) =>
{
    // Add this mod's component to the selected info sections.
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", "selectedInfoSectionComponents", SIPVehicleSelectorComponent)
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", "selectedInfoSectionComponents", SipVehicleProperties)
    // "Open in Vehicle Manager" button on a selected vehicle.
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", "selectedInfoSectionComponents", SIPVehicleManagerLink)

    // M2 prototype: floating Vehicle Manager window, opened via a top-left button.
    moduleRegistry.append("GameTopLeft", VehicleManager);

    // Registration is complete.
    console.log(mod.id + " registration complete.");
}

export default register;