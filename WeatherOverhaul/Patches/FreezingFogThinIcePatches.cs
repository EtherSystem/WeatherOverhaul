using WeatherOverhaul.Weather;

namespace WeatherOverhaul.Patches
{
    [HarmonyPatch(typeof(IceCrackingTrigger), nameof(IceCrackingTrigger.OnTriggerEnter))]
    internal static class IceCrackingTriggerOnTriggerEnterFreezingFogPatch
    {
        private static bool Prefix()
        {
            return !FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened;
        }
    }

    [HarmonyPatch(typeof(IceCrackingTrigger), nameof(IceCrackingTrigger.OnTriggerExit))]
    internal static class IceCrackingTriggerOnTriggerExitFreezingFogPatch
    {
        private static bool Prefix()
        {
            return !FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened;
        }
    }

    [HarmonyPatch(typeof(IceCrackingWarningTrigger), nameof(IceCrackingWarningTrigger.OnTriggerEnter))]
    internal static class IceCrackingWarningTriggerOnTriggerEnterFreezingFogPatch
    {
        private static bool Prefix()
        {
            return !FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened;
        }
    }

    [HarmonyPatch(typeof(IceCrackingWarningTrigger), nameof(IceCrackingWarningTrigger.OnTriggerExit))]
    internal static class IceCrackingWarningTriggerOnTriggerExitFreezingFogPatch
    {
        private static bool Prefix()
        {
            return !FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened;
        }
    }

    [HarmonyPatch(typeof(IceCrackingManager), nameof(IceCrackingManager.IsInsideTrigger))]
    internal static class IceCrackingManagerIsInsideTriggerFreezingFogPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(IceCrackingManager), nameof(IceCrackingManager.IsInsideFallTrigger))]
    internal static class IceCrackingManagerIsInsideFallTriggerFreezingFogPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(IceCrackingTrigger), nameof(IceCrackingTrigger.TryUpdateTimer))]
    internal static class IceCrackingTriggerTryUpdateTimerFreezingFogPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(IceCrackingTrigger), nameof(IceCrackingTrigger.IsTimerFinished))]
    internal static class IceCrackingTriggerIsTimerFinishedFreezingFogPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(IceCrackingTrigger), nameof(IceCrackingTrigger.BreakIce))]
    internal static class IceCrackingTriggerBreakIceFreezingFogPatch
    {
        private static bool Prefix()
        {
            return !FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened;
        }
    }

    [HarmonyPatch(typeof(IceCrackingTrigger), nameof(IceCrackingTrigger.FallInWater))]
    internal static class IceCrackingTriggerFallInWaterFreezingFogPatch
    {
        private static bool Prefix()
        {
            return !FreezingFogThinIceRuntime.IsCurrentRegionThinIceHardened;
        }
    }
}