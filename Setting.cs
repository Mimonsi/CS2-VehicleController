using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using VehicleController.Systems;

namespace VehicleController
{
    [FileLocation($"ModsSettings/{nameof(VehicleController)}/{nameof(VehicleController)}")]
    [SettingsUIGroupOrder(kSettingsGroup, kProbabilitiesGroup)]
    [SettingsUIShowGroupName(kSettingsGroup, kProbabilitiesGroup)]
    public class Setting : ModSetting
    {
        public static Setting Instance;
        public const string kMainSection = "Settings";
        public const string kSettingsGroup = "GeneralSettings";
        public const string kProbabilitiesGroup = "VehicleTypePropabilitySettings";
        public Setting(IMod mod) : base(mod)
        {

        }
        
        #region General Settings
        
        [SettingsUISection(kMainSection, kSettingsGroup)]
        [SettingsUIButton]
        public bool ApplyChanges
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

        [SettingsUISection(kMainSection, kSettingsGroup)]
        public bool ResetSettings
        {
            set
            {
                SetDefaults();
            }
        }
        
        [SettingsUISection(kMainSection, kSettingsGroup)]
        public bool ResetSettingsVanilla
        {
            set
            {
                SetVanillaDefaults();
            }
        }


        [SettingsUISection(kMainSection, kSettingsGroup)]
        public bool EnableImprovedCarBehaviour { get; set; } = true;
        
        [SettingsUISection(kMainSection, kSettingsGroup)]
        public bool EnableImprovedTrainBehaviour { get; set; } = true;

        #endregion
        

        #region Probabilities
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MotorbikeProbability { get; set; } = 25;

        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int ScooterProbability { get; set; } = 50;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int CityCarProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int HatchbackProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MinivanProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SedanProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SportsCarProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int PickupProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SUVProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MuscleCarProbability { get; set; } = 100;
        
        [SettingsUISection(kMainSection, kProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int VanProbability { get; set; } = 100;
        
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
            var values = new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Vehicle Controller" },
                {m_Setting.GetOptionGroupLocaleID(Setting.kSettingsGroup), "General Settings"},
                {m_Setting.GetOptionGroupLocaleID(Setting.kProbabilitiesGroup), "Probability Settings"},
                

                /*{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.MotorbikeProbability)), "Motorcycle Probability" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.MotorbikeProbability)),
                    $"Probability to spawn motorcycles. Default is 25%. 100% will spawn as many motorcycles as in vanilla, 0% will disable motorcycles."
                },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ScooterProbability)), "Scooter Probability" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ScooterProbability)),
                    $"Probability to spawn scooters. Default is 50%. 100% will spawn as many scooters as in vanilla, 0% will disable motorcycles."
                },*/
                
                {m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableImprovedCarBehaviour)), "Enable Improved Car Behaviour"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableImprovedCarBehaviour)),
                    $"Enable changed parameters for cars. This will impact acceleration, braking, and max speed. Requires restart."
                },
                
                {m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableImprovedTrainBehaviour)), "Enable Improved Train Behaviour"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableImprovedTrainBehaviour)),
                    $"Enable changed parameters for trains. This will impact acceleration, braking, and max speed. Requires restart."
                },

                {m_Setting.GetOptionLabelLocaleID(nameof(Setting.ApplyChanges)), "Apply Changes"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ApplyChanges)),
                    $"Apply the changes to the probabilities."
                },

                /*{m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeleteInstances)), "Delete motorcycle instances"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.DeleteInstances)),
                    $"Deletes a percentage of all existing motorcycles and scooters. Example: If 'Motorcycle Probability' is set to 25, 25% of all motorcycles will be deleted."
                },*/
                
                {m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Reset settings to default"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)),
                    $"Reset settings to default mod values."
                },
                
                {m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettingsVanilla)), "Reset settings to vanilla"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettingsVanilla)),
                    $"Reset settings to vanilla values. These settings will causes the same ratios of vehicles to spawn as in the base game."
                },
            };
            
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
