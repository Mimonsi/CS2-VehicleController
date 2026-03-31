import { useLocalization    } from "cs2/l10n";
import { ModuleResolver     } from "./ModuleResolver";
import React from "react";
import {Icon} from "cs2/ui";

interface ProbabilityComponentProps
{
    text: string,
    initialValue: number,
}
//<input type="text">Input</input>
// Custom component to combine the icon and label for a resource
// so the game does not split the icon from the label when wrapping in the company selector.
export const ProbabilityComponent = ({ text, initialValue }: ProbabilityComponentProps) =>
{
    const { translate } = useLocalization();
    
    return (
      <div>
          <input className="hex-input_hFc" type="text" vk-title="a" vk-description="" vk-type="text"/>
      </div>
    );
}