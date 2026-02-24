import { ModRegistrar } from "cs2/modding";

import { SIPVehicleSelectorComponent } from "./SIPVehicleSelectorComponent"
import mod from "../mod.json";
import {SipVehicleProperties} from "./SIPVehicleProperties";

const register: ModRegistrar = (moduleRegistry) =>
{
    // Add this mod's component to the selected info sections.
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", "selectedInfoSectionComponents", SIPVehicleSelectorComponent)
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", "selectedInfoSectionComponents", SipVehicleProperties)

    // Registration is complete.
    console.log(mod.id + " registration complete.");
}

export default register;