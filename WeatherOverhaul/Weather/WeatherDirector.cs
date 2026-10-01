namespace WeatherOverhaul.Weather
{
    internal static class WeatherDirector
    {
        private const float SceneChangeTransitionGameHours = 0.01f;
        private const float SceneChangeUnmanagedTransitionSeconds = 0.01f;

        private const float MinimumControlledDurationHours = 0.05f;
        private const float MinimumControlledRemainingHours = 0.02f;
        private const float RepeatedDirectorLogIntervalHours = 1f;
        private static WeatherRegionId s_LastAppliedRegion = WeatherRegionId.Unknown;
        private static WeatherStage s_LastAppliedStage = WeatherStage.Undefined;
        private static float s_LastAppliedWorldHour = -1000f;
        private static string s_LastAppliedReason = string.Empty;
        private static WeatherSetStage? s_LastActivatedWeatherSetStage;
        private static bool s_HasAuthorizedActivationPlan;
        private static WeatherActivationPlan s_AuthorizedActivationPlan;
        private static bool s_AuthorizedActivationImmediate;
        private static string s_AuthorizedActivationReason = string.Empty;
        private static WeatherStage s_LastPreparedActivationLogStage = WeatherStage.Undefined;
        private static string s_LastPreparedActivationLogReason = string.Empty;
        private static float s_LastPreparedActivationLogRealtime;
        private static readonly Dictionary<string, float> s_LastRepeatedDirectorLogWorldHourByKey = [];
        private readonly struct WeatherSetTimingBackup
        {
            internal readonly float Duration;
            internal readonly float Elapsed;
            internal readonly float Transition;

            internal WeatherSetTimingBackup(WeatherSetStage stage)
            {
                Duration = stage.m_CurrentDuration;
                Elapsed = stage.m_ElapsedTime;
                Transition = stage.m_CurrentTransitionTime;
            }
        }

        private static readonly Dictionary<WeatherSetStage, WeatherSetTimingBackup> s_WeatherSetTimingBackups = [];
        private static bool s_SuppressUnauthorizedForecastStageActivation;

        internal static bool Enabled { get; set; }
        internal static bool IsApplyingForecastStage { get; private set; }

        internal static void ResetForMainMenu()
        {
            RestoreControlledWeatherSetTimings();
            Enabled = false;
            IsApplyingForecastStage = false;
            s_LastAppliedRegion = WeatherRegionId.Unknown;
            s_LastAppliedStage = WeatherStage.Undefined;
            s_LastAppliedWorldHour = -1000f;
            s_LastAppliedReason = string.Empty;
            s_LastActivatedWeatherSetStage = null;
            s_HasAuthorizedActivationPlan = false;
            s_AuthorizedActivationPlan = default;
            s_AuthorizedActivationImmediate = false;
            s_AuthorizedActivationReason = string.Empty;
            s_LastPreparedActivationLogStage = WeatherStage.Undefined;
            s_LastPreparedActivationLogReason = string.Empty;
            s_LastPreparedActivationLogRealtime = 0f;
            s_SuppressUnauthorizedForecastStageActivation = false;
            s_LastRepeatedDirectorLogWorldHourByKey.Clear();
        }

        internal static void NotifyWeatherSetStageActivated(WeatherSetStage stage)
        {
            if (stage == null) return;
            s_LastActivatedWeatherSetStage = stage;
        }

        internal static bool ShouldSuppressUnauthorizedForecastStageActivation(WeatherSetStage stage)
        {
            if (!IsApplyingForecastStage) return false;
            if (!s_SuppressUnauthorizedForecastStageActivation) return false;
            if (!s_HasAuthorizedActivationPlan) return false;
            if (stage == null) return false;
            return stage.m_WeatherType != s_AuthorizedActivationPlan.Stage;
        }

        internal static void PrepareAuthorizedWeatherSetStageActivation(WeatherSetStage stage)
        {
            if (!IsApplyingForecastStage) return;
            if (!s_HasAuthorizedActivationPlan) return;
            if (stage == null) return;
            if (stage.m_WeatherType != s_AuthorizedActivationPlan.Stage) return;

            float durationHours = Math.Max(MinimumControlledDurationHours, s_AuthorizedActivationPlan.DurationHours);
            float transitionHours = s_AuthorizedActivationImmediate ? SceneChangeTransitionGameHours : ComputeSmoothTransitionGameHours(s_AuthorizedActivationPlan);

            BackupWeatherSetTiming(stage);
            stage.m_CurrentDuration = durationHours;
            stage.m_CurrentTransitionTime = transitionHours;

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation && ShouldLogPreparedActivation(stage.m_WeatherType, s_AuthorizedActivationReason))
            {
                string mode = s_AuthorizedActivationImmediate ? "immediate" : "smooth";
                Core.Log($"[WeatherDirector] Prepared authorized WeatherSetStage activation before vanilla blend | Stage={WeatherStageFormatter.ToDisplayName(stage.m_WeatherType)} | Duration={durationHours:0.##}h | Transition={transitionHours:0.##}h | Mode={mode} | Reason={s_AuthorizedActivationReason}");
            }
        }

        private static bool ShouldLogPreparedActivation(WeatherStage stage, string reason)
        {
            float now = Time.realtimeSinceStartup;
            if (stage == s_LastPreparedActivationLogStage && string.Equals(reason, s_LastPreparedActivationLogReason, StringComparison.Ordinal) && now - s_LastPreparedActivationLogRealtime < 1.0f) return false;
            s_LastPreparedActivationLogStage = stage;
            s_LastPreparedActivationLogReason = reason ?? string.Empty;
            s_LastPreparedActivationLogRealtime = now;
            return true;
        }

        internal static bool TryOverrideVanillaWeatherSetStageActivation(WeatherSetStage vanillaStage, float startAtFrac, WeatherStage previousStage)
        {
            if (!Enabled) return false;
            if (CustomWeatherStageRuntime.IsActive) return false;
            if (IsApplyingForecastStage) return false;
            if (vanillaStage == null) return false;
            if (vanillaStage.m_WeatherType == WeatherStage.Undefined) return false;

            WeatherSnapshot capturedSnapshot = WeatherSnapshot.Capture();
            if (WeatherOverhaulRuntime.ShouldDeferWeatherApplicationUntilDestinationLoaded(capturedSnapshot))
            {
                return true;
            }
            bool usingPendingDestinationAuthority = WeatherOverhaulRuntime.TryGetPendingDestinationAuthoritySnapshot(capturedSnapshot, "WeatherSetStage.Activate destination authority gate", out WeatherSnapshot snapshot);
            if (!usingPendingDestinationAuthority) snapshot = WeatherOverhaulRuntime.ResolveSceneAuthoritySnapshot(capturedSnapshot, "WeatherSetStage.Activate authority gate");
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;

            WeatherTransition transition = GameManager.GetWeatherTransitionComponent();
            if (transition == null) return false;

            GlobalWeatherPersistence.TryLoad(snapshot);
            snapshot = WeatherOverhaulRuntime.ResolveSceneAuthoritySnapshot(snapshot, "WeatherSetStage.Activate authority gate after persisted forecast load");
            if (!GlobalWeatherSimulation.EnsureUsableSimulation(snapshot, "WeatherSetStage.Activate authority gate")) return false;
            if (!GlobalWeatherSimulation.TryGetActivationPlan(snapshot.RegionId, snapshot.WorldHour, out WeatherActivationPlan plan)) return false;
            if (plan.Stage == WeatherStage.Undefined) return false;

            bool immediate = usingPendingDestinationAuthority || WeatherOverhaulRuntime.ShouldApplyWeatherImmediatelyForSceneAuthority(snapshot);

            string reason = usingPendingDestinationAuthority ? "WeatherSetStage.Activate pending destination authority gate" : "WeatherSetStage.Activate forecast authority gate";
            bool applied = TryApplyForecastStage(transition, snapshot, reason, immediate);
            if (!applied) return false;
            if (usingPendingDestinationAuthority) WeatherOverhaulRuntime.NotePendingDestinationWeatherApplied(snapshot, reason);

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                string mode = immediate ? "immediate" : "smooth";
                Core.Log($"[WeatherDirector] Blocked vanilla WeatherSetStage.Activate during forecast authority | Vanilla={WeatherStageFormatter.ToDisplayName(vanillaStage.m_WeatherType)} | Planned={plan.Definition.DisplayName} | StartAtFrac={startAtFrac:0.##} | Previous={WeatherStageFormatter.ToDisplayName(previousStage)} | Mode={mode} | {snapshot.RegionShortName}/{snapshot.RegionDisplayName}");
            }

            return true;
        }

        internal static bool TryChooseAndActivateNextWeatherSet(WeatherTransition transition)
        {
            if (!Enabled) return false;
            if (CustomWeatherStageRuntime.IsActive) return false;
            if (transition == null) return false;

            WeatherSnapshot capturedSnapshot = WeatherSnapshot.Capture();
            if (WeatherOverhaulRuntime.ShouldDeferWeatherApplicationUntilDestinationLoaded(capturedSnapshot))
            {
                return true;
            }
            bool usingPendingDestinationAuthority = WeatherOverhaulRuntime.TryGetPendingDestinationAuthoritySnapshot(capturedSnapshot, "vanilla weather selection destination authority", out WeatherSnapshot snapshot);
            if (!usingPendingDestinationAuthority) snapshot = WeatherOverhaulRuntime.ResolveSceneAuthoritySnapshot(capturedSnapshot, "vanilla weather selection");
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;
            GlobalWeatherPersistence.TryLoad(snapshot);
            snapshot = WeatherOverhaulRuntime.ResolveSceneAuthoritySnapshot(snapshot, "vanilla weather selection after persisted forecast load");
            if (!GlobalWeatherSimulation.EnsureUsableSimulation(snapshot, "vanilla weather selection")) return false;

            string reason = usingPendingDestinationAuthority ? "vanilla weather selection pending destination authority" : "vanilla weather selection";
            bool applied = TryApplyForecastStage(transition, snapshot, reason, immediate: usingPendingDestinationAuthority);
            if (applied && usingPendingDestinationAuthority) WeatherOverhaulRuntime.NotePendingDestinationWeatherApplied(snapshot, reason);
            return applied;
        }

        internal static bool TryApplyCurrentForecastToLoadedRegion(WeatherSnapshot snapshot, string reason, bool immediate)
        {
            if (!Enabled) return false;
            if (CustomWeatherStageRuntime.IsActive) return false;
            if (WeatherOverhaulRuntime.ShouldDeferWeatherApplicationUntilDestinationLoaded(snapshot)) return false;
            snapshot = WeatherOverhaulRuntime.ResolveSceneAuthoritySnapshot(snapshot, reason);
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return false;
            GlobalWeatherPersistence.TryLoad(snapshot);
            snapshot = WeatherOverhaulRuntime.ResolveSceneAuthoritySnapshot(snapshot, reason + " after persisted forecast load");
            if (!GlobalWeatherSimulation.EnsureUsableSimulation(snapshot, reason)) return false;

            WeatherTransition transition = GameManager.GetWeatherTransitionComponent();
            if (transition == null) return false;

            bool applied = TryApplyForecastStage(transition, snapshot, reason, immediate);
            if (applied) WeatherOverhaulRuntime.NotePendingDestinationWeatherApplied(snapshot, reason);
            return applied;
        }

        private static bool TryApplyForecastStage(WeatherTransition transition, WeatherSnapshot snapshot, string reason, bool immediate)
        {
            if (!GlobalWeatherSimulation.TryGetActivationPlan(snapshot.RegionId, snapshot.WorldHour, out WeatherActivationPlan plan)) return false;

            WeatherStage requestedStage = plan.Stage;
            if (requestedStage == WeatherStage.Undefined) return false;
            if (IsDuplicateApply(snapshot, requestedStage, reason))
            {
                CustomWeatherStageRuntime.ApplyForecastPlan(plan, immediate, suppressPrecipitationVisuals: snapshot.IsIndoorEnvironment, suppressOutdoorVisuals: snapshot.IsIndoorEnvironment);
                return true;
            }

            if (!immediate && CaptureCurrentStage() == requestedStage)
            {
                ApplyForecastTimingToActiveStage(plan, requestedStage, reason, immediate);
                CustomWeatherStageRuntime.ApplyForecastPlan(plan, immediate, suppressPrecipitationVisuals: snapshot.IsIndoorEnvironment, suppressOutdoorVisuals: snapshot.IsIndoorEnvironment);
                RememberApply(snapshot, requestedStage, reason);

                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation && ShouldLogRepeatedDirectorEvent("continued", snapshot.RegionId, requestedStage, plan.Reason, snapshot.WorldHour))
                {
                    string requestedName = plan.Definition.DisplayName;
                    Core.Log($"[WeatherDirector] Continued current forecast stage without WeatherSet reactivation | {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | Requested={requestedName} ({WeatherStageFormatter.ToDisplayName(requestedStage)}) | Plan={plan.StartWorldHour:0.##}h->{plan.EndWorldHour:0.##}h remaining={plan.RemainingHours:0.##}h | {plan.Reason}");
                }

                return true;
            }

            try
            {
                IsApplyingForecastStage = true;
                s_HasAuthorizedActivationPlan = true;
                s_AuthorizedActivationPlan = plan;
                s_AuthorizedActivationImmediate = immediate;
                s_AuthorizedActivationReason = reason;
                if (!immediate) CustomWeatherStageRuntime.CaptureForecastTransitionStart();
                WeatherStage actualStage = ActivateForecastStage(transition, plan, immediate);
                bool exact = actualStage == requestedStage;
                if (exact) ApplyForecastTimingToActiveStage(plan, requestedStage, reason, immediate);
                if (exact) CustomWeatherStageRuntime.ApplyForecastPlan(plan, immediate, suppressPrecipitationVisuals: snapshot.IsIndoorEnvironment, suppressOutdoorVisuals: snapshot.IsIndoorEnvironment);
                else
                {
                    CustomWeatherStageRuntime.DiscardForecastTransitionStart();
                    CustomWeatherStageRuntime.ClearForecastOverridesIfAny();
                }
                if (exact) RememberApply(snapshot, requestedStage, reason);

                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation && ShouldLogRepeatedDirectorEvent(exact ? "applied" : "failed", snapshot.RegionId, requestedStage, reason + "|" + plan.Reason, snapshot.WorldHour))
                {
                    string mode = immediate ? "immediate" : "transition";
                    string resultText = exact ? "applied" : "failed: requested stage not reached";
                    string requestedName = plan.Definition.DisplayName;
                    Core.Log($"[WeatherDirector] Forecast stage result ({reason}, {mode}, {resultText}) | {snapshot.RegionShortName}/{snapshot.RegionDisplayName} | Requested={requestedName} ({WeatherStageFormatter.ToDisplayName(requestedStage)}) | Actual={WeatherStageFormatter.ToDisplayName(actualStage)} | Plan={plan.StartWorldHour:0.##}h->{plan.EndWorldHour:0.##}h remaining={plan.RemainingHours:0.##}h | {plan.Reason}");
                }

                return exact;
            }
            catch (Exception e)
            {
                CustomWeatherStageRuntime.DiscardForecastTransitionStart();
                Core.LogExceptionOnce("weather-director-apply", "[WeatherDirector] Failed to apply the forecast weather stage.", e);
                return false;
            }
            finally
            {
                s_HasAuthorizedActivationPlan = false;
                s_AuthorizedActivationPlan = default;
                s_AuthorizedActivationImmediate = false;
                s_AuthorizedActivationReason = string.Empty;
                s_SuppressUnauthorizedForecastStageActivation = false;
                IsApplyingForecastStage = false;
            }
        }

        private static WeatherStage ActivateForecastStage(WeatherTransition transition, WeatherActivationPlan plan, bool immediate)
        {
            WeatherStage requestedStage = plan.Stage;
            if (IsClearAuroraStage(requestedStage))
            {
                WeatherStage auroraStage = ActivateClearAuroraForecastStage(transition, immediate, ComputeSmoothTransitionGameHours(plan));
                if (auroraStage == requestedStage) return auroraStage;
            }

            if (!immediate)
            {
                return ActivateSmoothManagedForecastStage(transition, plan);
            }

            transition.ActivateWeatherSetImmediate(requestedStage);
            WeatherStage actualStage = CaptureCurrentStage();
            if (actualStage == requestedStage) return actualStage;

            if (immediate)
            {
                transition.ForceTransitionToWeatherStage((int)requestedStage);
                actualStage = CaptureCurrentStage();
                if (actualStage == requestedStage) return actualStage;

                transition.ForceUnmanagedWeatherStage(requestedStage, SceneChangeUnmanagedTransitionSeconds);
                return CaptureCurrentStage();
            }

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[WeatherDirector] Smooth forecast transition could not reach requested stage without an instant force; leaving current weather untouched for retry | Requested={WeatherStageFormatter.ToDisplayName(requestedStage)} | Current={WeatherStageFormatter.ToDisplayName(actualStage)} | Transition={ComputeSmoothTransitionGameHours(plan):0.##}h | {plan.Reason}");
            }
            return actualStage;
        }

        private static WeatherStage ActivateSmoothManagedForecastStage(WeatherTransition transition, WeatherActivationPlan plan)
        {
            WeatherStage requestedStage = plan.Stage;
            float smoothTransitionHours = ComputeSmoothTransitionGameHours(plan);
            WeatherStage previousStage = CaptureCurrentStage();

            s_SuppressUnauthorizedForecastStageActivation = true;
            try
            {
                transition.ActivateWeatherSetAtFrac(requestedStage, 0f);
            }
            finally
            {
                s_SuppressUnauthorizedForecastStageActivation = false;
            }

            WeatherStage activatedStage = CaptureCurrentStage();
            if (activatedStage == requestedStage)
            {
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[WeatherDirector] Used smooth forecast WeatherSet transition | Requested={WeatherStageFormatter.ToDisplayName(requestedStage)} | Transition={smoothTransitionHours:0.##}h | PlanRemaining={plan.RemainingHours:0.##}h | {plan.Reason}");
                return activatedStage;
            }

            WeatherSetData currentSet = transition.m_CurrentWeatherSet;
            if (TryFindWeatherSetStageStartFraction(currentSet, requestedStage, out float startFraction, out int stageIndex))
            {
                transition.ActivateWeatherSet(currentSet, startFraction, previousStage);
                WeatherStage soughtStage = CaptureCurrentStage();
                if (soughtStage == requestedStage)
                {
                    if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[WeatherDirector] Used smooth forecast WeatherSet stage seek | Requested={WeatherStageFormatter.ToDisplayName(requestedStage)} | StageIndex={stageIndex} | StartFrac={startFraction:0.###} | Transition={smoothTransitionHours:0.##}h | PlanRemaining={plan.RemainingHours:0.##}h | Previous={WeatherStageFormatter.ToDisplayName(previousStage)} | {plan.Reason}");
                    return soughtStage;
                }

                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
                {
                    Core.Log($"[WeatherDirector] Smooth forecast WeatherSet stage seek did not reach requested stage; not forcing stage index to avoid visible hard weather pop | Requested={WeatherStageFormatter.ToDisplayName(requestedStage)} | StageIndex={stageIndex} | Current={WeatherStageFormatter.ToDisplayName(soughtStage)} | Transition={smoothTransitionHours:0.##}h | PlanRemaining={plan.RemainingHours:0.##}h | {plan.Reason}");
                }

                activatedStage = soughtStage;
            }

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[WeatherDirector] Smooth forecast WeatherSet transition could not reach requested stage; leaving current weather untouched for retry | Requested={WeatherStageFormatter.ToDisplayName(requestedStage)} | Current={WeatherStageFormatter.ToDisplayName(activatedStage)} | Transition={smoothTransitionHours:0.##}h | {plan.Reason}");
            }

            return activatedStage;
        }

        private static bool TryFindWeatherSetStageStartFraction(WeatherSetData weatherSet, WeatherStage requestedStage, out float startFraction, out int stageIndex)
        {
            startFraction = 0f;
            stageIndex = -1;
            if (weatherSet == null) return false;
            if (weatherSet.m_WeatherStages == null) return false;

            float totalDurationHours = 0f;
            for (int i = 0; i < weatherSet.m_WeatherStages.Length; i++)
            {
                WeatherSetStage stage = weatherSet.m_WeatherStages[i];
                if (stage == null) continue;
                totalDurationHours += Math.Max(0.001f, stage.m_CurrentDuration);
            }

            if (totalDurationHours <= 0f) return false;

            float elapsedBeforeStage = 0f;
            for (int i = 0; i < weatherSet.m_WeatherStages.Length; i++)
            {
                WeatherSetStage stage = weatherSet.m_WeatherStages[i];
                if (stage == null) continue;
                if (stage.m_WeatherType == requestedStage)
                {
                    startFraction = Math.Max(0f, Math.Min(1f, elapsedBeforeStage / totalDurationHours));
                    stageIndex = i;
                    return true;
                }

                elapsedBeforeStage += Math.Max(0.001f, stage.m_CurrentDuration);
            }

            return false;
        }

        private static WeatherStage ActivateClearAuroraForecastStage(WeatherTransition transition, bool immediate, float smoothTransitionHours)
        {
            transition.ChooseNextWeatherSet(null, true, true);
            WeatherStage actualStage = CaptureCurrentStage();
            if (actualStage == WeatherStage.ClearAurora) return actualStage;

            if (!immediate)
            {
                transition.ActivateWeatherSetAtFrac(WeatherStage.ClearAurora, 0f);
                actualStage = CaptureCurrentStage();
                if (actualStage == WeatherStage.ClearAurora) return actualStage;

                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
                {
                    Core.Log($"[WeatherDirector] Smooth Clear Aurora WeatherSet transition could not reach requested stage; leaving current weather untouched for retry | Current={WeatherStageFormatter.ToDisplayName(actualStage)} | Transition={smoothTransitionHours:0.##}h");
                }

                return actualStage;
            }

            if (immediate)
            {
                transition.ForceTransitionToWeatherStage((int)WeatherStage.ClearAurora);
                actualStage = CaptureCurrentStage();
                if (actualStage == WeatherStage.ClearAurora) return actualStage;

                transition.ForceUnmanagedWeatherStage(WeatherStage.ClearAurora, SceneChangeUnmanagedTransitionSeconds);
                return CaptureCurrentStage();
            }

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[WeatherDirector] Smooth Clear Aurora transition could not reach requested stage without an instant force; leaving current weather untouched for retry | Current={WeatherStageFormatter.ToDisplayName(actualStage)} | Transition={smoothTransitionHours:0.##}h");
            }
            return actualStage;
        }

        private static void ApplyForecastTimingToActiveStage(WeatherActivationPlan plan, WeatherStage requestedStage, string reason, bool immediate)
        {
            WeatherSetStage? activeStage = s_LastActivatedWeatherSetStage;
            if (activeStage == null)
            {
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Warn("[WeatherDirector] Could not control forecast duration: no WeatherSetStage activation was captured.");
                return;
            }

            if (activeStage.m_WeatherType != requestedStage)
            {
                WeatherStage currentStage = CaptureCurrentStage();
                if (currentStage != requestedStage)
                {
                    if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Warn($"[WeatherDirector] Could not control forecast duration: captured stage is {WeatherStageFormatter.ToDisplayName(activeStage.m_WeatherType)}, expected {WeatherStageFormatter.ToDisplayName(requestedStage)}, and current weather is {WeatherStageFormatter.ToDisplayName(currentStage)}.");
                    return;
                }

                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[WeatherDirector] Retiming captured WeatherSetStage despite type mismatch | Captured={WeatherStageFormatter.ToDisplayName(activeStage.m_WeatherType)} | Current={WeatherStageFormatter.ToDisplayName(currentStage)} | Expected={WeatherStageFormatter.ToDisplayName(requestedStage)}.");
            }

            BackupWeatherSetTiming(activeStage);
            float durationHours = Math.Max(MinimumControlledDurationHours, plan.DurationHours);
            float elapsedHours = Math.Max(0f, plan.ElapsedHours);
            elapsedHours = Math.Min(elapsedHours, Math.Max(0f, durationHours - MinimumControlledRemainingHours));
            float transitionHours = immediate ? SceneChangeTransitionGameHours : ComputeSmoothTransitionGameHours(plan);

            activeStage.m_CurrentDuration = durationHours;
            activeStage.m_ElapsedTime = elapsedHours;
            activeStage.m_CurrentTransitionTime = transitionHours;

            float logWorldHour = plan.StartWorldHour + elapsedHours;
            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation && ShouldLogRepeatedDirectorEvent("timing", WeatherRegionId.Unknown, requestedStage, reason, logWorldHour))
            {
                Core.Log($"[WeatherDirector] Controlled active WeatherSetStage timing | Stage={WeatherStageFormatter.ToDisplayName(requestedStage)} | Duration={durationHours:0.##}h | Elapsed={elapsedHours:0.##}h | Remaining={Math.Max(0f, durationHours - elapsedHours):0.##}h | Transition={transitionHours:0.##}h | Reason={reason}");
            }
        }


        private static float ComputeSmoothTransitionGameHours(WeatherActivationPlan plan)
        {
            return Math.Max(0.5f, Math.Min(1f, WeatherOverhaulSettingsManager.WeatherStageTransitionHours));
        }

        private static void BackupWeatherSetTiming(WeatherSetStage stage)
        {
            if (stage == null || s_WeatherSetTimingBackups.ContainsKey(stage)) return;
            s_WeatherSetTimingBackups[stage] = new WeatherSetTimingBackup(stage);
        }

        internal static void RestoreControlledWeatherSetTimings()
        {
            foreach (KeyValuePair<WeatherSetStage, WeatherSetTimingBackup> pair in s_WeatherSetTimingBackups)
            {
                WeatherSetStage stage = pair.Key;
                if (stage == null) continue;
                stage.m_CurrentDuration = pair.Value.Duration;
                stage.m_ElapsedTime = pair.Value.Elapsed;
                stage.m_CurrentTransitionTime = pair.Value.Transition;
            }

            s_WeatherSetTimingBackups.Clear();
            s_LastActivatedWeatherSetStage = null;
        }

        private static WeatherStage CaptureCurrentStage()
        {
            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            return snapshot.IsValid ? snapshot.Stage : WeatherStage.Undefined;
        }


        private static bool ShouldLogRepeatedDirectorEvent(string category, WeatherRegionId regionId, WeatherStage stage, string reason, float worldHour)
        {
            string key = category + "|" + (int)regionId + "|" + (int)stage + "|" + (reason ?? string.Empty);
            if (s_LastRepeatedDirectorLogWorldHourByKey.TryGetValue(key, out float lastWorldHour) && Math.Abs(worldHour - lastWorldHour) < RepeatedDirectorLogIntervalHours) return false;
            s_LastRepeatedDirectorLogWorldHourByKey[key] = worldHour;
            return true;
        }

        private static bool IsDuplicateApply(WeatherSnapshot snapshot, WeatherStage stage, string reason)
        {
            if (snapshot.RegionId != s_LastAppliedRegion || stage != s_LastAppliedStage) return false;
            if (!string.Equals(reason, s_LastAppliedReason, StringComparison.Ordinal)) return false;
            if (CaptureCurrentStage() != stage) return false;
            return Math.Abs(snapshot.WorldHour - s_LastAppliedWorldHour) <= 0.05f;
        }

        private static bool IsClearAuroraStage(WeatherStage stage)
        {
            return stage == WeatherStage.ClearAurora;
        }

        private static void RememberApply(WeatherSnapshot snapshot, WeatherStage stage, string reason)
        {
            s_LastAppliedRegion = snapshot.RegionId;
            s_LastAppliedStage = stage;
            s_LastAppliedWorldHour = snapshot.WorldHour;
            s_LastAppliedReason = reason;
        }
    }
}