using WeatherOverhaul.Weather;

namespace WeatherOverhaul.Patches
{
    [HarmonyPatch(typeof(Il2Cpp.Weather), nameof(Il2Cpp.Weather.CalculateCurrentTemperature))]
    internal static class WeatherCalculateCurrentTemperatureCustomWeatherPatch
    {
        private static int s_CustomOffsetCalculationDepth;

        internal static bool IsApplyingCustomTemperatureOffset => s_CustomOffsetCalculationDepth > 0;

        [HarmonyPriority(Priority.First)]
        private static void Prefix(Il2Cpp.Weather __instance, out float __state)
        {
            __state = 0f;
            if (__instance == null || __instance.IsIndoorEnvironment()) return;
            if (__instance.m_LockedAirTemperature > -1000f) return;
            if (!CustomWeatherStageRuntime.TryGetCalculatedTemperatureOffset(out float offsetCelsius)) return;

            __instance.m_ArtificalTempIncrease += offsetCelsius;
            __state = offsetCelsius;
            s_CustomOffsetCalculationDepth++;
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(Il2Cpp.Weather __instance, float __state)
        {
            if (Math.Abs(__state) <= 0.01f) return;

            if (__instance != null)
                __instance.m_ArtificalTempIncrease -= __state;

            if (s_CustomOffsetCalculationDepth > 0)
                s_CustomOffsetCalculationDepth--;
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.Weather), nameof(Il2Cpp.Weather.GetMinAirTemp))]
    internal static class WeatherGetMinAirTempCustomWeatherPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Il2Cpp.Weather __instance, ref float __result)
        {
            if (__instance == null || !WeatherCalculateCurrentTemperatureCustomWeatherPatch.IsApplyingCustomTemperatureOffset) return;
            __result = __instance.m_MinAirTemperature;
        }
    }
}