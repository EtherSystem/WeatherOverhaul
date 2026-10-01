using System.Text;
using WeatherOverhaul.Utilities;

namespace WeatherOverhaul.Weather
{
    internal static class GlobalWeatherSimulation
    {
        internal const float HorizonHours = 336f;
        private const float InternalHorizonHours = 336f;
        private const float NightEventPreparationLeadHours = 1.0f;
        private const float NightEventPreparationTailHours = 0.75f;
        private const float MinimumControlledStageDurationHours = 0.05f;
        private const float ClockShiftBehindForecastRebuildThresholdHours = 2.0f;
        private const int FirstNaturalNightEventDayNumber = 1;
        private const float NaturalNightStartClockHour = 20.0f;
        private const float NaturalMoonVisibleClockHour = 19.0f;
        private const float NaturalNightEndClockHour = 6.5f;
        private static DeterministicRng s_Rng = new(0x6602A79B);
        private static int s_GenerationSerial;
        private static float s_LastSnapshotLogWorldHour = -1000f;
        private static float s_LastIgnoredObservedAuroraWorldHour = -1000f;
        private static float s_LastAppendLogWorldHour = -1000f;
        private static WeatherRegionId s_LastLoggedRegion = WeatherRegionId.Unknown;

        internal static GlobalWeatherState Current { get; private set; } = GlobalWeatherState.Empty;
        internal static bool IsInitialized { get; private set; }

        internal static void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            Core.Log($"[GlobalSim] Initialized global forecast simulation with {RegionWeatherGraph.Regions.Count} weather region(s).");
        }

        internal static void Update(WeatherSnapshot snapshot)
        {
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul || !WeatherOverhaulSettingsManager.EnableGlobalSimulation) return;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;

            GlobalWeatherPersistence.TryLoad(snapshot);
            if (!EnsureUsableSimulation(snapshot, "forecast update")) return;

