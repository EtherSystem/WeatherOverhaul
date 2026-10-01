namespace WeatherOverhaul.Weather
{
    internal enum GlobalNightEventType
    {
        None = 0,
        ClearAurora = 1,
        ClearBloodMoon = 2,
        LightSnowBloodMoon = 3,
        CloudyAurora = 5,
        SnowyAurora = 6,
        FoggyAurora = 7
    }

    internal sealed class GlobalNightEventSchedule
    {
        internal static readonly GlobalNightEventSchedule Empty = new GlobalNightEventSchedule(new List<NightEventWindow>());

        private readonly List<NightEventWindow> m_Windows;

        internal GlobalNightEventSchedule(List<NightEventWindow> windows)
        {
            m_Windows = windows ?? new List<NightEventWindow>();
        }

        internal IReadOnlyList<NightEventWindow> Windows => m_Windows;

        internal bool TryGetActive(float worldHour, out NightEventWindow activeWindow)
        {
            for (int i = 0; i < m_Windows.Count; i++)
            {
                if (m_Windows[i].Contains(worldHour))
                {
                    activeWindow = m_Windows[i];
                    return true;
                }
            }

            activeWindow = default;
            return false;
        }

        internal bool TryGetPreparationWindow(float worldHour, float leadHours, float tailHours, out NightEventWindow window, out float startWorldHour, out float endWorldHour)
        {
            for (int i = 0; i < m_Windows.Count; i++)
            {
                NightEventWindow candidate = m_Windows[i];
                if (!candidate.UsesPreparationTail) continue;

                float leadStart = candidate.StartWorldHour - leadHours;
                if (worldHour >= leadStart && worldHour < candidate.StartWorldHour)
                {
                    window = candidate;
                    startWorldHour = leadStart;
                    endWorldHour = candidate.StartWorldHour;
                    return true;
                }

                float tailEnd = candidate.EndWorldHour + tailHours;
                if (worldHour >= candidate.EndWorldHour && worldHour < tailEnd)
                {
                    window = candidate;
                    startWorldHour = candidate.EndWorldHour;
                    endWorldHour = tailEnd;
                    return true;
                }
            }

            window = default;
            startWorldHour = 0f;
            endWorldHour = 0f;
            return false;
        }

        internal GlobalAuroraSchedule ToAuroraSchedule()
        {
            List<AuroraWindow> auroraWindows = new List<AuroraWindow>();
            for (int i = 0; i < m_Windows.Count; i++)
            {
                NightEventWindow window = m_Windows[i];
                if (!NightEventWindow.IsAuroraEvent(window.EventType)) continue;
                auroraWindows.Add(new AuroraWindow(window.StartWorldHour, window.EndWorldHour, window.StartClockHour, window.EndClockHour));
            }

            return new GlobalAuroraSchedule(auroraWindows);
        }

        internal string BuildSummary(float currentWorldHour, float visibleUntilWorldHour = float.PositiveInfinity)
        {
            for (int i = 0; i < m_Windows.Count; i++)
            {
                NightEventWindow window = m_Windows[i];
                if (window.EndWorldHour < currentWorldHour) continue;
                if (window.StartWorldHour >= visibleUntilWorldHour - 0.01f) continue;

                float clippedEnd = Math.Min(window.EndWorldHour, visibleUntilWorldHour);
                float startOffset = window.StartWorldHour - currentWorldHour;
                float endOffset = clippedEnd - currentWorldHour;
                string name = NightEventWindow.ToDisplayName(window.EventType);
                if (startOffset <= 0f) return $"{name} active until +{Math.Max(0f, endOffset):0.#}h";
                return $"next {name} at +{startOffset:0.#}h for {Math.Max(0f, clippedEnd - window.StartWorldHour):0.#}h";
            }

            return "none in visible horizon";
        }

    }

    internal readonly struct NightEventWindow
    {
        internal readonly GlobalNightEventType EventType;
        internal readonly float StartWorldHour;
        internal readonly float EndWorldHour;
        internal readonly float StartClockHour;
        internal readonly float EndClockHour;

        internal NightEventWindow(GlobalNightEventType eventType, float startWorldHour, float endWorldHour, float startClockHour, float endClockHour)
        {
            EventType = eventType;
            StartWorldHour = startWorldHour;
            EndWorldHour = endWorldHour;
            StartClockHour = startClockHour;
            EndClockHour = endClockHour;
        }

        internal float DurationHours => EndWorldHour - StartWorldHour;

        internal bool UsesPreparationTail => IsAuroraEvent(EventType);

        internal bool Contains(float worldHour)
        {
            return worldHour >= StartWorldHour && worldHour < EndWorldHour;
        }

        internal WeatherStageId GetActiveStageId()
        {
            return EventType switch
            {
                GlobalNightEventType.ClearAurora => WeatherStageId.ClearAurora,
                GlobalNightEventType.CloudyAurora => WeatherStageId.CloudyAurora,
                GlobalNightEventType.SnowyAurora => WeatherStageId.SnowyAurora,
                GlobalNightEventType.FoggyAurora => WeatherStageId.FoggyAurora,
                GlobalNightEventType.ClearBloodMoon => WeatherStageId.ClearBloodMoon,
                GlobalNightEventType.LightSnowBloodMoon => WeatherStageId.SnowBloodMoon,
                _ => WeatherStageId.Clear
            };
        }

        internal WeatherStageId GetPreparationStageId()
        {
            return EventType switch
            {
                GlobalNightEventType.LightSnowBloodMoon => WeatherStageId.PartlyCloudy,
                GlobalNightEventType.CloudyAurora => WeatherStageId.Cloudy,
                GlobalNightEventType.SnowyAurora => WeatherStageId.LightSnow,
                GlobalNightEventType.FoggyAurora => WeatherStageId.HeavyOvercast,
                _ => WeatherStageId.Clear
            };
        }

        internal static bool IsAuroraEvent(GlobalNightEventType eventType)
        {
            return eventType == GlobalNightEventType.ClearAurora || eventType == GlobalNightEventType.CloudyAurora || eventType == GlobalNightEventType.SnowyAurora || eventType == GlobalNightEventType.FoggyAurora;

        }

        internal static bool IsBloodMoonEvent(GlobalNightEventType eventType)
        {
            return eventType == GlobalNightEventType.ClearBloodMoon || eventType == GlobalNightEventType.LightSnowBloodMoon;
        }

        internal static bool IsSupportedEvent(GlobalNightEventType eventType)
        {
            return eventType == GlobalNightEventType.ClearAurora ||
                   eventType == GlobalNightEventType.CloudyAurora ||
                   eventType == GlobalNightEventType.SnowyAurora ||
                   eventType == GlobalNightEventType.FoggyAurora ||
                   eventType == GlobalNightEventType.ClearBloodMoon ||
                   eventType == GlobalNightEventType.LightSnowBloodMoon;
        }

        internal static string ToDisplayName(GlobalNightEventType eventType)
        {
            switch (eventType)
            {
                case GlobalNightEventType.ClearAurora: return "Clear aurora";
                case GlobalNightEventType.CloudyAurora: return "Cloudy aurora";
                case GlobalNightEventType.SnowyAurora: return "Snowy aurora";
                case GlobalNightEventType.FoggyAurora: return "Foggy aurora";
                case GlobalNightEventType.ClearBloodMoon: return "Clear blood moon";
                case GlobalNightEventType.LightSnowBloodMoon: return "Snow blood moon";
                default: return "None";
            }
        }
    }

    internal sealed class GlobalAuroraSchedule
    {
        internal static readonly GlobalAuroraSchedule Empty = new GlobalAuroraSchedule(new List<AuroraWindow>());

        private readonly List<AuroraWindow> m_Windows;

        internal GlobalAuroraSchedule(List<AuroraWindow> windows)
        {
            m_Windows = windows ?? new List<AuroraWindow>();
        }

        internal IReadOnlyList<AuroraWindow> Windows => m_Windows;

        internal bool IsActive(float worldHour)
        {
            for (int i = 0; i < m_Windows.Count; i++)
            {
                if (m_Windows[i].Contains(worldHour)) return true;
            }

            return false;
        }


        internal bool IsPreparationWindow(float worldHour, float leadHours, float tailHours)
        {
            for (int i = 0; i < m_Windows.Count; i++)
            {
                AuroraWindow window = m_Windows[i];
                if (worldHour >= window.StartWorldHour - leadHours && worldHour < window.StartWorldHour) return true;
                if (worldHour >= window.EndWorldHour && worldHour < window.EndWorldHour + tailHours) return true;
            }

            return false;
        }

        internal string BuildSummary(float currentWorldHour)
        {
            for (int i = 0; i < m_Windows.Count; i++)
            {
                AuroraWindow window = m_Windows[i];
                if (window.EndWorldHour < currentWorldHour) continue;
                float startOffset = window.StartWorldHour - currentWorldHour;
                float endOffset = window.EndWorldHour - currentWorldHour;
                if (startOffset <= 0f) return $"active until +{endOffset:0.#}h";
                return $"next at +{startOffset:0.#}h for {window.DurationHours:0.#}h";
            }

            return "none in horizon";
        }
    }

    internal readonly struct AuroraWindow
    {
        internal readonly float StartWorldHour;
        internal readonly float EndWorldHour;
        internal readonly float StartClockHour;
        internal readonly float EndClockHour;

        internal AuroraWindow(float startWorldHour, float endWorldHour, float startClockHour, float endClockHour)
        {
            StartWorldHour = startWorldHour;
            EndWorldHour = endWorldHour;
            StartClockHour = startClockHour;
            EndClockHour = endClockHour;
        }

        internal float DurationHours => EndWorldHour - StartWorldHour;

        internal bool Contains(float worldHour)
        {
            return worldHour >= StartWorldHour && worldHour < EndWorldHour;
        }
    }

    internal sealed class GlobalGlimmerFogSchedule
    {
        internal static readonly GlobalGlimmerFogSchedule Empty = new GlobalGlimmerFogSchedule(new Dictionary<WeatherRegionId, List<GlimmerFogWindow>>());

        private readonly Dictionary<WeatherRegionId, List<GlimmerFogWindow>> m_WindowsByRegion;

        internal GlobalGlimmerFogSchedule(Dictionary<WeatherRegionId, List<GlimmerFogWindow>> windowsByRegion)
        {
            m_WindowsByRegion = windowsByRegion ?? new Dictionary<WeatherRegionId, List<GlimmerFogWindow>>();
        }

        internal IReadOnlyDictionary<WeatherRegionId, List<GlimmerFogWindow>> WindowsByRegion => m_WindowsByRegion;

        internal bool IsActive(WeatherRegionId regionId, float worldHour)
        {
            if (!WeatherOverhaulSettingsManager.EnableGlimmerFog) return false;
            if (!IsGlimmerFogRegion(regionId)) return false;
            if (!m_WindowsByRegion.TryGetValue(regionId, out List<GlimmerFogWindow> windows)) return false;

            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i].Contains(worldHour)) return true;
            }

            return false;
        }

        internal string BuildSummary(float currentWorldHour, float visibleUntilWorldHour = float.PositiveInfinity, WeatherRegionId regionFilter = WeatherRegionId.Unknown)
        {
            GlimmerFogWindow? next = null;
            WeatherRegionId nextRegion = WeatherRegionId.Unknown;

            foreach (KeyValuePair<WeatherRegionId, List<GlimmerFogWindow>> pair in m_WindowsByRegion)
            {
                if (regionFilter != WeatherRegionId.Unknown && pair.Key != regionFilter) continue;
                List<GlimmerFogWindow> windows = pair.Value;
                for (int i = 0; i < windows.Count; i++)
                {
                    GlimmerFogWindow window = windows[i];
                    if (window.EndWorldHour < currentWorldHour) continue;
                    if (window.StartWorldHour >= visibleUntilWorldHour - 0.01f) continue;
                    if (next == null || window.StartWorldHour < next.Value.StartWorldHour)
                    {
                        next = window;
                        nextRegion = pair.Key;
                    }
                }
            }

            if (next == null) return "none in visible horizon";

            GlimmerFogWindow value = next.Value;
            string region = RegionWeatherGraph.Get(nextRegion).ShortName;
            float clippedEnd = Math.Min(value.EndWorldHour, visibleUntilWorldHour);
            float startOffset = value.StartWorldHour - currentWorldHour;
            float endOffset = clippedEnd - currentWorldHour;
            if (startOffset <= 0f) return $"active in {region} until +{Math.Max(0f, endOffset):0.#}h";
            return $"next in {region} at +{startOffset:0.#}h for {Math.Max(0f, clippedEnd - value.StartWorldHour):0.#}h";
        }

        internal static bool IsGlimmerFogRegion(WeatherRegionId regionId)
        {
            return regionId == WeatherRegionId.FA || regionId == WeatherRegionId.ZOC || regionId == WeatherRegionId.SP;
        }
    }

    internal readonly struct GlimmerFogWindow
    {
        internal readonly float StartWorldHour;
        internal readonly float EndWorldHour;
        internal readonly float StartClockHour;
        internal readonly float EndClockHour;

        internal GlimmerFogWindow(float startWorldHour, float endWorldHour, float startClockHour, float endClockHour)
        {
            StartWorldHour = startWorldHour;
            EndWorldHour = endWorldHour;
            StartClockHour = startClockHour;
            EndClockHour = endClockHour;
        }

        internal float DurationHours => EndWorldHour - StartWorldHour;

        internal bool Contains(float worldHour)
        {
            return worldHour >= StartWorldHour && worldHour < EndWorldHour;
        }
    }
}