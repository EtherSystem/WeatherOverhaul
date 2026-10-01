using System.Text.Json;

namespace WeatherOverhaul.Weather
{
    internal static class FreezingFogThinIceRuntime
    {
        internal const float RequiredFreezingFogHours = 4f;
        internal const float HardenedAfterFogHours = 12f;

        private const int SaveVersion = 1;
        private const string SaveSuffix = "FreezingFogThinIceState";
        private const float TimeToleranceHours = 0.01f;

        private static readonly ModDataManager s_DataManager = new("WeatherOverhaul");
        private static readonly HashSet<string> s_ThinIceHardeningSceneWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "MarshRegion"
        };
        private static readonly Dictionary<WeatherRegionId, float> s_HardenedUntilByRegion = [];
        private static bool s_LoadAttemptedForCurrentSave;
        private static WeatherRegionId s_CurrentRegion = WeatherRegionId.Unknown;
        private static float s_CurrentWorldHour;
        private static bool s_CurrentRegionHardened;

        internal static bool IsCurrentRegionThinIceHardened =>
            GameplaySceneState.IsGameplaySceneActive() &&
            WeatherOverhaulSettingsManager.EnableWeatherOverhaul &&
            WeatherOverhaulSettingsManager.EnableGlobalSimulation &&
            WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion &&
            WeatherOverhaulSettingsManager.EnableFreezingFog &&
            IsThinIceHardeningAllowed(s_CurrentRegion) &&
            s_CurrentRegionHardened;

        internal static void Update(WeatherSnapshot snapshot)
        {
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown)
            {
                ResetRuntimeCache();
                return;
            }

