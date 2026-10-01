using WeatherOverhaul.Weather;

namespace WeatherOverhaul.Patches
{
    [HarmonyPatch(typeof(Il2Cpp.Weather), nameof(Il2Cpp.Weather.CalculateCurrentTemperature))]
    internal static class WeatherCalculateCurrentTemperatureCustomWeatherPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Il2Cpp.Weather __instance)
        {
            if (__instance == null || __instance.IsIndoorEnvironment()) return;
            if (__instance.m_LockedAirTemperature > -1000f) return;
            if (!CustomWeatherStageRuntime.TryGetCalculatedTemperatureOffset(out float offsetCelsius)) return;

            __instance.m_CurrentTemperatureWithoutHeatSources += offsetCelsius;
            __instance.m_CurrentTemperature += offsetCelsius;
        }
    }
}