namespace WeatherOverhaul.Weather
{
    internal static class VanillaAuroraAuthority
    {
        private static bool s_OwnsForceDisable;
        private static bool s_OriginalForceDisable;
        private static bool s_LastBlockState;

        internal static void PrepareAuroraManagerUpdate()
        {
            if (ShouldBlockVanillaAurora())
            {
                AcquireForceDisable();
                return;
            }

            Release();
        }

        internal static bool ShouldBlockVanillaAurora()
        {
            if (!GameplaySceneState.IsGameplaySceneActive()) return false;
            if (!WeatherDirector.Enabled) return false;
            if (WeatherDirector.IsApplyingForecastStage || CustomWeatherStageRuntime.IsApplyingCustomBaseStage) return false;
            if (!HasLoadedRegionAuthority()) return false;

            if (!TryGetCurrentWorldHour(out float worldHour)) return true;
            if (!GlobalWeatherSimulation.Current.IsValid) return true;
            if (!GlobalWeatherSimulation.Current.NightEventSchedule.TryGetActive(worldHour, out NightEventWindow window)) return true;
            return !NightEventWindow.IsAuroraEvent(window.EventType);
        }

        internal static bool ShouldBlockStage(WeatherStage stage)
        {
            return stage == WeatherStage.ClearAurora && ShouldBlockVanillaAurora();
        }

        internal static void Release()
        {
            if (!s_OwnsForceDisable) return;

            AuroraManager.m_ForceDisable = s_OriginalForceDisable;
            s_OwnsForceDisable = false;
            LogStateChange(false);
        }

        private static void AcquireForceDisable()
        {
            if (!s_OwnsForceDisable)
            {
                s_OriginalForceDisable = AuroraManager.m_ForceDisable;
                s_OwnsForceDisable = true;
            }

            AuroraManager.m_ForceDisable = true;
            LogStateChange(true);
        }

        private static void LogStateChange(bool blocked)
        {
            if (s_LastBlockState == blocked) return;
            s_LastBlockState = blocked;
            Core.Log(blocked ? "[AuroraAuthority] Independent vanilla aurora generation blocked; WeatherOverhaul owns loaded-region aurora timing." : "[AuroraAuthority] Vanilla aurora manager released for a planned WeatherOverhaul aurora or disabled loaded-region control.");
        }

        private static bool HasLoadedRegionAuthority()
        {
            try
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                WeatherSnapshot snapshot = WeatherOverhaulRuntime.CurrentSnapshot;
                if (snapshot.IsValid && string.Equals(snapshot.SceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    if (snapshot.RegionId == WeatherRegionId.Unknown) return false;
                    if (RegionWeatherGraph.IsDynamicRegion(snapshot.RegionId) && !WeatherOverhaulSettingsManager.UseDefaultProfileForUnmappedRegions) return false;
                    return true;
                }

                WeatherRegionDefinition region = SceneRegionMapper.Resolve(sceneName);
                return region.Id != WeatherRegionId.Unknown;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("VanillaAuroraAuthority.HasLoadedRegionAuthority.1", "VanillaAuroraAuthority could not resolve loaded-region weather authority.", caughtException);
                return false;
            }
        }

        private static bool TryGetCurrentWorldHour(out float worldHour)
        {
            worldHour = 0f;
            try
            {
                Il2Cpp.TimeOfDay timeOfDay = GameManager.GetTimeOfDayComponent();
                if (timeOfDay == null) return false;

                int dayNumber = Math.Max(1, timeOfDay.GetDayNumber());
                worldHour = (dayNumber - 1) * 24f + timeOfDay.GetHour() + timeOfDay.GetMinutes() / 60f;
                return true;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("VanillaAuroraAuthority.TryGetCurrentWorldHour.1", "VanillaAuroraAuthority could not read the current game clock.", caughtException);
                return false;
            }
        }
    }
}