            s_CurrentRegion = snapshot.RegionId;
            s_CurrentWorldHour = snapshot.WorldHour;

            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul ||
                !WeatherOverhaulSettingsManager.EnableGlobalSimulation ||
                !WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion ||
                !WeatherOverhaulSettingsManager.EnableFreezingFog)
            {
                s_CurrentRegionHardened = false;
                return;
            }

            TryLoad(snapshot);
            ObserveQualifyingFreezingFog(snapshot);
            RemoveExpiredWindows(snapshot.WorldHour);
            RefreshCurrentRegionState();
        }

        internal static void ResetRuntimeCache()
        {
            s_CurrentRegion = WeatherRegionId.Unknown;
            s_CurrentWorldHour = 0f;
            s_CurrentRegionHardened = false;
        }

        internal static void ResetLoadAttempt()
        {
            s_LoadAttemptedForCurrentSave = false;
            s_HardenedUntilByRegion.Clear();
            ResetRuntimeCache();
        }

        private static void ObserveQualifyingFreezingFog(WeatherSnapshot snapshot)
        {
            GlobalWeatherState state = GlobalWeatherSimulation.Current;
            if (state == null || !state.IsValid) return;

            bool changed = false;
            foreach (KeyValuePair<WeatherRegionId, RegionWeatherTimeline> pair in state.Timelines)
            {
                if (!IsThinIceHardeningAllowed(pair.Key)) continue;

                List<RegionWeatherSegment> segments = pair.Value.Segments;
                for (int i = 0; i < segments.Count; i++)
                {
                    RegionWeatherSegment segment = segments[i];
                    if (segment.StageId != WeatherStageId.FreezingFog) continue;
                    if (!TryGetQualifyingEffectiveFreezingFogWindow(pair.Key, segment, snapshot.WorldHour, out float runStart, out float runEnd)) continue;

                    float hardenedUntil = runEnd + HardenedAfterFogHours;
                    if (snapshot.WorldHour >= hardenedUntil - TimeToleranceHours) continue;
                    if (s_HardenedUntilByRegion.TryGetValue(pair.Key, out float existingUntil) && existingUntil >= hardenedUntil - TimeToleranceHours) continue;

                    s_HardenedUntilByRegion[pair.Key] = hardenedUntil;
                    changed = true;

                    WeatherRegionDefinition region = RegionWeatherGraph.Get(pair.Key);
                    Core.Log($"[FreezingFog][ThinIce] Thin ice hardened in {region.ShortName}/{region.DisplayName} after {RequiredFreezingFogHours:0.#}h of effective continuous Freezing Fog ({runStart:0.##}h->{runEnd:0.##}h). Safe until world hour {hardenedUntil:0.##}.", false);
                }
            }

            if (changed) Save();
        }

        private static bool TryGetQualifyingEffectiveFreezingFogWindow(WeatherRegionId regionId, RegionWeatherSegment backgroundSegment, float currentWorldHour, out float qualifyingStart, out float qualifyingEnd)
        {
            qualifyingStart = 0f;
            qualifyingEnd = 0f;
            float cursor = backgroundSegment.StartWorldHour;
            float runStart = -1f;
            int safety = 0;

            while (cursor < backgroundSegment.EndWorldHour - TimeToleranceHours && safety++ < 128)
            {
                if (!GlobalWeatherSimulation.TryGetActivationPlan(regionId, cursor, out WeatherActivationPlan plan)) break;

                float intervalStart = Math.Max(cursor, backgroundSegment.StartWorldHour);
                float intervalEnd = Math.Min(plan.EndWorldHour, backgroundSegment.EndWorldHour);
                if (intervalEnd <= intervalStart + TimeToleranceHours)
                {
                    cursor += 0.05f;
                    continue;
                }

                if (plan.StageId == WeatherStageId.FreezingFog)
                {
                    if (runStart < 0f) runStart = intervalStart;
                }
                else if (runStart >= 0f)
                {
                    if (currentWorldHour + TimeToleranceHours >= runStart + RequiredFreezingFogHours)
                    {
                        qualifyingStart = runStart;
                        qualifyingEnd = intervalStart;
                        return qualifyingEnd > qualifyingStart + TimeToleranceHours;
                    }
                    runStart = -1f;
                }

                cursor = intervalEnd;
            }

            if (runStart < 0f) return false;
            if (currentWorldHour + TimeToleranceHours < runStart + RequiredFreezingFogHours) return false;
            qualifyingStart = runStart;
            qualifyingEnd = backgroundSegment.EndWorldHour;
            return qualifyingEnd > qualifyingStart + TimeToleranceHours;
        }

        private static void RefreshCurrentRegionState()
        {
            bool wasHardened = s_CurrentRegionHardened;
            s_CurrentRegionHardened = IsThinIceHardeningAllowed(s_CurrentRegion) && s_HardenedUntilByRegion.TryGetValue(s_CurrentRegion, out float hardenedUntil) && s_CurrentWorldHour < hardenedUntil - TimeToleranceHours;

            if (!wasHardened && s_CurrentRegionHardened) ClearActiveThinIceInteraction();
        }

        private static void ClearActiveThinIceInteraction()
        {
            try
            {
                IceCrackingManager manager = GameManager.GetIceCrackingManager();
                if (manager != null) manager.ExitAllTriggers();
            }
            catch (Exception exception)
            {
                Core.LogExceptionOnce("freezing-fog-thin-ice-clear", "[FreezingFog][ThinIce] Failed to clear the active thin-ice interaction.", exception);
            }
        }

        private static void RemoveExpiredWindows(float worldHour)
        {
            if (s_HardenedUntilByRegion.Count == 0) return;

            List<WeatherRegionId>? expired = null;
            foreach (KeyValuePair<WeatherRegionId, float> pair in s_HardenedUntilByRegion)
            {
                if (worldHour < pair.Value - TimeToleranceHours) continue;
                expired ??= [];
                expired.Add(pair.Key);
            }

            if (expired == null) return;
            for (int i = 0; i < expired.Count; i++) s_HardenedUntilByRegion.Remove(expired[i]);
            Save();
        }

        private static void TryLoad(WeatherSnapshot snapshot)
        {
            if (s_LoadAttemptedForCurrentSave) return;
            s_LoadAttemptedForCurrentSave = true;
            s_HardenedUntilByRegion.Clear();

            try
            {
                string? payload = s_DataManager.Load(SaveSuffix);
                if (string.IsNullOrWhiteSpace(payload)) return;

                PersistedThinIceState? persisted = JsonSerializer.Deserialize<PersistedThinIceState>(payload);
                if (persisted == null || persisted.Version != SaveVersion) return;

                for (int i = 0; i < persisted.Regions.Count; i++)
                {
                    PersistedRegionThinIceState region = persisted.Regions[i];
                    WeatherRegionId regionId = (WeatherRegionId)region.RegionId;
                    if (!IsThinIceHardeningAllowed(regionId)) continue;
                    if (region.HardenedUntilWorldHour <= snapshot.WorldHour + TimeToleranceHours) continue;
                    s_HardenedUntilByRegion[regionId] = region.HardenedUntilWorldHour;
                }
            }
            catch (Exception exception)
            {
                Core.LogExceptionOnce("freezing-fog-thin-ice-load", "[FreezingFog][ThinIce] Failed to load persisted thin-ice state.", exception);
            }
        }


        private static bool IsThinIceHardeningAllowed(WeatherRegionId regionId)
        {
            if (regionId == WeatherRegionId.Unknown) return false;
            WeatherRegionDefinition region = RegionWeatherGraph.Get(regionId);
            return !string.IsNullOrWhiteSpace(region.SceneName) && s_ThinIceHardeningSceneWhitelist.Contains(region.SceneName);
        }

        private static void Save()
        {
            try
            {
                PersistedThinIceState persisted = new() { Version = SaveVersion };
                foreach (KeyValuePair<WeatherRegionId, float> pair in s_HardenedUntilByRegion)
                {
                    persisted.Regions.Add(new PersistedRegionThinIceState
                    {
                        RegionId = (int)pair.Key,
                        HardenedUntilWorldHour = pair.Value
                    });
                }

                string payload = JsonSerializer.Serialize(persisted);
                if (!s_DataManager.Save(payload, SaveSuffix)) Core.Warn("[FreezingFog][ThinIce] Persisted thin-ice state save returned false.");
            }
            catch (Exception exception)
            {
                Core.LogExceptionOnce("freezing-fog-thin-ice-save", "[FreezingFog][ThinIce] Failed to save thin-ice state.", exception);
            }
        }

        private sealed class PersistedThinIceState
        {
            public int Version { get; set; }
            public List<PersistedRegionThinIceState> Regions { get; set; } = [];
        }

        private sealed class PersistedRegionThinIceState
        {
            public int RegionId { get; set; }
            public float HardenedUntilWorldHour { get; set; }
        }
    }
}