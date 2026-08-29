import { useValue } from "cs2/api";
import { FloatingButton, Portal } from "cs2/ui";
import React, { useEffect, useRef, useState } from "react";

import { openRequest$ } from "./api";
import { VehicleManagerPanel } from "./VehicleManagerPanel";
import vcLogo from "../images/VC.png";

const buttonSrc = vcLogo;

// Appended to "GameTopLeft": a toggle button that opens the floating Vehicle Manager window.
// The panel is rendered through a Portal so it floats above the game UI.
export const VehicleManager = () => {
  const [open, setOpen] = useState(false);
  const [focusPrefab, setFocusPrefab] = useState<string | null>(null);
  const lastNonce = useRef(0);

  const openReq = useValue(openRequest$);
  useEffect(() => {
    try {
      const r = JSON.parse(openReq);
      if (r && typeof r.nonce === "number" && r.nonce !== lastNonce.current) {
        lastNonce.current = r.nonce;
        setFocusPrefab(r.prefab ?? null);
        setOpen(true);
      }
    } catch (e) {
      // ignore malformed requests
    }
  }, [openReq]);

  return (
    <>
      <FloatingButton
        src={buttonSrc}
        tinted
        selected={open}
        tooltipLabel={"Vehicle manager"}
        onSelect={() => setOpen(o => !o)}
      />
      {open && (
        <Portal>
          <VehicleManagerPanel
            onClose={() => {
              setOpen(false);
              setFocusPrefab(null);
            }}
            focusPrefab={focusPrefab}
          />
        </Portal>
      )}
    </>
  );
};
