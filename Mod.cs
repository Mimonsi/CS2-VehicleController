using System;
using System.IO;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Citizens;
using Game.Modding;
using Game.SceneFlow;
using Unity.Entities;
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

        public static bool EnableProbabilitySystem = true;
        public static bool EnablePropertySystem = true;
        public static bool EnableVehicleCounterSystem = true;
        public static bool EnableChangeVehicleSection = true;
        public static bool EnableRoadSpeedLimitSystem = true;
        public static bool EnableVehicleStiffnessSystem = true;

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

            CopyEmbeddedFiles();
            
            if (EnableProbabilitySystem)
                updateSystem.UpdateAt<VehicleProbabilitySystem>(SystemUpdatePhase.MainLoop);
            if (EnablePropertySystem)
                updateSystem.UpdateAt<VehiclePropertySystem>(SystemUpdatePhase.MainLoop);
            if (EnableVehicleCounterSystem)
                updateSystem.UpdateAt<VehicleCounterSystem>(SystemUpdatePhase.MainLoop);
            if (EnableRoadSpeedLimitSystem)
                updateSystem.UpdateAt<RoadSpeedLimitSystem>(SystemUpdatePhase.MainLoop);
            if (EnableChangeVehicleSection)
                updateSystem.UpdateAt<ChangeVehicleSection>(SystemUpdatePhase.PreCulling);
            if (EnableVehicleStiffnessSystem)
                updateSystem.UpdateAt<VehicleStiffnessSystem>(SystemUpdatePhase.MainLoop);
            
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ChangeVehicleSection>();
            //updateSystem.UpdateAt<VehiclePropertySystem>(SystemUpdatePhase.MainLoop);

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting)); // Dynamic localization is still needed
            foreach (var item in new LocaleHelper("VehicleController.Locale.json").GetAvailableLanguages())
            {
                GameManager.instance.localizationManager.AddSource(item.LocaleId, item);
            }

            AssetDatabase.global.LoadSettings(nameof(VehicleController), m_Setting, new Setting(this));
            Setting.Instance = m_Setting;
            
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
    }
}