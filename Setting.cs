using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game.Input;
using Game.Modding;
using Game.Net;
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
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Disabled
    }

    public enum SpeedLimitOverride
    {
        Half,
        None,
        Double,
        Speed // x10
    }

    /// <summary>
    /// Stores all mod settings and exposes them to the game UI.
    /// </summary>
    [FileLocation("ModsSettings/VehicleController/VehicleController")]
    [SettingsUITabOrder(MainSection, SpawnBehaviorSection, VehiclePropertiesSection, VehicleSelectionSection, AboutSection, DebugSection)]
    [SettingsUIGroupOrder(MainGroup, VehicleProbabilityPackGroup, VehicleProbabilityGroup, VehicleStiffnessGroup, VehiclePropertyPackGroup, VehiclePropertiesGroup, RoadSpeedLimitGroup, VehicleSelectionGroup, InfoGroup, DebugGeneralGroup, DebugComponentsGroup)]
    [SettingsUIShowGroupName(MainGroup, VehicleProbabilityPackGroup, VehicleProbabilityGroup, VehicleStiffnessGroup, VehiclePropertyPackGroup, VehiclePropertiesGroup, RoadSpeedLimitGroup, VehicleSelectionGroup, DebugGeneralGroup, DebugComponentsGroup)]
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
        public const string VehicleStiffnessGroup = "Vehicle Stiffness";
        public const string VehiclePropertyPackGroup = "Vehicle Property Pack";
        public const string RoadSpeedLimitGroup = "Road Speed Limits";

        public const string VehicleSelectionSection = "Vehicle Selection";
        public const string VehicleSelectionGroup = "Vehicle Selection";


        public const string AboutSection = "About";
        public const string InfoGroup = "Info";

        public const string DebugSection = "Debug";
        public const string DebugGeneralGroup = "Debug General";
        public const string DebugComponentsGroup = "Debug Components";

        /// <summary>
        /// Constructs the setting container for the specified mod instance.
        /// </summary>
        public Setting(IMod mod) : base(mod)
        {

        }

        private bool IsIngame()
        {
            return VehicleConfigSystem.IsIngame;
        }

        #region MainSection

        private Level _loggingLevel = Level.Info;

        [SettingsUISection(MainSection, MainGroup)]
        public LogLevel LoggingLevel
        {
            get
            {
                if (_loggingLevel == Level.Verbose)
                    return LogLevel.Verbose;
                if (_loggingLevel == Level.Trace)
                    return LogLevel.Trace;
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
                    case LogLevel.Trace:
                        _loggingLevel = Level.Trace;
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

        [SettingsUISection(MainSection, MainGroup)]
        public bool ResetSettings
        {
            set
            {
                SetDefaults();
            }
        }
        #endregion

        #region VehicleProperties

        private bool _useImprovedStiffnessValues = true;

        [SettingsUISection(VehiclePropertiesSection, VehicleStiffnessGroup)]
        public bool UseImprovedStiffnessValues
        {
            get => _useImprovedStiffnessValues;
            set
            {
                StiffnessModifier = 3f;
                DampingModifier = 2f;
                _useImprovedStiffnessValues = value;
            }
        }

        private float _stiffnessModifier = 3f;
        [SettingsUISection(VehiclePropertiesSection, VehicleStiffnessGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(UseImprovedStiffnessValues), false)]
        [SettingsUISlider(min = 0.01f, max = 10f, step = 0.25f, unit = Unit.kFloatTwoFractions, scalarMultiplier = 1f)]
        public float StiffnessModifier
        {
            get => _stiffnessModifier;
            set
            {
                _stiffnessModifier = value;
                if (VehicleStiffnessSystem.Instance != null)
                    VehicleStiffnessSystem.Instance.SettingsUpdated();
            }
        }

        private float _dampingModifier = 2f;
        [SettingsUISection(VehiclePropertiesSection, VehicleStiffnessGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(UseImprovedStiffnessValues), false)]
        [SettingsUISlider(min = 0.01f, max = 10f, step = 0.25f, unit = Unit.kFloatTwoFractions, scalarMultiplier = 1f)]
        public float DampingModifier
        {
            get => _dampingModifier;
            set
            {
                _dampingModifier = value;
                if (VehicleStiffnessSystem.Instance != null)
                    VehicleStiffnessSystem.Instance.SettingsUpdated();
            }
        }

        [SettingsUISection(VehiclePropertiesSection, VehicleStiffnessGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(UseImprovedStiffnessValues), false)]
        public bool ResetStiffnessToDefault
        {
            set => VehicleStiffnessSystem.Instance?.ResetSettingsToDefault();
        }

        [SettingsUISection(VehiclePropertiesSection, VehicleStiffnessGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(UseImprovedStiffnessValues), false)]
        public bool ResetStiffnessToVanilla
        {
            set => VehicleStiffnessSystem.Instance?.ResetSettingsToVanilla();
        }

        #region RoadSpeedLimitGroup

        [SettingsUISection(VehiclePropertiesSection, RoadSpeedLimitGroup)]
        public bool ResetSpeedLimits
        {
            set => CompatibilityRoadSpeedLimitSystem.Instance?.ResetAllSpeedLimits();
        }

        #endregion

        #endregion

        #region VehicleSelection


        [SettingsUISection(VehicleSelectionSection, VehicleSelectionGroup)]
        public bool EnableExperimentalVehicleSelection { get; set; } = false;

        [SettingsUISection(VehicleSelectionSection, VehicleSelectionGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(EnableExperimentalVehicleSelection), true)]
        public bool EnableChangeVehicles { get; set; } = true;

        [SettingsUISection(VehicleSelectionSection, VehicleSelectionGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(EnableExperimentalVehicleSelection), true)]
        public bool DisplayVehiclePrefabNames { get; set; } = true;

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

        [SettingsUISection(DebugSection, DebugGeneralGroup)]
        [SettingsUIAdvanced]
        public bool CountPrefabInstances
        {
            set => VehicleCounterSystem.Instance.CountPrefabInstances();
        }

        // M0 cascade test hooks (temporary; replaced by the real UI later).
        [SettingsUISection(DebugSection, DebugGeneralGroup)]
        [SettingsUIAdvanced]
        public bool CreateExampleVehiclePack
        {
            set => VehicleConfigSystem.CreateExamplePack();
        }

        [SettingsUISection(DebugSection, DebugGeneralGroup)]
        [SettingsUIAdvanced]
        public bool ApplyExampleVehiclePack
        {
            set => VehicleConfigSystem.Instance?.ApplyPackByName("Example");
        }

        [SettingsUISection(DebugSection, DebugGeneralGroup)]
        [SettingsUIAdvanced]
        public bool ResetVehiclePackToVanilla
        {
            set => VehicleConfigSystem.Instance?.ResetToVanilla();
        }

        [SettingsUISection(DebugSection, DebugComponentsGroup)]
        [SettingsUIAdvanced]
        public bool RemoveAllowedVehiclePrefab
        {
            set => Systems.VehicleSelectionSection.Instance?.RemoveAllowedVehiclePrefabs();
        }

        // This simulates what happens when the game is saved
        [SettingsUISection(DebugSection, DebugComponentsGroup)]
        [SettingsUIAdvanced]
        public bool DebugCheckPrefabRefs
        {
            set => Systems.VehicleSelectionSection.Instance?.DebugCheckPrefabRefs();
        }

        // This simulates what happens when prefabs are deleted
        [SettingsUISection(DebugSection, DebugComponentsGroup)]
        [SettingsUIAdvanced]
        public bool DebugCheckEnabledEffects
        {
            set => Systems.VehicleSelectionSection.Instance?.DebugCheckEnabledEffects();
        }

        [SettingsUISection(DebugSection, DebugComponentsGroup)]
        [SettingsUIAdvanced]
        public bool CountAllSpeedLimits
        {
            set => CompatibilityRoadSpeedLimitSystem.Instance?.CountAllSpeedLimits();
        }

        #endregion

        /// <summary>
        /// Restores the mod's recommended defaults.
        /// </summary>
        public override void SetDefaults()
        {
            LoggingLevel = LogLevel.Info;
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

            // TODO: Move to Locale.json
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Verbose), "Verbose (Log EVERYTHING)");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Trace), "Trace (Extended Debug)");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Debug), "Debug");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Info), "Info (Recommended)");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Warning), "Warning");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Error), "Error");
            values.Add(m_Setting.GetEnumValueLocaleID(LogLevel.Disabled), "Disabled (No Logging)");

            return values;
        }

        /// <inheritdoc />
        public void Unload()
        {
        }
    }
}
