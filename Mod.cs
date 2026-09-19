using System;
using System.IO;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Citizens;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using Game.UI;
using Unity.Entities;
using UnityEngine.InputSystem;
using VehicleController.Systems;

namespace VehicleController
{
    /// <summary>
    /// Entry point for the Vehicle Controller mod.
    /// </summary>
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(VehicleController)}")
            .SetShowsErrorsInUI(false).SetShowsStackTraceAboveLevels(Level.Error);

        public static string Id = "VehicleController";
        private Setting m_Setting;
        private string path;
        public const string Name = "Vehicle Controller";
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public static bool EnableVehicleCounterSystem = true;
        public static bool EnableChangeVehicleSection = true;
        public static bool EnableRoadSpeedLimitSystem = true;
        public static bool EnableVehicleStiffnessSystem = true;
        public static bool EnableVehicleConfigSystem = true;
        
        // public static ProxyAction ResetSpeedLimitAction;
        // public const string ResetSpeedLimitActionName = "VehicleController_ResetRoadSpeedLimits";

        /// <summary>
        /// Called by the game when the mod is loaded.
        /// Registers systems and loads settings.
        /// </summary>
        public void OnLoad(UpdateSystem updateSystem)
        {
            //Logger.keepStreamOpen = false; // TEST: Solution for logger bug?
            
            log.Info("Loading VehicleController mod");
            //log.effectivenessLevel = Level.Debug;
            
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                path = asset.path;
            
            //TODO: Next: Check what log outputs when vehicle probability system loads, if it even works

            CopyEmbeddedFiles();
            updateSystem.UpdateAt<PrefabCacheSystem>(SystemUpdatePhase.MainLoop);
            if (EnableVehicleConfigSystem)
            {
                updateSystem.UpdateAt<VehicleConfigSystem>(SystemUpdatePhase.MainLoop);
                updateSystem.UpdateAt<VehicleManagerUISystem>(SystemUpdatePhase.UIUpdate);
                updateSystem.UpdateAt<VehicleManagerLinkSection>(SystemUpdatePhase.UIUpdate);
            }
            if (EnableVehicleCounterSystem)
                updateSystem.UpdateAt<VehicleCounterSystem>(SystemUpdatePhase.MainLoop);
            if (EnableRoadSpeedLimitSystem)
                updateSystem.UpdateAt<CompatibilityRoadSpeedLimitSystem>(SystemUpdatePhase.MainLoop); // TODO: Road Speed System causes an error on loading a savegame twice in a row
            if (EnableVehicleStiffnessSystem)
                updateSystem.UpdateAt<VehicleStiffnessSystem>(SystemUpdatePhase.MainLoop);

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting)); // Dynamic localization is still needed
            foreach (var item in new LocaleHelper("VehicleController.Locale.json").GetAvailableLanguages())
            {
                GameManager.instance.localizationManager.AddSource(item.LocaleId, item);
            }

            m_Setting.RegisterKeyBindings();
            AssetDatabase.global.LoadSettings(nameof(VehicleController), m_Setting, new Setting(this));
            Setting.Instance = m_Setting;

            // Experimental: per-building vehicle selection. The SIP section only edits the
            // AllowedVehiclePrefab buffer (+ delete/clipboard); the actual prefab swap happens in the
            // spawn enforcers below, which rebind newly Created vehicles at Modification1/4B — before
            // vanilla initializes effects/search/rendering from the PrefabRef (see docs/VCF_ANALYSIS.md).
            // Gated on a persisted setting read here in OnLoad, so toggling it in the UI requires a
            // restart. Must run after LoadSettings above.
            if (EnableChangeVehicleSection && Setting.Instance.EnableExperimentalVehicleSelection)
            {
                updateSystem.UpdateAt<VehicleSelectionSection>(SystemUpdatePhase.UIUpdate);
                updateSystem.UpdateAt<VehicleSpawnEnforcerEarlySystem>(SystemUpdatePhase.Modification1);
                updateSystem.UpdateAfter<VehicleSpawnEnforcerParkedSystem, Game.Common.ModificationBarrier4B>(SystemUpdatePhase.Modification4B);
            }
            //if (EnableProbabilitySystem || EnablePropertySystem) // TODO: Re-enabled
                //updateSystem.UpdateAt<VehiclePropertiesSection>(SystemUpdatePhase.UIUpdate);

            // ResetSpeedLimitAction = Setting.Instance.GetAction(ResetSpeedLimitActionName);
            // ResetSpeedLimitAction.shouldBeEnabled = true;
            // ResetSpeedLimitAction.onInteraction += (_, phase) =>
            // {
            //     if (phase == InputActionPhase.Performed)
            //         RoadSpeedLimitSystem.Instance.ResetAllSpeedLimits();
            // };
            
            log.Info("VehicleController mod loaded successfully with pack");
        }
        
        /// <summary>
        /// Copies embedded packs from the mod directory into the user data folder.
        /// </summary>
        private void CopyEmbeddedFiles()
        {
            try
            {
                var modPath = Path.GetDirectoryName(path);
                var srcPath = Path.Combine(modPath, "Resources");
                var destPath = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController));
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);
                CopyRecursively(srcPath, destPath);

                log.Debug($"Copied embedded files");
            }
            catch (Exception x)
            {
                log.Error("Error copying embedded files: " + x.Message);
            }
        }

        private void CopyRecursively(string sourcePath, string destinationPath)
        {
            foreach( var directory in Directory.GetDirectories(sourcePath))
            {
                var destDir = Path.Combine(destinationPath, Path.GetFileName(directory));
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                CopyRecursively(directory, destDir);
            }
            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                //if (!File.Exists(destFile))
                File.Copy(file, destFile, true);
                log.Debug($"Copied {file} to  {destFile}");
            }
        }

        /// <summary>
        /// Called when the mod is unloaded.
        /// </summary>
        public void OnDispose()
        {
            try
            {
                log.Info(nameof(OnDispose));
                m_Setting.UnregisterInOptionsUI();
            }
            catch (Exception e)
            {
                log.Error($"Error during {nameof(OnDispose)}: {e.Message}");
            }
        }

        public static void ShowMessageDialog(string title, string message, string confirmAction)
        {
            GameManager.instance.userInterface.appBindings.ShowMessageDialog(
                new MessageDialog(title, message, confirmAction), null);
            
        }
    }
}