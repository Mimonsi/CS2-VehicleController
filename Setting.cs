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
        public bool EnableImprovedTrainBehavior { get; set; } = true;

        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int MotorbikeMaxSpeed { get; set; } = 210;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int MotorbikeAcceleration { get; set; } = 10;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int MotorbikeBraking { get; set; } = 15;
        
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int ScooterMaxSpeed { get; set; } = 60;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int ScooterAcceleration { get; set; } = 6;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int ScooterBraking { get; set; } = 10;
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int CityCarMaxSpeed { get; set; } = 100;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int CityCarAcceleration { get; set; } = 12;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int CityCarBraking { get; set; } = 10;
        
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int HatchbackMaxSpeed { get; set; } = 160;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int HatchbackAcceleration { get; set; } = 8;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int HatchbackBraking { get; set; } = 15;
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int MinivanMaxSpeed { get; set; } = 180;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int MinivanAcceleration { get; set; } = 7;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int MinivanBraking { get; set; } = 15;
        
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int SedanMaxSpeed { get; set; } = 160;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int SedanAcceleration { get; set; } = 8;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int SedanBraking { get; set; } = 15;
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int SportsCarMaxSpeed { get; set; } = 260;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int SportsCarAcceleration { get; set; } = 11;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int SportsCarBraking { get; set; } = 18;
        
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int PickupMaxSpeed { get; set; } = 140;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int PickupAcceleration { get; set; } = 6;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int PickupBraking { get; set; } = 10;
        
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int SUVMaxSpeed { get; set; } = 160;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int SUVAcceleration { get; set; } = 6;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int SUVBraking { get; set; } = 10;
        
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int MuscleCarMaxSpeed { get; set; } = 240;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int MuscleCarAcceleration { get; set; } = 10;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int MuscleCarBraking { get; set; } = 16;
        
        
        
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int VanMaxSpeed { get; set; } = 140;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int VanAcceleration { get; set; } = 5;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int VanBraking { get; set; } = 11;
        
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
            MotorbikeMaxSpeed = 210;
            MotorbikeAcceleration = 10;
            MotorbikeBraking = 15;
            
            ScooterProbability = 50;
            ScooterMaxSpeed = 60;
            ScooterAcceleration = 6;
            ScooterBraking = 10;
            
            CityCarProbability = 100;
            CityCarMaxSpeed = 100;
            CityCarAcceleration = 12;
            CityCarBraking = 10;
            
            HatchbackProbability = 100;
            HatchbackMaxSpeed = 160; 
            HatchbackAcceleration = 8;
            HatchbackBraking = 15;
            
            MinivanProbability = 100;
            MinivanMaxSpeed = 180;
            MinivanAcceleration = 7;
            MinivanBraking = 15;
            
            SedanProbability = 100;
            SedanMaxSpeed = 160;
            SedanAcceleration = 8;
            SedanBraking = 15;
            
            SportsCarProbability = 100;
            SportsCarMaxSpeed = 260;
            SportsCarAcceleration = 11;
            SportsCarBraking = 18;
            
            PickupProbability = 100;
            PickupMaxSpeed = 140;
            PickupAcceleration = 6;
            PickupBraking = 10;
            
            SUVProbability = 100;
            SUVMaxSpeed = 160;
            SUVAcceleration = 6;
            SUVBraking = 10;
            
            MuscleCarProbability = 100;
            MuscleCarMaxSpeed = 240;
            MuscleCarAcceleration = 10;
            MuscleCarBraking = 16;
            
            VanProbability = 100;
            VanMaxSpeed = 140;
            VanAcceleration = 5;
            VanBraking = 11;
        }
        
        public void SetVanillaDefaults()
        {
            MotorbikeProbability = 100;
            MotorbikeMaxSpeed = 250;
            MotorbikeAcceleration = 8;
            MotorbikeBraking = 15;
            
            ScooterProbability = 100;
            ScooterMaxSpeed = 60;
            ScooterAcceleration = 8;
            ScooterBraking = 15;
            
            CityCarProbability = 100;
            CityCarMaxSpeed = 250;
            CityCarAcceleration = 8;
            CityCarBraking = 15;
            
            HatchbackProbability = 100;
            HatchbackMaxSpeed = 250; 
            HatchbackAcceleration = 8;
            HatchbackBraking = 15;
            
            MinivanProbability = 100;
            MinivanMaxSpeed = 250;
            MinivanAcceleration = 8;
            MinivanBraking = 15;
            
            SedanProbability = 100;
            SedanMaxSpeed = 250;
            SedanAcceleration = 8;
            SedanBraking = 15;
            
            SportsCarProbability = 100;
            SportsCarMaxSpeed = 250;
            SportsCarAcceleration = 8;
            SportsCarBraking = 15;
            
            PickupProbability = 100;
            PickupMaxSpeed = 250;
            PickupAcceleration = 8;
            PickupBraking = 15;
            
            SUVProbability = 100;
            SUVMaxSpeed = 250;
            SUVAcceleration = 8;
            SUVBraking = 15;
            
            MuscleCarProbability = 100;
            MuscleCarMaxSpeed = 300;
            MuscleCarAcceleration = 10;
            MuscleCarBraking = 16;
            
            VanProbability = 100;
            VanMaxSpeed = 250;
            VanAcceleration = 8;
            VanBraking = 15;
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
                
                values.Add(m_Setting.GetOptionLabelLocaleID($"{className}MaxSpeed"), $"{classFriendlyName} Max Speed");
                values.Add(m_Setting.GetOptionDescLocaleID($"{className}MaxSpeed"), $"Maximum speed for vehicles of this class.");
                
                values.Add(m_Setting.GetOptionLabelLocaleID($"{className}Acceleration"), $"{classFriendlyName} Acceleration");
                values.Add(m_Setting.GetOptionDescLocaleID($"{className}Acceleration"), $"Acceleration for vehicles of this class. Impacts how fast the vehicle can reach its maximum speed.");
                
                values.Add(m_Setting.GetOptionLabelLocaleID($"{className}Braking"), $"{classFriendlyName} Braking");
                values.Add(m_Setting.GetOptionDescLocaleID($"{className}Braking"), $"Braking for vehicles of this class. Impacts how fast the vehicle can stop.");
            }
            
            return values;
        }

        public void Unload()
        {
        }
    }
}
