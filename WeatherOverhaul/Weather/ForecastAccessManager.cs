using System.Text.Json;
using System.Text.Json.Serialization;
using Il2CppInteractiveObjects;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace WeatherOverhaul.Weather
{
    internal static class ForecastAccessManager
    {
        private const int TotalTransmitters = 6;
        private const float ForecastHoursPerTransmitter = 48f;
        private const float RefreshIntervalSeconds = 1f;
        private const int AuroraKnowledgeSaveVersion = 1;
        private const string AuroraKnowledgeSaveSuffix = "TransmitterForecastKnowledge";

        private sealed class SerializedTransmitterState
        {
            [JsonPropertyName("m_IsFixed")]
            public bool IsFixed { get; set; }
        }

        private sealed class PersistedAuroraForecastKnowledge
        {
            [JsonPropertyName("v")]
            public int Version { get; set; }

            [JsonPropertyName("has")]
            public bool HasAuroraRefresh { get; set; }

            [JsonPropertyName("start")]
            public float LastAuroraStartWorldHour { get; set; }

            [JsonPropertyName("end")]
            public float LastAuroraEndWorldHour { get; set; }

            [JsonPropertyName("count")]
            public int SynchronizedTransmitterCount { get; set; }

            [JsonPropertyName("sites")]
            public List<int> SynchronizedTransmitterRegions { get; set; } = new List<int>();
        }

        private static readonly ModDataManager s_DataManager = new ModDataManager("WeatherOverhaul");
        private static readonly HashSet<WeatherRegionId> s_RepairedTransmitterRegions = new HashSet<WeatherRegionId>();
        private static readonly HashSet<WeatherRegionId> s_CoveredRegions = new HashSet<WeatherRegionId>();
        private static readonly HashSet<WeatherRegionId> s_SynchronizedTransmitterRegions = new HashSet<WeatherRegionId>();
        private static readonly HashSet<WeatherRegionId> s_SynchronizedCoveredRegions = new HashSet<WeatherRegionId>();
        private static readonly Dictionary<string, WeatherRegionId> s_TransmitterRegionsByGuid = new Dictionary<string, WeatherRegionId>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> s_LoggedUnmappedRepairedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> s_LoggedUnresolvedTransmitterScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static float s_NextRefreshRealtime;
        private static int s_RepairedTransmitterCount;
        private static bool s_HasCompleteTransmitterGuidMap;
        private static string s_LastLoggedNetworkSignature = string.Empty;
        private static string s_LastLoggedGuidMapSignature = string.Empty;

        private static bool s_AuroraKnowledgeLoadAttempted;
        private static bool s_HasAuroraRefresh;
        private static float s_LastAuroraStartWorldHour = -1f;
        private static float s_LastAuroraEndWorldHour = -1f;
        private static int s_SynchronizedTransmitterCount;
        private static bool s_WasAuroraActive;
        private static float s_LastAuroraObservationWorldHour = -1f;
        private static string s_LastLoggedAuroraKnowledgeSignature = string.Empty;

        internal static bool IsPermanentAccess => WeatherOverhaulSettingsManager.ForecastAccess == ForecastAccessMode.Permanent;
        internal static bool RequiresAuroraRefresh => !IsPermanentAccess && WeatherOverhaulSettingsManager.RequireAuroraForTransmitterForecast;

        internal static void Update(WeatherSnapshot snapshot)
        {
            if (IsPermanentAccess) return;

            RefreshTransmitterState();
            if (!RequiresAuroraRefresh) return;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;

            EnsureAuroraKnowledgeLoaded();
            RefreshAuroraKnowledge(snapshot);
        }

        internal static void NotifySceneWasLoaded(string sceneName)
        {
            InvalidateTransmitterCache();
            if (!IsSaveBoundaryScene(sceneName)) return;

            s_RepairedTransmitterRegions.Clear();
            s_CoveredRegions.Clear();
            s_RepairedTransmitterCount = 0;
            s_LastLoggedNetworkSignature = string.Empty;
            s_LoggedUnmappedRepairedGuids.Clear();

            ResetAuroraKnowledgeRuntime();
        }

        internal static bool CanSeeCurrentStage(WeatherRegionId regionId, WeatherSnapshot snapshot)
        {
            if (regionId == WeatherRegionId.Unknown) return false;
            if (IsPermanentAccess) return true;
            if (snapshot.IsValid && regionId == snapshot.RegionId) return true;

            RefreshAccessState(snapshot);
            if (!RequiresAuroraRefresh) return s_CoveredRegions.Contains(regionId);

            float now = GetWorldHour(snapshot);
            return HasFreshAuroraKnowledge(regionId, now);
        }

        internal static bool CanSeeFutureForecast(WeatherRegionId regionId)
        {
            if (regionId == WeatherRegionId.Unknown) return false;
            if (IsPermanentAccess) return true;

            WeatherSnapshot snapshot = WeatherOverhaulRuntime.CurrentSnapshot;
            RefreshAccessState(snapshot);

            if (!RequiresAuroraRefresh) return s_CoveredRegions.Contains(regionId);

            float now = GetWorldHour(snapshot);
            return HasFreshAuroraKnowledge(regionId, now);
        }

        internal static float GetVisibleUntilWorldHour(WeatherRegionId regionId, WeatherSnapshot snapshot, float fullHorizonEndWorldHour)
        {
            float now = GetWorldHour(snapshot);
            if (IsPermanentAccess) return fullHorizonEndWorldHour;

            RefreshAccessState(snapshot);
            if (!RequiresAuroraRefresh)
            {
                if (!s_CoveredRegions.Contains(regionId)) return now;
                if (s_RepairedTransmitterCount >= TotalTransmitters) return fullHorizonEndWorldHour;
                return Math.Min(fullHorizonEndWorldHour, now + GetForecastDepthHours(s_RepairedTransmitterCount));
            }

            if (!HasFreshAuroraKnowledge(regionId, now)) return now;
            return Math.Min(fullHorizonEndWorldHour, GetAuroraKnowledgeVisibleUntilWorldHour(now));
        }

        internal static string BuildAccessLabel(WeatherRegionId regionId)
        {
            if (IsPermanentAccess) return "Forecast access: permanent";

            WeatherSnapshot snapshot = WeatherOverhaulRuntime.CurrentSnapshot;
            RefreshAccessState(snapshot);

            bool repairedCoverage = s_CoveredRegions.Contains(regionId);
            if (!RequiresAuroraRefresh)
            {
                string coverage = repairedCoverage ? "region covered" : "region not covered";
                return $"Forecast access: transmitter network ({s_RepairedTransmitterCount}/{TotalTransmitters} repaired, {coverage})";
            }

            if (!repairedCoverage)
                return $"Forecast access: transmitter network ({s_RepairedTransmitterCount}/{TotalTransmitters} repaired, region not covered)";

            if (!s_HasAuroraRefresh)
                return $"Forecast access: transmitter network ({s_RepairedTransmitterCount}/{TotalTransmitters} repaired, awaiting aurora sync)";

            if (!s_SynchronizedCoveredRegions.Contains(regionId))
                return $"Forecast access: transmitter network ({s_RepairedTransmitterCount}/{TotalTransmitters} repaired, awaiting next aurora sync)";

            float now = GetWorldHour(snapshot);
            bool fresh = GetAuroraKnowledgeVisibleUntilWorldHour(now) > now + 0.01f;
            string syncState = fresh ? "aurora-synchronized" : "aurora data expired";
            return $"Forecast access: transmitter network ({s_RepairedTransmitterCount}/{TotalTransmitters} repaired, last sync {s_SynchronizedTransmitterCount}/{TotalTransmitters}, {syncState})";
        }

        internal static string BuildHorizonLabel(WeatherRegionId regionId, WeatherSnapshot snapshot, float fullHorizonEndWorldHour)
        {
            if (IsPermanentAccess)
                return $"Forecast horizon: {GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, fullHorizonEndWorldHour)}";

            RefreshAccessState(snapshot);
            float now = GetWorldHour(snapshot);

            if (!RequiresAuroraRefresh)
            {
                if (!s_CoveredRegions.Contains(regionId)) return "Forecast horizon: unavailable";

                float visibleUntil = GetVisibleUntilWorldHour(regionId, snapshot, fullHorizonEndWorldHour);
                if (s_RepairedTransmitterCount < TotalTransmitters)
                {
                    float hours = Math.Max(0f, visibleUntil - now);
                    return $"Forecast horizon: {GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, visibleUntil)} ({hours:0.#}h)";
                }

                return $"Forecast horizon: {GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, fullHorizonEndWorldHour)}";
            }

            if (!s_CoveredRegions.Contains(regionId)) return "Forecast horizon: unavailable";
            if (!s_HasAuroraRefresh) return "Forecast horizon: awaiting aurora sync";
            if (!s_SynchronizedCoveredRegions.Contains(regionId)) return "Forecast horizon: awaiting next aurora sync";

            float visibleUntilAurora = Math.Min(fullHorizonEndWorldHour, GetAuroraKnowledgeVisibleUntilWorldHour(now));
            if (visibleUntilAurora <= now + 0.01f) return "Forecast horizon: expired - awaiting next aurora";

            float remainingHours = Math.Max(0f, visibleUntilAurora - now);
            bool auroraActive = IsCurrentSynchronizedAuroraActive(now);
            string suffix = auroraActive ? $"{remainingHours:0.#}h, refreshing during aurora" : $"{remainingHours:0.#}h remaining";
            return $"Forecast horizon: {GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, visibleUntilAurora)} ({suffix})";
        }

        internal static string BuildUnavailableMessage(WeatherRegionId regionId, WeatherSnapshot snapshot)
        {
            if (IsPermanentAccess) return "Forecast unavailable.";

            RefreshAccessState(snapshot);
            bool isCurrentRegion = snapshot.IsValid && regionId == snapshot.RegionId;
            bool repairedCoverage = s_CoveredRegions.Contains(regionId);

            if (!repairedCoverage)
            {
                if (isCurrentRegion)
                    return "Future forecast unavailable. The current weather can be observed locally, but this region is not covered by a repaired transmitter.";
                return "Future forecast unavailable. This region is not covered by a repaired transmitter.";
            }

            if (!RequiresAuroraRefresh)
                return "Forecast unavailable.";

            if (!s_HasAuroraRefresh)
            {
                if (isCurrentRegion)
                    return "Future forecast unavailable. The current weather can be observed locally, but repaired transmitters only receive forecast data while an aurora is powering the network.";
                return "Future forecast unavailable. This repaired transmitter network has not received an aurora-powered forecast update yet.";
            }

            if (!s_SynchronizedCoveredRegions.Contains(regionId))
            {
                if (isCurrentRegion)
                    return "Future forecast unavailable. The current weather can be observed locally, but this transmitter was repaired after the last aurora update. Wait for the next aurora.";
                return "Future forecast unavailable. This transmitter was repaired after the last aurora update. Wait for the next aurora to synchronize this region.";
            }

            float now = GetWorldHour(snapshot);
            if (GetAuroraKnowledgeVisibleUntilWorldHour(now) <= now + 0.01f)
            {
                if (isCurrentRegion)
                    return "Future forecast unavailable. The current weather can be observed locally, but the last aurora forecast has expired. Wait for the next aurora to refresh the network.";
                return "Future forecast unavailable. The last aurora forecast for this region has expired. Wait for the next aurora to refresh the network.";
            }

            return "Forecast unavailable.";
        }

        internal static void InvalidateTransmitterCache()
        {
            s_NextRefreshRealtime = 0f;
        }

        private static void RefreshAccessState(WeatherSnapshot snapshot)
        {
            RefreshTransmitterState();
            if (!RequiresAuroraRefresh) return;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown)
            {
                EnsureAuroraKnowledgeLoaded();
                return;
            }

            EnsureAuroraKnowledgeLoaded();
            RefreshAuroraKnowledge(snapshot);
        }

        private static void RefreshTransmitterState()
        {
            if (IsPermanentAccess) return;
            if (Time.realtimeSinceStartup < s_NextRefreshRealtime) return;
            s_NextRefreshRealtime = Time.realtimeSinceStartup + RefreshIntervalSeconds;

            try
            {
                TransmitterManager manager = GameManager.GetTransmitterManager();
                if (manager == null)
                {
                    CommitTransmitterState(new HashSet<WeatherRegionId>(), 0);
                    return;
                }

                EnsureTransmitterGuidMap(manager);

                string serialized = TransmitterManager.SerializeAll() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(serialized) || serialized == "{}")
                {
                    CommitTransmitterState(new HashSet<WeatherRegionId>(), 0);
                    return;
                }

                Dictionary<string, SerializedTransmitterState>? states = JsonSerializer.Deserialize<Dictionary<string, SerializedTransmitterState>>(serialized);
                if (states == null)
                {
                    CommitTransmitterState(new HashSet<WeatherRegionId>(), 0);
                    return;
                }

                HashSet<WeatherRegionId> repairedRegions = new HashSet<WeatherRegionId>();
                int repairedCount = 0;

                foreach (KeyValuePair<string, SerializedTransmitterState> pair in states)
                {
                    if (pair.Value == null || !pair.Value.IsFixed) continue;
                    repairedCount++;

                    if (!s_TransmitterRegionsByGuid.TryGetValue(pair.Key, out WeatherRegionId homeRegion))
                    {
                        if (s_LoggedUnmappedRepairedGuids.Add(pair.Key))
                            Core.Warn($"[ForecastAccess] Repaired transmitter GUID is not mapped yet | Guid={pair.Key}", true);
                        continue;
                    }

                    if (IsTransmitterHomeRegion(homeRegion)) repairedRegions.Add(homeRegion);
                }

                CommitTransmitterState(repairedRegions, repairedCount);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("ForecastAccessManager.RefreshTransmitterState.3", "ForecastAccessManager could not read vanilla transmitter state.", caughtException);
                s_NextRefreshRealtime = Math.Min(s_NextRefreshRealtime, Time.realtimeSinceStartup + 0.25f);
            }
        }

        private static void EnsureTransmitterGuidMap(TransmitterManager manager)
        {
            if (s_HasCompleteTransmitterGuidMap || manager == null) return;

            var transmitters = manager.GetAllTransmitters();
            if (transmitters == null) return;

            Il2CppObjectBase? transmitterCollectionObject = (object)transmitters as Il2CppObjectBase;
            if (transmitterCollectionObject == null) return;

            var collection = transmitterCollectionObject.TryCast<Il2CppSystem.Collections.Generic.ICollection<TransmitterData>>();
            if (collection == null) return;

            int count = collection.Count;
            if (count <= 0) return;
            if (count > 32) count = 32;

            Il2CppReferenceArray<TransmitterData> snapshot = new Il2CppReferenceArray<TransmitterData>(count);
            collection.CopyTo(snapshot, 0);

            for (int i = 0; i < count; i++)
            {
                TransmitterData transmitter = snapshot[i];
                if (transmitter == null || string.IsNullOrWhiteSpace(transmitter.Guid)) continue;

                WeatherRegionId homeRegion = ResolveTransmitterHomeRegion(transmitter);
                if (!IsTransmitterHomeRegion(homeRegion)) continue;

                s_TransmitterRegionsByGuid[transmitter.Guid] = homeRegion;
            }

            s_HasCompleteTransmitterGuidMap = s_TransmitterRegionsByGuid.Count >= TotalTransmitters;
            LogGuidMapIfChanged();
        }

        private static void LogGuidMapIfChanged()
        {
            string signature = string.Join(",", s_TransmitterRegionsByGuid.OrderBy(pair => pair.Value).Select(pair => $"{pair.Key}:{pair.Value}"));
            if (string.Equals(signature, s_LastLoggedGuidMapSignature, StringComparison.Ordinal)) return;

            s_LastLoggedGuidMapSignature = signature;
            string mapped = s_TransmitterRegionsByGuid.Count == 0
                ? "none"
                : string.Join(", ", s_TransmitterRegionsByGuid.OrderBy(pair => pair.Value).Select(pair => $"{pair.Value}={pair.Key}"));
            Core.Log($"[ForecastAccess] Transmitter GUID map | Mapped={s_TransmitterRegionsByGuid.Count}/{TotalTransmitters} | {mapped}");
        }

        private static void CommitTransmitterState(HashSet<WeatherRegionId> repairedRegions, int repairedCount)
        {
            s_RepairedTransmitterRegions.Clear();
            foreach (WeatherRegionId region in repairedRegions) s_RepairedTransmitterRegions.Add(region);

            s_CoveredRegions.Clear();
            foreach (WeatherRegionId homeRegion in s_RepairedTransmitterRegions) AddCoverage(s_CoveredRegions, homeRegion);

            s_RepairedTransmitterCount = Math.Clamp(repairedCount, 0, TotalTransmitters);

            string regionSignature = string.Join(",", s_RepairedTransmitterRegions.OrderBy(region => (int)region));
            string signature = $"{s_RepairedTransmitterCount}|{regionSignature}";
            if (string.Equals(signature, s_LastLoggedNetworkSignature, StringComparison.Ordinal)) return;

            s_LastLoggedNetworkSignature = signature;
            string repaired = s_RepairedTransmitterRegions.Count == 0
                ? "none"
                : string.Join(", ", s_RepairedTransmitterRegions.OrderBy(region => (int)region));
            Core.Log($"[ForecastAccess] Transmitter network updated | Repaired={s_RepairedTransmitterCount}/{TotalTransmitters} | Sites={repaired}");
        }

        private static void RefreshAuroraKnowledge(WeatherSnapshot snapshot)
        {
            if (!RequiresAuroraRefresh || !snapshot.IsValid) return;

            float now = snapshot.WorldHour;
            float previousObservation = s_LastAuroraObservationWorldHour;
            s_LastAuroraObservationWorldHour = now;

            if (!TryGetActiveAurora(now, out NightEventWindow activeAurora))
            {
                if (previousObservation >= 0f && now > previousObservation + 0.01f && TryGetTraversedAurora(previousObservation, now, out NightEventWindow traversedAurora))
                {
                    SynchronizeAuroraKnowledge(traversedAurora, "aurora traversed during time advance");
                }

                if (s_WasAuroraActive)
                {
                    s_WasAuroraActive = false;
                    if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
                    {
                        float visibleUntil = GetAuroraKnowledgeVisibleUntilWorldHour(now);
                        Core.Log($"[ForecastAccess] Aurora forecast update ended | Synced={s_SynchronizedTransmitterCount}/{TotalTransmitters} | VisibleUntil={visibleUntil:0.##}h");
                    }
                }

                return;
            }

            bool newAuroraWindow = !s_HasAuroraRefresh || !IsSameAuroraWindow(activeAurora, s_LastAuroraStartWorldHour, s_LastAuroraEndWorldHour);
            bool networkChanged = s_SynchronizedTransmitterCount != s_RepairedTransmitterCount || !s_SynchronizedTransmitterRegions.SetEquals(s_RepairedTransmitterRegions);
            if (newAuroraWindow || networkChanged) SynchronizeAuroraKnowledge(activeAurora, newAuroraWindow ? "aurora started" : "transmitter network changed during aurora");

            s_WasAuroraActive = true;
        }

        private static bool TryGetTraversedAurora(float fromWorldHour, float toWorldHour, out NightEventWindow traversedAurora)
        {
            traversedAurora = default;
            GlobalWeatherState state = GlobalWeatherSimulation.Current;
            if (state == null || !state.IsValid) return false;

            IReadOnlyList<NightEventWindow> windows = state.NightEventSchedule.Windows;
            bool found = false;
            for (int i = 0; i < windows.Count; i++)
            {
                NightEventWindow window = windows[i];
                if (!NightEventWindow.IsAuroraEvent(window.EventType)) continue;
                if (window.EndWorldHour <= fromWorldHour + 0.01f || window.StartWorldHour >= toWorldHour - 0.01f) continue;
                if (!found || window.EndWorldHour > traversedAurora.EndWorldHour)
                {
                    traversedAurora = window;
                    found = true;
                }
            }

            return found;
        }

        private static void SynchronizeAuroraKnowledge(NightEventWindow aurora, string reason)
        {
            bool changed = !s_HasAuroraRefresh ||
                           !IsSameAuroraWindow(aurora, s_LastAuroraStartWorldHour, s_LastAuroraEndWorldHour) ||
                           s_SynchronizedTransmitterCount != s_RepairedTransmitterCount ||
                           !s_SynchronizedTransmitterRegions.SetEquals(s_RepairedTransmitterRegions);
            if (!changed) return;

            s_HasAuroraRefresh = true;
            s_LastAuroraStartWorldHour = aurora.StartWorldHour;
            s_LastAuroraEndWorldHour = aurora.EndWorldHour;
            s_SynchronizedTransmitterCount = s_RepairedTransmitterCount;
            s_SynchronizedTransmitterRegions.Clear();
            foreach (WeatherRegionId region in s_RepairedTransmitterRegions) s_SynchronizedTransmitterRegions.Add(region);
            RebuildSynchronizedCoverage();
            SaveAuroraKnowledge(reason);
            LogAuroraKnowledgeIfChanged();
        }

        private static void EnsureAuroraKnowledgeLoaded()
        {
            if (s_AuroraKnowledgeLoadAttempted) return;
            s_AuroraKnowledgeLoadAttempted = true;

            try
            {
                string? payload = s_DataManager.Load(AuroraKnowledgeSaveSuffix);
                if (string.IsNullOrWhiteSpace(payload)) return;

                PersistedAuroraForecastKnowledge? persisted = JsonSerializer.Deserialize<PersistedAuroraForecastKnowledge>(payload);
                if (persisted == null || persisted.Version != AuroraKnowledgeSaveVersion) return;

                s_HasAuroraRefresh = persisted.HasAuroraRefresh;
                s_LastAuroraStartWorldHour = persisted.LastAuroraStartWorldHour;
                s_LastAuroraEndWorldHour = persisted.LastAuroraEndWorldHour;
                s_SynchronizedTransmitterCount = Math.Clamp(persisted.SynchronizedTransmitterCount, 0, TotalTransmitters);

                s_SynchronizedTransmitterRegions.Clear();
                if (persisted.SynchronizedTransmitterRegions != null)
                {
                    for (int i = 0; i < persisted.SynchronizedTransmitterRegions.Count; i++)
                    {
                        WeatherRegionId region = (WeatherRegionId)persisted.SynchronizedTransmitterRegions[i];
                        if (IsTransmitterHomeRegion(region)) s_SynchronizedTransmitterRegions.Add(region);
                    }
                }

                RebuildSynchronizedCoverage();
                LogAuroraKnowledgeIfChanged();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("ForecastAccessManager.EnsureAuroraKnowledgeLoaded.1", "ForecastAccessManager could not load persisted aurora forecast knowledge.", caughtException);
            }
        }

        private static void SaveAuroraKnowledge(string reason)
        {
            try
            {
                PersistedAuroraForecastKnowledge persisted = new PersistedAuroraForecastKnowledge
                {
                    Version = AuroraKnowledgeSaveVersion,
                    HasAuroraRefresh = s_HasAuroraRefresh,
                    LastAuroraStartWorldHour = s_LastAuroraStartWorldHour,
                    LastAuroraEndWorldHour = s_LastAuroraEndWorldHour,
                    SynchronizedTransmitterCount = s_SynchronizedTransmitterCount,
                    SynchronizedTransmitterRegions = s_SynchronizedTransmitterRegions.OrderBy(region => (int)region).Select(region => (int)region).ToList()
                };

                string payload = JsonSerializer.Serialize(persisted);
                if (!s_DataManager.Save(payload, AuroraKnowledgeSaveSuffix))
                {
                    Core.Warn("[ForecastAccess][ModData] Aurora forecast knowledge save returned false.");
                }
                else if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
                {
                    Core.Log($"[ForecastAccess][ModData] Saved aurora forecast knowledge ({reason}) | Aurora={s_LastAuroraStartWorldHour:0.##}->{s_LastAuroraEndWorldHour:0.##}h | Synced={s_SynchronizedTransmitterCount}/{TotalTransmitters}");
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("ForecastAccessManager.SaveAuroraKnowledge.1", "ForecastAccessManager could not save aurora forecast knowledge.", caughtException);
            }
        }

        private static void ResetAuroraKnowledgeRuntime()
        {
            s_AuroraKnowledgeLoadAttempted = false;
            s_HasAuroraRefresh = false;
            s_LastAuroraStartWorldHour = -1f;
            s_LastAuroraEndWorldHour = -1f;
            s_SynchronizedTransmitterCount = 0;
            s_SynchronizedTransmitterRegions.Clear();
            s_SynchronizedCoveredRegions.Clear();
            s_WasAuroraActive = false;
            s_LastAuroraObservationWorldHour = -1f;
            s_LastLoggedAuroraKnowledgeSignature = string.Empty;
        }

        private static void RebuildSynchronizedCoverage()
        {
            s_SynchronizedCoveredRegions.Clear();
            foreach (WeatherRegionId homeRegion in s_SynchronizedTransmitterRegions) AddCoverage(s_SynchronizedCoveredRegions, homeRegion);
        }

        private static void LogAuroraKnowledgeIfChanged()
        {
            string regions = string.Join(",", s_SynchronizedTransmitterRegions.OrderBy(region => (int)region));
            string signature = $"{s_HasAuroraRefresh}|{s_LastAuroraStartWorldHour:0.###}|{s_LastAuroraEndWorldHour:0.###}|{s_SynchronizedTransmitterCount}|{regions}";
            if (string.Equals(signature, s_LastLoggedAuroraKnowledgeSignature, StringComparison.Ordinal)) return;

            s_LastLoggedAuroraKnowledgeSignature = signature;
            string sites = s_SynchronizedTransmitterRegions.Count == 0
                ? "none"
                : string.Join(", ", s_SynchronizedTransmitterRegions.OrderBy(region => (int)region));
            Core.Log($"[ForecastAccess] Aurora forecast synchronized | Synced={s_SynchronizedTransmitterCount}/{TotalTransmitters} | Sites={sites} | Aurora={s_LastAuroraStartWorldHour:0.##}->{s_LastAuroraEndWorldHour:0.##}h");
        }

        private static bool HasFreshAuroraKnowledge(WeatherRegionId regionId, float now)
        {
            if (!s_HasAuroraRefresh) return false;
            if (!s_SynchronizedCoveredRegions.Contains(regionId)) return false;
            return GetAuroraKnowledgeVisibleUntilWorldHour(now) > now + 0.01f;
        }

        private static float GetAuroraKnowledgeVisibleUntilWorldHour(float now)
        {
            if (!s_HasAuroraRefresh) return now;

            float forecastDepth = GetForecastDepthHours(s_SynchronizedTransmitterCount);
            if (forecastDepth <= 0f) return now;

            if (IsCurrentSynchronizedAuroraActive(now)) return now + forecastDepth;
            return s_LastAuroraEndWorldHour + forecastDepth;
        }

        private static bool IsCurrentSynchronizedAuroraActive(float now)
        {
            if (!s_HasAuroraRefresh) return false;
            if (!TryGetActiveAurora(now, out NightEventWindow activeAurora)) return false;
            return IsSameAuroraWindow(activeAurora, s_LastAuroraStartWorldHour, s_LastAuroraEndWorldHour);
        }

        private static bool TryGetActiveAurora(float worldHour, out NightEventWindow activeAurora)
        {
            activeAurora = default;
            GlobalWeatherState state = GlobalWeatherSimulation.Current;
            if (!state.IsValid) return false;
            if (!state.NightEventSchedule.TryGetActive(worldHour, out NightEventWindow activeWindow)) return false;
            if (!NightEventWindow.IsAuroraEvent(activeWindow.EventType)) return false;

            activeAurora = activeWindow;
            return true;
        }

        private static bool IsSameAuroraWindow(NightEventWindow window, float startWorldHour, float endWorldHour)
        {
            return Math.Abs(window.StartWorldHour - startWorldHour) < 0.02f && Math.Abs(window.EndWorldHour - endWorldHour) < 0.02f;
        }

        private static float GetForecastDepthHours(int transmitterCount)
        {
            int safeCount = Math.Clamp(transmitterCount, 0, TotalTransmitters);
            if (safeCount >= TotalTransmitters) return GlobalWeatherSimulation.HorizonHours;
            return safeCount * ForecastHoursPerTransmitter;
        }

        private static float GetWorldHour(WeatherSnapshot snapshot)
        {
            if (snapshot.IsValid) return snapshot.WorldHour;
            if (WeatherOverhaulRuntime.CurrentSnapshot.IsValid) return WeatherOverhaulRuntime.CurrentSnapshot.WorldHour;
            if (GlobalWeatherSimulation.Current.IsValid) return GlobalWeatherSimulation.Current.GeneratedAtWorldHour;
            return 0f;
        }

        private static bool IsSaveBoundaryScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return true;
            return string.Equals(sceneName, "Empty", StringComparison.OrdinalIgnoreCase) || sceneName.StartsWith("MainMenu", StringComparison.OrdinalIgnoreCase);
        }

        private static WeatherRegionId ResolveTransmitterHomeRegion(TransmitterData transmitter)
        {
            if (transmitter == null || string.IsNullOrEmpty(transmitter.SceneName)) return WeatherRegionId.Unknown;

            string sceneName = transmitter.SceneName;
            if (TryResolveTransmitterScene(sceneName, out WeatherRegionId regionId)) return regionId;

            string parentSceneName = StripKnownChildSuffix(sceneName);
            if (!string.Equals(parentSceneName, sceneName, StringComparison.OrdinalIgnoreCase) &&
                TryResolveTransmitterScene(parentSceneName, out regionId))
            {
                return regionId;
            }

            string unresolvedKey = $"{transmitter.Guid}|{sceneName}";
            if (s_LoggedUnresolvedTransmitterScenes.Add(unresolvedKey))
                Core.Warn($"[ForecastAccess] Could not map transmitter data | Guid={transmitter.Guid} | Scene={sceneName}", true);

            return WeatherRegionId.Unknown;
        }

        private static bool TryResolveTransmitterScene(string sceneName, out WeatherRegionId regionId)
        {
            regionId = WeatherRegionId.Unknown;
            if (string.IsNullOrEmpty(sceneName)) return false;

            if (RegionWeatherGraph.TryGetBySceneName(sceneName, out WeatherRegionDefinition exact) && IsTransmitterHomeRegion(exact.Id))
            {
                regionId = exact.Id;
                return true;
            }

            try
            {
                string regionName = InterfaceManager.GetRegionForScene(sceneName) ?? string.Empty;
                if (RegionWeatherGraph.TryGetByRegionAlias(regionName, out WeatherRegionDefinition mapped) && IsTransmitterHomeRegion(mapped.Id))
                {
                    regionId = mapped.Id;
                    return true;
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("ForecastAccessManager.TryResolveTransmitterScene.1", "ForecastAccessManager could not map a vanilla transmitter scene to a weather region.", caughtException);
            }

            return false;
        }

        private static string StripKnownChildSuffix(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return string.Empty;

            string[] suffixes =
            {
                "_STORY",
                "_SANDBOX",
                "_MISSION",
                "_SIDEMISSION",
                "_THREEDAYSOFNIGHT",
                "_SANDBOX_WILDLIFE",
                "_DEADMANWALKING",
                "_DARKWALKER",
                "_DLC01",
                "_WILDLIFE",
                "_VFX"
            };

            for (int i = 0; i < suffixes.Length; i++)
            {
                string suffix = suffixes[i];
                if (sceneName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return sceneName.Substring(0, sceneName.Length - suffix.Length);
            }

            return sceneName;
        }

        private static bool IsTransmitterHomeRegion(WeatherRegionId regionId)
        {
            return regionId == WeatherRegionId.FA ||
                   regionId == WeatherRegionId.FM ||
                   regionId == WeatherRegionId.MT ||
                   regionId == WeatherRegionId.PV ||
                   regionId == WeatherRegionId.DP ||
                   regionId == WeatherRegionId.BRM;
        }

        private static void AddCoverage(HashSet<WeatherRegionId> target, WeatherRegionId transmitterRegion)
        {
            switch (transmitterRegion)
            {
                case WeatherRegionId.FA:
                    Add(target, WeatherRegionId.FA, WeatherRegionId.TP, WeatherRegionId.FRBL, WeatherRegionId.ZOC, WeatherRegionId.SP);
                    break;
                case WeatherRegionId.FM:
                    Add(target, WeatherRegionId.FM, WeatherRegionId.BR, WeatherRegionId.BI, WeatherRegionId.Rav);
                    break;
                case WeatherRegionId.MT:
                    Add(target, WeatherRegionId.MT, WeatherRegionId.HRV);
                    break;
                case WeatherRegionId.PV:
                    Add(target, WeatherRegionId.PV, WeatherRegionId.ML, WeatherRegionId.WR, WeatherRegionId.Rav);
                    break;
                case WeatherRegionId.DP:
                    Add(target, WeatherRegionId.DP, WeatherRegionId.CH, WeatherRegionId.CRH, WeatherRegionId.Rav);
                    break;
                case WeatherRegionId.BRM:
                    Add(target, WeatherRegionId.BRM, WeatherRegionId.TWM, WeatherRegionId.AC, WeatherRegionId.KP_North, WeatherRegionId.KP_South);
                    break;
            }
        }

        private static void Add(HashSet<WeatherRegionId> target, params WeatherRegionId[] regions)
        {
            for (int i = 0; i < regions.Length; i++) target.Add(regions[i]);
        }
    }
}