using Il2CppInterop.Runtime.Injection;
using WeatherOverhaul.UI;

[assembly: MelonInfo(typeof(WeatherOverhaul.Core), "WeatherOverhaul", "1.0.3", "EtherSystem", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace WeatherOverhaul
{
    public class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        private static readonly HashSet<string> ReportedExceptions = new(StringComparer.Ordinal);

        internal static void Log(string message, bool onlyWhenMLLogging = true)
        {
            if (onlyWhenMLLogging && !WeatherOverhaulSettingsManager.MLLogging) return;

            Instance?.LoggerInstance.Msg(message);
        }

        internal static void Warn(string message, bool onlyWhenMLLogging = false)
        {
            if (onlyWhenMLLogging && !WeatherOverhaulSettingsManager.MLLogging) return;

            Instance?.LoggerInstance.Warning(message);
        }

        internal static void Error(string message, bool onlyWhenMLLogging = false)
        {
            if (onlyWhenMLLogging && !WeatherOverhaulSettingsManager.MLLogging) return;

            Instance?.LoggerInstance.Error(message);
        }

        internal static void LogException(string context, Exception exception)
        {
            if (WeatherOverhaulSettingsManager.MLLogging)
            {
                Error(context + "\n" + exception);
                return;
            }

            string message = (exception.Message ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
            Error($"{context} {exception.GetType().Name}: {message}");
        }

        internal static void LogExceptionOnce(string key, string context, Exception exception)
        {
            string normalizedKey = string.IsNullOrWhiteSpace(key) ? context : key;
            if (!ReportedExceptions.Add(normalizedKey)) return;

            LogException(context, exception);
        }

        public override void OnInitializeMelon()
        {
            Instance = this;
            RegisterIl2CppTypes();
            WeatherOverhaulSettingsManager.Initialize();
            WeatherOverhaulRuntime.Initialize();
            DevConsoleCommands.Register();
            HarmonyInstance.PatchAll();
            Log("Initialized.", false);
        }

        public override void OnUpdate()
        {
            WeatherOverhaulRuntime.Update();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            WeatherOverhaulRuntime.NotifySceneWasLoaded(sceneName);
        }

        public override void OnGUI()
        {
            if (!WeatherOverhaulRuntime.IsGameplayRuntimeReady) return;
            if (WeatherOverhaulSettingsManager.Debug) GlobalWeatherDebugUi.Draw();
            WeatherMapOverlayUi.Draw();
        }

        private static void RegisterIl2CppTypes()
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<WeatherMapRegionClickProxy>();
            }
            catch (Exception exception)
            {
                LogExceptionOnce("register-map-region-click-proxy", "Failed to register the world-map click proxy.", exception);
            }
        }
    }
}