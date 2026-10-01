using WeatherOverhaul.Weather;

namespace WeatherOverhaul.Patches
{
    [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Awake))]
    internal static class BaseAiAwakeBloodMoonInfluencePatch
    {
        private static void Postfix(BaseAi __instance)
        {
            BloodMoonInfluenceRuntime.NotifyBaseAiAwake(__instance);
        }
    }

    [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Update))]
    internal static class BaseAiUpdateBloodMoonInfluencePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(BaseAi __instance)
        {
            BloodMoonInfluenceRuntime.NotifyBaseAiUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.UpdateWounds), new Type[] { typeof(float) })]
    internal static class BaseAiUpdateWoundsBloodMoonInfluencePatch
    {
        private static void Prefix(BaseAi __instance, ref float realtimeSeconds)
        {
            BloodMoonInfluenceRuntime.ScaleUpdateWoundsRealtime(__instance, ref realtimeSeconds);
        }
    }

    [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.ApplyDamage), new Type[] { typeof(float), typeof(float), typeof(DamageSource), typeof(string) })]
    internal static class BaseAiApplyDamageBloodMoonInfluencePatch
    {
        private static void Prefix(BaseAi __instance, out BloodMoonInfluenceRuntime.BleedState __state)
        {
            BloodMoonInfluenceRuntime.CaptureBleedStateBeforeApplyDamage(__instance, out __state);
        }

        private static void Postfix(BaseAi __instance, BloodMoonInfluenceRuntime.BleedState __state)
        {
            BloodMoonInfluenceRuntime.RestoreBleedStateAfterApplyDamage(__instance, __state);
        }
    }
}