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
    public class Mod : IMod
    {
        public static ILog Logger = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        public static string Id = "VehicleController";
        private Setting m_Setting;
        private string path;
        public const string Name = "Vehicle Controller";
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public static bool EnableProbabilitySystem = false;
        public static bool EnablePropertySystem = false;
        public static bool EnableVehicleCounterSystem = true;
        public static bool EnableChangeVehicleSection = true;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Logger.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                path = asset.path;
            
            if (EnableProbabilitySystem)
            {
                if (CopyEmbeddedPacks("probability"))
                    updateSystem.UpdateAt<VehicleProbabilitySystem>(SystemUpdatePhase.MainLoop);
                else
                    Logger.Info("Disabled VehicleProbabilitySystem due to error when copying embedded packs");
            }

            if (EnablePropertySystem)
            {
                if (CopyEmbeddedPacks("property"))
                    updateSystem.UpdateAt<VehiclePropertySystem>(SystemUpdatePhase.MainLoop);
                else
                    Logger.Info("Disabled VehiclePropertySystem due to error when copying embedded packs");
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
                Logger.Error("Error copying embedded packs: " + x.Message);
                return false;
            }
        }

        public void OnDispose()
        {
            try
            {
                Logger.Info(nameof(OnDispose));
                m_Setting.UnregisterInOptionsUI();
            }
            catch (Exception e)
            {
                Logger.Error($"Error during {nameof(OnDispose)}: {e.Message}");
            }
        }
    }
}