namespace WeatherOverhaul
{
    public static class WeatherOverhaulApi
    {
        public static bool TryGetCurrentStageDisplayName(out string displayName)
        {
            displayName = string.Empty;
            if (!GameplaySceneState.IsGameplaySceneActive()) return false;

            if (Weather.CustomWeatherStageRuntime.TryGetActiveStageId(out Weather.WeatherStageId customStageId))
            {
                displayName = Weather.WeatherStageCatalog.Get(customStageId).DisplayName;
                return !string.IsNullOrWhiteSpace(displayName);
            }

            Weather.WeatherSnapshot snapshot = Weather.WeatherSnapshot.Capture();
            if (!snapshot.IsValid || string.IsNullOrWhiteSpace(snapshot.StageLabel)) return false;

            displayName = snapshot.StageLabel;
            return true;
        }
    }
}