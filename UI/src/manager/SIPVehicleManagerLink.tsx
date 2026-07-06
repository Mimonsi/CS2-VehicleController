import { trigger } from "cs2/api";
import { SelectedInfoSectionBase } from "cs2/bindings";
import React from "react";

import { ModuleResolver } from "../ModuleResolver";

const linkGroup = "VehicleController.Systems.VehicleManagerLinkSection";
const managerGroup = "VehicleController.VehicleManager";

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
              onClick={() => trigger(managerGroup, "openManager", props.prefabName)}
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
