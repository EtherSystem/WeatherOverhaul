using WeatherOverhaul.Weather;

namespace WeatherOverhaul.Patches
{
    [HarmonyPatch(typeof(WeatherSetStage), nameof(WeatherSetStage.Activate))]
    internal static class WeatherSetStageActivatePatch
    {
        private static bool Prefix(WeatherSetStage __instance, float startAtFrac, WeatherStage previousStage, ref bool __state)
        {
            __state = false;
            if (__instance == null) return true;
            if (VanillaAuroraAuthority.ShouldBlockStage(__instance.m_WeatherType))
            {
                __state = true;
                return false;
            }

            if (CustomWeatherStageRuntime.IsActive && !CustomWeatherStageRuntime.IsApplyingCustomBaseStage)
            {
                __state = true;
                return false;
            }

            if (WeatherDirector.IsApplyingForecastStage)
            {
                if (WeatherDirector.ShouldSuppressUnauthorizedForecastStageActivation(__instance))
                {
                    __state = true;
                    return false;
                }

                WeatherDirector.PrepareAuthorizedWeatherSetStageActivation(__instance);
                return true;
            }

            if (!WeatherDirector.TryOverrideVanillaWeatherSetStageActivation(__instance, startAtFrac, previousStage)) return true;

            __state = true;
            return false;
        }

        private static void Postfix(WeatherSetStage __instance, bool __state)
        {
            if (__state) return;
            if (__instance == null) return;
            WeatherDirector.NotifyWeatherSetStageActivated(__instance);
            WeatherOverhaulRuntime.NotifyWeatherStageActivated(__instance.m_WeatherType, __instance.m_CurrentDuration, __instance.m_CurrentTransitionTime, __instance.m_ElapsedTime);
        }
    }

    [HarmonyPatch(typeof(WeatherTransition), nameof(WeatherTransition.ChooseNextWeatherSet))]
    internal static class WeatherTransitionChooseNextWeatherSetPatch
    {
        private static bool Prefix(WeatherTransition __instance, ref bool __state)
        {
            __state = false;
            if (WeatherDirector.IsApplyingForecastStage) return true;
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul) return true;

            if ((CustomWeatherStageRuntime.IsActive) && !CustomWeatherStageRuntime.IsApplyingCustomBaseStage)
            {
                __state = true;
                return false;
            }

            if (!WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion) return true;

            __state = WeatherDirector.TryChooseAndActivateNextWeatherSet(__instance);
            return !__state;
        }

