using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Environment;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;

namespace VehicleController
{
    [FileLocation($"ModsSettings/{nameof(VehicleController)}/{nameof(VehicleController)}")]
    public class Setting : ModSetting
    {
        public static Setting Instance;
        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MotorbikeProbability { get; set; } = 25;

        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int ScooterProbability { get; set; } = 50;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int CityCarProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int HatchbackProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MinivanProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SedanProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SportsCarProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int PickupProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SUVProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MuscleCarProbability { get; set; } = 100;
        
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int VanProbability { get; set; } = 100;
        

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

        public bool ResetSettings
        {
            set
            {
                SetDefaults();
            }
        }
        
        public bool ResetSettingsVanilla
        {
            set
            {
                SetVanillaDefaults();
            }
        }
        
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
