using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using UnityEngine;
using VehicleController.Data;
using VehicleController.Systems;


// Settings structure inspired by Simple Mod Checker Plus by StarQ
namespace VehicleController
{
    [FileLocation("ModsSettings/VehicleController/VehicleController")]
    [SettingsUITabOrder(MainSection, SpawnBehaviorSection, VehiclePropertiesSection, VehicleSelectionSection, AboutSection, DebugSection)]
    [SettingsUIGroupOrder(MainGroup, VehicleProbabilityPackGroup, VehicleProbabilityGroup, VehiclePropertyPackGroup, VehiclePropertiesGroup, VehicleSelectionGroup, InfoGroup)]
    [SettingsUIShowGroupName(MainGroup, VehicleProbabilityPackGroup, VehicleProbabilityGroup, VehiclePropertyPackGroup, VehiclePropertiesGroup, VehicleSelectionGroup)]
    public class Setting : ModSetting
    {
        public static Setting Instance;
        
        public const string MainSection = "Settings";
        public const string MainGroup = "General Settings";
        
        public const string SpawnBehaviorSection = "Spawning Behavior";
        public const string VehicleProbabilityPackGroup = "Probability Settings";
        public const string VehicleProbabilityGroup = "Probability Settings";
        
        public const string VehiclePropertiesSection = "Vehicle Properties";
        public const string VehiclePropertiesGroup = "Vehicle Properties";
        public const string VehiclePropertyPackGroup = "Vehicle Property Pack";
        
        public const string VehicleSelectionSection = "Vehicle Selection";
        public const string VehicleSelectionGroup = "Vehicle Selection";
        
        
        public const string AboutSection = "About";
        public const string InfoGroup = "Info";
        
        public const string DebugSection = "Debug";
        public const string DebugGroup = "Debug";
        
        public Setting(IMod mod) : base(mod)
        {

        }
        
        #region MainSection

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
        
