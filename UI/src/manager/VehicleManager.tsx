import { FloatingButton, Portal } from "cs2/ui";
import React, { useState } from "react";

import { VehicleManagerPanel } from "./VehicleManagerPanel";

const buttonSrc = "coui://uil/Standard/Car.svg";

// Appended to "GameTopRight": a toggle button that opens the floating
// Vehicle Manager window. Open state lives here (M2 prototype has no C# yet);
// the panel is rendered through a Portal so it floats above the game UI.
export const VehicleManager = () => {
  const [open, setOpen] = useState(false);

  return (
    <>
      <FloatingButton
        src={buttonSrc}
        selected={open}
        tooltipLabel={"Vehicle manager"}
        onSelect={() => setOpen(o => !o)}
      />
      {open && (
        <Portal>
          <VehicleManagerPanel onClose={() => setOpen(false)} />
        </Portal>
      )}
    </>
  );
};