        private static void Postfix(bool __state)
        {
            WeatherOverhaulRuntime.NotifyWeatherSelectionCompleted(__state);
        }
    }

    [HarmonyPatch(typeof(LoadScene), nameof(LoadScene.CompleteActivate), new System.Type[] { typeof(bool) })]
    internal static class LoadSceneCompleteActivatePatch
    {
        private static void Prefix(LoadScene __instance, bool transitionImmediately)
        {
            WeatherOverhaulRuntime.NotifyLoadSceneActivated(__instance, transitionImmediately ? "LoadScene.CompleteActivate immediate" : "LoadScene.CompleteActivate");
        }
    }

    [HarmonyPatch(typeof(LoadScene), nameof(LoadScene.PerformSceneLoad), new System.Type[] { })]
    internal static class LoadScenePerformSceneLoadPatch
    {
        private static void Prefix(LoadScene __instance)
        {
            WeatherOverhaulRuntime.NotifyLoadSceneActivated(__instance, "LoadScene.PerformSceneLoad");
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), nameof(UniStormWeatherSystem.Update))]
    internal static class UniStormWeatherSystemUpdatePatch
    {
        private static void Postfix(UniStormWeatherSystem __instance)
        {
            CustomWeatherStageRuntime.NotifyUniStormUpdated(__instance);
        }
    }

    [HarmonyPatch(typeof(AuroraManager), nameof(AuroraManager.Update))]
    internal static class AuroraManagerUpdatePatch
    {
        private static UniStormWeatherSystem? s_UniStorm;
        private static WeatherStage s_OriginalUniStormStage;
        private static bool s_Spoofed;

        private static void Prefix()
        {
            VanillaAuroraAuthority.PrepareAuroraManagerUpdate();
            s_Spoofed = false;
            s_UniStorm = null;

            if (!CustomWeatherStageRuntime.TryGetActiveStageId(out WeatherStageId stageId) || stageId != WeatherStageId.FoggyAurora) return;

            CustomWeatherStageRuntime.BeginFoggyAuroraStageSpoof();
            s_UniStorm = GameManager.GetUniStorm();
            if (s_UniStorm != null)
            {
                s_OriginalUniStormStage = s_UniStorm.m_CurrentWeatherStage;
                s_UniStorm.m_CurrentWeatherStage = WeatherStage.ClearAurora;
            }

            s_Spoofed = true;
        }

        private static void Postfix()
        {
            RestoreFoggyAuroraStageSpoof();
        }

        private static System.Exception? Finalizer(System.Exception? __exception)
        {
            RestoreFoggyAuroraStageSpoof();
            return __exception;
        }

        private static void RestoreFoggyAuroraStageSpoof()
        {
            if (!s_Spoofed) return;
            if (s_UniStorm != null) s_UniStorm.m_CurrentWeatherStage = s_OriginalUniStormStage;
            s_UniStorm = null;
            s_Spoofed = false;
            CustomWeatherStageRuntime.EndFoggyAuroraStageSpoof();
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.Weather), nameof(Il2Cpp.Weather.GetWeatherStage))]
    internal static class WeatherGetWeatherStageFoggyAuroraPatch
    {
        private static void Postfix(ref WeatherStage __result)
        {
            if (CustomWeatherWetnessRuntime.TryGetScopedWeatherStage(out WeatherStage scopedStage))
            {
                __result = scopedStage;
                return;
            }

            if (CustomWeatherStageRuntime.ShouldSpoofFoggyAuroraStage) __result = WeatherStage.ClearAurora;
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), nameof(UniStormWeatherSystem.GetWeatherStage))]
    internal static class UniStormGetWeatherStageFoggyAuroraPatch
    {
        private static void Postfix(ref WeatherStage __result)
        {
            if (CustomWeatherWetnessRuntime.TryGetScopedWeatherStage(out WeatherStage scopedStage))
            {
                __result = scopedStage;
                return;
            }

            if (CustomWeatherStageRuntime.ShouldSpoofFoggyAuroraStage) __result = WeatherStage.ClearAurora;
        }
    }


    [HarmonyPatch(typeof(UniStormWeatherSystem), nameof(UniStormWeatherSystem.GetClearAuroraIntensity))]
    internal static class UniStormGetClearAuroraIntensityFoggyAuroraPatch
    {
        private static void Postfix(ref float __result)
        {
            if (!CustomWeatherStageRuntime.IsFoggyAuroraActive) return;
            __result = Mathf.Max(__result, CustomWeatherStageRuntime.CurrentEffectBlend);
        }
    }

    [HarmonyPatch(typeof(AuroraManager), nameof(AuroraManager.AuroraIsActive))]
    internal static class AuroraManagerAuroraIsActiveFoggyAuroraPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (CustomWeatherStageRuntime.IsAuroraVariantActive && !CustomWeatherStageRuntime.IsAuroraGameplayFullyActive)
            {
                __result = false;
                return;
            }

            if (CustomWeatherStageRuntime.IsFoggyAuroraActive) __result = true;
        }
    }

    [HarmonyPatch(typeof(AuroraManager), nameof(AuroraManager.IsFullyActive))]
    internal static class AuroraManagerIsFullyActiveFoggyAuroraPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (CustomWeatherStageRuntime.IsFoggyAuroraActive && CustomWeatherStageRuntime.CurrentEffectBlend >= 0.999f) __result = true;
        }
    }

    [HarmonyPatch(typeof(AuroraManager), nameof(AuroraManager.GetNormalizedAlpha))]
    internal static class AuroraManagerGetNormalizedAlphaFoggyAuroraPatch
    {
        private static void Postfix(ref float __result)
        {
            if (!CustomWeatherStageRuntime.IsFoggyAuroraActive) return;
            __result = Mathf.Max(__result, CustomWeatherStageRuntime.CurrentEffectBlend);
        }
    }

    [HarmonyPatch(typeof(AuroraManager), nameof(AuroraManager.GetNormalizedAlphaSquare))]
    internal static class AuroraManagerGetNormalizedAlphaSquareFoggyAuroraPatch
    {
        private static void Postfix(ref float __result)
        {
            if (!CustomWeatherStageRuntime.IsFoggyAuroraActive) return;
            float alpha = CustomWeatherStageRuntime.CurrentEffectBlend;
            __result = Mathf.Max(__result, alpha * alpha);
        }
    }

    [HarmonyPatch(typeof(AuroraManager), nameof(AuroraManager.ForceAuroraNextOpportunity), new System.Type[] { typeof(bool), typeof(bool), typeof(float) })]
    internal static class AuroraManagerForceAuroraNextOpportunityPatch
    {
        private static bool Prefix()
        {
            return !VanillaAuroraAuthority.ShouldBlockVanillaAurora();
        }
    }

    [HarmonyPatch(typeof(WeatherTransition), nameof(WeatherTransition.ActivateWeatherSetImmediate), new System.Type[] { typeof(WeatherStage) })]
    internal static class WeatherTransitionActivateWeatherSetImmediateAuroraPatch
    {
        private static bool Prefix(WeatherStage __0)
        {
            return !VanillaAuroraAuthority.ShouldBlockStage(__0);
        }
    }

    [HarmonyPatch(typeof(WeatherTransition), nameof(WeatherTransition.ActivateWeatherSetAtFrac), new System.Type[] { typeof(WeatherStage), typeof(float) })]
    internal static class WeatherTransitionActivateWeatherSetAtFracAuroraPatch
    {
        private static bool Prefix(WeatherStage __0)
        {
            return !VanillaAuroraAuthority.ShouldBlockStage(__0);
        }
    }

    [HarmonyPatch(typeof(WeatherTransition), nameof(WeatherTransition.ActivateWeatherSet), new System.Type[] { typeof(WeatherStage) })]
    internal static class WeatherTransitionActivateWeatherSetAuroraPatch
    {
        private static bool Prefix(WeatherStage __0)
        {
            return !VanillaAuroraAuthority.ShouldBlockStage(__0);
        }
    }

    [HarmonyPatch(typeof(WeatherTransition), nameof(WeatherTransition.ForceUnmanagedWeatherStage), new System.Type[] { typeof(WeatherStage), typeof(float) })]
    internal static class WeatherTransitionForceUnmanagedWeatherStageAuroraPatch
    {
        private static bool Prefix(WeatherStage __0)
        {
            return !VanillaAuroraAuthority.ShouldBlockStage(__0);
        }
    }
}