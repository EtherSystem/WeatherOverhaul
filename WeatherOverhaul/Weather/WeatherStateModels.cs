namespace WeatherOverhaul.Weather
{
    internal readonly struct RegionWeatherSegment
    {
        internal readonly float StartWorldHour;
        internal readonly float EndWorldHour;
        internal readonly WeatherStageId StageId;
        internal readonly WeatherFamily Family;
        internal readonly WeatherStage RepresentativeStage;
        internal readonly string Reason;
        internal readonly bool IsExactObservedSourceStage;

        internal RegionWeatherSegment(float startWorldHour, float endWorldHour, WeatherStageId stageId, string reason, bool isExactObservedSourceStage = false)
        {
            WeatherStageDefinition stage = WeatherStageCatalog.Get(stageId);

            StartWorldHour = startWorldHour;
            EndWorldHour = endWorldHour;
            StageId = stage.Id;
            Family = stage.Family;
            RepresentativeStage = stage.EngineStage;
            Reason = string.IsNullOrEmpty(reason) ? "regional simulation" : reason;
            IsExactObservedSourceStage = isExactObservedSourceStage;
        }

        internal float DurationHours => EndWorldHour - StartWorldHour;

        internal bool Contains(float worldHour)
        {
            return worldHour >= StartWorldHour && worldHour < EndWorldHour;
        }
    }

    internal sealed class RegionWeatherTimeline
    {
        internal WeatherRegionId RegionId { get; }
        internal List<RegionWeatherSegment> Segments { get; } = [];

        internal RegionWeatherTimeline(WeatherRegionId regionId)
        {
            RegionId = regionId;
        }

        internal bool TryGetSegmentAt(float worldHour, out RegionWeatherSegment segment)
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].Contains(worldHour))
                {
                    segment = Segments[i];
                    return true;
                }
            }

            if (Segments.Count > 0)
            {
                RegionWeatherSegment first = Segments[0];
                RegionWeatherSegment last = Segments[^1];
                if (worldHour < first.StartWorldHour)
                {
                    segment = first;
                    return true;
                }

                if (worldHour >= last.EndWorldHour - 0.01f)
                {
                    segment = last;
                    return true;
                }
            }

            segment = default;
            return false;
        }
    }

    internal readonly struct WeatherSnapshot
    {
        internal static readonly WeatherSnapshot Invalid = new(false, 0f, 0, 0, 0, WeatherStage.Undefined, "Unavailable", 0f, 0f, 0f, WindStrength.Calm, false, "Unknown", WeatherRegionId.Unknown, "???", "Unknown");

        internal readonly bool IsValid;
        internal readonly float WorldHour;
        internal readonly int DayNumber;
        internal readonly int Hour;
        internal readonly int Minute;
        internal readonly WeatherStage Stage;
        internal readonly string StageLabel;
        internal readonly float TemperatureCelsius;
        internal readonly float WindSpeedMph;
        internal readonly float WindAngle;
        internal readonly WindStrength WindStrength;
        internal readonly bool IsIndoorEnvironment;
        internal readonly string SceneName;
        internal readonly WeatherRegionId RegionId;
        internal readonly string RegionShortName;
        internal readonly string RegionDisplayName;

        private WeatherSnapshot(bool isValid, float worldHour, int dayNumber, int hour, int minute, WeatherStage stage, string stageLabel, float temperatureCelsius, float windSpeedMph, float windAngle, WindStrength windStrength, bool isIndoorEnvironment, string sceneName, WeatherRegionId regionId, string regionShortName, string regionDisplayName)
        {
            IsValid = isValid;
            WorldHour = worldHour;
            DayNumber = dayNumber;
            Hour = hour;
            Minute = minute;
            Stage = stage;
            StageLabel = stageLabel;
            TemperatureCelsius = temperatureCelsius;
            WindSpeedMph = windSpeedMph;
            WindAngle = windAngle;
            WindStrength = windStrength;
            IsIndoorEnvironment = isIndoorEnvironment;
            SceneName = sceneName;
            RegionId = regionId;
            RegionShortName = regionShortName;
            RegionDisplayName = regionDisplayName;
        }

        internal static WeatherSnapshot Capture()
        {
            try
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (GameplaySceneState.IsMainMenuOrBootSceneName(sceneName)) return Invalid;
                if (string.IsNullOrEmpty(sceneName)) sceneName = "Unknown";

                TimeOfDay timeOfDay = GameManager.GetTimeOfDayComponent();
                Il2Cpp.Weather weather = GameManager.GetWeatherComponent();
                Wind wind = GameManager.GetWindComponent();
                if (timeOfDay == null || weather == null || wind == null) return Invalid;

                WeatherStage stage = weather.GetWeatherStage();

                WeatherRegionDefinition region = SceneRegionMapper.Resolve(sceneName);
                int dayNumber = timeOfDay.GetDayNumber();
                int hour = timeOfDay.GetHour();
                int minute = timeOfDay.GetMinutes();
                float clockWorldHour = GetClockWorldHour(dayNumber, hour, minute);

                return new WeatherSnapshot(
                    true,
                    clockWorldHour,
                    dayNumber,
                    hour,
                    minute,
                    stage,
                    WeatherStageFormatter.ToDisplayName(stage),
                    weather.GetCurrentTemperatureWithoutHeatSources(),
                    wind.GetSpeedMPH(),
                    wind.GetWindAngle(),
                    wind.GetStrength(),
                    weather.IsIndoorEnvironment(),
                    sceneName,
                    region.Id,
                    region.ShortName,
                    region.DisplayName);
            }
            catch (Exception ex)
            {
                Core.LogExceptionOnce("weather-snapshot-capture", "Failed to capture the current weather snapshot.", ex);
                return Invalid;
            }
        }


        internal WeatherSnapshot WithClockFrom(WeatherSnapshot clockSource)
        {
            if (!IsValid || !clockSource.IsValid) return this;

            return new WeatherSnapshot(
                true,
                clockSource.WorldHour,
                clockSource.DayNumber,
                clockSource.Hour,
                clockSource.Minute,
                Stage,
                StageLabel,
                TemperatureCelsius,
                WindSpeedMph,
                WindAngle,
                WindStrength,
                IsIndoorEnvironment,
                SceneName,
                RegionId,
                RegionShortName,
                RegionDisplayName);
        }

        internal WeatherSnapshot WithAuthorityWorldHour(float worldHour)
        {
            if (!IsValid) return this;

            return new WeatherSnapshot(
                true,
                worldHour,
                DayNumber,
                Hour,
                Minute,
                Stage,
                StageLabel,
                TemperatureCelsius,
                WindSpeedMph,
                WindAngle,
                WindStrength,
                IsIndoorEnvironment,
                SceneName,
                RegionId,
                RegionShortName,
                RegionDisplayName);
        }

        internal WeatherSnapshot WithRegionClock(string sceneName, WeatherRegionDefinition region, float worldHour)
        {
            if (!IsValid) return this;
            if (region.Id == WeatherRegionId.Unknown) return this;

            return new WeatherSnapshot(
                true,
                worldHour,
                DayNumber,
                Hour,
                Minute,
                Stage,
                StageLabel,
                TemperatureCelsius,
                WindSpeedMph,
                WindAngle,
                WindStrength,
                IsIndoorEnvironment,
                string.IsNullOrEmpty(sceneName) ? region.SceneName : sceneName,
                region.Id,
                region.ShortName,
                region.DisplayName);
        }

        private static float GetClockWorldHour(int dayNumber, int hour, int minute)
        {
            int dayIndex = Math.Max(0, dayNumber - 1);
            return dayIndex * 24f + hour + minute / 60f;
        }

        internal string GetClockText()
        {
            return $"Day {DayNumber}, {Hour:00}:{Minute:00}";
        }
    }

    internal sealed class GlobalWeatherState
    {
        internal static readonly GlobalWeatherState Empty = new(false, 0f, 0f, WeatherRegionId.Unknown, [], GlobalNightEventSchedule.Empty, GlobalAuroraSchedule.Empty, GlobalGlimmerFogSchedule.Empty, "No global weather simulation available.");

        internal bool IsValid { get; }
        internal float GeneratedAtWorldHour { get; }
        internal float HorizonEndWorldHour { get; }
        internal WeatherRegionId AnchorRegion { get; }
        internal Dictionary<WeatherRegionId, RegionWeatherTimeline> Timelines { get; }
        internal GlobalNightEventSchedule NightEventSchedule { get; }
        internal GlobalAuroraSchedule AuroraSchedule { get; }
        internal GlobalGlimmerFogSchedule GlimmerFogSchedule { get; }
        internal string Summary { get; }

        internal GlobalWeatherState(bool isValid, float generatedAtWorldHour, float horizonEndWorldHour, WeatherRegionId anchorRegion, Dictionary<WeatherRegionId, RegionWeatherTimeline> timelines, GlobalNightEventSchedule nightEventSchedule, GlobalAuroraSchedule auroraSchedule, GlobalGlimmerFogSchedule glimmerFogSchedule, string summary)
        {
            IsValid = isValid;
            GeneratedAtWorldHour = generatedAtWorldHour;
            HorizonEndWorldHour = horizonEndWorldHour;
            AnchorRegion = anchorRegion;
            Timelines = timelines;
            NightEventSchedule = nightEventSchedule ?? GlobalNightEventSchedule.Empty;
            AuroraSchedule = auroraSchedule ?? NightEventSchedule.ToAuroraSchedule();
            GlimmerFogSchedule = glimmerFogSchedule ?? GlobalGlimmerFogSchedule.Empty;
            Summary = summary;
        }

        internal bool TryGetSegment(WeatherRegionId regionId, float worldHour, out RegionWeatherSegment segment)
        {
            if (Timelines.TryGetValue(regionId, out RegionWeatherTimeline timeline)) return timeline.TryGetSegmentAt(worldHour, out segment);
            segment = default;
            return false;
        }
    }
}