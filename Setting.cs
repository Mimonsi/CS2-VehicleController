using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using UnityEngine;
using VehicleController.Systems;


// Settings structure inspired by Simple Mod Checker Plus by StarQ
namespace VehicleController
{
    [FileLocation("ModsSettings/VehicleController/VehicleController")]
    [SettingsUITabOrder(MainSection, SpawnBehaviorSection, VehiclePropertiesSection, AboutSection)]
    [SettingsUIGroupOrder(MainGroup, SpawnProbabilitiesGroup, VehiclePropertiesGroup, InfoGroup)]
    [SettingsUIShowGroupName(MainGroup, SpawnProbabilitiesGroup)]
    public class Setting : ModSetting
    {
        public static Setting Instance;
        
        public const string MainSection = "Settings";
        public const string MainGroup = "General Settings";
        
        public const string SpawnBehaviorSection = "Spawning Behavior";
        public const string SpawnProbabilitiesGroup = "Probability Settings";
        
        public const string VehiclePropertiesSection = "Vehicle Properties";
        public const string VehiclePropertiesGroup = "Vehicle Properties";
        
        
        public const string AboutSection = "About";
        public const string InfoGroup = "Info";
        
        public Setting(IMod mod) : base(mod)
        {

        }
        
        #region MainSection
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUIButton]
        public bool ApplyProbabilityChanges
        {
            set
            {
                VehicleControllerSystem.Instance.ApplySettings();
            }
        }

        /*[SettingsUIButton]
        public bool DeleteInstances
        {
            set
            {
                VehicleControllerSystem.Instance.DeleteInstances();
            }
        }*/

        [SettingsUISection(MainSection, MainGroup)]
        public bool ResetSettings
        {
            set
            {
                SetDefaults();
            }
        }
        
        [SettingsUISection(MainSection, MainGroup)]
        public bool ResetSettingsVanilla
        {
            set
            {
                SetVanillaDefaults();
            }
        }
        #endregion
        
        #region Probabilities
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MotorbikeProbability { get; set; } = 25;

        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int ScooterProbability { get; set; } = 50;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int CityCarProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int HatchbackProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MinivanProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SedanProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SportsCarProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int PickupProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SUVProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MuscleCarProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int VanProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int TrailerProbability { get; set; } = 100; // TODO: Implement this
        
        #endregion
        
        #region VehicleProperties
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        public bool EnableImprovedCarBehavior { get; set; } = true;
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        public bool EnableImprovedTrainBehavior { get; set; } = true;

        #endregion
        
        #region About
        
        [SettingsUISection(AboutSection, InfoGroup)]
        public string NameText => Mod.Name;

        [SettingsUISection(AboutSection, InfoGroup)]
        public string VersionText => Mod.Version;

        [SettingsUISection(AboutSection, InfoGroup)]
        public string AuthorText => "Mimonsi";

        [SettingsUIButtonGroup("Social")]
        [SettingsUIButton]
        [SettingsUISection(AboutSection, InfoGroup)]
        public bool KofiLink
        {
            set
            {
                try
                {
                    Application.OpenURL($"https://ko-fi.com/mimonsi");
                }
                catch (Exception e)
                {
                    Mod.log.Info(e);
                }
            }
        }
        [SettingsUIButtonGroup("Social")]
        [SettingsUIButton]
        [SettingsUISection(AboutSection, InfoGroup)]
        public bool Discord
        {
            set
            {
                try
                {
                    Application.OpenURL($"https://discord.com/channels/1024242828114673724/1330910837397000234");
                }
                catch (Exception e)
                {
                    Mod.log.Info(e);
                }
            }
        }
        
        #endregion
        
        public override void SetDefaults()
        {
            MotorbikeProbability = 25;
            ScooterProbability = 50;
            CityCarProbability = 100;
            HatchbackProbability = 100;
            MinivanProbability = 100;
            SedanProbability = 100;
            SportsCarProbability = 100;
            PickupProbability = 100;
            SUVProbability = 100;
            MuscleCarProbability = 100;
            VanProbability = 100;
        }
        
        public void SetVanillaDefaults()
        {
            MotorbikeProbability = 100;
            ScooterProbability = 100;
            CityCarProbability = 100;
            HatchbackProbability = 100;
            MinivanProbability = 100;
            SedanProbability = 100;
            SportsCarProbability = 100;
            PickupProbability = 100;
            SUVProbability = 100;
            MuscleCarProbability = 100;
            VanProbability = 100;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            var values = new Dictionary<string, string>();
            foreach(var (className, classFriendlyName) in VehicleClass.GetSettingClassNames())
            {
                values.Add(m_Setting.GetOptionLabelLocaleID($"{className}Probability"), $"{classFriendlyName} Probability");
                values.Add(m_Setting.GetOptionDescLocaleID($"{className}Probability"), $"Probability to spawn {classFriendlyName}. Default is 100%. 100% will spawn as many {className} as in vanilla, 0% will disable {className}.");
            }
            
            return values;
        }

        public void Unload()
        {
        }
    }
}
