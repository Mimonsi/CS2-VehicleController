import { ModRegistrar } from "cs2/modding";
import VehicleSelectorComponent from "mods/VehicleSelectorComponent";

const register: ModRegistrar = (moduleRegistry) => {

    moduleRegistry.append('GameTopLeft', VehicleSelectorComponent);
}

export default register;