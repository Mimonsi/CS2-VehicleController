using System;
using System.IO;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
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

        public static bool EnableProbabilitySystem = false;
        public static bool EnablePropertySystem = true;
        public static bool EnableVehicleCounterSystem = false;
        public static bool EnableChangeVehicleSection = false;

        /// <summary>
        /// Called by the game when the mod is loaded.
        /// Registers systems and loads settings.
        /// </summary>
        public void OnLoad(UpdateSystem updateSystem)
        {
            //Logger.keepStreamOpen = false; // TEST: Solution for logger bug?
            
            log.Info("Loading VehicleController mod");
            log.effectivenessLevel = Level.Debug;
            
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                path = asset.path;
            
            if (EnableProbabilitySystem)
            {
                if (CopyEmbeddedPacks("probability"))
                    updateSystem.UpdateAt<VehicleProbabilitySystem>(SystemUpdatePhase.MainLoop);
                else
                    log.Info("Disabled VehicleProbabilitySystem due to error when copying embedded packs");
            }

            if (EnablePropertySystem)
            {
                if (CopyEmbeddedPacks("property"))
                    updateSystem.UpdateAt<VehiclePropertySystem>(SystemUpdatePhase.MainLoop);
                else
                    log.Info("Disabled VehiclePropertySystem due to error when copying embedded packs");
            }
            
            if (EnableVehicleCounterSystem)
                updateSystem.UpdateAt<VehicleCounterSystem>(SystemUpdatePhase.MainLoop);
            if (EnableChangeVehicleSection)
                updateSystem.UpdateAt<ChangeVehicleSection>(SystemUpdatePhase.PreCulling);
            
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
        }
        
        /// <summary>
        /// Copies embedded packs from the mod directory into the user data folder.
        /// </summary>
        private bool CopyEmbeddedPacks(string subPath)
        {
            try
            {
                var modPath = Path.GetDirectoryName(path);
                var srcPath = Path.Combine(modPath, "packs", subPath);
                var destPath = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs",
                    subPath);
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);
                foreach (var file in Directory.GetFiles(srcPath))
                {
                    var destFile = Path.Combine(destPath, Path.GetFileName(file));
                    if (!File.Exists(destFile))
                        File.Copy(file, destFile);
                }

                return true;
            }
            catch (Exception x)
            {
                log.Error("Error copying embedded packs: " + x.Message);
                return false;
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