            LogPeriodicSnapshot(snapshot);
        }

        internal static void Rebuild(WeatherSnapshot snapshot, string reason)
        {
            if (!snapshot.IsValid) return;
            if (snapshot.RegionId == WeatherRegionId.Unknown)
            {
                Core.Warn($"[GlobalSim] Rebuild skipped ({reason}): scene {snapshot.SceneName} is not mapped to a weather region.");
                return;
            }

            WeatherRegionId anchorRegion = snapshot.RegionId;
            int seed = BuildSeed(snapshot, anchorRegion, ++s_GenerationSerial, reason);
            s_Rng = new DeterministicRng(seed);

            Dictionary<WeatherRegionId, RegionWeatherTimeline> timelines = BuildIndependentRegionalTimelines(snapshot, seed);
            float horizonEnd = GetTimelineEnd(timelines);
            s_Rng = new DeterministicRng(seed ^ 0x4E1A2B39);
            GlobalNightEventSchedule nightEventSchedule = BuildNightEventSchedule(snapshot);
            GlobalAuroraSchedule auroraSchedule = nightEventSchedule.ToAuroraSchedule();
            GlobalGlimmerFogSchedule glimmerFogSchedule = BuildGlimmerFogSchedule(snapshot);
            string summary = BuildSummary(snapshot, timelines, nightEventSchedule, glimmerFogSchedule);

            Current = new GlobalWeatherState(true, snapshot.WorldHour, horizonEnd, anchorRegion, timelines, nightEventSchedule, auroraSchedule, glimmerFogSchedule, summary);
            Core.Log($"[GlobalSim] Forecast rebuilt ({reason}) for {RegionWeatherGraph.Get(anchorRegion).DisplayName}; horizon={Math.Max(0f, horizonEnd - snapshot.WorldHour):0.#}h.", false);
            Core.Log($"[GlobalSim] Rebuilt forecast ({reason}) | {snapshot.GetClockText()} | Mode=independent regional timelines | Anchor={RegionWeatherGraph.Get(anchorRegion).ShortName}/{RegionWeatherGraph.Get(anchorRegion).DisplayName} | Stage={snapshot.StageLabel} | Seed={seed} | AuroraChance={WeatherOverhaulSettingsManager.AuroraChancePercent:0.#}% | BloodMoonChance={WeatherOverhaulSettingsManager.BloodMoonChancePercent:0.#}% | BloodMoonFullMoonOnly={(WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon ? "on" : "off")} | GlimmerChance={WeatherOverhaulSettingsManager.GlimmerFogChancePercent:0.#}% | NightEvent={nightEventSchedule.BuildSummary(snapshot.WorldHour)} | Glimmer={glimmerFogSchedule.BuildSummary(snapshot.WorldHour)}");
            Core.Log("[GlobalSim] " + summary);
            GlobalWeatherPersistence.Save(Current, snapshot, reason);
            LogCurrentRegionForecast(snapshot, true);
        }

        internal static void Restore(GlobalWeatherState state, WeatherSnapshot snapshot, string reason)
        {
            if (state == null || !state.IsValid) return;

            Current = state;
            NormalizePersistedNightWhiteouts(snapshot);
            s_LastLoggedRegion = WeatherRegionId.Unknown;
            s_LastSnapshotLogWorldHour = -1000f;
            WeatherRegionDefinition anchor = RegionWeatherGraph.Get(Current.AnchorRegion);
            Core.Log($"[GlobalSim] Forecast restored from save for {anchor.DisplayName}; horizon={Math.Max(0f, Current.HorizonEndWorldHour - snapshot.WorldHour):0.#}h.", false);
            Core.Log($"[GlobalSim] Restored forecast from ModData ({reason}) | GeneratedAt={Current.GeneratedAtWorldHour:0.##}h | HorizonEnd={Current.HorizonEndWorldHour:0.##}h | Mode=independent regional timelines | Anchor={anchor.ShortName}/{anchor.DisplayName} | NightEvent={Current.NightEventSchedule.BuildSummary(snapshot.WorldHour)} | Glimmer={Current.GlimmerFogSchedule.BuildSummary(snapshot.WorldHour)}");
            Core.Log("[GlobalSim] " + Current.Summary);
            LogCurrentRegionForecast(snapshot, true);
        }

        internal static void Clear(string reason)
        {
            if (!Current.IsValid) return;

            Current = GlobalWeatherState.Empty;
            s_LastLoggedRegion = WeatherRegionId.Unknown;
            s_LastSnapshotLogWorldHour = -1000f;
            Core.Log("[GlobalSim] Cleared carried forecast (" + reason + ").");
        }

        internal static bool IsSnapshotOlderThanCurrentForecast(WeatherSnapshot snapshot, float toleranceHours)
        {
            if (!Current.IsValid || !snapshot.IsValid) return false;
            return snapshot.WorldHour + toleranceHours < Current.GeneratedAtWorldHour;
        }

        internal static bool EnsureUsableSimulation(WeatherSnapshot snapshot, string reason)
        {
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul || !WeatherOverhaulSettingsManager.EnableGlobalSimulation) return false;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;
            if (GlobalWeatherPersistence.IsLoadDeferredForStableClock) return false;

            if (Current.IsValid && snapshot.WorldHour + ClockShiftBehindForecastRebuildThresholdHours < Current.GeneratedAtWorldHour)
            {
                if (LooksLikeLoadingDefaultClockAgainstCurrentForecast(snapshot))
                {
                    Core.Log($"[GlobalSim] Waiting for stable save-slot clock before using or rebuilding forecast. | Reason={reason} | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | ForecastGeneratedAt={Current.GeneratedAtWorldHour:0.##}h");
                    return false;
                }

                if (IsSceneLoadOrAuthorityReason(reason))
                {
                    Core.Log($"[GlobalSim] Refusing to rebuild from stale scene-load/weather-authority snapshot. | Reason={reason} | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | ForecastGeneratedAt={Current.GeneratedAtWorldHour:0.##}h");
                    return false;
                }

                Core.Warn($"[GlobalSim] Snapshot clock moved behind the carried forecast. Rebuilding from the stabilized scene clock. | Reason={reason} | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | ForecastGeneratedAt={Current.GeneratedAtWorldHour:0.##}h");
                Rebuild(snapshot, "snapshot clock stabilized behind carried forecast during " + reason);
                return Current.IsValid && snapshot.WorldHour + ClockShiftBehindForecastRebuildThresholdHours >= Current.GeneratedAtWorldHour;
            }

            if (IsSnapshotOlderThanCurrentForecast(snapshot, 12f))
            {
                if (IsSceneLoadOrAuthorityReason(reason))
                {
                    Core.Log($"[GlobalSim] Refusing to rebuild from stale scene-load/weather-authority snapshot. | Reason={reason} | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | ForecastGeneratedAt={Current.GeneratedAtWorldHour:0.##}h");
                    return false;
                }

                Core.Warn($"[GlobalSim] Current snapshot is behind the carried forecast. Rebuilding from the loaded scene instead of waiting forever. | Reason={reason} | Snapshot={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | ForecastGeneratedAt={Current.GeneratedAtWorldHour:0.##}h");
                Rebuild(snapshot, "snapshot clock moved behind carried forecast during " + reason);
                return Current.IsValid && !IsSnapshotOlderThanCurrentForecast(snapshot, 12f);
            }

            EnsureSimulation(snapshot);
            SynchronizeRuntimeFullMoonBloodMoon(snapshot);
            return Current.IsValid && !IsSnapshotOlderThanCurrentForecast(snapshot, 12f);
        }

        private static bool LooksLikeLoadingDefaultClockAgainstCurrentForecast(WeatherSnapshot snapshot)
        {
            if (!snapshot.IsValid || !Current.IsValid) return false;
            if (snapshot.DayNumber > 1) return false;
            if (snapshot.WorldHour >= 48f) return false;
            return Current.GeneratedAtWorldHour > 48f;
        }

        private static bool IsSceneLoadOrAuthorityReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return false;
            return reason.Contains("authority", StringComparison.OrdinalIgnoreCase) ||
                   reason.Contains("scene load", StringComparison.OrdinalIgnoreCase) ||
                   reason.Contains("mapped region", StringComparison.OrdinalIgnoreCase);
        }

        internal static void EnsureSimulation(WeatherSnapshot snapshot)
        {
            if (!Current.IsValid)
            {
                Rebuild(snapshot, "no active global simulation");
                return;
            }

            bool stateChanged = EnsureCurrentRegionTimeline(snapshot);
            if (EnsureObservedSpecialOverlays(snapshot)) stateChanged = true;
            int normalizedSegments = NormalizeCurrentTimelines(snapshot.WorldHour);
            if (normalizedSegments > 0) stateChanged = true;

            int consumedSegments = PruneConsumedSegments(snapshot.WorldHour);
            if (consumedSegments > 0) stateChanged = true;

            if (GetTimelineEnd(Current.Timelines) < snapshot.WorldHour + HorizonHours)
            {
                AppendRollingUntil(snapshot, snapshot.WorldHour + InternalHorizonHours, "forecast horizon catch-up");
                stateChanged = true;
            }

            if (stateChanged) GlobalWeatherPersistence.Save(Current, snapshot, "rolling forecast maintenance");
        }

        private static bool EnsureCurrentRegionTimeline(WeatherSnapshot snapshot)
        {
            if (!Current.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;
            if (Current.Timelines.ContainsKey(snapshot.RegionId)) return false;
            return TryAddRegionTimeline(snapshot, snapshot.RegionId, "current unmapped region");
        }

        internal static bool EnsureRegionTimeline(WeatherSnapshot snapshot, WeatherRegionId regionId, string reason)
        {
            if (!Current.IsValid || !snapshot.IsValid || regionId == WeatherRegionId.Unknown) return false;
            if (Current.Timelines.ContainsKey(regionId)) return true;
            return TryAddRegionTimeline(snapshot, regionId, reason);
        }

        private static bool TryAddRegionTimeline(WeatherSnapshot snapshot, WeatherRegionId regionId, string reason)
        {
            int seed = BuildRegionSeed(BuildSeed(snapshot, regionId, s_GenerationSerial, "runtime region timeline"), regionId, "runtime region timeline");
            RegionWeatherTimeline timeline = BuildIndependentRegionTimeline(snapshot, regionId, seed, Math.Max(Current.HorizonEndWorldHour, snapshot.WorldHour + InternalHorizonHours + 6f));
            if (timeline.Segments.Count == 0) return false;

            Current.Timelines[regionId] = timeline;
            float horizonEnd = GetTimelineEnd(Current.Timelines);
            Current = new GlobalWeatherState(true, Current.GeneratedAtWorldHour, horizonEnd, Current.AnchorRegion, Current.Timelines, Current.NightEventSchedule, Current.AuroraSchedule, Current.GlimmerFogSchedule, BuildSummaryFromCurrentTimelines(Current.NightEventSchedule, Current.GlimmerFogSchedule, snapshot.WorldHour));

            WeatherRegionDefinition region = RegionWeatherGraph.Get(regionId);
            Core.Log($"[GlobalSim] Added runtime forecast timeline for {region.ShortName}/{region.DisplayName} using the Default weather profile. | Reason={reason}");
            GlobalWeatherPersistence.Save(Current, snapshot, "runtime unmapped region timeline");
            return true;
        }

        private static bool EnsureObservedSpecialOverlays(WeatherSnapshot snapshot)
        {
            if (!Current.IsValid || snapshot.Stage != WeatherStage.ClearAurora) return false;
            if (IsAuthoritativeAuroraActive(snapshot.WorldHour)) return false;

            if (snapshot.WorldHour - s_LastIgnoredObservedAuroraWorldHour >= 0.25f)
            {
                s_LastIgnoredObservedAuroraWorldHour = snapshot.WorldHour;
                Core.Log($"[GlobalSim] Ignored observed vanilla Clear Aurora because WeatherOverhaul owns the active night-event schedule and no aurora event is planned now. | {snapshot.GetClockText()} | PlannedNightEvent={Current.NightEventSchedule.BuildSummary(snapshot.WorldHour)}");
            }

            return false;
        }

        private static int NormalizeCurrentTimelines(float worldHour)
        {
            if (!Current.IsValid) return 0;

            int merged = 0;
            foreach (KeyValuePair<WeatherRegionId, RegionWeatherTimeline> pair in Current.Timelines)
            {
                merged += MergeAdjacentSegments(pair.Value);
            }

            if (merged <= 0) return 0;

            Current = new GlobalWeatherState(
                true,
                Current.GeneratedAtWorldHour,
                GetTimelineEnd(Current.Timelines),
                Current.AnchorRegion,
                Current.Timelines,
                Current.NightEventSchedule,
                Current.AuroraSchedule,
                Current.GlimmerFogSchedule,
                BuildSummaryFromCurrentTimelines(Current.NightEventSchedule, Current.GlimmerFogSchedule, worldHour));

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[GlobalSim] Merged {merged} adjacent identical forecast segment(s) to keep custom stages continuous.");
            return merged;
        }

        private static int PruneConsumedSegments(float worldHour)
        {
            if (!Current.IsValid) return 0;
            int totalRemoved = 0;

            foreach (KeyValuePair<WeatherRegionId, RegionWeatherTimeline> pair in Current.Timelines)
            {
                List<RegionWeatherSegment> segments = pair.Value.Segments;
                while (segments.Count > 1 && worldHour >= segments[0].EndWorldHour - 0.01f)
                {
                    segments.RemoveAt(0);
                    totalRemoved++;
                }
            }

            return totalRemoved;
        }

        private static void AppendRollingUntil(WeatherSnapshot snapshot, float targetHorizonWorldHour, string reason)
        {
            int safety = 0;
            while (Current.IsValid && Current.HorizonEndWorldHour < targetHorizonWorldHour && safety < 128)
            {
                float previousHorizonEnd = Current.HorizonEndWorldHour;
                AppendRegionalStagesTowardTarget(snapshot, targetHorizonWorldHour, reason, safety);
                float newHorizonEnd = GetTimelineEnd(Current.Timelines);
                if (newHorizonEnd <= previousHorizonEnd + 0.001f) break;
                RefreshOverlaySchedulesAfterAppend(snapshot, previousHorizonEnd, newHorizonEnd, reason);
                safety++;
            }

            if (safety >= 128) Core.Warn($"[GlobalSim] Rolling forecast append stopped by safety limit. | Reason={reason} | HorizonEnd={Current.HorizonEndWorldHour:0.##}h | Target={targetHorizonWorldHour:0.##}h");
        }

        private static void AppendRegionalStagesTowardTarget(WeatherSnapshot snapshot, float targetHorizonWorldHour, string reason, int index)
        {
            for (int r = 0; r < RegionWeatherGraph.Regions.Count; r++)
            {
                WeatherRegionDefinition definition = RegionWeatherGraph.Regions[r];
                if (!Current.Timelines.TryGetValue(definition.Id, out RegionWeatherTimeline timeline))
                {
                    timeline = new RegionWeatherTimeline(definition.Id);
                    Current.Timelines[definition.Id] = timeline;
                }

                if (GetTimelineEnd(timeline) < targetHorizonWorldHour - 0.01f) AppendSingleRegionalStage(timeline, definition.Id, snapshot, reason, index);
            }
        }

        private static void RefreshOverlaySchedulesAfterAppend(WeatherSnapshot snapshot, float previousHorizonEnd, float newHorizonEnd, string reason)
        {
            List<NightEventWindow> nightEventWindows = CopyFutureNightEventWindows(Current.NightEventSchedule, snapshot.WorldHour - 0.1f);
            AppendNightEventWindowsForRange(nightEventWindows, snapshot, previousHorizonEnd, newHorizonEnd + 12f);

            Dictionary<WeatherRegionId, List<GlimmerFogWindow>> glimmerWindows = CopyFutureGlimmerWindows(Current.GlimmerFogSchedule, snapshot.WorldHour - 0.1f);
            AppendGlimmerFogWindowsForRange(glimmerWindows, snapshot, previousHorizonEnd, newHorizonEnd + 12f);

            GlobalNightEventSchedule nightEventSchedule = new(nightEventWindows);
            GlobalAuroraSchedule auroraSchedule = nightEventSchedule.ToAuroraSchedule();
            GlobalGlimmerFogSchedule glimmerFogSchedule = new(glimmerWindows);
            string summary = BuildSummaryFromCurrentTimelines(nightEventSchedule, glimmerFogSchedule, snapshot.WorldHour);
            Current = new GlobalWeatherState(true, Current.GeneratedAtWorldHour, newHorizonEnd, Current.AnchorRegion, Current.Timelines, nightEventSchedule, auroraSchedule, glimmerFogSchedule, summary);

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation && ShouldLogAppendMaintenance(snapshot, reason, previousHorizonEnd, newHorizonEnd)) Core.Log($"[GlobalSim] Extended only regional timelines below the target horizon ({reason}) | HorizonEnd {previousHorizonEnd:0.##}h -> {newHorizonEnd:0.##}h | NightEvent={nightEventSchedule.BuildSummary(snapshot.WorldHour)} | Glimmer={glimmerFogSchedule.BuildSummary(snapshot.WorldHour)}");
        }


        private static bool ShouldLogAppendMaintenance(WeatherSnapshot snapshot, string reason, float previousHorizonEnd, float newHorizonEnd)
        {
            string safeReason = reason ?? string.Empty;
            if (safeReason.Contains("manual", StringComparison.OrdinalIgnoreCase)) return true;
            if (safeReason.Contains("settings", StringComparison.OrdinalIgnoreCase)) return true;
            if (newHorizonEnd - previousHorizonEnd >= 12f) return true;
            if (snapshot.WorldHour - s_LastAppendLogWorldHour < 6f) return false;

            s_LastAppendLogWorldHour = snapshot.WorldHour;
            return true;
        }

        private static void AppendSingleRegionalStage(RegionWeatherTimeline timeline, WeatherRegionId regionId, WeatherSnapshot snapshot, string reason, int index)
        {
            if (timeline.Segments.Count == 0)
            {
                float start = Current.IsValid ? Current.HorizonEndWorldHour : 0f;
                WeatherFamily first = RegionalWeatherModel.PickInitialWeightedStage(regionId, s_Rng);
                float firstDuration = DurationFor(first, regionId);
                WeatherFamily restrictedFirst = RestrictWhiteoutToDaylight(first, snapshot, start, firstDuration);
                if (restrictedFirst != first)
                {
                    first = restrictedFirst;
                    firstDuration = DurationFor(first, regionId);
                }

                AddOrMergeSegment(timeline, new RegionWeatherSegment(start, start + firstDuration, WeatherStageCatalog.ForFamily(first), "independent regional fallback seed", false));
                return;
            }

            RegionWeatherSegment previous = timeline.Segments[^1];
            float startWorldHour = previous.EndWorldHour;
            int seed = BuildRollingSeed(regionId, startWorldHour, previous.Family, index, reason);
            s_Rng = new DeterministicRng(seed);

            WeatherFamily next = RegionalWeatherModel.PickIndependentWeightedStage(timeline, regionId, s_Rng);
            float duration = DurationFor(next, regionId);
            WeatherFamily restrictedNext = RestrictWhiteoutToDaylight(next, snapshot, startWorldHour, duration);
            if (restrictedNext != next)
            {
                next = restrictedNext;
                duration = DurationFor(next, regionId);
            }

            float endWorldHour = startWorldHour + duration;
            AddOrMergeSegment(timeline, new RegionWeatherSegment(startWorldHour, endWorldHour, WeatherStageCatalog.ForFamily(next), ReasonFor(previous.Family, next) + " | independent regional table", false));
        }

        private static void AddOrMergeSegment(RegionWeatherTimeline timeline, RegionWeatherSegment segment)
        {
            if (timeline.Segments.Count == 0)
            {
                timeline.Segments.Add(segment);
                return;
            }

            RegionWeatherSegment previous = timeline.Segments[^1];
            if (!CanMergeSegments(previous, segment))
            {
                timeline.Segments.Add(segment);
                return;
            }

            timeline.Segments[^1] = MergeSegments(previous, segment);
        }

        private static int MergeAdjacentSegments(RegionWeatherTimeline timeline)
        {
            if (timeline.Segments.Count < 2) return 0;

            int merged = 0;
            for (int i = 1; i < timeline.Segments.Count; i++)
            {
                RegionWeatherSegment previous = timeline.Segments[i - 1];
                RegionWeatherSegment current = timeline.Segments[i];
                if (!CanMergeSegments(previous, current)) continue;

                timeline.Segments[i - 1] = MergeSegments(previous, current);
                timeline.Segments.RemoveAt(i);
                i--;
                merged++;
            }

            return merged;
        }

        private static bool CanMergeSegments(RegionWeatherSegment previous, RegionWeatherSegment next)
        {
            if (WeatherFamilyClassifier.IsSevereCooldownFamily(previous.Family) || WeatherFamilyClassifier.IsSevereCooldownFamily(next.Family)) return false;
            if (previous.StageId != next.StageId) return false;
            if (Math.Abs(previous.EndWorldHour - next.StartWorldHour) > 0.05f) return false;
            return true;
        }

        private static RegionWeatherSegment MergeSegments(RegionWeatherSegment previous, RegionWeatherSegment next)
        {
            string reason = previous.Family == next.Family ? previous.Reason + " | continued identical stage" : previous.Reason;
            return new RegionWeatherSegment(
                previous.StartWorldHour,
                Math.Max(previous.EndWorldHour, next.EndWorldHour),
                previous.StageId,
                reason,
                previous.IsExactObservedSourceStage && next.IsExactObservedSourceStage);
        }

        private static float GetTimelineEnd(RegionWeatherTimeline timeline)
        {
            if (timeline == null || timeline.Segments.Count == 0) return float.MinValue;
            return timeline.Segments[^1].EndWorldHour;
        }

        private static float GetTimelineEnd(Dictionary<WeatherRegionId, RegionWeatherTimeline> timelines)
        {
            float result = float.MaxValue;
            foreach (KeyValuePair<WeatherRegionId, RegionWeatherTimeline> pair in timelines)
            {
                List<RegionWeatherSegment> segments = pair.Value.Segments;
                if (segments.Count == 0) continue;
                float end = segments[^1].EndWorldHour;
                if (end < result) result = end;
            }

            return result == float.MaxValue ? Current.HorizonEndWorldHour : result;
        }

        internal static bool TryGetActivationPlan(WeatherRegionId regionId, float worldHour, out WeatherActivationPlan plan)
        {
            plan = default;
            if (!Current.IsValid) return false;
            if (!Current.TryGetSegment(regionId, worldHour, out RegionWeatherSegment segment)) return false;

            WeatherRegionDefinition region = RegionWeatherGraph.Get(regionId);
            WeatherStageId stageId = RegionalWeatherModel.ResolveDisplayedStageId(segment, region, Current.AuroraSchedule, Current.GlimmerFogSchedule, worldHour);
            float startWorldHour = segment.StartWorldHour;
            float endWorldHour = segment.EndWorldHour;
            string reason = segment.Reason;

            if (TryGetRuntimeValidActiveNightEvent(worldHour, out NightEventWindow nightEventWindow))
            {
                stageId = nightEventWindow.GetActiveStageId();
                startWorldHour = nightEventWindow.StartWorldHour;
                endWorldHour = nightEventWindow.EndWorldHour;
                reason = "global " + NightEventWindow.ToDisplayName(nightEventWindow.EventType) + " night event";
            }
            else if (TryGetActiveGlimmerFogWindow(regionId, worldHour, out GlimmerFogWindow glimmerFogWindow))
            {
                stageId = WeatherStageId.GlimmerFog;
                startWorldHour = glimmerFogWindow.StartWorldHour;
                endWorldHour = glimmerFogWindow.EndWorldHour;
                reason = "regional Glimmer Fog window";
            }
            else if (Current.NightEventSchedule.TryGetPreparationWindow(worldHour, NightEventPreparationLeadHours, NightEventPreparationTailHours, out NightEventWindow preparationWindow, out float preparationStartWorldHour, out float preparationEndWorldHour))
            {
                stageId = preparationWindow.GetPreparationStageId();
                startWorldHour = preparationStartWorldHour;
                endWorldHour = preparationEndWorldHour;
                reason = NightEventWindow.ToDisplayName(preparationWindow.EventType) + " preparation/tail";
            }
            else
            {
                float nextOverrideBoundaryWorldHour = FindNextOverrideBoundary(regionId, worldHour, endWorldHour);
                if (nextOverrideBoundaryWorldHour > worldHour + 0.01f && nextOverrideBoundaryWorldHour < endWorldHour) endWorldHour = nextOverrideBoundaryWorldHour;
            }

            if (endWorldHour <= worldHour + 0.01f) endWorldHour = worldHour + MinimumControlledStageDurationHours;
            if (startWorldHour > worldHour) startWorldHour = worldHour;

            float durationHours = Math.Max(MinimumControlledStageDurationHours, endWorldHour - startWorldHour);
            float elapsedHours = Math.Max(0f, worldHour - startWorldHour);
            elapsedHours = Math.Min(elapsedHours, Math.Max(0f, durationHours - 0.01f));
            float remainingHours = Math.Max(MinimumControlledStageDurationHours, durationHours - elapsedHours);

            plan = new WeatherActivationPlan(stageId, startWorldHour, endWorldHour, durationHours, elapsedHours, remainingHours, reason);
            return true;
        }


        private static bool TryGetActiveGlimmerFogWindow(WeatherRegionId regionId, float worldHour, out GlimmerFogWindow activeWindow)
        {
            if (!GlobalGlimmerFogSchedule.IsGlimmerFogRegion(regionId))
            {
                activeWindow = default;
                return false;
            }

            if (!Current.GlimmerFogSchedule.WindowsByRegion.TryGetValue(regionId, out List<GlimmerFogWindow> windows))
            {
                activeWindow = default;
                return false;
            }

            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i].Contains(worldHour))
                {
                    activeWindow = windows[i];
                    return true;
                }
            }

            activeWindow = default;
            return false;
        }

        private static float FindNextOverrideBoundary(WeatherRegionId regionId, float worldHour, float fallbackWorldHour)
        {
            float boundary = fallbackWorldHour;
            IReadOnlyList<NightEventWindow> nightEventWindows = Current.NightEventSchedule.Windows;
            for (int i = 0; i < nightEventWindows.Count; i++)
            {
                NightEventWindow window = nightEventWindows[i];
                if (window.UsesPreparationTail) ConsiderBoundary(window.StartWorldHour - NightEventPreparationLeadHours, worldHour, ref boundary);
                ConsiderBoundary(window.StartWorldHour, worldHour, ref boundary);
                ConsiderBoundary(window.EndWorldHour, worldHour, ref boundary);
                if (window.UsesPreparationTail) ConsiderBoundary(window.EndWorldHour + NightEventPreparationTailHours, worldHour, ref boundary);
            }

            if (GlobalGlimmerFogSchedule.IsGlimmerFogRegion(regionId) && Current.GlimmerFogSchedule.WindowsByRegion.TryGetValue(regionId, out List<GlimmerFogWindow> glimmerWindows))
            {
                for (int i = 0; i < glimmerWindows.Count; i++)
                {
                    GlimmerFogWindow window = glimmerWindows[i];
                    ConsiderBoundary(window.StartWorldHour, worldHour, ref boundary);
                    ConsiderBoundary(window.EndWorldHour, worldHour, ref boundary);
                }
            }

            return boundary;
        }

        private static void ConsiderBoundary(float candidateWorldHour, float worldHour, ref float boundary)
        {
            if (candidateWorldHour <= worldHour + 0.01f) return;
            if (candidateWorldHour < boundary) boundary = candidateWorldHour;
        }

        internal static List<WeatherActivationPlan> BuildActivationPlanSequence(WeatherRegionId regionId, float worldHour, int maxPlans)
        {
            List<WeatherActivationPlan> plans = [];
            if (!Current.IsValid || regionId == WeatherRegionId.Unknown || maxPlans <= 0) return plans;

            float cursor = worldHour;
            while (plans.Count < maxPlans && cursor < Current.HorizonEndWorldHour - 0.01f)
            {
                if (!TryGetActivationPlan(regionId, cursor, out WeatherActivationPlan plan)) break;
                plan = ClipPlanStartToCursor(plan, cursor);
                plans.Add(plan);

                float nextCursor = plan.EndWorldHour;
                if (nextCursor <= cursor + 0.01f) nextCursor = cursor + MinimumControlledStageDurationHours;
                cursor = nextCursor;
            }

            return plans;
        }

        private static WeatherActivationPlan ClipPlanStartToCursor(WeatherActivationPlan plan, float cursorWorldHour)
        {
            if (plan.StartWorldHour >= cursorWorldHour - 0.01f) return plan;

            float startWorldHour = cursorWorldHour;
            float endWorldHour = plan.EndWorldHour;
            if (endWorldHour <= startWorldHour + 0.01f) endWorldHour = startWorldHour + MinimumControlledStageDurationHours;

            float durationHours = Math.Max(MinimumControlledStageDurationHours, endWorldHour - startWorldHour);
            return new WeatherActivationPlan(plan.StageId, startWorldHour, endWorldHour, durationHours, 0f, durationHours, plan.Reason);
        }

        internal static string BuildRegionForecastLine(WeatherRegionId regionId, float worldHour, WeatherSnapshot snapshot)
        {
            WeatherRegionDefinition definition = RegionWeatherGraph.Get(regionId);
            List<WeatherActivationPlan> plans = BuildActivationPlanSequence(regionId, worldHour, 4);
            string first = plans.Count > 0 ? FormatPlanForLog(plans[0], snapshot, true) : "unavailable";
            string second = plans.Count > 1 ? FormatPlanForLog(plans[1], snapshot, false) : "-";
            string third = plans.Count > 2 ? FormatPlanForLog(plans[2], snapshot, false) : "-";
            string fourth = plans.Count > 3 ? FormatPlanForLog(plans[3], snapshot, false) : "-";
            return $"{definition.ShortName,-6} {first,-36} | {second,-33} | {third,-33} | {fourth,-33}";
        }

        internal static void LogCurrentRegionForecast(WeatherSnapshot snapshot, bool force)
        {
            if (!WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) return;
            if (!Current.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;
            if (!force && s_LastLoggedRegion == snapshot.RegionId && snapshot.WorldHour - s_LastSnapshotLogWorldHour < 6f) return;

            s_LastLoggedRegion = snapshot.RegionId;
            s_LastSnapshotLogWorldHour = snapshot.WorldHour;
            WeatherRegionDefinition current = RegionWeatherGraph.Get(snapshot.RegionId);
            Core.Log($"[GlobalSim] {snapshot.GetClockText()} | Scene={snapshot.SceneName} -> {current.ShortName}/{current.DisplayName} | Mode=independent regional timelines | NightEvent={Current.NightEventSchedule.BuildSummary(snapshot.WorldHour)} | Glimmer={Current.GlimmerFogSchedule.BuildSummary(snapshot.WorldHour)}");
            Core.Log("[GlobalSim] Region forecast timeline | Region Current planned stage                 | Next planned stage                | Later planned stage               | Later planned stage");
            Core.Log("[GlobalSim] " + BuildRegionForecastLine(snapshot.RegionId, snapshot.WorldHour, snapshot));
            LogNeighborForecasts(snapshot);
        }

        private static void LogPeriodicSnapshot(WeatherSnapshot snapshot)
        {
            if (snapshot.RegionId != s_LastLoggedRegion)
            {
                LogCurrentRegionForecast(snapshot, true);
                return;
            }

            if (snapshot.WorldHour - s_LastSnapshotLogWorldHour >= 6f) LogCurrentRegionForecast(snapshot, false);
        }

        private static void LogNeighborForecasts(WeatherSnapshot snapshot)
        {
            WeatherRegionDefinition definition = RegionWeatherGraph.Get(snapshot.RegionId);
            for (int i = 0; i < definition.Neighbors.Count; i++)
            {
                WeatherRegionId neighbor = definition.Neighbors[i];
                Core.Log("[GlobalSim] " + BuildRegionForecastLine(neighbor, snapshot.WorldHour, snapshot));
            }
        }

        private static Dictionary<WeatherRegionId, RegionWeatherTimeline> BuildIndependentRegionalTimelines(WeatherSnapshot snapshot, int seed)
        {
            Dictionary<WeatherRegionId, RegionWeatherTimeline> timelines = [];
            float targetEndWorldHour = snapshot.WorldHour + InternalHorizonHours + 6f;
            for (int i = 0; i < RegionWeatherGraph.Regions.Count; i++)
            {
                WeatherRegionDefinition definition = RegionWeatherGraph.Regions[i];
                int regionSeed = BuildRegionSeed(seed, definition.Id, "initial independent regional timeline");
                timelines[definition.Id] = BuildIndependentRegionTimeline(snapshot, definition.Id, regionSeed, targetEndWorldHour);
            }

            return timelines;
        }

        private static RegionWeatherTimeline BuildIndependentRegionTimeline(WeatherSnapshot snapshot, WeatherRegionId regionId, int regionSeed, float targetEndWorldHour)
        {
            RegionWeatherTimeline timeline = new(regionId);
            s_Rng = new DeterministicRng(regionSeed);

            WeatherStageId observedStageId = regionId == snapshot.RegionId ? GetObservedStageId(snapshot) : WeatherStageId.Undefined;
            bool exactObservedStage = regionId == snapshot.RegionId && ShouldKeepExactObservedStage(observedStageId);
            WeatherFamily previous = exactObservedStage ? WeatherStageCatalog.Get(observedStageId).Family : RegionalWeatherModel.PickInitialWeightedStage(regionId, s_Rng);
            string firstReason = exactObservedStage
                ? (WeatherStageCatalog.Get(observedStageId).IsCustom ? "current observed WeatherOverhaul stage" : "current observed vanilla stage")
                : "independent regional table seed";
            float cursor = snapshot.WorldHour;
            float durationMultiplier = Math.Max(0.05f, WeatherOverhaulSettingsManager.StageDurationMultiplier);
            float firstDuration = Clamp(DurationFor(previous, regionId) * 0.45f, 0.75f * durationMultiplier, 2.5f * durationMultiplier);
            if (!exactObservedStage)
            {
                WeatherFamily restrictedPrevious = RestrictWhiteoutToDaylight(previous, snapshot, cursor, firstDuration);
                if (restrictedPrevious != previous)
                {
                    previous = restrictedPrevious;
                    firstDuration = Clamp(DurationFor(previous, regionId) * 0.45f, 0.75f * durationMultiplier, 2.5f * durationMultiplier);
                }
            }

            WeatherStageId firstStageId = exactObservedStage ? observedStageId : WeatherStageCatalog.ForFamily(previous);
            AddOrMergeSegment(timeline, new RegionWeatherSegment(cursor, cursor + firstDuration, firstStageId, firstReason, exactObservedStage));
            cursor += firstDuration;

            while (cursor < targetEndWorldHour)
            {
                WeatherFamily next = RegionalWeatherModel.PickIndependentWeightedStage(timeline, regionId, s_Rng);
                float duration = DurationFor(next, regionId);
                WeatherFamily restrictedNext = RestrictWhiteoutToDaylight(next, snapshot, cursor, duration);
                if (restrictedNext != next)
                {
                    next = restrictedNext;
                    duration = DurationFor(next, regionId);
                }

                AddOrMergeSegment(timeline, new RegionWeatherSegment(cursor, cursor + duration, WeatherStageCatalog.ForFamily(next), ReasonFor(previous, next) + " | independent regional table", false));
                previous = next;
                cursor += duration;
            }

            return timeline;
        }

        private static WeatherStageId GetObservedStageId(WeatherSnapshot snapshot)
        {
            if (CustomWeatherStageRuntime.TryGetActiveStageId(out WeatherStageId stageId)) return stageId;
            return WeatherStageCatalog.FromEngineStage(snapshot.Stage);
        }

        private static bool ShouldKeepExactObservedStage(WeatherStageId stageId)
        {
            return stageId != WeatherStageId.Undefined
                && stageId != WeatherStageId.ClearAurora
                && stageId != WeatherStageId.CloudyAurora
                && stageId != WeatherStageId.SnowyAurora
                && stageId != WeatherStageId.FoggyAurora
                && stageId != WeatherStageId.ClearBloodMoon
                && stageId != WeatherStageId.SnowBloodMoon
                && stageId != WeatherStageId.GlimmerFog;
        }

        private static GlobalNightEventSchedule BuildNightEventSchedule(WeatherSnapshot snapshot)
        {
            List<NightEventWindow> windows = [];
            PreserveCurrentAuthoritativeNightEventWindow(windows, snapshot.WorldHour);
            AppendNightEventWindowsForRange(windows, snapshot, snapshot.WorldHour, snapshot.WorldHour + InternalHorizonHours + 12f);
            windows.Sort((a, b) => a.StartWorldHour.CompareTo(b.StartWorldHour));
            return new GlobalNightEventSchedule(windows);
        }

        private static GlobalGlimmerFogSchedule BuildGlimmerFogSchedule(WeatherSnapshot snapshot)
        {
            if (!WeatherOverhaulSettingsManager.EnableGlimmerFog) return GlobalGlimmerFogSchedule.Empty;

            Dictionary<WeatherRegionId, List<GlimmerFogWindow>> windowsByRegion = [];
            AppendGlimmerFogWindowsForRange(windowsByRegion, snapshot, snapshot.WorldHour, snapshot.WorldHour + InternalHorizonHours + 12f);
            return new GlobalGlimmerFogSchedule(windowsByRegion);
        }

        private static List<NightEventWindow> CopyFutureNightEventWindows(GlobalNightEventSchedule schedule, float keepAfterWorldHour)
        {
            List<NightEventWindow> windows = [];
            IReadOnlyList<NightEventWindow> source = schedule.Windows;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].EndWorldHour > keepAfterWorldHour) windows.Add(source[i]);
            }

            return windows;
        }

        private static Dictionary<WeatherRegionId, List<GlimmerFogWindow>> CopyFutureGlimmerWindows(GlobalGlimmerFogSchedule schedule, float keepAfterWorldHour)
        {
            Dictionary<WeatherRegionId, List<GlimmerFogWindow>> copy = [];
            IReadOnlyDictionary<WeatherRegionId, List<GlimmerFogWindow>> source = schedule.WindowsByRegion;
            foreach (KeyValuePair<WeatherRegionId, List<GlimmerFogWindow>> pair in source)
            {
                List<GlimmerFogWindow> windows = [];
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (pair.Value[i].EndWorldHour > keepAfterWorldHour) windows.Add(pair.Value[i]);
                }

                if (windows.Count > 0) copy[pair.Key] = windows;
            }

            return copy;
        }

        private static void AppendNightEventWindowsForRange(List<NightEventWindow> windows, WeatherSnapshot snapshot, float rangeStartWorldHour, float rangeEndWorldHour)
        {
            if (rangeEndWorldHour <= rangeStartWorldHour) return;

            float currentClockWorldHour = GetClockWorldHour(snapshot);
            float rangeStartClockWorldHour = currentClockWorldHour + (rangeStartWorldHour - snapshot.WorldHour);
            float rangeEndClockWorldHour = currentClockWorldHour + (rangeEndWorldHour - snapshot.WorldHour);
            int startDay = (int)Math.Floor(rangeStartClockWorldHour / 24f) - 1;
            int endDay = (int)Math.Ceiling(rangeEndClockWorldHour / 24f) + 1;

            for (int day = startDay; day <= endDay; day++)
            {
                if (day < FirstNaturalNightEventDayNumber - 1) continue;

                float nightStartClockWorldHour = day * 24f + NaturalNightStartClockHour;
                float nightEndClockWorldHour = (day + 1) * 24f + NaturalNightEndClockHour;
                if (nightEndClockWorldHour <= nightStartClockWorldHour + 0.5f) continue;

                bool requiresFullMoonCheck = WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon && WeatherOverhaulSettingsManager.BloodMoonChanceProbability > 0f && (WeatherOverhaulSettingsManager.EnableClearBloodMoon || WeatherOverhaulSettingsManager.EnableSnowBloodMoon);
                bool isFullMoonNight = !requiresFullMoonCheck;
                DeterministicRng eventRng = CreateScheduleRng(snapshot, day, 0x17A53C21, WeatherRegionId.Unknown);
                GlobalNightEventType eventType = PickNightEventType(isFullMoonNight, eventRng);
                if (eventType == GlobalNightEventType.None) continue;

                float startClockWorldHour = nightStartClockWorldHour;
                float endClockWorldHour = nightEndClockWorldHour;
                if (NightEventWindow.IsAuroraEvent(eventType))
                {
                    startClockWorldHour = day * 24f + 21f + eventRng.Range(-0.4f, 0.7f);
                    float duration = eventRng.Range(3.0f, 7.0f);
                    endClockWorldHour = Math.Min(startClockWorldHour + duration, nightEndClockWorldHour);
                    if (endClockWorldHour <= startClockWorldHour + 0.5f) continue;
                }

                if (eventType == GlobalNightEventType.ClearBloodMoon || eventType == GlobalNightEventType.LightSnowBloodMoon)
                {
                    startClockWorldHour = day * 24f + NaturalMoonVisibleClockHour;
                    endClockWorldHour = nightEndClockWorldHour;
                }

                float startWorldHour = snapshot.WorldHour + (startClockWorldHour - currentClockWorldHour);
                float endWorldHour = snapshot.WorldHour + (endClockWorldHour - currentClockWorldHour);
                if (endWorldHour < rangeStartWorldHour) continue;
                if (startWorldHour > rangeEndWorldHour) continue;

                AddNightEventWindowIfDistinct(windows, new NightEventWindow(eventType, startWorldHour, endWorldHour, startClockWorldHour % 24f, endClockWorldHour % 24f));
            }

            windows.Sort((a, b) => a.StartWorldHour.CompareTo(b.StartWorldHour));
        }

        internal static bool TryForceNextNightEventVariant(GlobalNightEventType targetEventType, WeatherSnapshot snapshot, out string message)
        {
            message = string.Empty;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown)
            {
                message = "WeatherOverhaul cannot force a night event without a valid gameplay weather snapshot.";
                return false;
            }

            bool targetIsAurora = NightEventWindow.IsAuroraEvent(targetEventType);
            bool targetIsBloodMoon = NightEventWindow.IsBloodMoonEvent(targetEventType);
            if (!targetIsAurora && !targetIsBloodMoon)
            {
                message = "WeatherOverhaul received an unsupported forced night-event type.";
                return false;
            }

            GlobalWeatherPersistence.TryLoad(snapshot);
            if (!EnsureUsableSimulation(snapshot, "developer console forced next night event"))
            {
                message = "WeatherOverhaul could not prepare a usable global forecast.";
                return false;
            }

            List<NightEventWindow> windows = new(Current.NightEventSchedule.Windows);
            int replaceIndex = -1;
            float earliestStart = float.MaxValue;
            for (int i = 0; i < windows.Count; i++)
            {
                NightEventWindow candidate = windows[i];
                if (candidate.StartWorldHour <= snapshot.WorldHour + 0.05f) continue;

                bool categoryMatches = targetIsAurora
                    ? NightEventWindow.IsAuroraEvent(candidate.EventType)
                    : NightEventWindow.IsBloodMoonEvent(candidate.EventType);
                if (!categoryMatches || candidate.StartWorldHour >= earliestStart) continue;

                replaceIndex = i;
                earliestStart = candidate.StartWorldHour;
            }

            NightEventWindow forcedWindow;
            bool replacedExistingWindow = replaceIndex >= 0;
            if (replacedExistingWindow)
            {
                NightEventWindow existing = windows[replaceIndex];
                forcedWindow = new NightEventWindow(targetEventType, existing.StartWorldHour, existing.EndWorldHour, existing.StartClockHour, existing.EndClockHour);
                windows[replaceIndex] = forcedWindow;
            }
            else
            {
                forcedWindow = BuildNextForcedNightEventWindow(targetEventType, snapshot);
                for (int i = windows.Count - 1; i >= 0; i--)
                {
                    NightEventWindow existing = windows[i];
                    bool overlaps = forcedWindow.StartWorldHour < existing.EndWorldHour && forcedWindow.EndWorldHour > existing.StartWorldHour;
                    if (overlaps) windows.RemoveAt(i);
                }

                windows.Add(forcedWindow);
            }

            windows.Sort((a, b) => a.StartWorldHour.CompareTo(b.StartWorldHour));
            GlobalNightEventSchedule schedule = new(windows);
            string summary = BuildSummaryFromCurrentTimelines(schedule, Current.GlimmerFogSchedule, snapshot.WorldHour);
            Current = new GlobalWeatherState(
                Current.IsValid,
                Current.GeneratedAtWorldHour,
                Current.HorizonEndWorldHour,
                Current.AnchorRegion,
                Current.Timelines,
                schedule,
                schedule.ToAuroraSchedule(),
                Current.GlimmerFogSchedule,
                summary);

            string displayName = NightEventWindow.ToDisplayName(targetEventType);
            string source = replacedExistingWindow ? "replaced the next planned event of this category" : "created a test window on the next natural night because none was planned";
            GlobalWeatherPersistence.Save(Current, snapshot, "developer console forced next " + displayName);
            message = $"WeatherOverhaul: next {displayName} forced; {source}. Starts in {Math.Max(0f, forcedWindow.StartWorldHour - snapshot.WorldHour):0.##}h and lasts {forcedWindow.DurationHours:0.##}h.";
            Core.Log("[GlobalSim][Debug] " + message, false);
            return true;
        }

        private static NightEventWindow BuildNextForcedNightEventWindow(GlobalNightEventType eventType, WeatherSnapshot snapshot)
        {
            float currentClockWorldHour = GetClockWorldHour(snapshot);
            int day = (int)Math.Floor(currentClockWorldHour / 24f);
            bool aurora = NightEventWindow.IsAuroraEvent(eventType);
            float startClockHour = aurora ? 21f : NaturalMoonVisibleClockHour;
            float startClockWorldHour = day * 24f + startClockHour;
            if (startClockWorldHour <= currentClockWorldHour + 0.05f)
            {
                day++;
                startClockWorldHour = day * 24f + startClockHour;
            }

            float nightEndClockWorldHour = (day + 1) * 24f + NaturalNightEndClockHour;
            float endClockWorldHour = aurora ? Math.Min(startClockWorldHour + 5f, nightEndClockWorldHour) : nightEndClockWorldHour;
            float startWorldHour = snapshot.WorldHour + (startClockWorldHour - currentClockWorldHour);
            float endWorldHour = snapshot.WorldHour + (endClockWorldHour - currentClockWorldHour);
            return new NightEventWindow(eventType, startWorldHour, endWorldHour, startClockWorldHour % 24f, endClockWorldHour % 24f);
        }

        private static GlobalNightEventType PickNightEventType(bool isFullMoonNight, DeterministicRng rng)
        {
            bool bloodMoonEligible = (!WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon || isFullMoonNight) && (WeatherOverhaulSettingsManager.EnableClearBloodMoon || WeatherOverhaulSettingsManager.EnableSnowBloodMoon);

            if (bloodMoonEligible && rng.Chance(WeatherOverhaulSettingsManager.BloodMoonChanceProbability))
            {
                if (WeatherOverhaulSettingsManager.EnableClearBloodMoon && WeatherOverhaulSettingsManager.EnableSnowBloodMoon) return rng.Chance(0.5f) ? GlobalNightEventType.ClearBloodMoon : GlobalNightEventType.LightSnowBloodMoon;
                if (WeatherOverhaulSettingsManager.EnableClearBloodMoon) return GlobalNightEventType.ClearBloodMoon;
                if (WeatherOverhaulSettingsManager.EnableSnowBloodMoon) return GlobalNightEventType.LightSnowBloodMoon;
            }

            if (rng.Chance(WeatherOverhaulSettingsManager.AuroraChanceProbability)) return PickAuroraNightEventType(rng);
            return GlobalNightEventType.None;
        }

        private static GlobalNightEventType PickAuroraNightEventType(DeterministicRng rng)
        {
            const float ClearWeight = 65f;
            float cloudyWeight = WeatherOverhaulSettingsManager.EnableCloudyAurora ? 25f : 0f;
            float snowyWeight = WeatherOverhaulSettingsManager.EnableSnowyAurora ? 10f : 0f;
            float foggyWeight = WeatherOverhaulSettingsManager.EnableFoggyAurora ? 10f : 0f;
            float total = ClearWeight + cloudyWeight + snowyWeight + foggyWeight;
            if (total <= 0f) return GlobalNightEventType.ClearAurora;

            float roll = rng.Range(0f, total);
            roll -= ClearWeight;
            if (roll <= 0f) return GlobalNightEventType.ClearAurora;
            roll -= cloudyWeight;
            if (roll <= 0f) return GlobalNightEventType.CloudyAurora;
            roll -= snowyWeight;
            if (roll <= 0f) return GlobalNightEventType.SnowyAurora;
            return GlobalNightEventType.FoggyAurora;
        }

        internal static bool IsRuntimeFullMoonAuthoritative(float expectedWorldHour)
        {
            return MoonPhaseRuntime.IsAuthoritativeFullMoon(expectedWorldHour);
        }

        private static bool TryGetRuntimeValidActiveNightEvent(float worldHour, out NightEventWindow activeWindow)
        {
            if (!Current.NightEventSchedule.TryGetActive(worldHour, out activeWindow)) return false;
            if (!NightEventWindow.IsSupportedEvent(activeWindow.EventType))
            {
                activeWindow = default;
                return false;
            }

            if (NightEventWindow.IsBloodMoonEvent(activeWindow.EventType) &&
                WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon &&
                !MoonPhaseRuntime.IsAuthoritativeFullMoon())
            {
                activeWindow = default;
                return false;
            }

            return true;
        }

        private static void SynchronizeRuntimeFullMoonBloodMoon(WeatherSnapshot snapshot)
        {
            if (!Current.IsValid || !snapshot.IsValid) return;

            List<NightEventWindow> windows = new(Current.NightEventSchedule.Windows);
            bool changed = false;
            string reason = string.Empty;

            if (WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon)
            {
                bool fullMoonAvailable = MoonPhaseRuntime.TryIsFullMoon(snapshot.WorldHour, out bool isFullMoon);
                for (int i = windows.Count - 1; i >= 0; i--)
                {
                    NightEventWindow window = windows[i];
                    if (!window.Contains(snapshot.WorldHour) || !NightEventWindow.IsBloodMoonEvent(window.EventType)) continue;
                    if (fullMoonAvailable && isFullMoon) continue;

                    windows.RemoveAt(i);
                    changed = true;
                    reason = "cancelled Blood Moon outside an authoritative full moon";
                }

                if (fullMoonAvailable && isFullMoon && IsInsideNaturalBloodMoonWindow(snapshot))
                {
                    int nightDay = GetCurrentNaturalNightStartDay(snapshot);
                    if (!HasActiveBloodMoonWindow(windows, snapshot.WorldHour) &&
                        TryPickRuntimeFullMoonBloodMoon(nightDay, out GlobalNightEventType eventType))
                    {
                        float currentClockWorldHour = GetClockWorldHour(snapshot);
                        float startClockWorldHour = nightDay * 24f + NaturalMoonVisibleClockHour;
                        float endClockWorldHour = (nightDay + 1) * 24f + NaturalNightEndClockHour;
                        float startWorldHour = snapshot.WorldHour + (startClockWorldHour - currentClockWorldHour);
                        float endWorldHour = snapshot.WorldHour + (endClockWorldHour - currentClockWorldHour);

                        for (int i = windows.Count - 1; i >= 0; i--)
                        {
                            NightEventWindow existing = windows[i];
                            bool overlaps = startWorldHour < existing.EndWorldHour && endWorldHour > existing.StartWorldHour;
                            if (overlaps) windows.RemoveAt(i);
                        }

                        windows.Add(new NightEventWindow(eventType, startWorldHour, endWorldHour, NaturalMoonVisibleClockHour, NaturalNightEndClockHour));
                        windows.Sort((a, b) => a.StartWorldHour.CompareTo(b.StartWorldHour));
                        changed = true;
                        reason = "created runtime-authoritative full-moon Blood Moon";
                    }
                }
            }

            if (!changed) return;

            GlobalNightEventSchedule schedule = new(windows);
            string summary = BuildSummaryFromCurrentTimelines(schedule, Current.GlimmerFogSchedule, snapshot.WorldHour);
            Current = new GlobalWeatherState(
                Current.IsValid,
                Current.GeneratedAtWorldHour,
                Current.HorizonEndWorldHour,
                Current.AnchorRegion,
                Current.Timelines,
                schedule,
                schedule.ToAuroraSchedule(),
                Current.GlimmerFogSchedule,
                summary);

            GlobalWeatherPersistence.Save(Current, snapshot, reason);
            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[GlobalSim] Night-event schedule synchronized ({reason}) | FullMoonOnly={(WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon ? "on" : "off")} | NightEvent={schedule.BuildSummary(snapshot.WorldHour)}");
            }
        }

        private static bool IsInsideNaturalBloodMoonWindow(WeatherSnapshot snapshot)
        {
            float clockHour = snapshot.Hour + snapshot.Minute / 60f;
            return clockHour >= NaturalMoonVisibleClockHour || clockHour < NaturalNightEndClockHour;
        }

        private static int GetCurrentNaturalNightStartDay(WeatherSnapshot snapshot)
        {
            float currentClockWorldHour = GetClockWorldHour(snapshot);
            int day = (int)Math.Floor(currentClockWorldHour / 24f);
            float clockHour = currentClockWorldHour - day * 24f;
            return Math.Max(0, clockHour < NaturalNightEndClockHour ? day - 1 : day);
        }

        private static bool HasActiveBloodMoonWindow(List<NightEventWindow> windows, float worldHour)
        {
            for (int i = 0; i < windows.Count; i++)
            {
                NightEventWindow window = windows[i];
                if (window.Contains(worldHour) && NightEventWindow.IsBloodMoonEvent(window.EventType)) return true;
            }

            return false;
        }

        private static bool TryPickRuntimeFullMoonBloodMoon(int nightDay, out GlobalNightEventType eventType)
        {
            eventType = GlobalNightEventType.None;
            if (!WeatherOverhaulSettingsManager.EnableClearBloodMoon && !WeatherOverhaulSettingsManager.EnableSnowBloodMoon) return false;
            if (WeatherOverhaulSettingsManager.BloodMoonChanceProbability <= 0f) return false;

            int seed = unchecked((nightDay + 1) * 486187739 ^ StableHash(WeatherOverhaulSettingsManager.EventSettingsSignature) ^ 0x5E71B10D);
            DeterministicRng rng = new(seed);
            if (!rng.Chance(WeatherOverhaulSettingsManager.BloodMoonChanceProbability)) return false;

            if (WeatherOverhaulSettingsManager.EnableClearBloodMoon && WeatherOverhaulSettingsManager.EnableSnowBloodMoon)
            {
                eventType = rng.Chance(0.5f) ? GlobalNightEventType.ClearBloodMoon : GlobalNightEventType.LightSnowBloodMoon;
                return true;
            }

            eventType = WeatherOverhaulSettingsManager.EnableClearBloodMoon
                ? GlobalNightEventType.ClearBloodMoon
                : GlobalNightEventType.LightSnowBloodMoon;
            return true;
        }

        private static void PreserveCurrentAuthoritativeNightEventWindow(List<NightEventWindow> windows, float worldHour)
        {
            if (!Current.IsValid) return;
            if (!TryGetRuntimeValidActiveNightEvent(worldHour, out NightEventWindow activeWindow)) return;
            if (activeWindow.EventType == GlobalNightEventType.None) return;

            AddNightEventWindowIfDistinct(windows, activeWindow);
        }

        private static bool IsAuthoritativeAuroraActive(float worldHour)
        {
            if (!Current.IsValid) return false;
            if (!Current.NightEventSchedule.TryGetActive(worldHour, out NightEventWindow activeWindow)) return false;
            return NightEventWindow.IsAuroraEvent(activeWindow.EventType);
        }

        private static void AddNightEventWindowIfDistinct(List<NightEventWindow> windows, NightEventWindow candidate)
        {
            if (candidate.EventType == GlobalNightEventType.None) return;
            if (candidate.EndWorldHour <= candidate.StartWorldHour + 0.1f) return;
            for (int i = 0; i < windows.Count; i++)
            {
                NightEventWindow existing = windows[i];
                bool overlaps = candidate.StartWorldHour < existing.EndWorldHour && candidate.EndWorldHour > existing.StartWorldHour;
                if (overlaps) return;
            }

            windows.Add(candidate);
        }

        private static void AppendGlimmerFogWindowsForRange(Dictionary<WeatherRegionId, List<GlimmerFogWindow>> windowsByRegion, WeatherSnapshot snapshot, float rangeStartWorldHour, float rangeEndWorldHour)
        {
            if (rangeEndWorldHour <= rangeStartWorldHour) return;
            if (!WeatherOverhaulSettingsManager.EnableGlimmerFog) return;

            float currentClockWorldHour = GetClockWorldHour(snapshot);
            float rangeStartClockWorldHour = currentClockWorldHour + (rangeStartWorldHour - snapshot.WorldHour);
            float rangeEndClockWorldHour = currentClockWorldHour + (rangeEndWorldHour - snapshot.WorldHour);
            int startDay = (int)Math.Floor(rangeStartClockWorldHour / 24f) - 1;
            int endDay = (int)Math.Ceiling(rangeEndClockWorldHour / 24f) + 1;
            WeatherRegionId[] glimmerRegions = [WeatherRegionId.FA, WeatherRegionId.ZOC, WeatherRegionId.SP];

            for (int r = 0; r < glimmerRegions.Length; r++)
            {
                WeatherRegionId regionId = glimmerRegions[r];
                if (!windowsByRegion.TryGetValue(regionId, out List<GlimmerFogWindow> windows))
                {
                    windows = [];
                    windowsByRegion[regionId] = windows;
                }

                for (int day = startDay; day <= endDay; day++)
                {
                    DeterministicRng glimmerRng = CreateScheduleRng(snapshot, day, 0x2C59D41B, regionId);
                    if (!glimmerRng.Chance(WeatherOverhaulSettingsManager.GlimmerFogChanceProbability)) continue;

                    float startClockWorldHour = day * 24f + glimmerRng.Range(8.0f, 15.5f);
                    float duration = glimmerRng.Range(2.5f, 6.0f);
                    float endClockWorldHour = Math.Min(startClockWorldHour + duration, day * 24f + 19.0f);
                    if (endClockWorldHour <= startClockWorldHour + 0.75f) continue;

                    float startWorldHour = snapshot.WorldHour + (startClockWorldHour - currentClockWorldHour);
                    float endWorldHour = snapshot.WorldHour + (endClockWorldHour - currentClockWorldHour);
                    if (endWorldHour < rangeStartWorldHour) continue;
                    if (startWorldHour > rangeEndWorldHour) continue;

                    AddGlimmerFogWindowIfDistinct(windows, new GlimmerFogWindow(startWorldHour, endWorldHour, startClockWorldHour % 24f, endClockWorldHour % 24f));
                }

                windows.Sort((a, b) => a.StartWorldHour.CompareTo(b.StartWorldHour));
                if (windows.Count == 0) windowsByRegion.Remove(regionId);
            }
        }

        private static void AddGlimmerFogWindowIfDistinct(List<GlimmerFogWindow> windows, GlimmerFogWindow candidate)
        {
            if (candidate.EndWorldHour <= candidate.StartWorldHour + 0.1f) return;
            for (int i = 0; i < windows.Count; i++)
            {
                GlimmerFogWindow existing = windows[i];
                bool overlaps = candidate.StartWorldHour < existing.EndWorldHour && candidate.EndWorldHour > existing.StartWorldHour;
                if (overlaps) return;
            }

            windows.Add(candidate);
        }

        private static DeterministicRng CreateScheduleRng(WeatherSnapshot snapshot, int day, int salt, WeatherRegionId regionId)
        {
            unchecked
            {
                float generatedAt = Current.IsValid ? Current.GeneratedAtWorldHour : snapshot.WorldHour;
                int seed = (int)Math.Round(generatedAt * 100f);
                seed = (seed * 397) ^ day;
                seed = (seed * 397) ^ salt;
                seed = (seed * 397) ^ (int)regionId;
                return new DeterministicRng(seed);
            }
        }

        private static string FormatPlanForLog(WeatherActivationPlan plan, WeatherSnapshot snapshot, bool isCurrent)
        {
            string start = isCurrent ? "Now" : FormatWorldHourForSnapshot(snapshot, plan.StartWorldHour);
            string end = FormatWorldHourForSnapshot(snapshot, plan.EndWorldHour);
            string stage = FormatPlanDisplayName(plan);
            return isCurrent ? $"{start}->{end} {stage} ({plan.RemainingHours:0.#}h left)" : $"{start}->{end} {stage}";
        }

        internal static string FormatWorldHourForSnapshot(WeatherSnapshot snapshot, float worldHour)
        {
            float deltaHours = worldHour - snapshot.WorldHour;
            int currentAbsoluteMinutes = ((Math.Max(1, snapshot.DayNumber) - 1) * 24 * 60) + (snapshot.Hour * 60) + snapshot.Minute;
            int targetAbsoluteMinutes = currentAbsoluteMinutes + (int)Math.Round(deltaHours * 60f);
            if (targetAbsoluteMinutes < 0) targetAbsoluteMinutes = 0;

            int day = (targetAbsoluteMinutes / (24 * 60)) + 1;
            int minuteOfDay = targetAbsoluteMinutes % (24 * 60);
            int hour = minuteOfDay / 60;
            int minute = minuteOfDay % 60;
            return $"D{day} {hour:00}:{minute:00}";
        }

        internal static string FormatPlanDisplayName(WeatherActivationPlan plan)
        {
            return plan.Definition.DisplayName;
        }

        private static WeatherFamily RestrictWhiteoutToDaylight(WeatherFamily family, WeatherSnapshot snapshot, float startWorldHour, float durationHours)
        {
            if (family != WeatherFamily.Whiteout) return family;
            return IsEntirelyInsideWhiteoutDaylightWindow(snapshot, startWorldHour, startWorldHour + durationHours)
                ? family
                : WeatherFamily.HeavySnow;
        }

        private static bool IsEntirelyInsideWhiteoutDaylightWindow(WeatherSnapshot snapshot, float startWorldHour, float endWorldHour)
        {
            float currentClockWorldHour = GetClockWorldHour(snapshot);
            float startClockWorldHour = currentClockWorldHour + (startWorldHour - snapshot.WorldHour);
            float endClockWorldHour = currentClockWorldHour + (endWorldHour - snapshot.WorldHour);
            int day = (int)Math.Floor(startClockWorldHour / 24f);
            float daylightStart = day * 24f + NaturalNightEndClockHour;
            float daylightEnd = day * 24f + NaturalMoonVisibleClockHour;
            return startClockWorldHour >= daylightStart - 0.01f && endClockWorldHour <= daylightEnd + 0.01f;
        }

        private static void NormalizePersistedNightWhiteouts(WeatherSnapshot snapshot)
        {
            if (!Current.IsValid) return;

            int replaced = 0;
            foreach (KeyValuePair<WeatherRegionId, RegionWeatherTimeline> pair in Current.Timelines)
            {
                List<RegionWeatherSegment> segments = pair.Value.Segments;
                for (int i = 0; i < segments.Count; i++)
                {
                    RegionWeatherSegment segment = segments[i];
                    if (segment.StageId != WeatherStageId.Whiteout) continue;
                    if (IsEntirelyInsideWhiteoutDaylightWindow(snapshot, segment.StartWorldHour, segment.EndWorldHour)) continue;

                    segments[i] = new RegionWeatherSegment(
                        segment.StartWorldHour,
                        segment.EndWorldHour,
                        WeatherStageId.HeavySnow,
                        segment.Reason + " | Whiteout restricted to daylight",
                        false);
                    replaced++;
                }
            }

            if (replaced > 0 && WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[GlobalSim] Replaced {replaced} persisted night-overlapping Whiteout segment(s) with Heavy Snow.");
            }
        }

        private static float DurationFor(WeatherFamily family, WeatherRegionId regionId)
        {
            float baseDuration;
            switch (family)
            {
                case WeatherFamily.Clear: baseDuration = s_Rng.Range(2.0f, 5.5f); break;
                case WeatherFamily.PartlyCloudy: baseDuration = s_Rng.Range(1.8f, 4.8f); break;
                case WeatherFamily.Cloudy: baseDuration = s_Rng.Range(1.8f, 4.8f); break;
                case WeatherFamily.LowOvercast: baseDuration = s_Rng.Range(1.6f, 4.2f); break;
                case WeatherFamily.LightSnow: baseDuration = s_Rng.Range(0.7f, 2.6f); break;
                case WeatherFamily.HeavySnow: baseDuration = s_Rng.Range(0.8f, 3.2f); break;
                case WeatherFamily.HeavyOvercast: baseDuration = s_Rng.Range(1.4f, 4.2f); break;
                case WeatherFamily.VeryHeavySnow: baseDuration = s_Rng.Range(1.0f, 3.5f); break;
                case WeatherFamily.Whiteout: baseDuration = s_Rng.Range(0.9f, 2.8f); break;
                case WeatherFamily.WindyLightSnow: baseDuration = s_Rng.Range(1.0f, 3.0f); break;
                case WeatherFamily.Blizzard: baseDuration = s_Rng.Range(3.5f, 9.0f); break;
                case WeatherFamily.ViolentBlizzard: baseDuration = s_Rng.Range(5.0f, 11.0f); break;
                case WeatherFamily.LightFog: baseDuration = s_Rng.Range(0.6f, 2.3f); break;
                case WeatherFamily.FreezingFog: baseDuration = s_Rng.Range(2.0f, 5.5f); break;
                case WeatherFamily.DenseFog: baseDuration = s_Rng.Range(1.0f, 3.4f); break;
                case WeatherFamily.VeryDenseFog: baseDuration = s_Rng.Range(1.6f, 4.4f); break;
                case WeatherFamily.Ashfall: baseDuration = s_Rng.Range(1.2f, 3.8f); break;
                case WeatherFamily.ElectrostaticFog: baseDuration = s_Rng.Range(1.4f, 4.6f); break;
                case WeatherFamily.ClearBloodMoon:
                case WeatherFamily.LightSnowBloodMoon:
                    return 9.25f;
                default: baseDuration = s_Rng.Range(1.5f, 3.5f); break;
            }

            return Math.Max(MinimumControlledStageDurationHours, baseDuration * Math.Max(0.05f, WeatherOverhaulSettingsManager.StageDurationMultiplier) * RegionalWeatherModel.GetSeverityDurationMultiplier(regionId, family));
        }

        private static string ReasonFor(WeatherFamily previous, WeatherFamily next)
        {
            if (previous == next) return "same vanilla-like family continues";
            return $"source family {WeatherFamilyFormatter.ToDisplayName(previous)} -> {WeatherFamilyFormatter.ToDisplayName(next)}";
        }

        private static string BuildSummary(WeatherSnapshot snapshot, Dictionary<WeatherRegionId, RegionWeatherTimeline> timelines, GlobalNightEventSchedule nightEventSchedule, GlobalGlimmerFogSchedule glimmerFogSchedule)
        {
            StringBuilder builder = new();
            builder.Append("Independent regional timelines | ");
            builder.Append(BuildTimelinePreview(snapshot.RegionId, timelines, snapshot.WorldHour));
            builder.Append(" | NightEvent ");
            builder.Append(nightEventSchedule.BuildSummary(snapshot.WorldHour));
            builder.Append(" | Glimmer ");
            builder.Append(glimmerFogSchedule.BuildSummary(snapshot.WorldHour));
            return builder.ToString();
        }

        private static string BuildSummaryFromCurrentTimelines(GlobalNightEventSchedule nightEventSchedule, GlobalGlimmerFogSchedule glimmerFogSchedule, float worldHour)
        {
            StringBuilder builder = new();
            builder.Append("Carried independent regional timelines | ");
            builder.Append(BuildTimelinePreview(Current.AnchorRegion, Current.Timelines, worldHour));
            builder.Append(" | NightEvent ");
            builder.Append(nightEventSchedule.BuildSummary(worldHour));
            builder.Append(" | Glimmer ");
            builder.Append(glimmerFogSchedule.BuildSummary(worldHour));
            return builder.ToString();
        }

        private static string BuildTimelinePreview(WeatherRegionId regionId, Dictionary<WeatherRegionId, RegionWeatherTimeline> timelines, float worldHour)
        {
            WeatherRegionDefinition definition = RegionWeatherGraph.Get(regionId);
            StringBuilder builder = new();
            builder.Append(definition.ShortName);
            builder.Append(": ");
            if (!timelines.TryGetValue(regionId, out RegionWeatherTimeline timeline)) return builder.Append("unavailable").ToString();

            int written = 0;
            for (int i = 0; i < timeline.Segments.Count && written < 8; i++)
            {
                RegionWeatherSegment segment = timeline.Segments[i];
                if (segment.EndWorldHour <= worldHour + 0.01f) continue;
                if (written > 0) builder.Append(" -> ");
                builder.Append(WeatherFamilyFormatter.ToDisplayName(segment.Family));
                written++;
            }

            if (written == 0) builder.Append("unavailable");
            return builder.ToString();
        }

        private static int BuildSeed(WeatherSnapshot snapshot, WeatherRegionId anchorRegion, int serial, string reason)
        {
            unchecked
            {
                int seed = 17;
                seed = seed * 31 + snapshot.DayNumber;
                seed = seed * 31 + snapshot.Hour;
                seed = seed * 31 + snapshot.Minute;
                seed = seed * 31 + (int)anchorRegion;
                seed = seed * 31 + serial;
                seed = seed * 31 + StableHash(reason ?? string.Empty);
                return seed;
            }
        }

        private static int BuildRollingSeed(WeatherRegionId regionId, float startWorldHour, WeatherFamily previous, int index, string reason)
        {
            unchecked
            {
                int seed = 31;
                seed = seed * 37 + (int)regionId;
                seed = seed * 37 + (int)previous;
                seed = seed * 37 + (int)Math.Round(startWorldHour * 10f);
                seed = seed * 37 + index;
                seed = seed * 37 + StableHash(reason ?? string.Empty);
                return seed;
            }
        }

        private static int BuildRegionSeed(int seed, WeatherRegionId regionId, string reason)
        {
            unchecked
            {
                int result = seed;
                result = result * 41 + (int)regionId;
                result = result * 41 + StableHash(reason ?? string.Empty);
                return result;
            }
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < text.Length; i++) hash = hash * 31 + text[i];
                return hash;
            }
        }

        private static float GetClockWorldHour(WeatherSnapshot snapshot)
        {
            int dayIndex = Math.Max(0, snapshot.DayNumber - 1);
            return dayIndex * 24f + snapshot.Hour + snapshot.Minute / 60f;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}