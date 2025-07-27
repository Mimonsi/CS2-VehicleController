import React from "react";
import { getModule } from "cs2/modding";

const InfoButton = getModule(
  "game-ui/game/components/selected-info-panel/shared-components/info-button/info-button.tsx",
  "InfoButton"
);

// Debug-Ausgabe
console.log("InfoButton type:", typeof InfoButton, "value:", InfoButton);

interface ActionButtonProps {
  text: string;
}

// This components create buttons allowing the player to copy and paste the vehicle selection
// Options include copying to other buildings of the exact same prefab, copying to all buildings of the same type, copying to clipboard. Also differentiate between district-wide copying and
// city-wide copying (global)
export const ActionButtons = ({ text }: ActionButtonProps) => {
  return (
    <>
      {InfoButton ? (
        <InfoButton
          label={text}
          icon={"Media/Game/Icons/GenericVehicle.svg"}
          selected={false}
          onSelect={() => console.log("Button clicked!")}
        />
      ) : (
        <div>InfoButton not found</div>
      )}
    </>
  );
};
