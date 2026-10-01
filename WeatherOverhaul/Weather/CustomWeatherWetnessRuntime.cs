namespace WeatherOverhaul.Weather
{
    internal static class CustomWeatherWetnessRuntime
    {
        private static int s_ScopeDepth;
        private static WeatherStage s_SpoofStage = WeatherStage.Undefined;
        private static float s_WetnessMultiplier = 1f;
        private static UniStormWeatherSystem? s_SpoofedUniStorm;
        private static WeatherStage s_OriginalUniStormStage = WeatherStage.Undefined;

        internal static bool BeginEvaluation()
        {
            if (s_ScopeDepth > 0)
            {
                s_ScopeDepth++;
                return true;
            }

            Il2Cpp.Weather weather = GameManager.GetWeatherComponent();
            if (weather == null || weather.IsIndoorEnvironment()) return false;
            if (!CustomWeatherStageRuntime.TryGetActiveStageId(out WeatherStageId stageId)) return false;
            if (!TryGetWetnessProfile(stageId, out WeatherStage wetnessStage, out float multiplier)) return false;

            s_SpoofStage = wetnessStage;
            s_WetnessMultiplier = multiplier;
            s_ScopeDepth = 1;
            SpoofUniStormStage();
            return true;
        }

        internal static void EndEvaluation(bool scopeStarted)
        {
            if (!scopeStarted || s_ScopeDepth <= 0) return;

            s_ScopeDepth--;
            if (s_ScopeDepth > 0) return;

            RestoreUniStormStage();
            s_SpoofStage = WeatherStage.Undefined;
            s_WetnessMultiplier = 1f;
        }

        internal static bool TryGetScopedWeatherStage(out WeatherStage stage)
        {
            stage = s_SpoofStage;
            return s_ScopeDepth > 0 && stage != WeatherStage.Undefined;
        }

        internal static void ScaleWetnessIncrease(ref float wetnessPercentIncrease)
        {
            if (s_ScopeDepth <= 0) return;
            wetnessPercentIncrease *= s_WetnessMultiplier;
        }

        private static bool TryGetWetnessProfile(WeatherStageId stageId, out WeatherStage wetnessStage, out float multiplier)
        {
            switch (stageId)
            {
                case WeatherStageId.LowOvercast:
                    wetnessStage = WeatherStage.LightSnow;
                    multiplier = 0.5f;
                    return true;
                case WeatherStageId.SnowyAurora:
                    wetnessStage = WeatherStage.LightSnow;
                    multiplier = 1f;
                    return true;
                case WeatherStageId.VeryHeavySnow:
                    wetnessStage = WeatherStage.HeavySnow;
                    multiplier = 1.5f;
                    return true;
                default:
                    wetnessStage = WeatherStage.Undefined;
                    multiplier = 1f;
                    return false;
            }
        }

        private static void SpoofUniStormStage()
        {
            try
            {
                UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
                if (uniStorm == null) return;

                s_SpoofedUniStorm = uniStorm;
                s_OriginalUniStormStage = uniStorm.m_CurrentWeatherStage;
                uniStorm.m_CurrentWeatherStage = s_SpoofStage;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherWetnessRuntime.SpoofUniStormStage", "Could not apply the custom-weather wetness stage.", caughtException);
                s_SpoofedUniStorm = null;
            }
        }

        private static void RestoreUniStormStage()
        {
            if (s_SpoofedUniStorm == null) return;

            try
            {
                s_SpoofedUniStorm.m_CurrentWeatherStage = s_OriginalUniStormStage;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherWetnessRuntime.RestoreUniStormStage", "Could not restore the UniStorm stage after the custom-weather wetness calculation.", caughtException);
            }
            finally
            {
                s_SpoofedUniStorm = null;
                s_OriginalUniStormStage = WeatherStage.Undefined;
            }
        }
    }
}