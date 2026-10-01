using WeatherOverhaul.UI;
using WeatherOverhaul.Weather;

namespace WeatherOverhaul
{
    internal static class WeatherOverhaulRuntime
    {
        private const float SnapshotRefreshSeconds = 0.5f;
        private const float StableStageLogIntervalHours = 3f;
        private const float WeatherSelectionLogIntervalHours = 1f;
        private const float ForecastEnforcementIntervalSeconds = 0.5f;
        private const float SceneWeatherAuthoritySeconds = 8.0f;
        private const float DestinationWeatherAuthoritySeconds = 45.0f;
        private const float SaveLoadClockMaximumWaitSeconds = 10.0f;
        private const float SaveLoadClockSettleSeconds = 0.75f;
        private const float SaveLoadClockMeaningfulChangeHours = 0.25f;
        private const float SaveLoadClockCandidateToleranceHours = 0.08f;

        private static float s_NextSnapshotRefreshRealtime;
        private static string s_LastSceneName = string.Empty;
        private static WeatherRegionId s_LastRegionId = WeatherRegionId.Unknown;
        private static bool s_Initialized;
        private static bool s_MainMenuSuspended;
        private static bool s_LastWeatherOverhaulEnabled;
        private static bool s_LastGlobalSimulationEnabled;
        private static bool s_LastLocalWeatherApplyEnabled;
        private static string s_LastEventSettingsSignature = string.Empty;
        private static float s_LastStageLogWorldHour = -1000f;
        private static WeatherStage s_LastStageLogStage = WeatherStage.Undefined;
        private static float s_LastCurrentStageLogWorldHour = -1000f;
        private static WeatherStage s_LastCurrentStageLogged = WeatherStage.Undefined;
        private static string s_LastCurrentStageScene = string.Empty;
        private static float s_LastVanillaSelectionLogWorldHour = -1000f;
        private static WeatherStage s_LastVanillaSelectionLogStage = WeatherStage.Undefined;
        private static string s_LastVanillaSelectionLogScene = string.Empty;
        private static bool s_LastWeatherSelectionLogWasHandled;
        private static float s_NextForecastEnforcementRealtime;
        private static string s_SceneWeatherAuthorityScene = string.Empty;
        private static WeatherRegionId s_SceneWeatherAuthorityRegion = WeatherRegionId.Unknown;
        private static float s_SceneWeatherAuthorityUntilRealtime;
        private static WeatherSnapshot s_LastStableClockSnapshot = WeatherSnapshot.Invalid;
        private static bool s_WaitingForSaveClockStabilization;
        private static float s_SaveClockStabilizationStartedRealtime;
        private static WeatherSnapshot s_SaveClockInitialSnapshot = WeatherSnapshot.Invalid;
        private static WeatherSnapshot s_SaveClockCandidateSnapshot = WeatherSnapshot.Invalid;
        private static float s_SaveClockCandidateSinceRealtime;
        private static string s_SaveClockStabilizationScene = string.Empty;
        private enum DestinationWeatherAuthorityState
        {
            None,
            Prepared,
            Loaded,
            AppliedGuard
        }

        private static DestinationWeatherAuthorityState s_DestinationWeatherAuthorityState = DestinationWeatherAuthorityState.None;
        private static string s_PendingDestinationScene = string.Empty;
        private static WeatherRegionId s_PendingDestinationRegion = WeatherRegionId.Unknown;
        private static float s_PendingDestinationWorldHour = -1f;
        private static WeatherStageId s_PendingDestinationStageId = WeatherStageId.Undefined;
        private static WeatherStage s_PendingDestinationStage = WeatherStage.Undefined;
        private static float s_PendingDestinationUntilRealtime;
        private static string s_LastDestinationAuthorityLogKey = string.Empty;
        private static float s_LastDestinationAuthorityLogRealtime;

        internal static WeatherSnapshot CurrentSnapshot { get; private set; } = WeatherSnapshot.Invalid;
        internal static bool IsGameplayRuntimeReady => !s_MainMenuSuspended && !s_WaitingForSaveClockStabilization && GameplaySceneState.IsGameplaySceneActive();

        private static bool HasDestinationWeatherAuthority => s_DestinationWeatherAuthorityState != DestinationWeatherAuthorityState.None;

        internal static void Initialize()
        {
            if (s_Initialized) return;
            s_Initialized = true;
            GlobalWeatherSimulation.Initialize();

            if (GameplaySceneState.IsMainMenuOrBootSceneActive())
            {
                EnterMainMenuSuspension("initialization");
                return;
            }

            ApplyMainSystemState(force: true);
        }

        internal static void NotifySettingsConfirmed()
        {
            if (!s_Initialized) return;
            ForecastAccessManager.InvalidateTransmitterCache();

            if (WeatherOverhaulSettingsManager.ConsumePrepareForUninstallRequest())
            {
                PrepareCurrentSaveForUninstall();
                return;
            }

            if (GameplaySceneState.IsMainMenuOrBootSceneActive()) EnterMainMenuSuspension("settings confirmed in main menu");
            else ApplyMainSystemState(force: true);

            Core.Log($"[Settings] Applied. Global simulation={(WeatherOverhaulSettingsManager.EnableGlobalSimulation ? "on" : "off")}; loaded-region control={(WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion ? "on" : "off")}; ML Logging={(WeatherOverhaulSettingsManager.MLLogging ? "on" : "off")}.", false);
        }

        internal static bool RestoreForecastNow(out string message)
        {
            CustomWeatherStageRuntime.ClearDebugStageOverride(out _);
            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            if (!snapshot.IsValid) snapshot = CurrentSnapshot;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown)
            {
                message = "Forecast override cleared, but no valid outdoor weather authority is available yet.";
                return false;
            }

            CurrentSnapshot = snapshot;
            GlobalWeatherPersistence.TryLoad(snapshot);
            if (!GlobalWeatherSimulation.EnsureUsableSimulation(snapshot, "Weather Lab restore forecast"))
            {
                message = "Forecast override cleared; forecast authority is not ready yet.";
                return false;
            }

            if (!WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion)
            {
                message = "Forecast override cleared; vanilla weather remains in control because loaded-region application is disabled.";
                return true;
            }

            WeatherDirector.Enabled = true;
            bool applied = WeatherDirector.TryApplyCurrentForecastToLoadedRegion(snapshot, "Weather Lab restore forecast", immediate: true);
            message = applied ? "Forecast restored immediately." : "Forecast override cleared; the planned stage will be retried by weather authority.";
            return applied;
        }

        private static void PrepareCurrentSaveForUninstall()
        {
            GlobalWeatherDebugUi.Show = false;
            CustomWeatherStageRuntime.ClearDebugStageOverride(out _);
            CustomWeatherStageRuntime.ClearForecastOverridesIfAny();
            WeatherDirector.RestoreControlledWeatherSetTimings();
            WeatherDirector.Enabled = false;
            VanillaAuroraAuthority.Release();

            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            if (!snapshot.IsValid) snapshot = CurrentSnapshot;
            if (snapshot.IsValid && snapshot.RegionId != WeatherRegionId.Unknown)
            {
                GlobalWeatherSimulation.Rebuild(snapshot, "prepare save for uninstall");
            }

            Core.Warn("[Uninstall] WeatherOverhaul runtime overrides and controlled WeatherSet timing were restored. Save the game now and quit before removing the mod. Do not force another Weather Lab stage before saving.");
        }

