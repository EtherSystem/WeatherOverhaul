namespace WeatherOverhaul.Weather
{
    internal static class MoonPhaseRuntime
    {
        private const float AuthorityClockToleranceHours = 0.25f;
        private const int DefaultMoonPhaseCount = 8;
        private const int FullMoonPhaseIndex = 5;

        internal static bool TryIsFullMoon(out bool isFullMoon)
        {
            if (!TryGetNormalizedMoonPhase(out int phaseIndex, out int phaseCount))
            {
                isFullMoon = false;
                return false;
            }

            int fullMoonIndex = Mathf.Clamp(FullMoonPhaseIndex, 0, Math.Max(0, phaseCount - 1));
            isFullMoon = phaseIndex == fullMoonIndex;
            return true;
        }

        internal static bool TryIsFullMoon(float expectedWorldHour, out bool isFullMoon)
        {
            WeatherSnapshot currentSnapshot = WeatherSnapshot.Capture();
            if (!currentSnapshot.IsValid || Math.Abs(currentSnapshot.WorldHour - expectedWorldHour) > AuthorityClockToleranceHours)
            {
                isFullMoon = false;
                return false;
            }

            return TryIsFullMoon(out isFullMoon);
        }

        internal static bool IsAuthoritativeFullMoon(float expectedWorldHour)
        {
            return TryIsFullMoon(expectedWorldHour, out bool isFullMoon) && isFullMoon;
        }

        internal static bool IsAuthoritativeFullMoon()
        {
            return TryIsFullMoon(out bool isFullMoon) && isFullMoon;
        }

        internal static float GetCurrentIllumination01()
        {
            if (!TryGetNormalizedMoonPhase(out int phaseIndex, out int phaseCount)) return 0f;

            int fullMoonIndex = Mathf.Clamp(FullMoonPhaseIndex, 0, Math.Max(0, phaseCount - 1));
            if (phaseIndex == fullMoonIndex) return 1f;

            int distance = Math.Abs(phaseIndex - fullMoonIndex);
            distance = Math.Min(distance, phaseCount - distance);
            float halfCycle = Math.Max(1f, phaseCount * 0.5f);
            return Mathf.Clamp01(1f - distance / halfCycle);
        }

        private static bool TryGetNormalizedMoonPhase(out int phaseIndex, out int phaseCount)
        {
            phaseIndex = 0;
            phaseCount = DefaultMoonPhaseCount;

            try
            {
                UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
                if (uniStorm == null) return false;

                if (uniStorm.m_MoonPhases != null && uniStorm.m_MoonPhases.Length > 0)
                {
                    phaseCount = uniStorm.m_MoonPhases.Length;
                }

                phaseCount = Math.Max(1, phaseCount);
                phaseIndex = PositiveModulo(uniStorm.m_CurrentMoonPhaseIndex, phaseCount);
                return true;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("MoonPhaseRuntime.TryGetNormalizedMoonPhase", "MoonPhaseRuntime could not read the current moon phase.", caughtException);
                phaseIndex = 0;
                phaseCount = DefaultMoonPhaseCount;
                return false;
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0) return 0;
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }
}
