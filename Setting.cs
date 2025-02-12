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

        [SettingsUIButton]
        public bool ApplyChanges
        {
            set
            {
                VehicleControllerSystem.Instance.ApplySettings();
            }
        }

        [SettingsUIButton]
        public bool DeleteInstances
        {
            set
            {
                VehicleControllerSystem.Instance.DeleteInstances();
            }
        }

        public override void SetDefaults()
        {
            MotorbikeProbability = 25;
            ScooterProbability = 50;
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
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Less Motorcycles" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MotorbikeProbability)), "Motorcycle Probability" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.MotorbikeProbability)),
                    $"Probability to spawn motorcycles. Default is 25%. 100% will spawn as many motorcycles as in vanilla, 0% will disable motorcycles."
                },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ScooterProbability)), "Scooter Probability" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ScooterProbability)),
                    $"Probability to spawn scooters. Default is 50%. 100% will spawn as many scooters as in vanilla, 0% will disable motorcycles."
                },

                {m_Setting.GetOptionLabelLocaleID(nameof(Setting.ApplyChanges)), "Apply Changes"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ApplyChanges)),
                    $"Apply the changes to the probabilities."
                },

                {m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeleteInstances)), "Delete motorcycle instances"},
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.DeleteInstances)),
                    $"Deletes a percentage of all existing motorcycles and scooters. Example: If 'Motorcycle Probability' is set to 25, 25% of all motorcycles will be deleted."
                },
            };
        }

        public void Unload()
        {
        }
    }
}
