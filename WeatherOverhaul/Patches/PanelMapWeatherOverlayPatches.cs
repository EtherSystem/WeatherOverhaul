using WeatherOverhaul.UI;

namespace WeatherOverhaul.Patches
{
    [HarmonyPatch(typeof(Panel_Map), nameof(Panel_Map.Enable), typeof(bool))]
    internal static class PanelMapEnableOneArgWeatherOverlayPatch
    {
        private static void Postfix(Panel_Map __instance, bool enable)
        {
            WeatherMapOverlayUi.NotifyPanelMapEnable(__instance, enable);
        }
    }

    [HarmonyPatch(typeof(Panel_Map), nameof(Panel_Map.Enable), typeof(bool), typeof(bool))]
    internal static class PanelMapEnableTwoArgWeatherOverlayPatch
    {
        private static void Postfix(Panel_Map __instance, bool enable)
        {
            WeatherMapOverlayUi.NotifyPanelMapEnable(__instance, enable);
        }
    }

    [HarmonyPatch(typeof(Panel_Map), nameof(Panel_Map.ToggleWorldMap))]
    internal static class PanelMapToggleWorldMapWeatherOverlayPatch
    {
        private static void Postfix(Panel_Map __instance)
        {
            WeatherMapOverlayUi.NotifyPanelMapChanged(__instance, "ToggleWorldMap");
        }
    }

    [HarmonyPatch(typeof(Panel_Map), nameof(Panel_Map.EnableWorldMapOfCurrentScene))]
    internal static class PanelMapEnableWorldMapWeatherOverlayPatch
    {
        private static void Postfix(Panel_Map __instance)
        {
            WeatherMapOverlayUi.NotifyPanelMapChanged(__instance, "EnableWorldMapOfCurrentScene");
        }
    }

    [HarmonyPatch(typeof(Panel_Map), nameof(Panel_Map.DisableWorldMaps))]
    internal static class PanelMapDisableWorldMapsWeatherOverlayPatch
    {
        private static void Postfix(Panel_Map __instance)
        {
            WeatherMapOverlayUi.NotifyPanelMapChanged(__instance, "DisableWorldMaps");
        }
    }
}