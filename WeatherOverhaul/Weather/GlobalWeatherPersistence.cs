using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherOverhaul.Weather
{
    internal static class GlobalWeatherPersistence
    {
        private const int SaveVersion = 22;
        private const int OldestSupportedSaveVersion = 17;
        private const string SaveSuffix = "GlobalWeatherState";
        private const string RollingForecastMaintenanceReason = "rolling forecast maintenance";
        private const float RollingSaveLogIntervalHours = 6f;
        private const float StabilizedClockFutureForecastToleranceHours = 0.25f;
        private static readonly ModDataManager s_DataManager = new("WeatherOverhaul");
        private static bool s_LoadAttemptedForCurrentSave;
        private static float s_LastRollingSaveLogWorldHour = -1000f;
        private static float s_LastRollingSaveLogGeneratedAt = -1000f;
        private static float s_LastRollingSaveLogHorizonEnd = -1000f;
        internal static bool IsLoadDeferredForStableClock { get; private set; }

        internal static void ResetLoadAttempt()
        {
            s_LoadAttemptedForCurrentSave = false;
            IsLoadDeferredForStableClock = false;
            s_LastRollingSaveLogWorldHour = -1000f;
            s_LastRollingSaveLogGeneratedAt = -1000f;
            s_LastRollingSaveLogHorizonEnd = -1000f;
        }

        internal static void TryLoad(WeatherSnapshot snapshot)
        {
            if (IsLoadDeferredForStableClock)
            {
                if (snapshot.DayNumber <= 1 && snapshot.WorldHour < 48f) return;
                IsLoadDeferredForStableClock = false;
                s_LoadAttemptedForCurrentSave = false;
            }

            if (s_LoadAttemptedForCurrentSave) return;
            if (GlobalWeatherSimulation.Current.IsValid) return;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;

            try
            {
                string? payload = s_DataManager.Load(SaveSuffix);
                s_LoadAttemptedForCurrentSave = true;
                if (string.IsNullOrWhiteSpace(payload))
                {
                    Core.Log("[GlobalSim][ModData] No persisted carried forecast found for this save slot.");
                    return;
                }

                PersistedGlobalWeatherState? persisted = JsonSerializer.Deserialize<PersistedGlobalWeatherState>(payload);
                if (persisted == null || persisted.Version < OldestSupportedSaveVersion || persisted.Version > SaveVersion)
                {
                    Core.Log("[GlobalSim][ModData] Persisted forecast ignored: unsupported or empty save payload.");
                    return;
                }

                GlobalWeatherState? restored = ToRuntimeState(persisted);
                if (restored == null || !restored.IsValid)
                {
                    Core.Warn("[GlobalSim][ModData] Persisted forecast ignored: data could not be restored.");
                    return;
                }

                if (!PersistedEventSettingsMatch(persisted))
                {
                    if (DeferRejectedForecastUntilStableClock(persisted, snapshot, "event settings migration")) return;
                    Core.Log("[GlobalSim][ModData] Persisted forecast ignored: global weather event settings changed.");
                    return;
                }

                if (restored.GeneratedAtWorldHour > snapshot.WorldHour + 12f)
                {
                    if (!WeatherOverhaulRuntime.IsGameplayRuntimeReady && IsLikelyLoadingDefaultClockForPersistedForecast(snapshot, restored))
                    {
                        IsLoadDeferredForStableClock = false;
                        WeatherSnapshot authoritySnapshot = snapshot.WithAuthorityWorldHour(restored.GeneratedAtWorldHour);
                        Core.Log($"[GlobalSim][ModData] Restoring persisted forecast while scene clock is still at its loading default. | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | UsingPersistedGeneratedAt={restored.GeneratedAtWorldHour:0.##}h");
                        GlobalWeatherSimulation.Restore(restored, authoritySnapshot, "save slot load using persisted authority clock");
                        return;
                    }

                    Core.Warn("[GlobalSim][ModData] Persisted forecast ignored: generated time is ahead of the current save time.");
                    return;
                }

                if (WeatherOverhaulRuntime.IsGameplayRuntimeReady && restored.GeneratedAtWorldHour > snapshot.WorldHour + StabilizedClockFutureForecastToleranceHours)
                {
                    Core.Warn($"[GlobalSim][ModData] Persisted forecast ignored: generated time is ahead of the stabilized save clock. | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | PersistedGeneratedAt={restored.GeneratedAtWorldHour:0.##}h");
                    return;
                }

                IsLoadDeferredForStableClock = false;
                GlobalWeatherSimulation.Restore(restored, snapshot, "save slot load");
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("global-weather-persistence-load", "[GlobalSim][ModData] Failed to load persisted forecast.", e);
            }
        }

        internal static void Save(GlobalWeatherState state, WeatherSnapshot snapshot, string reason)
        {
            if (state == null || !state.IsValid) return;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;
            if (IsLoadDeferredForStableClock && snapshot.WorldHour < 1f)
            {
                Core.Log($"[GlobalSim][ModData] Skipped forecast save while save-slot clock is still unstable. | Reason={reason} | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h)");
                return;
            }

            try
            {
                PersistedGlobalWeatherState persisted = FromRuntimeState(state);
                string payload = JsonSerializer.Serialize(persisted);
                if (s_DataManager.Save(payload, SaveSuffix))
                {
                    if (ShouldLogSuccessfulSave(state, snapshot, reason)) Core.Log($"[GlobalSim][ModData] Persisted carried forecast ({reason}) | GeneratedAt={state.GeneratedAtWorldHour:0.##}h | HorizonEnd={state.HorizonEndWorldHour:0.##}h");
                }
                else
                {
                    Core.Warn("[GlobalSim][ModData] Persisted forecast save returned false.");
                }
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("global-weather-persistence-save", "[GlobalSim][ModData] Failed to save carried forecast.", e);
            }
        }



        private static bool DeferRejectedForecastUntilStableClock(PersistedGlobalWeatherState persisted, WeatherSnapshot snapshot, string reason)
        {
            if (WeatherOverhaulRuntime.IsGameplayRuntimeReady) return false;
            if (!IsLikelyLoadingDefaultClockForPersistedForecast(snapshot, persisted.GeneratedAtWorldHour)) return false;

            IsLoadDeferredForStableClock = true;
            s_LoadAttemptedForCurrentSave = false;
            Core.Log($"[GlobalSim][ModData] Deferring persisted forecast migration until the save-slot clock is stable. | Reason={reason} | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | PersistedGeneratedAt={persisted.GeneratedAtWorldHour:0.##}h");
            return true;
        }

        private static bool IsLikelyLoadingDefaultClockForPersistedForecast(WeatherSnapshot snapshot, GlobalWeatherState restored)
        {
            if (restored == null || !restored.IsValid) return false;
            return IsLikelyLoadingDefaultClockForPersistedForecast(snapshot, restored.GeneratedAtWorldHour);
        }

        private static bool IsLikelyLoadingDefaultClockForPersistedForecast(WeatherSnapshot snapshot, float persistedGeneratedAtWorldHour)
        {
            if (!snapshot.IsValid) return false;
            if (snapshot.DayNumber > 1) return false;
            if (snapshot.WorldHour >= 48f) return false;
            return persistedGeneratedAtWorldHour > 48f;
        }

        private static bool ShouldLogSuccessfulSave(GlobalWeatherState state, WeatherSnapshot snapshot, string reason)
        {
            if (!WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) return false;
            if (!string.Equals(reason, RollingForecastMaintenanceReason, StringComparison.Ordinal)) return true;

            bool generatedAtChanged = Math.Abs(state.GeneratedAtWorldHour - s_LastRollingSaveLogGeneratedAt) > 0.01f;
            bool horizonChanged = Math.Abs(state.HorizonEndWorldHour - s_LastRollingSaveLogHorizonEnd) > 0.01f;
            bool intervalElapsed = snapshot.WorldHour - s_LastRollingSaveLogWorldHour >= RollingSaveLogIntervalHours;
            if (!generatedAtChanged && !horizonChanged && !intervalElapsed) return false;

            s_LastRollingSaveLogWorldHour = snapshot.WorldHour;
            s_LastRollingSaveLogGeneratedAt = state.GeneratedAtWorldHour;
            s_LastRollingSaveLogHorizonEnd = state.HorizonEndWorldHour;
            return true;
        }

        private static bool PersistedEventSettingsMatch(PersistedGlobalWeatherState persisted)
        {
            if (Math.Abs(persisted.AuroraChanceProbability - WeatherOverhaulSettingsManager.AuroraChanceProbability) > 0.001f) return false;
            if (Math.Abs(persisted.BloodMoonChanceProbability - WeatherOverhaulSettingsManager.BloodMoonChanceProbability) > 0.001f) return false;
            if (persisted.BloodMoonRequiresFullMoon != WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon) return false;
            if (Math.Abs(persisted.GlimmerFogChanceProbability - WeatherOverhaulSettingsManager.GlimmerFogChanceProbability) > 0.001f) return false;
            if (!string.Equals(persisted.RegionalSeveritySignature ?? string.Empty, WeatherOverhaulSettingsManager.EventSettingsSignature, StringComparison.Ordinal)) return false;
            return true;
        }

        private static PersistedGlobalWeatherState FromRuntimeState(GlobalWeatherState state)
        {
            PersistedGlobalWeatherState persisted = new()
            {
                Version = SaveVersion,
                GeneratedAtWorldHour = state.GeneratedAtWorldHour,
                HorizonEndWorldHour = state.HorizonEndWorldHour,
                AnchorRegion = (int)state.AnchorRegion,
                AuroraChanceProbability = WeatherOverhaulSettingsManager.AuroraChanceProbability,
                BloodMoonChanceProbability = WeatherOverhaulSettingsManager.BloodMoonChanceProbability,
                BloodMoonRequiresFullMoon = WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon,
                GlimmerFogChanceProbability = WeatherOverhaulSettingsManager.GlimmerFogChanceProbability,
                RegionalSeveritySignature = WeatherOverhaulSettingsManager.EventSettingsSignature
            };

            IReadOnlyList<NightEventWindow> nightEventWindows = state.NightEventSchedule.Windows;
            for (int i = 0; i < nightEventWindows.Count; i++)
            {
                NightEventWindow window = nightEventWindows[i];
                persisted.NightEventWindows.Add(new PersistedNightEventWindow
                {
                    EventType = (int)window.EventType,
                    StartWorldHour = window.StartWorldHour,
                    EndWorldHour = window.EndWorldHour,
                    StartClockHour = window.StartClockHour,
                    EndClockHour = window.EndClockHour
                });
            }

            IReadOnlyDictionary<WeatherRegionId, List<GlimmerFogWindow>> glimmerWindows = state.GlimmerFogSchedule.WindowsByRegion;
            foreach (KeyValuePair<WeatherRegionId, List<GlimmerFogWindow>> pair in glimmerWindows)
            {
                PersistedRegionGlimmerFogWindows persistedWindows = new()
                {
                    RegionId = (int)pair.Key
                };

                List<GlimmerFogWindow> windowsForRegion = pair.Value;
                for (int i = 0; i < windowsForRegion.Count; i++)
                {
                    GlimmerFogWindow window = windowsForRegion[i];
                    persistedWindows.Windows.Add(new PersistedGlimmerFogWindow
                    {
                        StartWorldHour = window.StartWorldHour,
                        EndWorldHour = window.EndWorldHour,
                        StartClockHour = window.StartClockHour,
                        EndClockHour = window.EndClockHour
                    });
                }

                persisted.GlimmerFogWindows.Add(persistedWindows);
            }

            foreach (KeyValuePair<WeatherRegionId, RegionWeatherTimeline> pair in state.Timelines)
            {
                WeatherRegionDefinition definition = RegionWeatherGraph.Get(pair.Key);
                if (definition.IsDynamic)
                {
                    persisted.DynamicRegions.Add(new PersistedDynamicRegion
                    {
                        RegionId = (int)definition.Id,
                        SceneName = definition.SceneName,
                        DisplayName = definition.DisplayName,
                        ShortName = definition.ShortName
                    });
                }
            }

            foreach (KeyValuePair<WeatherRegionId, RegionWeatherTimeline> pair in state.Timelines)
            {
                PersistedRegionTimeline timeline = new()
                {
                    RegionId = (int)pair.Key
                };

                List<RegionWeatherSegment> segments = pair.Value.Segments;
                for (int i = 0; i < segments.Count; i++)
                {
                    RegionWeatherSegment segment = segments[i];
                    timeline.Segments.Add(new PersistedRegionSegment
                    {
                        StartWorldHour = segment.StartWorldHour,
                        EndWorldHour = segment.EndWorldHour,
                        StageId = (int)segment.StageId,
                        Family = (int)segment.Family,
                        RepresentativeStage = (int)segment.RepresentativeStage,
                        IsExactObservedSourceStage = segment.IsExactObservedSourceStage
                    });
                }

                persisted.Timelines.Add(timeline);
            }

            return persisted;
        }

        private static GlobalWeatherState? ToRuntimeState(PersistedGlobalWeatherState persisted)
        {
            for (int i = 0; i < persisted.DynamicRegions.Count; i++)
            {
                PersistedDynamicRegion savedRegion = persisted.DynamicRegions[i];
                RegionWeatherGraph.RestoreDynamicRegion((WeatherRegionId)savedRegion.RegionId, savedRegion.SceneName ?? string.Empty, savedRegion.DisplayName ?? string.Empty, savedRegion.ShortName ?? string.Empty);
            }

            WeatherRegionId anchorRegion = (WeatherRegionId)persisted.AnchorRegion;
            if (anchorRegion == WeatherRegionId.Unknown) return null;

            List<NightEventWindow> nightEventWindows = [];
            for (int i = 0; i < persisted.NightEventWindows.Count; i++)
            {
                PersistedNightEventWindow saved = persisted.NightEventWindows[i];
                if (saved.EndWorldHour <= saved.StartWorldHour) continue;

                GlobalNightEventType eventType = (GlobalNightEventType)saved.EventType;
                if (!NightEventWindow.IsSupportedEvent(eventType)) continue;
                nightEventWindows.Add(new NightEventWindow(eventType, saved.StartWorldHour, saved.EndWorldHour, saved.StartClockHour, saved.EndClockHour));
            }

            GlobalNightEventSchedule nightEventSchedule = new(nightEventWindows);
            GlobalAuroraSchedule auroraSchedule = nightEventSchedule.ToAuroraSchedule();

            Dictionary<WeatherRegionId, List<GlimmerFogWindow>> glimmerWindowsByRegion = [];
            for (int i = 0; i < persisted.GlimmerFogWindows.Count; i++)
            {
                PersistedRegionGlimmerFogWindows savedRegion = persisted.GlimmerFogWindows[i];
                WeatherRegionId regionId = (WeatherRegionId)savedRegion.RegionId;
                if (!GlobalGlimmerFogSchedule.IsGlimmerFogRegion(regionId)) continue;

                List<GlimmerFogWindow> windowsForRegion = [];
                for (int w = 0; w < savedRegion.Windows.Count; w++)
                {
                    PersistedGlimmerFogWindow saved = savedRegion.Windows[w];
                    if (saved.EndWorldHour <= saved.StartWorldHour) continue;
                    windowsForRegion.Add(new GlimmerFogWindow(saved.StartWorldHour, saved.EndWorldHour, saved.StartClockHour, saved.EndClockHour));
                }

                if (windowsForRegion.Count > 0) glimmerWindowsByRegion[regionId] = windowsForRegion;
            }

            Dictionary<WeatherRegionId, RegionWeatherTimeline> timelines = [];
            for (int i = 0; i < persisted.Timelines.Count; i++)
            {
                PersistedRegionTimeline savedTimeline = persisted.Timelines[i];
                WeatherRegionId regionId = (WeatherRegionId)savedTimeline.RegionId;
                if (regionId == WeatherRegionId.Unknown) continue;
                if (RegionWeatherGraph.Get(regionId).Id == WeatherRegionId.Unknown)
                {
                    Core.Warn($"[GlobalSim][ModData] Ignoring orphaned regional timeline {savedTimeline.RegionId}; its region definition is not available yet.");
                    continue;
                }

                RegionWeatherTimeline timeline = new(regionId);
                float previousEnd = float.NaN;
                for (int s = 0; s < savedTimeline.Segments.Count; s++)
                {
                    PersistedRegionSegment savedSegment = savedTimeline.Segments[s];
                    if (!float.IsFinite(savedSegment.StartWorldHour) || !float.IsFinite(savedSegment.EndWorldHour) || savedSegment.EndWorldHour <= savedSegment.StartWorldHour) return null;
                    if (!float.IsNaN(previousEnd) && Math.Abs(savedSegment.StartWorldHour - previousEnd) > 0.01f) return null;

                    WeatherStageId stageId = (WeatherStageId)savedSegment.StageId;
                    if (!WeatherStageCatalog.IsKnown(stageId))
                    {
                        stageId = WeatherStageCatalog.Resolve((WeatherFamily)savedSegment.Family, (WeatherStage)savedSegment.RepresentativeStage);
                    }
                    if (!WeatherStageCatalog.IsKnown(stageId)) return null;

                    timeline.Segments.Add(new RegionWeatherSegment(
                        savedSegment.StartWorldHour,
                        savedSegment.EndWorldHour,
                        stageId,
                        "persisted carried forecast",
                        savedSegment.IsExactObservedSourceStage));
                    previousEnd = savedSegment.EndWorldHour;
                }

                if (timeline.Segments.Count > 0) timelines[regionId] = timeline;
            }

            if (timelines.Count == 0) return null;

            float actualHorizonEnd = float.PositiveInfinity;
            foreach (RegionWeatherTimeline timeline in timelines.Values)
            {
                if (timeline.Segments.Count == 0) continue;
                actualHorizonEnd = Math.Min(actualHorizonEnd, timeline.Segments[^1].EndWorldHour);
            }
            if (!float.IsFinite(actualHorizonEnd)) return null;

            return new GlobalWeatherState(true, persisted.GeneratedAtWorldHour, actualHorizonEnd, anchorRegion, timelines, nightEventSchedule, auroraSchedule, new GlobalGlimmerFogSchedule(glimmerWindowsByRegion), "Persisted carried global weather forecast.");
        }
    }

    internal sealed class PersistedGlobalWeatherState
    {
        [JsonPropertyName("v")]
        public int Version { get; set; }
        [JsonPropertyName("g")]
        public float GeneratedAtWorldHour { get; set; }
        [JsonPropertyName("h")]
        public float HorizonEndWorldHour { get; set; }
        [JsonPropertyName("sr")]
        public int AnchorRegion { get; set; }
        [JsonPropertyName("ac")]
        public float AuroraChanceProbability { get; set; }
        [JsonPropertyName("bc")]
        public float BloodMoonChanceProbability { get; set; }
        [JsonPropertyName("bf")]
        public bool BloodMoonRequiresFullMoon { get; set; }
        [JsonPropertyName("gc")]
        public float GlimmerFogChanceProbability { get; set; }
        [JsonPropertyName("sig")]
        public string? RegionalSeveritySignature { get; set; }
        [JsonPropertyName("ne")]
        public List<PersistedNightEventWindow> NightEventWindows { get; set; } = [];
        [JsonPropertyName("gf")]
        public List<PersistedRegionGlimmerFogWindows> GlimmerFogWindows { get; set; } = [];
        [JsonPropertyName("dr")]
        public List<PersistedDynamicRegion> DynamicRegions { get; set; } = [];
        [JsonPropertyName("tl")]
        public List<PersistedRegionTimeline> Timelines { get; set; } = [];
    }

    internal sealed class PersistedDynamicRegion
    {
        [JsonPropertyName("id")]
        public int RegionId { get; set; }
        [JsonPropertyName("sc")]
        public string? SceneName { get; set; }
        [JsonPropertyName("dn")]
        public string? DisplayName { get; set; }
        [JsonPropertyName("sn")]
        public string? ShortName { get; set; }
    }

    internal sealed class PersistedNightEventWindow
    {
        [JsonPropertyName("t")]
        public int EventType { get; set; }
        [JsonPropertyName("s")]
        public float StartWorldHour { get; set; }
        [JsonPropertyName("e")]
        public float EndWorldHour { get; set; }
        [JsonPropertyName("sc")]
        public float StartClockHour { get; set; }
        [JsonPropertyName("ec")]
        public float EndClockHour { get; set; }
    }

    internal sealed class PersistedRegionGlimmerFogWindows
    {
        [JsonPropertyName("r")]
        public int RegionId { get; set; }
        [JsonPropertyName("w")]
        public List<PersistedGlimmerFogWindow> Windows { get; set; } = [];
    }

    internal sealed class PersistedGlimmerFogWindow
    {
        [JsonPropertyName("s")]
        public float StartWorldHour { get; set; }
        [JsonPropertyName("e")]
        public float EndWorldHour { get; set; }
        [JsonPropertyName("sc")]
        public float StartClockHour { get; set; }
        [JsonPropertyName("ec")]
        public float EndClockHour { get; set; }
    }

    internal sealed class PersistedRegionTimeline
    {
        [JsonPropertyName("r")]
        public int RegionId { get; set; }
        [JsonPropertyName("s")]
        public List<PersistedRegionSegment> Segments { get; set; } = [];
    }

    internal sealed class PersistedRegionSegment
    {
        [JsonPropertyName("s")]
        public float StartWorldHour { get; set; }
        [JsonPropertyName("e")]
        public float EndWorldHour { get; set; }
        [JsonPropertyName("id")]
        public int StageId { get; set; }
        [JsonPropertyName("f")]
        public int Family { get; set; }
        [JsonPropertyName("rs")]
        public int RepresentativeStage { get; set; }
        [JsonPropertyName("x")]
        public bool IsExactObservedSourceStage { get; set; }
    }
}