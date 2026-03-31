import {bindValue, trigger, useValue} from "cs2/api";
import { SelectedInfoSectionBase    } from "cs2/bindings";
import { useLocalization            } from "cs2/l10n";
import {FormattedParagraphs, } from "cs2/ui";
import { ProbabilityComponent            } from "./ProbabilityComponent";
import { ModuleResolver             } from "./ModuleResolver";
import   mod                          from "../mod.json";
import React, { useState, useEffect } from "react";

const uilStandard =                          "coui://uil/Standard/";
const minimizeSrc =                     uilStandard + "ArrowsMinimize.svg";
const expandSrc =                       uilStandard + "ArrowsExpand.svg";

const group = "VehicleController.Systems.VehiclePropertiesSection";
const Minimized$ = bindValue<boolean>(group, "Minimized");

export const SipVehicleProperties = (componentList: any): any =>
{
  interface VehiclePropertiesSection extends SelectedInfoSectionBase
  {
    prefabName: string,
    overrideProbability: boolean,
    probability: number,
    overrideProperties: boolean,
    maxSpeed: number,
    acceleration: number,
    braking: number,
  }

  componentList["VehicleController.Systems.VehiclePropertiesSection"] = (props: VehiclePropertiesSection) =>
  {
    const Minimized = useValue(Minimized$);
    const { translate } = useLocalization();
    const sectionHeading: string = translate(mod.id + ".VehicleController") || "Vehicle Controller";

    const [pendingProbability, setPendingProbability] = useState(props.probability);

    // Reset local state when the selected vehicle changes
    useEffect(() =>
    {
      setPendingProbability(props.probability);
    }, [props.probability, props.prefabName]);

    const probabilityTooltipText = "Change the spawn **probability** for this vehicle prefab. Values range from 0–255 (default 100). A value of 200 makes this vehicle appear twice as often; 50 makes it appear half as often.";

    function onApplyProbabilityClicked()
    {
      trigger(group, "ApplyProbabilityClicked", pendingProbability);
    }

    function minimizeClick()
    {
      trigger(group, "Minimize");
    }

    return (
      <ModuleResolver.instance.InfoSection>
        <ModuleResolver.instance.InfoRow
          left={sectionHeading}
          uppercase={true}
          right={
            <ModuleResolver.instance.ToolButton
              src={Minimized ? expandSrc : minimizeSrc}
              focusKey={ModuleResolver.instance.FOCUS_DISABLED}
              tooltip={Minimized ? "Expand" : "Minimize"}
              className={ModuleResolver.instance.toolButtonTheme.button}
              onSelect={() => minimizeClick()}
            />
          }
          disableFocus={true}
        />
        {!Minimized && (
          <>
            <ModuleResolver.instance.InfoRow
              left={"Probability"}
              uppercase={false}
              tooltip={<FormattedParagraphs>{probabilityTooltipText}</FormattedParagraphs>}
              right={
                <ModuleResolver.instance.ToolButton
                  src={"coui://uil/Standard/Checkmark.svg"}
                  focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                  selected={props.overrideProbability}
                  tooltip={"Apply to all vehicles of this prefab"}
                  className={ModuleResolver.instance.toolButtonTheme.button}
                  onSelect={() => onApplyProbabilityClicked()}
                />
              }
              disableFocus={false}
            />
            <ModuleResolver.instance.InfoRow
              left={<ProbabilityComponent value={pendingProbability} onChange={setPendingProbability} />}
              uppercase={false}
              disableFocus={false}
            />
          </>
        )}
      </ModuleResolver.instance.InfoSection>
    );
  }

  return componentList as any;
}