        internal static void NotifyLoadSceneActivated(LoadScene loadScene, string reason)
        {
            if (loadScene == null) return;
            if (s_WaitingForSaveClockStabilization) return;
            CustomWeatherStageRuntime.PrepareForSceneTransition();
            if (CustomWeatherStageRuntime.IsActive) return;
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul) return;
            if (!WeatherOverhaulSettingsManager.EnableGlobalSimulation) return;
            if (!WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion) return;

            string destinationScene = GetSceneToLoad(loadScene);
            if (string.IsNullOrEmpty(destinationScene)) return;

            WeatherRegionDefinition destination = SceneRegionMapper.Resolve(destinationScene);
            if (destination.Id == WeatherRegionId.Unknown) return;

            WeatherSnapshot sourceSnapshot = GetBestSourceSnapshotForDestinationAuthority(reason);
            if (!sourceSnapshot.IsValid || sourceSnapshot.RegionId == WeatherRegionId.Unknown) return;
            if (sourceSnapshot.RegionId == destination.Id && string.Equals(sourceSnapshot.SceneName, destinationScene, StringComparison.Ordinal)) return;

            GlobalWeatherPersistence.TryLoad(sourceSnapshot);
            sourceSnapshot = ResolveSceneAuthoritySnapshot(sourceSnapshot, reason + " destination authority source");
            if (!GlobalWeatherSimulation.EnsureUsableSimulation(sourceSnapshot, reason + " destination authority preparation")) return;
            if (!GlobalWeatherSimulation.EnsureRegionTimeline(sourceSnapshot, destination.Id, reason + " destination authority preparation")) return;
            if (!GlobalWeatherSimulation.TryGetActivationPlan(destination.Id, sourceSnapshot.WorldHour, out WeatherActivationPlan plan)) return;
            if (plan.Stage == WeatherStage.Undefined) return;

            if (HasDestinationWeatherAuthority &&
                s_PendingDestinationRegion == destination.Id &&
                string.Equals(s_PendingDestinationScene, destinationScene, StringComparison.Ordinal) &&
                s_PendingDestinationStageId == plan.StageId &&
                Math.Abs(s_PendingDestinationWorldHour - sourceSnapshot.WorldHour) <= 0.05f)
            {
                s_PendingDestinationUntilRealtime = Math.Max(s_PendingDestinationUntilRealtime, Time.realtimeSinceStartup + DestinationWeatherAuthoritySeconds);
                s_SceneWeatherAuthorityUntilRealtime = Math.Max(s_SceneWeatherAuthorityUntilRealtime, Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds);
                return;
            }

            s_DestinationWeatherAuthorityState = DestinationWeatherAuthorityState.Prepared;
            s_PendingDestinationScene = destinationScene;
            s_PendingDestinationRegion = destination.Id;
            s_PendingDestinationWorldHour = sourceSnapshot.WorldHour;
            s_PendingDestinationStageId = plan.StageId;
            s_PendingDestinationStage = plan.Stage;
            s_PendingDestinationUntilRealtime = Time.realtimeSinceStartup + DestinationWeatherAuthoritySeconds;

