import {bindValue, trigger, useValue} from "cs2/api";
import { SelectedInfoSectionBase    } from "cs2/bindings";
import { useLocalization            } from "cs2/l10n";
import {FormattedParagraphs, } from "cs2/ui";
import { VehicleSelector            } from "./VehicleSelector";
import { ProbabilityComponent            } from "./ProbabilityComponent";
import { ClipboardActions } from "./ClipboardActions";
import { ModuleResolver             } from "./ModuleResolver";
import   mod                          from "../mod.json";
import React from "react";
import {Wrapbox} from "./Wrapbox";

const uilStandard =                          "coui://uil/Standard/";
const uilColored =                           "coui://uil/Colored/";
const minimizeSrc =                     uilStandard + "ArrowsMinimize.svg";
const expandSrc =                       uilStandard + "ArrowsExpand.svg";

const group = "VehicleController.Systems.VehiclePropertiesSection";
const Minimized$ = bindValue<boolean>(group, "Minimized");
export const SipVehicleProperties = (componentList: any): any =>
{

  // Define props for change vehicle section.
  // Adapted from bindings.d.ts for the game's sections.
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

  // Add ChangeVehicleSection to the component list.
  // Make sure section name is unique by including the mod id.
  componentList["VehicleController.Systems.VehiclePropertiesSection"] = (props: VehiclePropertiesSection) =>
  {
    const Minimized = useValue(Minimized$);
    // Get the mod's translated text for the section heading and button.
    const { translate } = useLocalization();
    const sectionHeading: string = translate(mod.id + ".VehicleController") || "Vehicle Controller";
    const probabilityHeading: string = translate(mod.id + ".Probability"    ) || "Probability";

    // Get the mod's translated formatted tooltip text based on property type.
    //const tooltipText: string = translate(mod.id + ".SectionTooltip" + PropertyType[props.propertyType]) ||
    //    "Select a company from the dropdown and click Change Now.";
    const probabilityTooltipText = "Change vehicle **probabilities** for this prefab. Changes will be applied to all vehicles of this prefab. Values range from 0-255, 100 being the default. Vehicles with a probability of 200 will appear twice as often as vehicles with a probability of 100, while vehicles with a probability of 50 will appear half as often. 0 probability **DOES NOT** mean the vehicle will stop to appear at all.";
    //const formattedParagraphsProps: FormattedParagraphsProps = { children: tooltipText };
    //const formattedTooltip: JSX.Element = ModuleResolver.instance.FormattedParagraphs(formattedParagraphsProps);

    function onApplyProbabilitiesClicked()
    {
      trigger(group, "ApplyProbabilitiesClicked");
    }

    function onApplyPropertiesClicked()
    {
      trigger(group, "ApplyPropertiesClicked");
    }
    function minimizeClick()
    {
      trigger(group, "Minimize");
    }

    // Construct the change company section.
    // Info row 1 has section heading and Change Now button.
    // Info row 2 has the actual dropdown
    // Info row 3 has two additional buttons
    // All further rows are for actions buttons, e.g. copy, paste, export, etc.
    return (
      <ModuleResolver.instance.InfoSection>
        <ModuleResolver.instance.InfoRow
          left={sectionHeading}
          uppercase={true}
          right={
            <ModuleResolver.instance.ToolButton
              src={Minimized? expandSrc : minimizeSrc}
              focusKey={ModuleResolver.instance.FOCUS_DISABLED}
              tooltip = {Minimized? "Expand" : "Minimize"}
              className = {ModuleResolver.instance.toolButtonTheme.button}
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
                <>
                  <ModuleResolver.instance.ToolButton
                    src = {"coui://uil/Standard/Checkmark.svg"}
                    focusKey={ModuleResolver.instance.FOCUS_DISABLED}
                    selected={false}
                    tooltip = {"Apply to all vehicles of this prefab"}
                    className = {ModuleResolver.instance.toolButtonTheme.button}
                    onSelect={() => onApplyProbabilitiesClicked()}
                  />
                </>
              }
              disableFocus={false}
            />
            <ModuleResolver.instance.InfoRow
              left={
              <ProbabilityComponent text={"Probability"} initialValue={100}/>
              }
              uppercase={false}
              tooltip={<FormattedParagraphs>Hello **there**</FormattedParagraphs>}
              disableFocus={false}
            />
          </>
        )}
      </ModuleResolver.instance.InfoSection>
    );
  }

  // Return the updated component list.
  return componentList as any;
}