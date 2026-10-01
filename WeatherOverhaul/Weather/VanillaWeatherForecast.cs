using System.Globalization;
using System.Text.RegularExpressions;

namespace WeatherOverhaul.Weather
{
    internal readonly struct VanillaWeatherForecastSnapshot
    {
        internal VanillaWeatherForecastSnapshot(bool isValid, WeatherRegionId regionId, WeatherStageId stageId, WeatherStage stage, bool hasTiming, float elapsedHours, float totalDurationHours, float remainingHours)
        {
            IsValid = isValid;
            RegionId = regionId;
            StageId = stageId;
            Stage = stage;
            HasTiming = hasTiming;
            ElapsedHours = elapsedHours;
            TotalDurationHours = totalDurationHours;
            RemainingHours = remainingHours;
        }

        internal bool IsValid { get; }
        internal WeatherRegionId RegionId { get; }
        internal WeatherStageId StageId { get; }
        internal WeatherStage Stage { get; }
        internal bool HasTiming { get; }
        internal float ElapsedHours { get; }
        internal float TotalDurationHours { get; }
        internal float RemainingHours { get; }
        internal WeatherStageDefinition Definition => WeatherStageCatalog.Get(StageId);
    }

    internal static class VanillaWeatherForecast
    {
        private const float RefreshIntervalSeconds = 0.25f;
        private static readonly Regex s_TransitionDurationRegex = new(@"(?<elapsed>\d+(?:[\.,]\d+)?)/(?<total>\d+(?:[\.,]\d+)?)\s*hrs", RegexOptions.CultureInvariant);

        private static WeatherTransition? s_CachedTransition;
        private static VanillaWeatherForecastSnapshot s_CachedSnapshot;
        private static float s_NextRefreshRealtime;
        private static string s_CachedSceneName = string.Empty;

        internal static bool TryGetCurrent(WeatherSnapshot weatherSnapshot, out VanillaWeatherForecastSnapshot forecast)
        {
            forecast = default;
            if (!weatherSnapshot.IsValid || weatherSnapshot.RegionId == WeatherRegionId.Unknown) return false;

            bool sceneChanged = !string.Equals(s_CachedSceneName, weatherSnapshot.SceneName, StringComparison.Ordinal);
            bool stageChanged = !s_CachedSnapshot.IsValid || s_CachedSnapshot.Stage != weatherSnapshot.Stage || s_CachedSnapshot.RegionId != weatherSnapshot.RegionId;
            if (sceneChanged || stageChanged || Time.realtimeSinceStartup >= s_NextRefreshRealtime)
            {
                Refresh(weatherSnapshot, sceneChanged);
            }

            if (!s_CachedSnapshot.IsValid || s_CachedSnapshot.RegionId != weatherSnapshot.RegionId) return false;
            forecast = s_CachedSnapshot;
            return true;
        }

        internal static bool TryGetCurrentPlan(WeatherSnapshot weatherSnapshot, out WeatherActivationPlan plan)
        {
            plan = default;
            if (!TryGetCurrent(weatherSnapshot, out VanillaWeatherForecastSnapshot forecast)) return false;

            float now = weatherSnapshot.WorldHour;
            float start = forecast.HasTiming ? now - forecast.ElapsedHours : now;
            float end = forecast.HasTiming ? now + forecast.RemainingHours : now;
            float duration = forecast.HasTiming ? forecast.TotalDurationHours : 0f;
            float elapsed = forecast.HasTiming ? forecast.ElapsedHours : 0f;
            float remaining = forecast.HasTiming ? forecast.RemainingHours : 0f;

            plan = new WeatherActivationPlan(forecast.StageId, start, end, duration, elapsed, remaining, "vanilla WeatherTransition observation");
            return true;
        }

        private static void Refresh(WeatherSnapshot weatherSnapshot, bool sceneChanged)
        {
            s_NextRefreshRealtime = Time.realtimeSinceStartup + RefreshIntervalSeconds;
            s_CachedSceneName = weatherSnapshot.SceneName ?? string.Empty;
            if (sceneChanged) s_CachedTransition = null;

            WeatherStage stage = weatherSnapshot.Stage;
            try
            {
                Il2Cpp.Weather weather = GameManager.GetWeatherComponent();
                if (weather != null) stage = weather.GetWeatherStage();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("VanillaWeatherForecast.Refresh.WeatherStage", "VanillaWeatherForecast could not read the current vanilla weather stage.", caughtException);
            }

            WeatherStageId stageId = stage == WeatherStage.Undefined ? WeatherStageId.Undefined : WeatherStageCatalog.FromEngineStage(stage);
            bool hasTiming = TryReadTransitionTiming(out float elapsedHours, out float totalDurationHours, out float remainingHours);

            s_CachedSnapshot = new VanillaWeatherForecastSnapshot(
                true,
                weatherSnapshot.RegionId,
                stageId,
                stage,
                hasTiming,
                elapsedHours,
                totalDurationHours,
                remainingHours);
        }

        private static bool TryReadTransitionTiming(out float elapsedHours, out float totalDurationHours, out float remainingHours)
        {
            elapsedHours = 0f;
            totalDurationHours = 0f;
            remainingHours = 0f;

            try
            {
                if (s_CachedTransition == null) s_CachedTransition = UnityEngine.Object.FindObjectOfType<WeatherTransition>();
                if (s_CachedTransition == null) return false;

                string debug = s_CachedTransition.GetDebugString();
                if (string.IsNullOrWhiteSpace(debug)) return false;

                string[] lines = debug.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrEmpty(line) || !line.Contains(" >> ", StringComparison.Ordinal)) continue;

                    Match match = s_TransitionDurationRegex.Match(line);
                    if (!match.Success) continue;
                    if (!TryParseDebugFloat(match.Groups["elapsed"].Value, out elapsedHours)) continue;
                    if (!TryParseDebugFloat(match.Groups["total"].Value, out totalDurationHours)) continue;

                    elapsedHours = Math.Max(0f, elapsedHours);
                    totalDurationHours = Math.Max(0f, totalDurationHours);
                    remainingHours = Math.Max(0f, totalDurationHours - elapsedHours);
                    return totalDurationHours > 0.001f;
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("VanillaWeatherForecast.TryReadTransitionTiming", "VanillaWeatherForecast could not read WeatherTransition timing.", caughtException);
                s_CachedTransition = null;
            }

            return false;
        }

        private static bool TryParseDebugFloat(string value, out float result)
        {
            string normalized = (value ?? string.Empty).Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}