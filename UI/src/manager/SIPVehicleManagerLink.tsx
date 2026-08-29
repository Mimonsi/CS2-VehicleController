import { SelectedInfoSectionBase } from "cs2/bindings";
import React from "react";

import { ModuleResolver } from "../ModuleResolver";
import { openManagerFor } from "./api";

const linkGroup = "VehicleController.Systems.VehicleManagerLinkSection";

// Selected-Info-Panel section: shows an "Open in Vehicle Manager" button for the selected vehicle,
// which opens the manager window focused on that prefab (see VehicleManagerLinkSection.cs).
export const SIPVehicleManagerLink = (componentList: any): any => {
  interface VehicleManagerLinkSection extends SelectedInfoSectionBase {
    prefabName: string;
  }

  componentList[linkGroup] = (props: VehicleManagerLinkSection) => {
    return (
      <ModuleResolver.instance.InfoSection>
        <ModuleResolver.instance.InfoRow
          left={"Vehicle Controller"}
          uppercase={true}
          right={
            <div
              onClick={() => openManagerFor(props.prefabName)}
              style={{
                cursor: "pointer",
                padding: "3rem 10rem",
                borderRadius: "3rem",
                border: "1rem solid rgba(255, 255, 255, 0.25)",
                fontSize: "13rem",
              }}
            >
              Open in Vehicle Manager
            </div>
          }
          disableFocus={true}
        />
      </ModuleResolver.instance.InfoSection>
    );
  };

  return componentList as any;
};
