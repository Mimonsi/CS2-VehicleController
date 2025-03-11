import classNames from "classnames";
import { Button, Tooltip } from "cs2/ui";
import { tool } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import mod from "mod.json";
import styles from "./ModIconButton.module.scss";
import rbIcon from "images/RB_ModIcon.svg";
import rbIconActive from "images/RB_ModIconActive.svg";

export default () => {
  const VehicleSelectorComponent = "Vehicle Controller";

  function toggleTool() {
    
  }

  return (
    <Tooltip tooltip="Vehicle Controller">
      <Button
        variant="floating"
        onSelect={toggleTool}
      >
      </Button>
    </Tooltip>
  );
};