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