        private ProbabilityPack _currentProbabilityPack = ProbabilityPack.LoadFromFile("Default");
        public static int CurrentProbabilityPackVersion { get; set; }

        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityPackGroup)]
        [SettingsUIValueVersion(typeof(Setting), nameof(CurrentProbabilityPackVersion))]
        [SettingsUIDropdown(typeof(Setting), nameof(GetProbabilityPacksDropdownItems))]
        public string CurrentProbabilityPack
        {
            get => _currentProbabilityPack.Name;
            set
            {
                _currentProbabilityPack = ProbabilityPack.LoadFromFile(value);
                VehicleProbabilitySystem.Instance.LoadProbabilityPack(_currentProbabilityPack);
                ApplyProbabilityChanges = true;
            }
        }
        
        public DropdownItem<string>[] GetProbabilityPacksDropdownItems()
        {
            var names =  ProbabilityPack.GetPackNames();

            List<DropdownItem<string>> items = new List<DropdownItem<string>>();
            foreach(string s in names)
            {
                items.Add(new DropdownItem<string>()
                {
                    value = s,
                    displayName = s,
                });
            }
            Mod.log.Info("Displaying " + items.Count + " probability packs");
            return items.ToArray();
        }
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUIButton]
        public bool ApplyProbabilityChanges
        {
            set
            {
                VehicleProbabilitySystem.SaveValueChanges();
            }
        }
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MotorbikeProbability { get; set; } = 25;

        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int ScooterProbability { get; set; } = 50;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int CityCarProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int HatchbackProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MinivanProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SedanProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SportsCarProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int PickupProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int SUVProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int MuscleCarProbability { get; set; } = 100;
        
        [SettingsUISection(SpawnBehaviorSection, VehicleProbabilityGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int VanProbability { get; set; } = 100;
        
        /*[SettingsUISection(SpawnBehaviorSection, SpawnProbabilitiesGroup)]
        [SettingsUISlider(min = 0, max=200, step = 5, unit=Unit.kPercentage)]
        public int TrailerProbability { get; set; } = 100; // TODO: Implement this*/
        
        #endregion
        
        #region VehicleProperties
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        public bool EnableImprovedTrainBehavior { get; set; } = true;
        
        private string _currentVehicleClass = "Sedan";
        private static int CurrentVehicleClassVersion { get; set; }

        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUIValueVersion(typeof(Setting), nameof(CurrentVehicleClassVersion))]
        [SettingsUIDropdown(typeof(Setting), nameof(GetVehicleClassDropdownItems))]
        public string CurrentVehicleClass
        {
            get => _currentVehicleClass;
            set
            {
                _currentVehicleClass = value;
                Mod.log.Info("Current vehicle class set to: " + value);
            }
        }

        public DropdownItem<string>[] GetVehicleClassDropdownItems()
        {
            var items = new List<DropdownItem<string>>();
            foreach (var vehicleClass in VehicleClass.GetNames())
            {
                items.Add(new DropdownItem<string>()
                {
                    displayName = vehicleClass,
                    value = vehicleClass,
                });
                Mod.log.Info("Added vehicle class: " + vehicleClass);
            }

            if (items.Count == 0)
            {
                Mod.log.Info("No vehicle classes found, adding dummy classes");
                items.Add(new DropdownItem<string>()
                {
                    displayName = "Dummy Sedan",
                    value = "Sedan",
                });
            }
            
            return items.ToArray();
        }

        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUITextInput]
        public string PackName { get; set; } = "My Custom Pack";

        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUIButton]
        public bool ExportPack
        {
            set
            {
                // TODO: Implement
            }
        }

        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 5, max=400, step = 5, unit=Unit.kInteger)]
        public int VehicleMaxSpeed { get; set; } = 210;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int VehicleAcceleration { get; set; } = 10;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUISlider(min = 1, max=50, step = 1, unit=Unit.kInteger)]
        public int VehicleBraking { get; set; } = 15;
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertiesGroup)]
        [SettingsUIButton]
        public bool SavePropertyChanges
        {
            set => VehiclePropertySystem.Instance.ApplySettings();
        }
        
        #endregion
        
        #region VehicleSelection

        [SettingsUISection(VehicleSelectionSection, VehicleSelectionGroup)]
        public bool DeleteVehicleInstances { get; set; } = false;
        
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
        
        #region Debug Options
        
        [SettingsUISection(DebugSection, DebugGroup)]
        [SettingsUIAdvanced]
        public bool CreateExamplePack
        {
            set => ProbabilityPack.Example().SaveToFile();
        }
        
        [SettingsUISection(DebugSection, DebugGroup)]
        [SettingsUIAdvanced]
        public bool CountPrefabInstances
        {
            set => VehicleCounterSystem.Instance.CountPrefabInstances();
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
            
            values.Add(m_Setting.GetOptionLabelLocaleID($"VehicleMaxSpeed"), $"Selected Class Max Speed");
            values.Add(m_Setting.GetOptionDescLocaleID($"VehicleMaxSpeed"), $"Maximum speed for vehicles of this class.");
                
            values.Add(m_Setting.GetOptionLabelLocaleID($"VehicleAcceleration"), $"Selected Class Acceleration");
            values.Add(m_Setting.GetOptionDescLocaleID($"VehicleAcceleration"), $"Acceleration for vehicles of this class. Impacts how fast the vehicle can reach its maximum speed.");
                
            values.Add(m_Setting.GetOptionLabelLocaleID($"VehicleBraking"), $"Selected Class Braking");
            values.Add(m_Setting.GetOptionDescLocaleID($"VehicleBraking"), $"Braking for vehicles of this class. Impacts how fast the vehicle can stop.");

            // TODO: Values for planes and other vehicle types


            values.Add(m_Setting.GetOptionLabelLocaleID(nameof(Setting.CreateExamplePack)), "Create Example Pack");
            values.Add(m_Setting.GetOptionDescLocaleID(nameof(Setting.CreateExamplePack)), "Create an example probability pack with some default values. This will create a file in the ModsData folder of VehicleController.");
            values.Add(m_Setting.GetOptionLabelLocaleID(nameof(Setting.CountPrefabInstances)), "Count Prefab Instances");
            values.Add(m_Setting.GetOptionDescLocaleID(nameof(Setting.CountPrefabInstances)), "Counts occurrences of each prefab in the game and logs them to the console. Useful for debugging and understanding how many instances of each prefab are present in the game.");
            values.Add(m_Setting.GetOptionTabLocaleID(nameof(Setting.DebugSection)), "Debug");
            values.Add(m_Setting.GetOptionGroupLocaleID(nameof(Setting.DebugGroup)), "Debugging Tools");
            
            return values;
        }

        public void Unload()
        {
        }
    }
}