            s_SceneWeatherAuthorityScene = destinationScene;
            s_SceneWeatherAuthorityRegion = destination.Id;
            s_SceneWeatherAuthorityUntilRealtime = Math.Max(s_SceneWeatherAuthorityUntilRealtime, Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds);

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[WeatherDirector] Prepared destination weather authority before scene load | Reason={reason} | From={sourceSnapshot.SceneName} -> {sourceSnapshot.RegionShortName}/{sourceSnapshot.RegionDisplayName} | To={destinationScene} -> {destination.ShortName}/{destination.DisplayName} | Planned={plan.Definition.DisplayName} | ForecastClock={sourceSnapshot.WorldHour:0.##}h");
            }
        }

        internal static void NotifySceneWasLoaded(string sceneName)
        {
            s_NextSnapshotRefreshRealtime = 0f;
            ForecastAccessManager.NotifySceneWasLoaded(sceneName);
            CustomWeatherStageRuntime.NotifySceneWasLoaded(sceneName);

            if (GameplaySceneState.IsMainMenuOrBootSceneName(sceneName))
            {
                EnterMainMenuSuspension("scene loaded: " + sceneName);
                return;
            }

            if (s_MainMenuSuspended && GameplaySceneState.IsGameplaySceneName(sceneName))
            {
                BeginSaveClockStabilization(sceneName);
                return;
            }

            if (s_WaitingForSaveClockStabilization) return;

            if (IsAdditiveWeatherSubsceneName(sceneName))
            {
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[WeatherDirector] Ignored additive non-authoritative weather subscene | Scene={sceneName}");
                return;
            }

            if (IsSaveBoundarySceneName(sceneName)) return;

            BeginSceneWeatherAuthority(sceneName);
            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[WeatherDirector] Primary scene loaded; next weather snapshot will run immediately | Scene={sceneName}");

            WeatherSnapshot snapshot = ResolveSceneAuthoritySnapshot(WeatherSnapshot.Capture(), "scene load notification");
            if (!snapshot.IsValid || IsSaveBoundaryScene(snapshot) || snapshot.RegionId == WeatherRegionId.Unknown) return;
            if (CustomWeatherStageRuntime.IsActive) return;
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul) return;

            TryApplySceneWeatherImmediately(snapshot, "scene load immediate apply");
        }

        internal static bool ShouldDeferWeatherApplicationUntilDestinationLoaded(WeatherSnapshot snapshot)
        {
            if (s_DestinationWeatherAuthorityState != DestinationWeatherAuthorityState.Prepared) return false;
            if (!snapshot.IsValid) return true;
            if (!string.IsNullOrEmpty(s_PendingDestinationScene)) return string.Equals(snapshot.SceneName, s_PendingDestinationScene, StringComparison.Ordinal);
            return s_PendingDestinationRegion != WeatherRegionId.Unknown && snapshot.RegionId == s_PendingDestinationRegion;
        }

        internal static bool ShouldApplyWeatherImmediatelyForSceneAuthority(WeatherSnapshot snapshot)
        {
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;
            if (IsPendingDestinationAuthorityForSnapshot(snapshot)) return true;
            if (Time.realtimeSinceStartup <= s_SceneWeatherAuthorityUntilRealtime)
            {
                if (string.Equals(snapshot.SceneName, s_SceneWeatherAuthorityScene, StringComparison.Ordinal)) return true;
                if (s_SceneWeatherAuthorityRegion != WeatherRegionId.Unknown && snapshot.RegionId == s_SceneWeatherAuthorityRegion) return true;
            }

            if (!string.Equals(snapshot.SceneName, s_LastSceneName, StringComparison.Ordinal)) return true;
            if (snapshot.RegionId != WeatherRegionId.Unknown && snapshot.RegionId != s_LastRegionId) return true;
            return false;
        }

        internal static WeatherSnapshot ResolveSceneAuthoritySnapshot(WeatherSnapshot snapshot, string reason)
        {
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return snapshot;
            if (TryGetPendingDestinationAuthoritySnapshot(snapshot, reason, out WeatherSnapshot pendingAuthoritySnapshot)) return pendingAuthoritySnapshot;
            if (!GlobalWeatherSimulation.IsSnapshotOlderThanCurrentForecast(snapshot, 12f)) return snapshot;

            WeatherSnapshot clockSource = s_LastStableClockSnapshot.IsValid ? s_LastStableClockSnapshot : CurrentSnapshot;
            if (clockSource.IsValid && !GlobalWeatherSimulation.IsSnapshotOlderThanCurrentForecast(clockSource, 12f) && clockSource.WorldHour > snapshot.WorldHour + 12f)
            {
                WeatherSnapshot correctedFromStableClock = snapshot.WithClockFrom(clockSource);
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
                {
                    Core.Log($"[WeatherDirector] Corrected stale scene-load authority snapshot clock | Reason={reason} | Scene={snapshot.SceneName} -> {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | Captured={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | UsingClock={clockSource.GetClockText()} ({clockSource.WorldHour:0.##}h)");
                }

                return correctedFromStableClock;
            }

            if (snapshot.WorldHour < 1f && GlobalWeatherSimulation.Current.IsValid && GlobalWeatherSimulation.Current.GeneratedAtWorldHour > snapshot.WorldHour + 12f)
            {
                WeatherSnapshot correctedFromForecast = snapshot.WithAuthorityWorldHour(GlobalWeatherSimulation.Current.GeneratedAtWorldHour);
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
                {
                    Core.Log($"[WeatherDirector] Corrected stale scene-load authority snapshot with persisted forecast clock | Reason={reason} | Scene={snapshot.SceneName} -> {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | Captured={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | UsingForecastClock={GlobalWeatherSimulation.Current.GeneratedAtWorldHour:0.##}h");
                }

                return correctedFromForecast;
            }

            return snapshot;
        }

        internal static void NoteSceneWeatherAuthorityUsed(WeatherSnapshot snapshot)
        {
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;
            if (string.IsNullOrEmpty(s_SceneWeatherAuthorityScene)) s_SceneWeatherAuthorityScene = snapshot.SceneName ?? string.Empty;
            if (s_SceneWeatherAuthorityRegion == WeatherRegionId.Unknown) s_SceneWeatherAuthorityRegion = snapshot.RegionId;
            s_SceneWeatherAuthorityUntilRealtime = Math.Max(s_SceneWeatherAuthorityUntilRealtime, Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds);
        }

        internal static void Update()
        {
            if (GameplaySceneState.IsMainMenuOrBootSceneActive())
            {
                EnterMainMenuSuspension("runtime update");
                return;
            }

            if (s_MainMenuSuspended)
            {
                if (!GameplaySceneState.IsGameplaySceneActive()) return;
                BeginSaveClockStabilization(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }

            if (s_WaitingForSaveClockStabilization)
            {
                UpdateSaveClockStabilization();
                return;
            }

            ApplyMainSystemState(force: false);

            HandleInput();
            ClearExpiredDestinationWeatherAuthority();

            if (Time.realtimeSinceStartup < s_NextSnapshotRefreshRealtime) return;
            s_NextSnapshotRefreshRealtime = Time.realtimeSinceStartup + SnapshotRefreshSeconds;

            WeatherSnapshot snapshot = ResolveSceneAuthoritySnapshot(WeatherSnapshot.Capture(), "runtime snapshot");
            CurrentSnapshot = snapshot;
            if (!snapshot.IsValid)
            {
                CustomWeatherStageRuntime.SuspendVisualsForNonGameplayScene();
                BloodMoonInfluenceRuntime.Update(WeatherSnapshot.Invalid);
                FreezingFogThinIceRuntime.ResetRuntimeCache();
                return;
            }

            if (IsSaveBoundaryScene(snapshot))
            {
                if (ShouldPreserveForecastThroughTransitionBridge(snapshot))
                {
                    if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[GlobalSim] Preserved carried forecast through transition bridge scene | Scene={snapshot.SceneName} | PendingDestination={s_PendingDestinationScene} -> {RegionWeatherGraph.Get(s_PendingDestinationRegion).ShortName}/{RegionWeatherGraph.Get(s_PendingDestinationRegion).DisplayName}");
                }
                else
                {
                    GlobalWeatherSimulation.Clear("save boundary scene " + snapshot.SceneName);
                    GlobalWeatherPersistence.ResetLoadAttempt();
                    FreezingFogThinIceRuntime.ResetLoadAttempt();
                    ClearPendingDestinationWeatherAuthority();
                    s_SceneWeatherAuthorityScene = string.Empty;
                    s_SceneWeatherAuthorityRegion = WeatherRegionId.Unknown;
                    s_SceneWeatherAuthorityUntilRealtime = 0f;
                    s_LastStableClockSnapshot = WeatherSnapshot.Invalid;
                }

                CustomWeatherStageRuntime.SuspendVisualsForNonGameplayScene();
                BloodMoonInfluenceRuntime.Update(WeatherSnapshot.Invalid);
                FreezingFogThinIceRuntime.ResetRuntimeCache();
                Remember(snapshot);
                return;
            }

            if (IsAdditiveWeatherSubsceneName(snapshot.SceneName))
            {
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[WeatherDirector] Skipped runtime weather application while an additive subscene was active | Scene={snapshot.SceneName}");
                return;
            }

            if (WeatherOverhaulSettingsManager.EnableWeatherOverhaul)
            {
                bool sceneChanged = !string.Equals(snapshot.SceneName, s_LastSceneName, StringComparison.Ordinal);
                bool regionChanged = snapshot.RegionId != WeatherRegionId.Unknown && snapshot.RegionId != s_LastRegionId;
                if (sceneChanged || regionChanged)
                {
                    bool appliedImmediateSceneWeather = TryApplySceneWeatherImmediately(snapshot, sceneChanged ? "scene load immediate apply" : "mapped region immediate apply");
                    if (appliedImmediateSceneWeather)
                    {
                        WeatherSnapshot refreshedSnapshot = ResolveSceneAuthoritySnapshot(WeatherSnapshot.Capture(), "post scene immediate apply refresh");
                        if (refreshedSnapshot.IsValid) snapshot = refreshedSnapshot;
                        CurrentSnapshot = snapshot;
                    }
                }
            }

            UpdateCurrentStageLogger(snapshot);
            UpdateSceneAndRegionLogs(snapshot);

            CustomWeatherStageRuntime.Update(snapshot);
            BloodMoonInfluenceRuntime.Update(snapshot);
            ForecastAccessManager.Update(snapshot);
            FreezingFogThinIceRuntime.Update(snapshot);
            if (CustomWeatherStageRuntime.IsActive)
            {
                Remember(snapshot);
                RememberStableClock(snapshot);
                return;
            }

            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul)
            {
                Remember(snapshot);
                return;
            }

            GlobalWeatherSimulation.Update(snapshot);
            EnforceCurrentForecastIfNeeded(snapshot);

            Remember(snapshot);
            RememberStableClock(snapshot);
        }

        internal static void RebuildGlobalSimulation(bool userRequested)
        {
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul) return;
            if (!WeatherOverhaulSettingsManager.EnableGlobalSimulation) return;

            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            CurrentSnapshot = snapshot;
            if (!snapshot.IsValid) return;

            if (snapshot.RegionId == WeatherRegionId.Unknown)
            {
                if (userRequested) Core.Warn($"[GlobalSim] Cannot rebuild global simulation: scene {snapshot.SceneName} is not mapped to a weather region.");
                return;
            }

            GlobalWeatherSimulation.Rebuild(snapshot, userRequested ? "manual rebuild" : "runtime rebuild");
        }

        internal static void NotifyWeatherStageActivated(WeatherStage stage, float durationHours, float transitionHours, float elapsedHours)
        {
            if (!GameplaySceneState.IsGameplaySceneActive()) return;
            if (!WeatherOverhaulSettingsManager.LogCurrentWeatherStage) return;

            WeatherSnapshot snapshot = ResolveSceneAuthoritySnapshot(WeatherSnapshot.Capture(), "stage activation log");
            float now = snapshot.IsValid ? snapshot.WorldHour : -1f;
            if (stage == s_LastStageLogStage && Math.Abs(now - s_LastStageLogWorldHour) < 0.02f) return;

            s_LastStageLogStage = stage;
            s_LastStageLogWorldHour = now;
            Core.Log($"[CurrentStage] Activated {WeatherStageFormatter.ToDisplayName(stage)} | Duration={durationHours:0.##}h | Transition={transitionHours:0.##}h | Elapsed={elapsedHours:0.##}h");
        }

        internal static void NotifyWeatherSelectionCompleted(bool handledByWeatherOverhaul)
        {
            if (!WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) return;
            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            if (!snapshot.IsValid) return;
            if (snapshot.Stage == s_LastVanillaSelectionLogStage && string.Equals(snapshot.SceneName, s_LastVanillaSelectionLogScene, StringComparison.Ordinal) && handledByWeatherOverhaul == s_LastWeatherSelectionLogWasHandled && Math.Abs(snapshot.WorldHour - s_LastVanillaSelectionLogWorldHour) < WeatherSelectionLogIntervalHours) return;

            s_LastVanillaSelectionLogStage = snapshot.Stage;
            s_LastVanillaSelectionLogScene = snapshot.SceneName;
            s_LastVanillaSelectionLogWorldHour = snapshot.WorldHour;
            s_LastWeatherSelectionLogWasHandled = handledByWeatherOverhaul;

            string source = handledByWeatherOverhaul ? "WeatherOverhaul" : "vanilla";
            Core.Log($"[WeatherHook] ChooseNextWeatherSet completed by {source} | {snapshot.GetClockText()} | Scene={snapshot.SceneName} -> {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | Stage={snapshot.StageLabel}");
        }

        private static void ApplyMainSystemState(bool force)
        {
            if (s_WaitingForSaveClockStabilization)
            {
                WeatherDirector.Enabled = false;
                VanillaAuroraAuthority.Release();
                return;
            }

            bool enabled = WeatherOverhaulSettingsManager.EnableWeatherOverhaul;
            bool globalSimulationEnabled = WeatherOverhaulSettingsManager.EnableGlobalSimulation;
            bool localWeatherApplyEnabled = enabled && globalSimulationEnabled && WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion;
            string eventSettingsSignature = WeatherOverhaulSettingsManager.EventSettingsSignature;

            if (!force &&
                enabled == s_LastWeatherOverhaulEnabled &&
                globalSimulationEnabled == s_LastGlobalSimulationEnabled &&
                localWeatherApplyEnabled == s_LastLocalWeatherApplyEnabled &&
                eventSettingsSignature == s_LastEventSettingsSignature)
            {
                return;
            }

            bool eventSettingsChanged = eventSettingsSignature != s_LastEventSettingsSignature;
            s_LastWeatherOverhaulEnabled = enabled;
            s_LastGlobalSimulationEnabled = globalSimulationEnabled;
            s_LastLocalWeatherApplyEnabled = localWeatherApplyEnabled;
            s_LastEventSettingsSignature = eventSettingsSignature;

            WeatherDirector.Enabled = localWeatherApplyEnabled;
            if (!localWeatherApplyEnabled)
            {
                VanillaAuroraAuthority.Release();
                WeatherDirector.RestoreControlledWeatherSetTimings();
                CustomWeatherStageRuntime.ClearDebugStageOverride(out _);
                CustomWeatherStageRuntime.ClearForecastOverridesIfAny();
            }
            if (!enabled || !globalSimulationEnabled || !WeatherOverhaulSettingsManager.Debug) GlobalWeatherDebugUi.Show = false;

            if (enabled)
            {
                Core.Log("[Settings] WeatherOverhaul 1.0.0 enabled | GlobalSimulation=" + (WeatherOverhaulSettingsManager.EnableGlobalSimulation ? "on" : "off") + " | LocalApply=" + (WeatherDirector.Enabled ? "on" : "off") + " | UnmappedRegions=" + WeatherOverhaulSettingsManager.UnmappedRegionBehavior + " | Transition=" + (WeatherOverhaulSettingsManager.WeatherStageTransitionHours * 60f).ToString("0") + "min | DurationPreset=" + WeatherOverhaulSettingsManager.StageDurationPreset + " | AuroraChance=" + WeatherOverhaulSettingsManager.AuroraChancePercent.ToString("0.#") + "% | BloodMoonChance=" + WeatherOverhaulSettingsManager.BloodMoonChancePercent.ToString("0.#") + "% | BloodMoonFullMoonOnly=" + (WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon ? "on" : "off") + " | GlimmerChance=" + WeatherOverhaulSettingsManager.GlimmerFogChancePercent.ToString("0.#") + "% | CustomStages=" + WeatherOverhaulSettingsManager.CustomWeatherStageSignature + " | Debug=" + (WeatherOverhaulSettingsManager.Debug ? "on" : "off") + " | MLLogging=" + (WeatherOverhaulSettingsManager.MLLogging ? "on" : "off"));
                if (globalSimulationEnabled)
                {
                    WeatherSnapshot snapshot = WeatherSnapshot.Capture();
                    if (snapshot.IsValid && snapshot.RegionId != WeatherRegionId.Unknown)
                    {
                        if (eventSettingsChanged) GlobalWeatherSimulation.Rebuild(snapshot, "global weather event settings changed");
                        else GlobalWeatherSimulation.Update(snapshot);
                    }
                    else if (snapshot.IsValid)
                    {
                        VanillaAuroraAuthority.Release();
                        CustomWeatherStageRuntime.ClearForecastOverridesIfAny();
                    }
                }
            }
            else
            {
                Core.Log("[Settings] WeatherOverhaul systems disabled. Carried forecast propagation, debug UI, and local weather application are inactive; vanilla weather is untouched.");
            }
        }


        private static bool TryApplySceneWeatherImmediately(WeatherSnapshot snapshot, string reason)
        {
            if (ShouldDeferWeatherApplicationUntilDestinationLoaded(snapshot)) return false;
            if (CustomWeatherStageRuntime.IsActive) return false;
            if (!WeatherDirector.Enabled) return false;
            snapshot = ResolveSceneAuthoritySnapshot(snapshot, reason);
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;

            if (IsDestinationWeatherAuthorityGuardingForSnapshot(snapshot) && snapshot.Stage == s_PendingDestinationStage)
            {
                WeatherSnapshot authoritySnapshot = snapshot;
                TryGetPendingDestinationAuthoritySnapshot(snapshot, reason + " matched destination guard maintenance", out authoritySnapshot);
                MaintainForecastOverridesForSnapshot(authoritySnapshot, reason + " matched destination guard maintenance", immediate: true);
                NoteSceneWeatherAuthorityUsed(authoritySnapshot);
                return true;
            }

            GlobalWeatherPersistence.TryLoad(snapshot);
            snapshot = ResolveSceneAuthoritySnapshot(snapshot, reason + " after persisted forecast load");
            if (!GlobalWeatherSimulation.EnsureUsableSimulation(snapshot, reason)) return false;

            bool applied = WeatherDirector.TryApplyCurrentForecastToLoadedRegion(snapshot, reason, immediate: true);
            if (applied)
            {
                NoteSceneWeatherAuthorityUsed(snapshot);
                NotePendingDestinationWeatherApplied(snapshot, reason);
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[WeatherDirector] Applied forecast stage immediately on scene/region change | {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | Reason={reason}");
            }

            return applied;
        }

        internal static bool TryGetPendingDestinationAuthoritySnapshot(WeatherSnapshot capturedSnapshot, string reason, out WeatherSnapshot authoritySnapshot)
        {
            authoritySnapshot = capturedSnapshot;
            if (!HasDestinationWeatherAuthority) return false;
            if (s_DestinationWeatherAuthorityState == DestinationWeatherAuthorityState.Prepared) return false;
            if (Time.realtimeSinceStartup > s_PendingDestinationUntilRealtime)
            {
                ClearExpiredDestinationWeatherAuthority();
                return false;
            }

            if (!capturedSnapshot.IsValid) return false;
            if (!IsPendingDestinationAuthorityForSnapshot(capturedSnapshot)) return false;

            WeatherRegionDefinition destination = RegionWeatherGraph.Get(s_PendingDestinationRegion);
            authoritySnapshot = capturedSnapshot.WithRegionClock(s_PendingDestinationScene, destination, s_PendingDestinationWorldHour);
            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation && ShouldLogDestinationAuthoritySnapshot(reason))
            {
                Core.Log($"[WeatherDirector] Using pending destination weather authority snapshot | State={s_DestinationWeatherAuthorityState} | Reason={reason} | Scene={capturedSnapshot.SceneName} -> {capturedSnapshot.RegionShortName}/{capturedSnapshot.RegionDisplayName} | Authority={destination.ShortName}/{destination.DisplayName} | Planned={WeatherStageCatalog.Get(s_PendingDestinationStageId).DisplayName} | ForecastClock={s_PendingDestinationWorldHour:0.##}h");
            }

            return true;
        }

        internal static void NotePendingDestinationWeatherApplied(WeatherSnapshot snapshot, string reason)
        {
            if (!IsPendingDestinationAuthorityForSnapshot(snapshot)) return;
            if (s_DestinationWeatherAuthorityState == DestinationWeatherAuthorityState.AppliedGuard) return;

            s_DestinationWeatherAuthorityState = DestinationWeatherAuthorityState.AppliedGuard;
            s_PendingDestinationUntilRealtime = Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds;
            s_SceneWeatherAuthorityScene = s_PendingDestinationScene;
            s_SceneWeatherAuthorityRegion = s_PendingDestinationRegion;
            s_SceneWeatherAuthorityUntilRealtime = Math.Max(s_SceneWeatherAuthorityUntilRealtime, s_PendingDestinationUntilRealtime);
            s_NextForecastEnforcementRealtime = 0f;

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[WeatherDirector] Destination weather authority applied; keeping short guard window for late vanilla scene activations | Reason={reason} | Destination={s_PendingDestinationScene} -> {RegionWeatherGraph.Get(s_PendingDestinationRegion).ShortName}/{RegionWeatherGraph.Get(s_PendingDestinationRegion).DisplayName} | Planned={WeatherStageCatalog.Get(s_PendingDestinationStageId).DisplayName} | Guard={SceneWeatherAuthoritySeconds:0.#}s");
            }
        }

        private static void EnforceCurrentForecastIfNeeded(WeatherSnapshot snapshot)
        {
            if (!WeatherDirector.Enabled) return;
            if (!GlobalWeatherSimulation.Current.IsValid) return;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;
            if (IsDestinationWeatherAuthorityGuardingForSnapshot(snapshot))
            {
                EnforceDestinationWeatherGuardIfNeeded(snapshot);
                return;
            }

            if (Time.realtimeSinceStartup < s_NextForecastEnforcementRealtime) return;

            if (!GlobalWeatherSimulation.TryGetActivationPlan(snapshot.RegionId, snapshot.WorldHour, out WeatherActivationPlan plan)) return;
            WeatherStage expectedStage = plan.Stage;
            if (expectedStage == WeatherStage.Undefined) return;
            if (snapshot.Stage == expectedStage)
            {
                CustomWeatherStageRuntime.ApplyForecastPlan(plan, immediate: false, suppressPrecipitationVisuals: snapshot.IsIndoorEnvironment, suppressOutdoorVisuals: snapshot.IsIndoorEnvironment);
                return;
            }

            bool applied = WeatherDirector.TryApplyCurrentForecastToLoadedRegion(snapshot, "forecast mismatch enforcement", immediate: false);
            s_NextForecastEnforcementRealtime = Time.realtimeSinceStartup + (applied ? ForecastEnforcementIntervalSeconds : Math.Max(ForecastEnforcementIntervalSeconds, 2.0f));
        }

        private static void EnforceDestinationWeatherGuardIfNeeded(WeatherSnapshot snapshot)
        {
            if (Time.realtimeSinceStartup < s_NextForecastEnforcementRealtime) return;
            if (!TryGetPendingDestinationAuthoritySnapshot(snapshot, "destination weather guard enforcement", out WeatherSnapshot authoritySnapshot)) return;

            bool applied;
            if (snapshot.Stage == s_PendingDestinationStage)
            {
                applied = MaintainForecastOverridesForSnapshot(authoritySnapshot, "destination weather guard matched-stage maintenance", immediate: true);
            }
            else
            {
                applied = WeatherDirector.TryApplyCurrentForecastToLoadedRegion(authoritySnapshot, "destination weather guard enforcement", immediate: true);
            }

            s_NextForecastEnforcementRealtime = Time.realtimeSinceStartup + (applied ? ForecastEnforcementIntervalSeconds : Math.Max(ForecastEnforcementIntervalSeconds, 2.0f));
        }

        private static bool MaintainForecastOverridesForSnapshot(WeatherSnapshot snapshot, string reason, bool immediate)
        {
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;
            if (CustomWeatherStageRuntime.IsActive) return false;

            GlobalWeatherPersistence.TryLoad(snapshot);
            snapshot = ResolveSceneAuthoritySnapshot(snapshot, reason + " after persisted forecast load");
            if (!GlobalWeatherSimulation.EnsureUsableSimulation(snapshot, reason)) return false;
            if (!GlobalWeatherSimulation.TryGetActivationPlan(snapshot.RegionId, snapshot.WorldHour, out WeatherActivationPlan plan)) return false;
            if (plan.Stage == WeatherStage.Undefined) return false;

            CustomWeatherStageRuntime.ApplyForecastPlan(plan, immediate, suppressPrecipitationVisuals: snapshot.IsIndoorEnvironment, suppressOutdoorVisuals: snapshot.IsIndoorEnvironment);
            return true;
        }

        private static void BeginSceneWeatherAuthority(string sceneName)
        {
            if (HasDestinationWeatherAuthority && !string.IsNullOrEmpty(sceneName))
            {
                WeatherRegionDefinition loadedDefinition = SceneRegionMapper.Resolve(sceneName);
                bool matchesPendingDestination = !string.IsNullOrEmpty(s_PendingDestinationScene)
                    ? string.Equals(sceneName, s_PendingDestinationScene, StringComparison.Ordinal)
                    : loadedDefinition.Id != WeatherRegionId.Unknown && loadedDefinition.Id == s_PendingDestinationRegion;
                if (matchesPendingDestination)
                {
                    if (s_DestinationWeatherAuthorityState == DestinationWeatherAuthorityState.Prepared)
                    {
                        s_DestinationWeatherAuthorityState = DestinationWeatherAuthorityState.Loaded;
                    }

                    s_SceneWeatherAuthorityScene = s_PendingDestinationScene;
                    s_SceneWeatherAuthorityRegion = s_PendingDestinationRegion;
                    s_SceneWeatherAuthorityUntilRealtime = Math.Max(s_SceneWeatherAuthorityUntilRealtime, Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds);
                    return;
                }
            }

            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            if (snapshot.IsValid && snapshot.RegionId != WeatherRegionId.Unknown)
            {
                s_SceneWeatherAuthorityScene = snapshot.SceneName ?? string.Empty;
                s_SceneWeatherAuthorityRegion = snapshot.RegionId;
                s_SceneWeatherAuthorityUntilRealtime = Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds;
                return;
            }

            if (string.IsNullOrEmpty(sceneName)) return;
            WeatherRegionDefinition definition = SceneRegionMapper.Resolve(sceneName);
            if (definition.Id == WeatherRegionId.Unknown && s_SceneWeatherAuthorityRegion != WeatherRegionId.Unknown)
            {
                s_SceneWeatherAuthorityUntilRealtime = Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds;
                return;
            }

            s_SceneWeatherAuthorityScene = sceneName;
            s_SceneWeatherAuthorityRegion = definition.Id;
            s_SceneWeatherAuthorityUntilRealtime = Time.realtimeSinceStartup + SceneWeatherAuthoritySeconds;
        }

        private static void UpdateCurrentStageLogger(WeatherSnapshot snapshot)
        {
            if (!WeatherOverhaulSettingsManager.LogCurrentWeatherStage) return;

            bool sceneChanged = !string.Equals(snapshot.SceneName, s_LastCurrentStageScene, StringComparison.Ordinal);
            bool stageChanged = snapshot.Stage != s_LastCurrentStageLogged;
            bool intervalElapsed = snapshot.WorldHour - s_LastCurrentStageLogWorldHour >= StableStageLogIntervalHours;
            if (!sceneChanged && !stageChanged && !intervalElapsed) return;

            string transition = stageChanged ? $"{WeatherStageFormatter.ToDisplayName(s_LastCurrentStageLogged)} -> {snapshot.StageLabel}" : snapshot.StageLabel;
            Core.Log($"[CurrentStage] {snapshot.GetClockText()} | {transition} | Temp={snapshot.TemperatureCelsius:0.#}C | Wind={snapshot.WindStrength} {snapshot.WindSpeedMph:0.#}mph | Scene={snapshot.SceneName} -> {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | {(snapshot.IsIndoorEnvironment ? "Indoor" : "Outdoor")}");

            s_LastCurrentStageLogged = snapshot.Stage;
            s_LastCurrentStageLogWorldHour = snapshot.WorldHour;
            s_LastCurrentStageScene = snapshot.SceneName;
        }

        private static void UpdateSceneAndRegionLogs(WeatherSnapshot snapshot)
        {
            bool sceneChanged = !string.Equals(snapshot.SceneName, s_LastSceneName, StringComparison.Ordinal);
            bool regionChanged = snapshot.RegionId != s_LastRegionId;
            if (!sceneChanged && !regionChanged) return;

            if (snapshot.RegionId == WeatherRegionId.Unknown)
            {
                if (!WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) return;
                if (IsIgnoredNonWeatherScene(snapshot))
                {
                    Core.Log($"[RegionMap] Ignored non-weather scene | Scene={snapshot.SceneName} | Stage={snapshot.StageLabel} | {(snapshot.IsIndoorEnvironment ? "Indoor" : "Outdoor")}");
                    return;
                }

                Core.Warn($"[RegionMap] Unknown weather region mapping | Scene={snapshot.SceneName} | Stage={snapshot.StageLabel} | {(snapshot.IsIndoorEnvironment ? "Indoor" : "Outdoor")}");
                return;
            }

            Core.Log($"[RegionMap] Scene={snapshot.SceneName} -> {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | Stage={snapshot.StageLabel} | {(snapshot.IsIndoorEnvironment ? "Indoor" : "Outdoor")}");
        }

        private static void HandleInput()
        {
            if (!GameplaySceneState.IsGameplaySceneActive()) return;
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul) return;
            if (WeatherMapOverlayUi.HandleRuntimeInput()) return;
            if (!WeatherOverhaulSettingsManager.Debug)
            {
                GlobalWeatherDebugUi.Show = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                if (!WeatherOverhaulSettingsManager.EnableGlobalSimulation)
                {
                    GlobalWeatherDebugUi.Show = false;
                    Core.Warn("[GlobalUI] Cannot show global weather UI while global simulation is disabled.");
                }
                else
                {
                    GlobalWeatherDebugUi.Show = !GlobalWeatherDebugUi.Show;
                    Core.Log("[GlobalUI] " + (GlobalWeatherDebugUi.Show ? "shown" : "hidden") + ".");
                }
            }

            if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                RebuildGlobalSimulation(true);
            }

            if (Input.GetKeyDown(KeyCode.Keypad3))
            {
                WeatherSnapshot snapshot = WeatherSnapshot.Capture();
                if (!snapshot.IsValid) snapshot = CurrentSnapshot;
                if (!snapshot.IsValid)
                {
                    Core.Warn("[GlobalSim] Cannot log current region forecast: no valid weather snapshot.");
                    return;
                }

                CurrentSnapshot = snapshot;
                if (snapshot.RegionId == WeatherRegionId.Unknown)
                {
                    Core.Warn($"[GlobalSim] Cannot log current region forecast: scene {snapshot.SceneName} is not mapped to a weather region.");
                    return;
                }

                GlobalWeatherSimulation.LogCurrentRegionForecast(snapshot, true);
            }
        }

        private static string GetSceneToLoad(LoadScene loadScene)
        {
            try
            {
                string? scene = loadScene.GetSceneToLoad();
                if (!string.IsNullOrEmpty(scene)) return scene;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("WeatherOverhaulRuntime.GetSceneToLoad.1", "WeatherOverhaulRuntime.GetSceneToLoad failed.", caughtException);
            }

            try
            {
                return loadScene.m_SceneToLoad ?? string.Empty;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("WeatherOverhaulRuntime.GetSceneToLoad.2", "WeatherOverhaulRuntime.GetSceneToLoad failed.", caughtException);
                return string.Empty;
            }
        }

        private static WeatherSnapshot GetBestSourceSnapshotForDestinationAuthority(string reason)
        {
            WeatherSnapshot captured = ResolveSceneAuthoritySnapshot(WeatherSnapshot.Capture(), reason + " captured source");
            if (captured.IsValid && captured.RegionId != WeatherRegionId.Unknown && !IsSaveBoundaryScene(captured)) return captured;
            if (CurrentSnapshot.IsValid && CurrentSnapshot.RegionId != WeatherRegionId.Unknown && !IsSaveBoundaryScene(CurrentSnapshot)) return CurrentSnapshot;
            if (s_LastStableClockSnapshot.IsValid && s_LastStableClockSnapshot.RegionId != WeatherRegionId.Unknown) return s_LastStableClockSnapshot;
            return WeatherSnapshot.Invalid;
        }

        private static bool IsPendingDestinationAuthorityForSnapshot(WeatherSnapshot snapshot)
        {
            if (!HasDestinationWeatherAuthority) return false;
            if (!snapshot.IsValid) return false;
            if (Time.realtimeSinceStartup > s_PendingDestinationUntilRealtime) return false;
            if (!string.IsNullOrEmpty(s_PendingDestinationScene)) return string.Equals(snapshot.SceneName, s_PendingDestinationScene, StringComparison.Ordinal);
            return s_PendingDestinationRegion != WeatherRegionId.Unknown && snapshot.RegionId == s_PendingDestinationRegion;
        }

        private static bool IsDestinationWeatherAuthorityGuardingForSnapshot(WeatherSnapshot snapshot)
        {
            return s_DestinationWeatherAuthorityState == DestinationWeatherAuthorityState.AppliedGuard && IsPendingDestinationAuthorityForSnapshot(snapshot);
        }

        private static bool ShouldLogDestinationAuthoritySnapshot(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return false;
            if (!reason.Contains("scene load notification") && !reason.Contains("scene load immediate apply") && !reason.Contains("WeatherSetStage.Activate") && !reason.Contains("weather selection")) return false;

            string key = s_DestinationWeatherAuthorityState + "|" + s_PendingDestinationScene + "|" + s_PendingDestinationRegion + "|" + s_PendingDestinationStageId + "|" + reason;
            float now = Time.realtimeSinceStartup;
            if (string.Equals(key, s_LastDestinationAuthorityLogKey, StringComparison.Ordinal) && now - s_LastDestinationAuthorityLogRealtime < 1.5f) return false;

            s_LastDestinationAuthorityLogKey = key;
            s_LastDestinationAuthorityLogRealtime = now;
            return true;
        }

        private static bool ShouldPreserveForecastThroughTransitionBridge(WeatherSnapshot snapshot)
        {
            if (!HasDestinationWeatherAuthority) return false;
            if (Time.realtimeSinceStartup > s_PendingDestinationUntilRealtime) return false;
            return string.Equals(snapshot.SceneName, "Empty", StringComparison.Ordinal);
        }

        private static void ClearExpiredDestinationWeatherAuthority()
        {
            if (!HasDestinationWeatherAuthority) return;
            if (Time.realtimeSinceStartup <= s_PendingDestinationUntilRealtime) return;

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                string destination = s_PendingDestinationRegion == WeatherRegionId.Unknown ? s_PendingDestinationScene : $"{s_PendingDestinationScene} -> {RegionWeatherGraph.Get(s_PendingDestinationRegion).ShortName}/{RegionWeatherGraph.Get(s_PendingDestinationRegion).DisplayName}";
                if (s_DestinationWeatherAuthorityState == DestinationWeatherAuthorityState.AppliedGuard)
                {
                    Core.Log($"[WeatherDirector] Destination weather guard expired | Destination={destination} | Planned={WeatherStageCatalog.Get(s_PendingDestinationStageId).DisplayName}");
                }
                else
                {
                    Core.Warn($"[WeatherDirector] Expired pending destination weather authority before it was applied | Destination={destination} | Planned={WeatherStageCatalog.Get(s_PendingDestinationStageId).DisplayName}");
                }
            }

            ClearPendingDestinationWeatherAuthority();
        }

        private static void ClearPendingDestinationWeatherAuthority()
        {
            s_DestinationWeatherAuthorityState = DestinationWeatherAuthorityState.None;
            s_PendingDestinationScene = string.Empty;
            s_PendingDestinationRegion = WeatherRegionId.Unknown;
            s_PendingDestinationWorldHour = -1f;
            s_PendingDestinationStageId = WeatherStageId.Undefined;
            s_PendingDestinationStage = WeatherStage.Undefined;
            s_PendingDestinationUntilRealtime = 0f;
        }

        private static void BeginSaveClockStabilization(string sceneName)
        {
            s_MainMenuSuspended = false;
            s_WaitingForSaveClockStabilization = true;
            s_SaveClockStabilizationStartedRealtime = Time.realtimeSinceStartup;
            s_SaveClockStabilizationScene = sceneName ?? string.Empty;
            s_SaveClockInitialSnapshot = WeatherSnapshot.Capture();
            s_SaveClockCandidateSnapshot = WeatherSnapshot.Invalid;
            s_SaveClockCandidateSinceRealtime = 0f;
            CurrentSnapshot = WeatherSnapshot.Invalid;
            WeatherDirector.Enabled = false;
            VanillaAuroraAuthority.Release();
            s_NextSnapshotRefreshRealtime = 0f;
            s_NextForecastEnforcementRealtime = 0f;

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                string initialClock = s_SaveClockInitialSnapshot.IsValid ? $"{s_SaveClockInitialSnapshot.GetClockText()} ({s_SaveClockInitialSnapshot.WorldHour:0.##}h)" : "unavailable";
                Core.Log($"[GlobalSim] Waiting for save-slot clock stabilization before enabling forecast authority | Scene={s_SaveClockStabilizationScene} | InitialClock={initialClock}");
            }
        }

        private static void UpdateSaveClockStabilization()
        {
            if (!s_WaitingForSaveClockStabilization) return;
            if (!GameplaySceneState.IsGameplaySceneActive()) return;

            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            float now = Time.realtimeSinceStartup;
            float elapsed = now - s_SaveClockStabilizationStartedRealtime;
            if (!snapshot.IsValid) return;

            if (!s_SaveClockInitialSnapshot.IsValid)
            {
                s_SaveClockInitialSnapshot = snapshot;
                s_SaveClockCandidateSnapshot = WeatherSnapshot.Invalid;
                s_SaveClockCandidateSinceRealtime = 0f;
                return;
            }

            bool clockMovedMeaningfully = snapshot.DayNumber != s_SaveClockInitialSnapshot.DayNumber ||
                                           Math.Abs(snapshot.WorldHour - s_SaveClockInitialSnapshot.WorldHour) >= SaveLoadClockMeaningfulChangeHours;

            if (clockMovedMeaningfully)
            {
                if (!s_SaveClockCandidateSnapshot.IsValid || Math.Abs(snapshot.WorldHour - s_SaveClockCandidateSnapshot.WorldHour) > SaveLoadClockCandidateToleranceHours)
                {
                    s_SaveClockCandidateSnapshot = snapshot;
                    s_SaveClockCandidateSinceRealtime = now;
                }
                else if (now - s_SaveClockCandidateSinceRealtime >= SaveLoadClockSettleSeconds)
                {
                    CompleteSaveClockStabilization(snapshot, "restored save clock replaced the initial loading clock");
                    return;
                }
            }

            if (elapsed >= SaveLoadClockMaximumWaitSeconds)
            {
                CompleteSaveClockStabilization(snapshot, clockMovedMeaningfully ? "clock stabilization maximum wait reached after a restored-clock change" : "clock remained unchanged through the stabilization window");
            }
        }

        private static void CompleteSaveClockStabilization(WeatherSnapshot snapshot, string reason)
        {
            if (!s_WaitingForSaveClockStabilization) return;

            float waitedSeconds = Math.Max(0f, Time.realtimeSinceStartup - s_SaveClockStabilizationStartedRealtime);
            string initialClock = s_SaveClockInitialSnapshot.IsValid ? $"{s_SaveClockInitialSnapshot.GetClockText()} ({s_SaveClockInitialSnapshot.WorldHour:0.##}h)" : "unavailable";

            s_WaitingForSaveClockStabilization = false;
            s_SaveClockStabilizationStartedRealtime = 0f;
            s_SaveClockCandidateSnapshot = WeatherSnapshot.Invalid;
            s_SaveClockCandidateSinceRealtime = 0f;
            s_SaveClockStabilizationScene = string.Empty;
            CurrentSnapshot = snapshot;

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[GlobalSim] Save-slot clock stabilized; enabling forecast authority | Clock={snapshot.GetClockText()} ({snapshot.WorldHour:0.##}h) | InitialClock={initialClock} | Waited={waitedSeconds:0.##}s | Reason={reason}");
            }

            ApplyMainSystemState(force: true);

            WeatherSnapshot authoritySnapshot = ResolveSceneAuthoritySnapshot(WeatherSnapshot.Capture(), "post save-load clock stabilization");
            if (!authoritySnapshot.IsValid) authoritySnapshot = snapshot;
            CurrentSnapshot = authoritySnapshot;

            if (authoritySnapshot.IsValid && !IsSaveBoundaryScene(authoritySnapshot) && authoritySnapshot.RegionId != WeatherRegionId.Unknown)
            {
                BeginSceneWeatherAuthority(authoritySnapshot.SceneName);
                TryApplySceneWeatherImmediately(authoritySnapshot, "post save-load clock stabilization immediate apply");
                RememberStableClock(authoritySnapshot);
                UpdateCurrentStageLogger(authoritySnapshot);
                UpdateSceneAndRegionLogs(authoritySnapshot);
                Remember(authoritySnapshot);
            }

            s_SaveClockInitialSnapshot = WeatherSnapshot.Invalid;
        }

        private static void CacheCurrentSettingsState()
        {
            bool enabled = WeatherOverhaulSettingsManager.EnableWeatherOverhaul;
            bool globalSimulationEnabled = WeatherOverhaulSettingsManager.EnableGlobalSimulation;
            s_LastWeatherOverhaulEnabled = enabled;
            s_LastGlobalSimulationEnabled = globalSimulationEnabled;
            s_LastLocalWeatherApplyEnabled = enabled && globalSimulationEnabled && WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion;
            s_LastEventSettingsSignature = WeatherOverhaulSettingsManager.EventSettingsSignature;
        }

        private static void EnterMainMenuSuspension(string reason)
        {
            CurrentSnapshot = WeatherSnapshot.Invalid;
            GlobalWeatherDebugUi.Show = false;
            WeatherDirector.ResetForMainMenu();
            VanillaAuroraAuthority.Release();
            CacheCurrentSettingsState();
            s_WaitingForSaveClockStabilization = false;
            s_SaveClockStabilizationStartedRealtime = 0f;
            s_SaveClockInitialSnapshot = WeatherSnapshot.Invalid;
            s_SaveClockCandidateSnapshot = WeatherSnapshot.Invalid;
            s_SaveClockCandidateSinceRealtime = 0f;
            s_SaveClockStabilizationScene = string.Empty;

            if (s_MainMenuSuspended) return;
            s_MainMenuSuspended = true;

            CustomWeatherStageRuntime.ResetForMainMenu();
            SceneRegionMapper.ResetSession();
            BloodMoonInfluenceRuntime.Update(WeatherSnapshot.Invalid);
            FreezingFogThinIceRuntime.ResetLoadAttempt();
            GlobalWeatherSimulation.Clear(reason);
            GlobalWeatherPersistence.ResetLoadAttempt();
            ClearPendingDestinationWeatherAuthority();

            s_SceneWeatherAuthorityScene = string.Empty;
            s_SceneWeatherAuthorityRegion = WeatherRegionId.Unknown;
            s_SceneWeatherAuthorityUntilRealtime = 0f;
            s_LastStableClockSnapshot = WeatherSnapshot.Invalid;
            s_LastSceneName = string.Empty;
            s_LastRegionId = WeatherRegionId.Unknown;
            s_NextForecastEnforcementRealtime = 0f;
        }

        private static bool IsSaveBoundarySceneName(string sceneName)
        {
            return GameplaySceneState.IsSaveBoundarySceneName(sceneName);
        }

        private static bool IsAdditiveWeatherSubsceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return sceneName.EndsWith("_WILDLIFE", StringComparison.OrdinalIgnoreCase) ||
                   sceneName.EndsWith("_SANDBOX", StringComparison.OrdinalIgnoreCase) ||
                   sceneName.EndsWith("_VFX", StringComparison.OrdinalIgnoreCase) ||
                   sceneName.EndsWith("_DLC01", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSaveBoundaryScene(WeatherSnapshot snapshot)
        {
            return IsSaveBoundarySceneName(snapshot.SceneName);
        }

        private static bool IsIgnoredNonWeatherScene(WeatherSnapshot snapshot)
        {
            if (snapshot.IsIndoorEnvironment) return true;
            if (string.IsNullOrEmpty(snapshot.SceneName)) return true;
            if (snapshot.SceneName == "Empty") return true;
            if (snapshot.SceneName.StartsWith("MainMenu", StringComparison.OrdinalIgnoreCase)) return true;
            if (snapshot.SceneName.StartsWith("Boot", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void RememberStableClock(WeatherSnapshot snapshot)
        {
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return;
            if (IsSaveBoundaryScene(snapshot)) return;
            if (GlobalWeatherSimulation.IsSnapshotOlderThanCurrentForecast(snapshot, 12f)) return;

            s_LastStableClockSnapshot = snapshot;
        }

        private static void Remember(WeatherSnapshot snapshot)
        {
            s_LastSceneName = snapshot.SceneName;
            s_LastRegionId = snapshot.RegionId;
        }
    }
}