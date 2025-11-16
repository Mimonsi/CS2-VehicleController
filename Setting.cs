using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
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
    public enum LogLevel {
        Verbose,
        Debug,
        Info,
        Warning,
        Error,
        Disabled
    }
    
    public enum SpeedLimitOverride
    {
        None,
        Half,
        Double,
        Speed // x10
    }
    
    /// <summary>
    /// Stores all mod settings and exposes them to the game UI.
    /// </summary>
    [FileLocation("ModsSettings/VehicleController/VehicleController")]
    [SettingsUITabOrder(MainSection, SpawnBehaviorSection, VehiclePropertiesSection, VehicleSelectionSection, AboutSection, DebugSection)]
    [SettingsUIGroupOrder(MainGroup, VehicleProbabilityPackGroup, VehicleProbabilityGroup, VehiclePropertyPackGroup, VehiclePropertiesGroup, VehicleSelectionGroup, InfoGroup)]
    [SettingsUIShowGroupName(MainGroup, VehicleProbabilityPackGroup, VehicleProbabilityGroup, VehiclePropertyPackGroup, VehiclePropertiesGroup, VehicleSelectionGroup)]
    public class Setting : ModSetting
    {
        public static Setting? Instance;
        
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
        
        /// <summary>
        /// Constructs the setting container for the specified mod instance.
        /// </summary>
        public Setting(IMod mod) : base(mod)
        {

        }

        private bool IsIngame()
        {
            return VehiclePropertySystem.IsIngame;
        }
        
        #region MainSection
        
        private Level _loggingLevel = Level.Info;
        
        // TODO: Add Localization
        [SettingsUISection(MainSection, MainGroup)]
        public LogLevel LoggingLevel
        {
            get
            {
                if (_loggingLevel == Level.Verbose)
                    return LogLevel.Verbose;
                if (_loggingLevel == Level.Debug)
                    return LogLevel.Debug;
                if (_loggingLevel == Level.Info)
                    return LogLevel.Info;
                if (_loggingLevel == Level.Warn)
                    return LogLevel.Warning;
                if (_loggingLevel == Level.Error)
                    return LogLevel.Error;
                if (_loggingLevel == Level.Disabled)
                    return LogLevel.Disabled;
                return LogLevel.Info;
            }
            set
            {
                switch (value)
                {
                    case LogLevel.Verbose:
                        _loggingLevel = Level.Verbose;
                        break;
                    case LogLevel.Debug:
                        _loggingLevel = Level.Debug;
                        break;
                    case LogLevel.Info:
                        _loggingLevel = Level.Info;
                        break;
                    case LogLevel.Warning:
                        _loggingLevel = Level.Warn;
                        break;
                    case LogLevel.Error:
                        _loggingLevel = Level.Error;
                        break;
                    case LogLevel.Disabled:
                        _loggingLevel = Level.Disabled;
                        break;
                }
                Mod.log.effectivenessLevel = _loggingLevel;
                Mod.log.Info("Logging level set to: " + _loggingLevel);
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
                if (VehicleProbabilitySystem.Instance == null) // System might be disabled
                    return;
                _currentProbabilityPack = ProbabilityPack.LoadFromFile(value);
                VehicleProbabilitySystem.Instance.LoadProbabilityPack(_currentProbabilityPack);
                ApplyProbabilityChanges = true;
            }
        }
        
        /// <summary>
        /// Populates the dropdown list for probability packs.
        /// </summary>
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
        [SettingsUIAdvanced]
        public bool IncreasePropertyPackItemsVersion
        {
            set => PropertyPackDropdownItemsVersion++;
        }
        
        private int PropertyPackDropdownItemsVersion { get; set; }

        private string _defaultPropertyPackDropdown = "";
        [SettingsUIDropdown(typeof(Setting), nameof(GetPropertyPackDropdownItems))]
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUIValueVersion(typeof(Setting), nameof(PropertyPackDropdownItemsVersion))]
        public string DefaultPropertyPackDropdown
        {
            get => _defaultPropertyPackDropdown;
            set
            {
                _defaultPropertyPackDropdown = value;
                VehiclePropertySystem.DefaultPackSettingChanged();
            }
        }
        
        public DropdownItem<string>[] GetPropertyPackDropdownItems()
        {
            var names = PropertyPack.GetPackNames();
            List<DropdownItem<string>> items = new List<DropdownItem<string>>();
            foreach(string s in names)
            {
                items.Add(new DropdownItem<string>()
                {
                    value = s,
                    displayName = s, // TODO: Get name from pack metadata
                });
            }
            return items.ToArray();
        }
        
        private string _savegamePropertyPackDropdown = "Default";
        [SettingsUIDropdown(typeof(Setting), nameof(GetSavegamePropertyPackDropdownItems))]
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsIngame), true)]
        [SettingsUIValueVersion(typeof(Setting), nameof(PropertyPackDropdownItemsVersion))]
        public string SavegamePropertyPackDropdown
        {
            get => _savegamePropertyPackDropdown;
            set
            {
                _savegamePropertyPackDropdown = value;
                VehiclePropertySystem.SavegamePackSettingChanged();
            }
        }
        
        public DropdownItem<string>[] GetSavegamePropertyPackDropdownItems()
        {
            var items = GetPropertyPackDropdownItems(); // Reuse same items
            DropdownItem<string>[] extendedItems = new DropdownItem<string>[items.Length + 1];
            extendedItems[0] = new DropdownItem<string>()
            {
                value = "Default",
                displayName = "Default (Use global setting)",
            };
            for (int i = 0; i < items.Length; i++)
            {
                extendedItems[i + 1] = items[i];
            }

            return extendedItems;
        }
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUIAdvanced]
        public bool ExportVanillaPack
        {
            set => VehiclePropertySystem.Instance.SaveVanillaPack();
        }

        /*private bool enableRealisticSpeedLimits = false;
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        public bool EnableRealisticSpeedLimits
        {
            get => enableRealisticSpeedLimits;
            set
            {
                enableRealisticSpeedLimits = value;
                RoadSpeedLimitSystem.TriggerSpeedLimitUpdate();
            }
        }*/
        
        

        private SpeedLimitOverride _speedLimitOverride = SpeedLimitOverride.None;
        
        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        public SpeedLimitOverride SpeedLimitOverride
        {
            get => _speedLimitOverride;
            set
            {
                _speedLimitOverride = value;
                RoadSpeedLimitSystem.TriggerSpeedLimitUpdate();
            }
        }
        
        

        /*[SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUITextInput]
        public string PackName { get; set; } = "My Custom Pack";

        [SettingsUISection(VehiclePropertiesSection, VehiclePropertyPackGroup)]
        [SettingsUIButton]
        public bool ExportPack
        {
            set
            {
                // TO DO: Implement
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
        }*/
        
        #endregion
        
        #region VehicleSelection


        [SettingsUISection(VehicleSelectionSection, VehicleSelectionGroup)]
        public bool EnableChangeVehicles { get; set; } = true;
        
        [SettingsUISection(VehicleSelectionSection, VehicleSelectionGroup)]
        public bool RemoveAllModComponents 
        {
            set => ChangeVehicleSection.RemoveAllModComponents();
        }
        
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
        
        /// <summary>
        /// Restores the mod's recommended default probabilities.
        /// </summary>
        public override void SetDefaults()
        {
            LoggingLevel = LogLevel.Info;
            
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
        
        /// <summary>
        /// Resets probabilities to match the game's vanilla values.
        /// </summary>
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

    /// <summary>
    /// Provides dynamic localisation entries for the options UI.
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        /// <summary>
        /// Creates the localisation source bound to the given setting instance.
        /// </summary>
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        /// <inheritdoc />
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
            values.Add(m_Setting.GetOptionLabelLocaleID(nameof(Setting.LoggingLevel)), "Logging Level");
            values.Add(m_Setting.GetOptionDescLocaleID(nameof(Setting.LoggingLevel)), "Sets the logging level for the mod. Higher levels log more information, but might impact performance. Recommended is 'Info' for normal operation, 'Debug' for detailed information, and 'Verbose' for full debug output. Please use 'Verbose' when reporting bugs");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Verbose), "Verbose (Log EVERYTHING)");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Debug), "Debug");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Info), "Info (Recommended)");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Warning), "Warning");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Error), "Error");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Disabled), "Disabled (No Logging)");
            values.Add(m_Setting.GetOptionTabLocaleID(nameof(Setting.DebugSection)), "Debug");
            values.Add(m_Setting.GetOptionGroupLocaleID(nameof(Setting.DebugGroup)), "Debugging Tools");
            
            return values;
        }

        /// <inheritdoc />
        public void Unload()
        {
        }
    }
}
