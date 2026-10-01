using WeatherOverhaul.Weather;

namespace WeatherOverhaul.Patches
{
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.UpdateClothingWetness))]
    internal static class PlayerManagerUpdateClothingWetnessCustomWeatherPatch
    {
        private static void Prefix(ref bool __state)
        {
            __state = CustomWeatherWetnessRuntime.BeginEvaluation();
        }

        private static Exception? Finalizer(Exception? __exception, bool __state)
        {
            CustomWeatherWetnessRuntime.EndEvaluation(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ClothingItem), nameof(ClothingItem.IncreaseWetnessPercent))]
    internal static class ClothingItemIncreaseWetnessPercentCustomWeatherPatch
    {
        private static void Prefix(ref float __0)
        {
            CustomWeatherWetnessRuntime.ScaleWetnessIncrease(ref __0);
        }
    }
}