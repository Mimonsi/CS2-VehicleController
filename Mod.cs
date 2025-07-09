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
        public static ILog log = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        private Setting m_Setting;
        private string path;
        public const string Name = "Vehicle Controller";
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                path = asset.path;
            
            CopyEmbeddedPacks("probability");
            CopyEmbeddedPacks("property");
            updateSystem.UpdateAt<VehicleCounterSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<VehicleProbabilitySystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<VehicleProbabilitySystem>(SystemUpdatePhase.MainLoop);
            //updateSystem.UpdateAt<CreatedServiceVehicleModifierSystem>(SystemUpdatePhase.PreCulling);
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
        
        private void CopyEmbeddedPacks(string subPath)
        {
            var modPath = Path.GetDirectoryName(path);
            var srcPath = Path.Combine(modPath, "packs", subPath);
            var destPath = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(VehicleController), "packs", subPath);
            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);
            foreach(var file in Directory.GetFiles(srcPath))
            {
                var destFile = Path.Combine(destPath, Path.GetFileName(file));
                if (!File.Exists(destFile))
                    File.Copy(file, destFile);
            }
        }

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