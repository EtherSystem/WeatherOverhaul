namespace WeatherOverhaul.Weather
{
    internal static class CustomWeatherStageRuntime
    {
        private const float MaintenanceIntervalSeconds = 0.25f;
        private const float ForceTransitionSeconds = 0.05f;
        private const float PrimarySceneVisualDelaySeconds = 0f;
        private const float AdditiveSceneVisualDelaySeconds = 0f;
        private const float PrecipitationRestoreRetrySeconds = 0.25f;
        private const float FogScaleNone = -1f;
        private const float WindUnlockedMPH = -1f;
        private const float WindTargetIntervalMinSeconds = 1.25f;
        private const float WindTargetIntervalMaxSeconds = 3.25f;
        private const float SnowBloodMoonSnowIntensity = 0.14f;
        private const float BloodMoonMasterAmbient = 0.26f;
        private static readonly Color BloodMoonMoonColor = new(0.56f, 0.105f, 0.085f, 1f);
        private static readonly Color BloodMoonMoonLightColor = new(0.085f, 0.026f, 0.022f, 1f);
        private static readonly Color BloodMoonStarColor = new(0.30f, 0.12f, 0.105f, 1f);
        private static readonly Color BloodMoonMoonGlowColor = new(0.34f, 0.07f, 0.055f, 1f);
        private static readonly Color SnowBloodMoonSnowColor = new(0.22f, 0.075f, 0.065f, 0.48f);
        private static readonly Color SnowyAuroraSnowColor = new(0.68f, 0.86f, 0.82f, 0.35f);


        private const int ModeAshfall = 1;
        private const int ModeWhiteout = 2;
        private const int ModeVeryDenseFog = 3;
        private const int ModeVeryHeavySnow = 4;
        private const int ModeWindyLightSnow = 5;
        private const int ModeViolentBlizzard = 6;
        private const int ModeLowOvercast = 7;
        private const int ModeFreezingFog = 8;
        private const int ModeHeavyOvercast = 9;
        private const int ModeCloudyAurora = 10;
        private const int ModeFoggyAurora = 11;
        private const int ModeBloodMoon = 20;
        private const int ModeSnowyAurora = 21;
        private const int ModeLightSnowBloodMoon = 22;

        private static int s_RequestedMode;
        private static int s_ActiveMode;
        private static bool s_FoggyAuroraStageSpoofActive;
        private static bool s_ForecastOverrideActive;
        private static bool s_ImmediateForecastPlanLatched;
        private static WeatherStageId s_ImmediateForecastStageId = WeatherStageId.Undefined;
        private static float s_ImmediateForecastStartWorldHour = -1f;
        private static float s_ImmediateForecastEndWorldHour = -1f;
        private static int s_LastAppliedLogMode;
        private static WeatherStage s_LastForcedBaseStage = WeatherStage.Undefined;
        private static float s_NextMaintenanceRealtime;
        private static bool s_HasFogScaleBackup;
        private static float s_OriginalFogScale = 1f;
        private static bool s_WindLockedByCustomStage;
        private static float s_WindRangeMinMPH = -1f;
        private static float s_WindRangeMaxMPH = -1f;
        private static float s_CurrentWindLockMPH = -1f;
        private static float s_TargetWindLockMPH = -1f;
        private static float s_NextWindTargetRealtime;
        private static float s_LastWindUpdateRealtime;
        private static float s_CalculatedTemperatureOffsetCelsius;
        private static bool s_SnowColorOverridden;
        private static bool s_HasParticleStartColorBackup;
        private static ParticleSystem.MinMaxGradient s_OriginalParticleStartColor;
        private static bool s_HasSnowMaterialColorBackup;
        private static Color s_OriginalSnowMaterialColor = Color.white;
        private static bool s_SnowPresetOverridden;
        private static WeatherStage s_CurrentSnowPresetStage = WeatherStage.Undefined;
        private static Il2Cpp.Weather? s_CurrentSnowPresetWeather;
        private static bool s_SuppressForecastPrecipitationVisuals;
        private static bool s_InteriorPrecipitationVisualsSuppressed;
        private static bool s_InteriorPrecipitationRestorePending;
        private static float s_PrecipitationRestoreNotBeforeRealtime;
        private static float s_NextPrecipitationRestoreAttemptRealtime;
        private static bool s_SuppressOutdoorNightEventVisuals;
        private static float s_OutdoorVisualsNotBeforeRealtime;
        private static float s_CurrentSnowIntensity = 1f;
        private static bool s_FallingSnowSuppressedByCustomStage;
        private static Il2Cpp.Weather? s_CachedFallingSnowWeather;
        private static GameObject? s_CachedFallingSnowRoot;
        private static ParticleSystem[] s_CachedFallingSnowSystems = Array.Empty<ParticleSystem>();
        private static Color s_CurrentSnowColor = Color.white;
        private static bool s_CustomFogScaleActive;
        private static float s_CurrentCustomFogScale = 1f;
        private static float s_CurrentEffectBlend = 1f;
        private static int s_EffectTransitionMode;
        private static float s_EffectStartFogScale = 1f;
        private static float s_EffectStartSnowIntensity = 1f;
        private static Color s_EffectStartSnowColor = Color.white;
        private static float s_EffectStartWindMinMPH = -1f;
        private static float s_EffectStartWindMaxMPH = -1f;
        private static float s_EffectStartTemperatureOffsetCelsius;
        private static bool s_AuroraAlphaOverridden;
        private static bool s_HasAuroraAlphaBackup;
        private static float s_OriginalAuroraAlpha;
        private static float s_CurrentAuroraAlpha;
        private static float s_EffectStartAuroraAlpha;
        private static bool s_MasterAmbientOverridden;
        private static bool s_HasMasterAmbientBackup;
        private static float s_OriginalMasterAmbient = 1f;
        private static float s_CurrentMasterAmbient = 1f;
        private static float s_EffectStartMasterAmbient = 1f;
        private static bool s_FogColorOverridden;
        private static bool s_HasFogColorBackup;
        private static Color s_OriginalRenderSettingsFogColor = Color.white;
        private static bool s_HasSkyboxFogColorBackup;
        private static Color s_OriginalSkyboxFogColor = Color.white;
        private static bool s_HasHeightFogSettingsBackup;
        private static HeightFogSettings s_OriginalHeightFogSettings;
        private static bool s_DirectFogDensityOverridden;
        private static bool s_HasDirectFogDensityBackup;
        private static float s_OriginalRenderSettingsFogDensity;
        private static bool s_OriginalRenderSettingsFogEnabled;
        private static FogMode s_OriginalRenderSettingsFogMode;
        private static bool s_OriginalHeightFogObjectActive;
        private static bool s_OriginalHeightFogRendererEnabled;
        private static bool s_HasHeightFogActivationBackup;
        private static float s_OriginalUniStormFogDensity;
        private static float s_CurrentDirectFogDensity;
        private static float s_EffectStartDirectFogDensity;
        private sealed class AuroraCloudRendererBackup
        {
            internal bool RendererEnabled;
            internal bool GameObjectActive;
            internal Material? Material;
            internal Color MaterialColor;
            internal Material? SharedMaterial;
            internal Color SharedMaterialColor;
            internal MaterialPropertyBlock PropertyBlock = new();
        }

        private static readonly Dictionary<Renderer, AuroraCloudRendererBackup> s_AuroraCloudRendererBackups = [];
        private static bool s_HasAuroraCloudStateBackup;
        private static float s_OriginalCloudCoverage;
        private static bool s_OriginalCloudParentActive;
        private static TODStateData? s_OriginalAuroraCloudTodState;
        private static Color s_OriginalHorizonCloudColor1 = Color.white;
        private static Color s_OriginalHorizonCloudColor2 = Color.white;
        private static Color s_OriginalHorizonCloudColor3 = Color.white;
        private static Vector4 s_OriginalCloudAlphas;

        private static MaterialPropertyBlock? s_AuroraVariantPropertyBlock;
        private static MaterialPropertyBlock? s_SharedSkyPropertyBlock;
        private static MaterialPropertyBlock? s_OriginalBloodMoonStarSpherePropertyBlock;
        private static bool s_HasBloodMoonStarSpherePropertyBlockBackup;
        private static readonly int s_TintColorShaderId = Shader.PropertyToID("_TintColor");
        private static readonly int s_MoonColorShaderId = Shader.PropertyToID("_MoonColor");
        private static Color s_CurrentFogColor = Color.white;
        private static Color s_EffectStartFogColor = Color.white;
        private static bool s_BloodMoonVisualsOverridden;
        private static bool s_HasBloodMoonVisualsBackup;
        private static bool s_OriginalUseMoonColourOverride;
        private static Color s_OriginalMoonColourOverride = Color.white;
        private static Color s_OriginalMoonLightColor = Color.white;
        private static Color s_OriginalStarColor = Color.white;
        private static bool s_HasSkyboxMoonColorBackup;
        private static Color s_OriginalSkyboxMoonColor = Color.white;
        private static bool s_HasSkyboxMoonGlowBackup;
        private static Color s_OriginalSkyboxMoonGlow = Color.white;
        private static bool s_HasSkyboxSunHaloBackup;
        private static float s_OriginalSkyboxSunSize;
        private static float s_OriginalSkyboxSunDiffusion;
        private static bool s_MoonTextureOverridden;
        private static bool s_HasMoonTextureBackup;
        private static int s_OriginalMoonTextureIndex = -1;
        private static Texture2D? s_OriginalMoonPhaseTexture;
        private static bool s_HasSkyboxMoonTextureBackup;
        private static Texture? s_OriginalSkyboxMoonTexture;
        private static Texture2D? s_GeneratedBloodMoonTexture;
        private static Color s_CurrentMoonColour = Color.white;
        private static Color s_CurrentMoonLightColour = Color.white;
        private static Color s_CurrentStarColour = Color.white;
        private static Color s_CurrentMoonGlowColour = Color.white;
        private static Color s_EffectStartMoonColour = Color.white;
        private static Color s_EffectStartMoonLightColour = Color.white;
        private static Color s_EffectStartStarColour = Color.white;
        private static Color s_EffectStartMoonGlowColour = Color.white;
        private static bool s_HasPendingForecastTransitionStart;
        private static EffectTransitionStartSnapshot s_PendingForecastTransitionStart;
        private static bool s_ForecastFadeOutTransitionActive;
        private static WeatherStageId s_ForecastFadeOutTargetStageId = WeatherStageId.Undefined;
        private static bool s_IsApplyingCustomBaseStage;

        private struct EffectTransitionStartSnapshot
        {
            public float FogScale;
            public float SnowIntensity;
            public Color SnowColor;
            public float WindMinMPH;
            public float WindMaxMPH;
            public float TemperatureOffsetCelsius;
            public float AuroraAlpha;
            public float MasterAmbient;
            public Color FogColor;
            public float DirectFogDensity;
            public Color MoonColour;
            public Color MoonLightColour;
            public Color StarColour;
            public Color MoonGlowColour;
        }

        internal static bool IsActive => GameplaySceneState.IsGameplaySceneActive() && s_RequestedMode > 0;
        internal static bool IsClearBloodMoonActive => GameplaySceneState.IsGameplaySceneActive() && (s_ActiveMode == ModeBloodMoon || s_RequestedMode == ModeBloodMoon);
        internal static bool IsSnowBloodMoonActive => GameplaySceneState.IsGameplaySceneActive() && (s_ActiveMode == ModeLightSnowBloodMoon || s_RequestedMode == ModeLightSnowBloodMoon);
        internal static bool IsAnyBloodMoonActive => IsClearBloodMoonActive || IsSnowBloodMoonActive;
        internal static bool IsFoggyAuroraActive => GameplaySceneState.IsGameplaySceneActive() && (s_ActiveMode == ModeFoggyAurora || s_RequestedMode == ModeFoggyAurora);
        internal static bool IsAuroraVariantActive => GameplaySceneState.IsGameplaySceneActive() && (IsAuroraWeatherVariantMode(s_ActiveMode) || IsAuroraWeatherVariantMode(s_RequestedMode));
        internal static bool IsAuroraGameplayFullyActive => !IsAuroraVariantActive || CurrentEffectBlend >= 0.999f;
        internal static bool IsApplyingCustomBaseStage => GameplaySceneState.IsGameplaySceneActive() && s_IsApplyingCustomBaseStage;
        internal static bool ShouldSpoofFoggyAuroraStage => GameplaySceneState.IsGameplaySceneActive() && s_FoggyAuroraStageSpoofActive && IsFoggyAuroraActive;

        internal static void BeginFoggyAuroraStageSpoof()
        {
            if (s_ActiveMode == ModeFoggyAurora || s_RequestedMode == ModeFoggyAurora) s_FoggyAuroraStageSpoofActive = true;
        }

        internal static void EndFoggyAuroraStageSpoof()
        {
            s_FoggyAuroraStageSpoofActive = false;
        }
        internal static float CurrentEffectBlend => Mathf.Clamp01(s_CurrentEffectBlend);
        internal static WeatherStageId DebugRequestedStageId => s_RequestedMode > 0 ? GetStageIdFromMode(s_RequestedMode) : WeatherStageId.Undefined;

        internal static bool TryForceDebugStage(WeatherStageId stageId, out string message)
        {
            message = string.Empty;

            WeatherStageDefinition definition = WeatherStageCatalog.Get(stageId);
            int mode = GetModeFromStageId(stageId);
            if (!definition.IsCustom || mode == 0)
            {
                message = $"Debug override does not support {definition.DisplayName}.";
                return false;
            }

            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown)
            {
                message = "Debug override requires a valid gameplay weather scene.";
                return false;
            }

            if (snapshot.IsIndoorEnvironment)
            {
                message = "Custom weather showcase override is outdoor-only.";
                return false;
            }

            ClearRuntimeOverrides(clearRequestedMode: true, log: false);
            s_RequestedMode = mode;
            s_NextMaintenanceRealtime = 0f;

            message = $"Forced custom stage: {definition.DisplayName}. Forecast authority is paused until Restore forecast is pressed.";
            Core.Log("[CustomWeatherStage][Debug] " + message, false);
            return true;
        }

        internal static bool ClearDebugStageOverride(out string message)
        {
            bool hadDebugOverride = s_RequestedMode > 0;
            ClearRuntimeOverrides(clearRequestedMode: true, log: false);

            message = hadDebugOverride
                ? "Custom stage override cleared; forecast will resume on the next weather update."
                : "No custom stage override was active.";
            Core.Log("[CustomWeatherStage][Debug] " + message, false);
            return hadDebugOverride;
        }

        internal static void NotifySceneWasLoaded(string sceneName)
        {
            float now = Time.realtimeSinceStartup;
            if (IsAdditiveWeatherSubsceneName(sceneName))
            {
                s_OutdoorVisualsNotBeforeRealtime = Math.Max(s_OutdoorVisualsNotBeforeRealtime, now + AdditiveSceneVisualDelaySeconds);
                s_PrecipitationRestoreNotBeforeRealtime = Math.Max(s_PrecipitationRestoreNotBeforeRealtime, now + AdditiveSceneVisualDelaySeconds);
                return;
            }

            ClearFallingSnowParticleCache();
            s_OutdoorVisualsNotBeforeRealtime = now + PrimarySceneVisualDelaySeconds;
            s_PrecipitationRestoreNotBeforeRealtime = now + PrimarySceneVisualDelaySeconds;
            s_NextPrecipitationRestoreAttemptRealtime = s_PrecipitationRestoreNotBeforeRealtime;

            if (!IsSaveBoundarySceneName(sceneName)) return;
            SuspendVisualsForNonGameplayScene();
        }

        internal static void PrepareForSceneTransition()
        {
            if (s_ForecastOverrideActive || s_ActiveMode > 0) ClearRuntimeOverrides(clearRequestedMode: false, log: false);
            s_SuppressOutdoorNightEventVisuals = true;
            s_InteriorPrecipitationRestorePending = s_InteriorPrecipitationVisualsSuppressed;
        }

        internal static void SuspendVisualsForNonGameplayScene()
        {
            s_SuppressOutdoorNightEventVisuals = true;
            s_InteriorPrecipitationRestorePending = s_InteriorPrecipitationVisualsSuppressed;
        }

        internal static void ResetForMainMenu()
        {
            ClearRuntimeOverrides(clearRequestedMode: true, log: false);
            s_RequestedMode = 0;
            s_ActiveMode = 0;
            s_FoggyAuroraStageSpoofActive = false;
            s_ForecastOverrideActive = false;
            ClearImmediateForecastPlanLatch();
            s_LastAppliedLogMode = 0;
            s_LastForcedBaseStage = WeatherStage.Undefined;
            s_NextMaintenanceRealtime = 0f;
            s_IsApplyingCustomBaseStage = false;

            s_HasFogScaleBackup = false;
            s_CustomFogScaleActive = false;
            s_CurrentCustomFogScale = 1f;

            s_WindLockedByCustomStage = false;
            s_WindRangeMinMPH = WindUnlockedMPH;
            s_WindRangeMaxMPH = WindUnlockedMPH;
            s_CurrentWindLockMPH = WindUnlockedMPH;
            s_TargetWindLockMPH = WindUnlockedMPH;
            s_NextWindTargetRealtime = 0f;
            s_LastWindUpdateRealtime = 0f;
            s_CalculatedTemperatureOffsetCelsius = 0f;

            s_SnowColorOverridden = false;
            s_HasParticleStartColorBackup = false;
            s_HasSnowMaterialColorBackup = false;
            DiscardSnowPresetOverrideState();
            s_SuppressForecastPrecipitationVisuals = false;
            s_InteriorPrecipitationVisualsSuppressed = false;
            s_InteriorPrecipitationRestorePending = false;
            s_PrecipitationRestoreNotBeforeRealtime = 0f;
            s_NextPrecipitationRestoreAttemptRealtime = 0f;
            s_SuppressOutdoorNightEventVisuals = true;
            s_OutdoorVisualsNotBeforeRealtime = 0f;
            s_CurrentSnowColor = Color.white;

            s_CurrentEffectBlend = 1f;
            s_EffectTransitionMode = 0;
            s_EffectStartTemperatureOffsetCelsius = 0f;
            s_HasPendingForecastTransitionStart = false;
            s_ForecastFadeOutTransitionActive = false;
            s_ForecastFadeOutTargetStageId = WeatherStageId.Undefined;

            s_AuroraAlphaOverridden = false;
            s_HasAuroraAlphaBackup = false;
            s_CurrentAuroraAlpha = 0f;
            s_MasterAmbientOverridden = false;
            s_HasMasterAmbientBackup = false;
            s_CurrentMasterAmbient = 1f;
            s_FogColorOverridden = false;
            s_HasFogColorBackup = false;
            s_HasSkyboxFogColorBackup = false;
            s_HasHeightFogSettingsBackup = false;
            s_HasHeightFogActivationBackup = false;
            s_DirectFogDensityOverridden = false;
            s_HasDirectFogDensityBackup = false;
            s_CurrentDirectFogDensity = 0f;
            s_CurrentFogColor = Color.white;

            s_BloodMoonVisualsOverridden = false;
            s_HasBloodMoonVisualsBackup = false;
            s_HasSkyboxMoonColorBackup = false;
            s_HasSkyboxMoonGlowBackup = false;
            s_HasSkyboxSunHaloBackup = false;
            s_HasBloodMoonStarSpherePropertyBlockBackup = false;
            s_OriginalBloodMoonStarSpherePropertyBlock?.Clear();
            s_MoonTextureOverridden = false;
            s_HasMoonTextureBackup = false;
            s_HasSkyboxMoonTextureBackup = false;
            s_OriginalMoonTextureIndex = -1;
            s_OriginalMoonPhaseTexture = null;
            s_OriginalSkyboxMoonTexture = null;
            s_CurrentMoonColour = Color.white;
            s_CurrentMoonLightColour = Color.white;
            s_CurrentStarColour = Color.white;
            s_CurrentMoonGlowColour = Color.white;
            s_AuroraCloudRendererBackups.Clear();
            s_HasAuroraCloudStateBackup = false;
            s_OriginalAuroraCloudTodState = null;

            ClearFallingSnowParticleCache();
        }


        internal static void Update(WeatherSnapshot snapshot)
        {
            UpdateOutdoorVisualContext(snapshot);
            TryRestoreInteriorPrecipitationVisuals(snapshot);

            if (s_RequestedMode == 0)
            {
                if (s_ForecastOverrideActive && (!snapshot.IsValid || !WeatherDirector.Enabled)) ClearRuntimeOverrides(clearRequestedMode: false, log: false);
                if (s_ForecastOverrideActive && IsNightEventVisualMode(s_ActiveMode) && s_SuppressOutdoorNightEventVisuals) SuspendOutdoorNightEventVisualOverrides();
                MaintainForecastVisualOverrides();
                if (s_ForecastOverrideActive && snapshot.IsIndoorEnvironment && s_SuppressForecastPrecipitationVisuals) SuppressInteriorPrecipitationVisuals();
                return;
            }

            if (!snapshot.IsValid || snapshot.IsIndoorEnvironment)
            {
                if (s_ActiveMode != 0) ClearRuntimeOverrides(clearRequestedMode: false, log: false);
                return;
            }

            if (s_RequestedMode != s_ActiveMode)
            {
                ClearRuntimeOverrides(clearRequestedMode: false, log: false);
                s_ActiveMode = s_RequestedMode;
                s_NextMaintenanceRealtime = 0f;
                s_LastForcedBaseStage = WeatherStage.Undefined;
                s_CurrentEffectBlend = 1f;
                ApplyEffectTransitionStart(s_RequestedMode, CaptureEffectTransitionStartSnapshot());
            }

            if (!ModeNeedsContinuousSnowMaintenance(s_RequestedMode))
            {
                if (Time.realtimeSinceStartup < s_NextMaintenanceRealtime) return;
                s_NextMaintenanceRealtime = Time.realtimeSinceStartup + MaintenanceIntervalSeconds;
            }
            else
            {
                s_NextMaintenanceRealtime = 0f;
            }

            if (!ApplyMode(s_RequestedMode)) ClearRuntimeOverrides(clearRequestedMode: true, log: true);
        }

        internal static void CaptureForecastTransitionStart()
        {
            s_PendingForecastTransitionStart = CaptureEffectTransitionStartSnapshot();
            s_HasPendingForecastTransitionStart = true;
        }

        internal static void DiscardForecastTransitionStart()
        {
            s_HasPendingForecastTransitionStart = false;
        }

        internal static void ApplyForecastPlan(WeatherActivationPlan plan, bool immediate, bool suppressPrecipitationVisuals = false, bool suppressOutdoorVisuals = false)
        {
            if (IsActive) return;

            int mode = GetModeFromStageId(plan.StageId);
            SynchronizeImmediateForecastPlanLatch(plan, immediate, mode);
            s_SuppressOutdoorNightEventVisuals = suppressOutdoorVisuals || !CanApplyOutdoorNightEventVisuals(WeatherSnapshot.Capture());
            if (s_SuppressOutdoorNightEventVisuals && IsNightEventVisualMode(s_ActiveMode)) SuspendOutdoorNightEventVisualOverrides();
            if (IsNightEventVisualMode(mode) && s_SuppressOutdoorNightEventVisuals) SuspendOutdoorNightEventVisualOverrides();

            s_SuppressForecastPrecipitationVisuals = suppressPrecipitationVisuals && ForecastPlanUsesPrecipitationVisuals(plan);
            if (!s_SuppressForecastPrecipitationVisuals && s_InteriorPrecipitationVisualsSuppressed && ForecastPlanUsesPrecipitationVisuals(plan)) RequestInteriorPrecipitationVisualRestore();

            if (mode == 0)
            {
                ClearImmediateForecastPlanLatch();
                DiscardForecastTransitionStart();
                FadeForecastOverridesOut(plan, immediate);
                if (s_SuppressForecastPrecipitationVisuals) SuppressInteriorPrecipitationVisuals();
                return;
            }

            s_ForecastFadeOutTransitionActive = false;

            if (!s_ForecastOverrideActive || s_ActiveMode != mode)
            {
                PrepareForecastModeSwitch(mode);
            }

            if (immediate) LatchImmediateForecastPlan(plan);

            if (!ModeNeedsContinuousSnowMaintenance(mode))
            {
                if (Time.realtimeSinceStartup < s_NextMaintenanceRealtime)
                {
                    if (s_SuppressForecastPrecipitationVisuals) SuppressInteriorPrecipitationVisuals();
                    return;
                }
                s_NextMaintenanceRealtime = Time.realtimeSinceStartup + MaintenanceIntervalSeconds;
            }
            else
            {
                s_NextMaintenanceRealtime = 0f;
            }

            bool useImmediateRehydration = immediate || IsImmediateForecastPlan(plan);
            float targetEffectBlend = useImmediateRehydration ? 1f : ComputeForecastEffectBlend(plan);
            s_CurrentEffectBlend = targetEffectBlend;
            ApplyMode(mode);
            if (s_SuppressForecastPrecipitationVisuals) SuppressInteriorPrecipitationVisuals();
        }

        private static void FadeSnowOverridesOut(WeatherActivationPlan plan, float remainingBlend)
        {
            if (!s_SnowPresetOverridden && !s_SnowColorOverridden) return;

            if (remainingBlend <= 0.001f)
            {
                RestoreSnowColor();
                RestoreSnowPresetOverrideToStage(GetVanillaSnowPresetStageForPlan(plan));
                return;
            }

            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;

            WeatherStage targetStage = GetVanillaSnowPresetStageForPlan(plan);
            WeatherStage fadeStage = targetStage != WeatherStage.Clear && targetStage != WeatherStage.Undefined ? targetStage : (s_CurrentSnowPresetStage != WeatherStage.Undefined ? s_CurrentSnowPresetStage : WeatherStage.LightSnow);
            float targetIntensity = GetVanillaSnowIntensityForPlan(plan);
            float intensity = Mathf.Lerp(targetIntensity, Math.Max(0f, s_EffectStartSnowIntensity), remainingBlend);
            Color color = Color.Lerp(Color.white, s_EffectStartSnowColor, remainingBlend);

            if (TrySetSnowPresetBlend(weather, fadeStage))
            {
                if (IsFallingSnowSuppressionMode(s_ActiveMode)) ApplyFallingSnowIntensity(weather, intensity);
                else BoostFallingSnowParticles(weather.m_FallingSnowParticleSystem, weather.m_FallingSnowParticleSystemRenderer, Math.Max(0.1f, intensity));
            }
            if (s_SnowColorOverridden || remainingBlend < 0.999f) ApplySnowColor(color);
        }

        private static WeatherStage GetVanillaSnowPresetStageForPlan(WeatherActivationPlan plan)
        {
            return plan.Stage switch
            {
                WeatherStage.LightSnow or WeatherStage.HeavySnow or WeatherStage.Blizzard => plan.Stage,
                _ => WeatherStage.Clear
            };
        }

        private static float GetVanillaSnowIntensityForPlan(WeatherActivationPlan plan)
        {
            return plan.Stage switch
            {
                WeatherStage.LightSnow or WeatherStage.HeavySnow or WeatherStage.Blizzard => 1f,
                _ => 0f
            };
        }

        private static void FadeWindLockOut(WeatherActivationPlan plan, float remainingBlend)
        {
            if (!s_WindLockedByCustomStage) return;

            if (remainingBlend <= 0.001f)
            {
                RestoreWindLock();
                return;
            }

            float target = GetVanillaWindTargetMPH(plan.Stage);
            float start = s_EffectStartWindMaxMPH >= 0f ? s_EffectStartWindMaxMPH : Math.Max(0f, s_CurrentWindLockMPH);
            ApplyWindLockValue(Mathf.Lerp(target, start, Mathf.Clamp01(remainingBlend)));
        }


        private static float GetVanillaWindTargetMPH(WeatherStage stage)
        {
            return stage switch
            {
                WeatherStage.Blizzard => 55f,
                WeatherStage.HeavySnow => 30f,
                WeatherStage.LightSnow => 16f,
                WeatherStage.Cloudy or WeatherStage.PartlyCloudy => 10f,
                WeatherStage.LightFog or WeatherStage.DenseFog => 3f,
                _ => 4f
            };
        }

        private static void FadeTemperatureOffsetOut(float remainingBlend)
        {
            if (Math.Abs(s_CalculatedTemperatureOffsetCelsius) <= 0.01f) return;

            if (remainingBlend <= 0.001f)
            {
                s_CalculatedTemperatureOffsetCelsius = 0f;
                return;
            }

            s_CalculatedTemperatureOffsetCelsius = Mathf.Lerp(0f, s_EffectStartTemperatureOffsetCelsius, Mathf.Clamp01(remainingBlend));
        }

        private static void UpdateOutdoorVisualContext(WeatherSnapshot snapshot)
        {
            s_SuppressOutdoorNightEventVisuals = !CanApplyOutdoorNightEventVisuals(snapshot);
        }

        private static bool CanApplyOutdoorNightEventVisuals(WeatherSnapshot snapshot)
        {
            if (!snapshot.IsValid || snapshot.IsIndoorEnvironment) return false;
            if (IsSaveBoundarySceneName(snapshot.SceneName) || IsAdditiveWeatherSubsceneName(snapshot.SceneName)) return false;
            if (Time.realtimeSinceStartup < s_OutdoorVisualsNotBeforeRealtime) return false;
            return GameManager.GetUniStorm() != null;
        }

        private static bool ShouldDeferOutdoorNightEventVisuals(int mode)
        {
            if (!IsNightEventVisualMode(mode) || !s_SuppressOutdoorNightEventVisuals) return false;
            SuspendOutdoorNightEventVisualOverrides();
            return true;
        }

        private static bool IsNightEventVisualMode(int mode)
        {
            return mode == ModeCloudyAurora || mode == ModeFoggyAurora || mode == ModeSnowyAurora || mode == ModeBloodMoon || mode == ModeLightSnowBloodMoon;
        }

        private static void SuspendOutdoorNightEventVisualOverrides()
        {
            RestoreFogScale();
            RestoreFogColor();
            RestoreDirectFogDensity();
            RestoreMasterAmbient();
            RestoreAuroraCloudState();
            RestoreBloodMoonVisuals();
            RestoreSnowColor();
            RestoreVisualAuroraAlpha();
            RestoreWindLock();
            RestoreTemperatureLock();
            DiscardSnowPresetOverrideState();
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

        private static bool ForecastPlanUsesPrecipitationVisuals(WeatherActivationPlan plan)
        {
            return StageUsesPrecipitationVisuals(plan.Stage) || FamilyUsesPrecipitationVisuals(plan.Family);
        }

        private static void SynchronizeImmediateForecastPlanLatch(WeatherActivationPlan plan, bool immediate, int mode)
        {
            if (mode == 0)
            {
                ClearImmediateForecastPlanLatch();
                return;
            }

            if (immediate) return;
            if (s_ImmediateForecastPlanLatched && !IsImmediateForecastPlan(plan)) ClearImmediateForecastPlanLatch();
        }

        private static void LatchImmediateForecastPlan(WeatherActivationPlan plan)
        {
            s_ImmediateForecastPlanLatched = true;
            s_ImmediateForecastStageId = plan.StageId;
            s_ImmediateForecastStartWorldHour = plan.StartWorldHour;
            s_ImmediateForecastEndWorldHour = plan.EndWorldHour;
        }

        private static bool IsImmediateForecastPlan(WeatherActivationPlan plan)
        {
            if (!s_ImmediateForecastPlanLatched) return false;
            if (plan.StageId != s_ImmediateForecastStageId) return false;
            if (Math.Abs(plan.StartWorldHour - s_ImmediateForecastStartWorldHour) > 0.01f) return false;
            return Math.Abs(plan.EndWorldHour - s_ImmediateForecastEndWorldHour) <= 0.01f;
        }

        private static void ClearImmediateForecastPlanLatch()
        {
            s_ImmediateForecastPlanLatched = false;
            s_ImmediateForecastStageId = WeatherStageId.Undefined;
            s_ImmediateForecastStartWorldHour = -1f;
            s_ImmediateForecastEndWorldHour = -1f;
        }

        private static bool StageUsesPrecipitationVisuals(WeatherStage stage)
        {
            return stage == WeatherStage.LightSnow || stage == WeatherStage.HeavySnow || stage == WeatherStage.Blizzard;
        }

        private static bool FamilyUsesPrecipitationVisuals(WeatherFamily family)
        {
            return family switch
            {
                WeatherFamily.LightSnow or WeatherFamily.HeavySnow or WeatherFamily.HeavyOvercast or WeatherFamily.VeryHeavySnow or WeatherFamily.Whiteout or WeatherFamily.WindyLightSnow or WeatherFamily.Blizzard or WeatherFamily.ViolentBlizzard or WeatherFamily.LowOvercast or WeatherFamily.Ashfall or WeatherFamily.SnowyAurora or WeatherFamily.FoggyAurora or WeatherFamily.LightSnowBloodMoon => true,
                _ => false
            };
        }

        internal static void ClearForecastOverridesIfAny()
        {
            if (!s_ForecastOverrideActive) return;
            ClearRuntimeOverrides(clearRequestedMode: false, log: false);
        }

        private static void PrepareForecastModeSwitch(int mode)
        {
            s_ForecastFadeOutTransitionActive = false;
            EffectTransitionStartSnapshot transitionStart = ConsumeForecastTransitionStartSnapshot();
            if (s_ForecastOverrideActive && ShouldPreserveGenericTransitionIntoMode(s_ActiveMode, mode))
            {
                RestoreBloodMoonVisuals();
                s_ForecastOverrideActive = true;
                s_ActiveMode = mode;
                s_NextMaintenanceRealtime = 0f;
                s_LastForcedBaseStage = WeatherStage.Undefined;
                s_LastAppliedLogMode = 0;
                ApplyEffectTransitionStart(mode, transitionStart);
                return;
            }

            ClearRuntimeOverrides(clearRequestedMode: false, log: false);
            s_ForecastOverrideActive = true;
            s_ActiveMode = mode;
            s_NextMaintenanceRealtime = 0f;
            s_LastForcedBaseStage = WeatherStage.Undefined;
            s_LastAppliedLogMode = 0;
            ApplyEffectTransitionStart(mode, transitionStart);
        }

        private static bool ShouldPreserveGenericTransitionIntoMode(int previousMode, int nextMode)
        {
            if (previousMode <= 0) return false;
            if (previousMode == nextMode) return false;
            if (nextMode != ModeBloodMoon && nextMode != ModeLightSnowBloodMoon) return false;
            return true;
        }

        private static void FadeForecastOverridesOut(WeatherActivationPlan plan, bool immediate)
        {
            if (!s_ForecastOverrideActive) return;
            if (immediate || s_SuppressForecastPrecipitationVisuals || (s_SuppressOutdoorNightEventVisuals && IsNightEventVisualMode(s_ActiveMode)))
            {
                ClearRuntimeOverrides(clearRequestedMode: false, log: false);
                return;
            }

            EnsureForecastFadeOutTransitionStart(plan);

            float remainingBlend = 1f - ComputeForecastEffectBlend(plan);
            if (remainingBlend <= 0.001f)
            {
                ClearRuntimeOverrides(clearRequestedMode: false, log: false);
                return;
            }

            s_CurrentEffectBlend = remainingBlend;
            try
            {
                FadeGenericOverridesOut(plan, remainingBlend);

                if (s_ActiveMode == ModeBloodMoon || s_ActiveMode == ModeLightSnowBloodMoon) FadeBloodMoonVisualsOut(remainingBlend);
            }
            finally
            {
                s_CurrentEffectBlend = 1f;
            }
        }

        private static void EnsureForecastFadeOutTransitionStart(WeatherActivationPlan plan)
        {
            if (s_ForecastFadeOutTransitionActive && s_ForecastFadeOutTargetStageId == plan.StageId) return;

            ApplyEffectTransitionStart(s_ActiveMode, CaptureEffectTransitionStartSnapshot());
            s_ForecastFadeOutTransitionActive = true;
            s_ForecastFadeOutTargetStageId = plan.StageId;
        }

        private static void FadeGenericOverridesOut(WeatherActivationPlan plan, float remainingBlend)
        {
            if (s_CustomFogScaleActive || s_HasFogScaleBackup)
            {
                float targetFogScale = s_HasFogScaleBackup ? s_OriginalFogScale : 1f;
                ApplyFogScaleImmediate(Mathf.Lerp(targetFogScale, s_CurrentCustomFogScale, remainingBlend), customOverrideActive: true);
            }

            if (s_MasterAmbientOverridden || s_HasMasterAmbientBackup)
            {
                float targetMasterAmbient = s_HasMasterAmbientBackup ? s_OriginalMasterAmbient : 1f;
                ApplyMasterAmbient(Mathf.Lerp(targetMasterAmbient, s_CurrentMasterAmbient, remainingBlend));
            }

            if (s_FogColorOverridden || s_HasFogColorBackup)
            {
                Color targetFogColor = s_HasFogColorBackup ? s_OriginalRenderSettingsFogColor : GetCurrentFogColor();
                ApplyFogColor(Color.Lerp(targetFogColor, s_CurrentFogColor, remainingBlend));
            }

            if (s_DirectFogDensityOverridden || s_HasDirectFogDensityBackup)
            {
                float targetDirectFogDensity = s_HasDirectFogDensityBackup ? s_OriginalRenderSettingsFogDensity : GetCurrentDirectFogDensity();
                ApplyDirectFogDensity(Mathf.Lerp(targetDirectFogDensity, s_CurrentDirectFogDensity, remainingBlend));
            }

            if (s_AuroraAlphaOverridden || s_HasAuroraAlphaBackup)
            {
                float targetAuroraAlpha = s_HasAuroraAlphaBackup ? s_OriginalAuroraAlpha : 0f;
                ApplyVisualAuroraAlpha(Mathf.Lerp(targetAuroraAlpha, s_CurrentAuroraAlpha, remainingBlend));
            }

            FadeSnowOverridesOut(plan, remainingBlend);
            FadeWindLockOut(plan, remainingBlend);
            FadeTemperatureOffsetOut(remainingBlend);
        }

        private static void FadeBloodMoonVisualsOut(float remainingBlend)
        {
            if (!s_BloodMoonVisualsOverridden && !s_HasBloodMoonVisualsBackup && !s_HasSkyboxMoonColorBackup && !s_HasSkyboxMoonGlowBackup) return;

            Color targetMoon = s_HasBloodMoonVisualsBackup ? s_OriginalMoonColourOverride : GetCurrentMoonColourOverride();
            Color targetMoonLight = s_HasBloodMoonVisualsBackup ? s_OriginalMoonLightColor : GetCurrentMoonLightColor();
            Color targetStar = s_HasBloodMoonVisualsBackup ? s_OriginalStarColor : GetCurrentStarColor();
            Color targetGlow = s_HasSkyboxMoonGlowBackup ? s_OriginalSkyboxMoonGlow : GetCurrentMoonGlowColor();

            ApplyBloodMoonVisuals(
                Color.Lerp(targetMoon, s_CurrentMoonColour, remainingBlend),
                Color.Lerp(targetMoonLight, s_CurrentMoonLightColour, remainingBlend),
                Color.Lerp(targetStar, s_CurrentStarColour, remainingBlend),
                Color.Lerp(targetGlow, s_CurrentMoonGlowColour, remainingBlend));
        }


        private static bool ModeNeedsContinuousSnowMaintenance(int mode)
        {
            return mode == ModeLowOvercast || mode == ModeHeavyOvercast || IsAuroraWeatherVariantMode(mode);
        }

        private static bool IsFallingSnowSuppressionMode(int mode)
        {
            return mode == ModeHeavyOvercast || mode == ModeFoggyAurora;
        }

        private static float ComputeForecastEffectBlend(WeatherActivationPlan plan)
        {
            return Mathf.Clamp01(plan.ElapsedHours / GetForecastTransitionHours(plan));
        }

        private static float GetForecastTransitionHours(WeatherActivationPlan plan)
        {
            return Math.Max(0.5f, Math.Min(1f, WeatherOverhaulSettingsManager.WeatherStageTransitionHours));
        }

        private static EffectTransitionStartSnapshot ConsumeForecastTransitionStartSnapshot()
        {
            if (!s_HasPendingForecastTransitionStart) return CaptureEffectTransitionStartSnapshot();
            s_HasPendingForecastTransitionStart = false;
            return s_PendingForecastTransitionStart;
        }

        private static EffectTransitionStartSnapshot CaptureEffectTransitionStartSnapshot()
        {
            Wind wind = GameManager.GetWindComponent();
            float currentWind = wind != null ? Math.Max(0f, wind.GetSpeedMPH()) : Math.Max(0f, s_CurrentWindLockMPH);
            return new EffectTransitionStartSnapshot
            {
                FogScale = s_CustomFogScaleActive ? s_CurrentCustomFogScale : UniStormWeatherSystem.m_FogScale,
                SnowIntensity = s_SnowPresetOverridden ? s_CurrentSnowIntensity : GetCurrentSnowIntensity(),
                SnowColor = s_SnowColorOverridden ? s_CurrentSnowColor : GetCurrentSnowColor(),
                WindMinMPH = currentWind,
                WindMaxMPH = currentWind,
                TemperatureOffsetCelsius = s_CalculatedTemperatureOffsetCelsius,
                AuroraAlpha = s_AuroraAlphaOverridden ? s_CurrentAuroraAlpha : GetCurrentAuroraAlpha(),
                MasterAmbient = s_MasterAmbientOverridden ? s_CurrentMasterAmbient : GetCurrentMasterAmbient(),
                FogColor = s_FogColorOverridden ? s_CurrentFogColor : GetCurrentFogColor(),
                DirectFogDensity = s_DirectFogDensityOverridden ? s_CurrentDirectFogDensity : GetCurrentDirectFogDensity(),
                MoonColour = s_BloodMoonVisualsOverridden ? s_CurrentMoonColour : GetCurrentMoonColourOverride(),
                MoonLightColour = s_BloodMoonVisualsOverridden ? s_CurrentMoonLightColour : GetCurrentMoonLightColor(),
                StarColour = s_BloodMoonVisualsOverridden ? s_CurrentStarColour : GetCurrentStarColor(),
                MoonGlowColour = s_BloodMoonVisualsOverridden ? s_CurrentMoonGlowColour : GetCurrentMoonGlowColor()
            };
        }

        private static void ApplyEffectTransitionStart(int mode, EffectTransitionStartSnapshot snapshot)
        {
            s_EffectTransitionMode = mode;
            s_EffectStartFogScale = snapshot.FogScale;
            s_EffectStartSnowIntensity = snapshot.SnowIntensity;
            s_EffectStartSnowColor = snapshot.SnowColor;
            s_EffectStartWindMinMPH = snapshot.WindMinMPH;
            s_EffectStartWindMaxMPH = snapshot.WindMaxMPH;
            s_EffectStartTemperatureOffsetCelsius = snapshot.TemperatureOffsetCelsius;
            s_EffectStartAuroraAlpha = snapshot.AuroraAlpha;
            s_EffectStartMasterAmbient = snapshot.MasterAmbient;
            s_EffectStartFogColor = snapshot.FogColor;
            s_EffectStartDirectFogDensity = snapshot.DirectFogDensity;
            s_EffectStartMoonColour = snapshot.MoonColour;
            s_EffectStartMoonLightColour = snapshot.MoonLightColour;
            s_EffectStartStarColour = snapshot.StarColour;
            s_EffectStartMoonGlowColour = snapshot.MoonGlowColour;
        }

        private static float BlendFogScale(float targetFogScale)
        {
            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartFogScale : (s_CustomFogScaleActive ? s_CurrentCustomFogScale : 1f);
            return Mathf.Lerp(start, targetFogScale, s_CurrentEffectBlend);
        }

        private static float BlendSnowIntensity(float targetIntensity)
        {
            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartSnowIntensity : s_CurrentSnowIntensity;
            return Mathf.Lerp(start, targetIntensity, s_CurrentEffectBlend);
        }

        private static float GetCurrentSnowIntensity()
        {
            if (s_SnowPresetOverridden) return s_CurrentSnowIntensity;

            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return 0f;

            WeatherStage stage = weather.GetWeatherStage();
            if (stage != WeatherStage.LightSnow && stage != WeatherStage.HeavySnow && stage != WeatherStage.Blizzard) return 0f;
            if (weather.m_FallingSnow == null || !weather.m_FallingSnow.activeSelf) return 0f;
            if (weather.m_FallingSnowParticleSystem == null) return 0f;

            try
            {
                ParticleSystem.EmissionModule emission = weather.m_FallingSnowParticleSystem.emission;
                if (!emission.enabled) return 0f;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentSnowIntensity.1", "CustomWeatherStageRuntime.GetCurrentSnowIntensity failed.", caughtException);
            }

            return 1f;
        }

        private static Color GetCurrentSnowColor()
        {
            var weather = GameManager.GetWeatherComponent();
            if (weather != null)
            {
                try
                {
                    ParticleSystem particleSystem = weather.m_FallingSnowParticleSystem;
                    if (particleSystem != null) return particleSystem.main.startColor.color;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentSnowColor.1", "CustomWeatherStageRuntime.GetCurrentSnowColor failed.", caughtException);
                }
            }

            return s_SnowColorOverridden ? s_CurrentSnowColor : Color.white;
        }

        private static Color BlendSnowColor(Color targetColor)
        {
            Color start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartSnowColor : s_CurrentSnowColor;
            return Color.Lerp(start, targetColor, s_CurrentEffectBlend);
        }

        private static void ApplyWindRangeBlended(float targetMinMPH, float targetMaxMPH)
        {
            float startMin = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartWindMinMPH : Math.Max(0f, s_CurrentWindLockMPH);
            float startMax = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartWindMaxMPH : Math.Max(startMin, s_CurrentWindLockMPH);
            float min = Mathf.Lerp(startMin, targetMinMPH, s_CurrentEffectBlend);
            float max = Mathf.Lerp(startMax, targetMaxMPH, s_CurrentEffectBlend);
            ApplyWindRange(min, max);
        }

        private static void ApplyTemperatureOffsetBlended(float targetOffsetCelsius)
        {
            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartTemperatureOffsetCelsius : s_CalculatedTemperatureOffsetCelsius;
            s_CalculatedTemperatureOffsetCelsius = Mathf.Lerp(start, targetOffsetCelsius, s_CurrentEffectBlend);
        }

        private static void ApplyVisualAuroraAlphaBlended(float targetAlpha)
        {
            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartAuroraAlpha : s_CurrentAuroraAlpha;
            ApplyVisualAuroraAlpha(Mathf.Lerp(start, targetAlpha, s_CurrentEffectBlend));
        }

        private static void ApplyMasterAmbientBlended(float targetIntensity)
        {
            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMasterAmbient : s_CurrentMasterAmbient;
            ApplyMasterAmbient(Mathf.Lerp(start, targetIntensity, s_CurrentEffectBlend));
        }

        private static void ApplyFogColorBlended(Color targetColor, bool applySkyboxFogColor = true)
        {
            Color start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartFogColor : s_CurrentFogColor;
            ApplyFogColor(Color.Lerp(start, targetColor, s_CurrentEffectBlend), applySkyboxFogColor);
        }

        private static void ApplyDirectFogDensityBlended(float targetDensity)
        {
            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartDirectFogDensity : GetCurrentDirectFogDensity();
            ApplyDirectFogDensity(Mathf.Lerp(start, Math.Max(0f, targetDensity), s_CurrentEffectBlend));
        }
        private static void ApplyBloodMoonVisualsBlended(Color moonColor, Color moonLightColor, Color starColor, Color moonGlowColor)
        {
            Color startMoon = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMoonColour : s_CurrentMoonColour;
            Color startMoonLight = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMoonLightColour : s_CurrentMoonLightColour;
            Color startStar = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartStarColour : s_CurrentStarColour;
            Color startGlow = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMoonGlowColour : s_CurrentMoonGlowColour;
            ApplyBloodMoonVisuals(Color.Lerp(startMoon, moonColor, s_CurrentEffectBlend), Color.Lerp(startMoonLight, moonLightColor, s_CurrentEffectBlend), Color.Lerp(startStar, starColor, s_CurrentEffectBlend), Color.Lerp(startGlow, moonGlowColor, s_CurrentEffectBlend));
        }

        private static void RestoreBloodMoonVisualsTowardDefault()
        {
            if (!s_BloodMoonVisualsOverridden && !s_HasBloodMoonVisualsBackup && !s_HasSkyboxMoonColorBackup && !s_HasSkyboxMoonGlowBackup) return;

            if (s_CurrentEffectBlend >= 0.999f)
            {
                RestoreBloodMoonVisuals();
                return;
            }

            Color targetMoon = s_HasBloodMoonVisualsBackup ? s_OriginalMoonColourOverride : GetCurrentMoonColourOverride();
            Color targetMoonLight = s_HasBloodMoonVisualsBackup ? s_OriginalMoonLightColor : GetCurrentMoonLightColor();
            Color targetStar = s_HasBloodMoonVisualsBackup ? s_OriginalStarColor : GetCurrentStarColor();
            Color targetGlow = s_HasSkyboxMoonGlowBackup ? s_OriginalSkyboxMoonGlow : GetCurrentMoonGlowColor();
            Color startMoon = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMoonColour : s_CurrentMoonColour;
            Color startMoonLight = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMoonLightColour : s_CurrentMoonLightColour;
            Color startStar = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartStarColour : s_CurrentStarColour;
            Color startGlow = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMoonGlowColour : s_CurrentMoonGlowColour;

            ApplyBloodMoonVisuals(Color.Lerp(startMoon, targetMoon, s_CurrentEffectBlend), Color.Lerp(startMoonLight, targetMoonLight, s_CurrentEffectBlend), Color.Lerp(startStar, targetStar, s_CurrentEffectBlend), Color.Lerp(startGlow, targetGlow, s_CurrentEffectBlend));
        }

        private static void RestoreMasterAmbientTowardDefault()
        {
            if (!s_MasterAmbientOverridden && !s_HasMasterAmbientBackup) return;

            if (s_CurrentEffectBlend >= 0.999f)
            {
                RestoreMasterAmbient();
                return;
            }

            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartMasterAmbient : s_CurrentMasterAmbient;
            ApplyMasterAmbient(Mathf.Lerp(start, s_OriginalMasterAmbient, s_CurrentEffectBlend));
        }

        private static void RestoreVisualAuroraAlphaTowardDefault()
        {
            if (!s_AuroraAlphaOverridden && !s_HasAuroraAlphaBackup) return;

            if (s_CurrentEffectBlend >= 0.999f)
            {
                RestoreVisualAuroraAlpha();
                return;
            }

            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartAuroraAlpha : s_CurrentAuroraAlpha;
            ApplyVisualAuroraAlpha(Mathf.Lerp(start, s_OriginalAuroraAlpha, s_CurrentEffectBlend));
        }

        private static void RestoreWindLockTowardDefault(WeatherStage targetStage)
        {
            if (!s_WindLockedByCustomStage) return;

            if (s_CurrentEffectBlend >= 0.999f)
            {
                RestoreWindLock();
                return;
            }

            float target = GetVanillaWindTargetMPH(targetStage);
            float start = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartWindMaxMPH : Math.Max(0f, s_CurrentWindLockMPH);
            ApplyWindLockValue(Mathf.Lerp(start, target, s_CurrentEffectBlend));
        }

        private static bool ApplyMode(int mode)
        {
            switch (mode)
            {
                case ModeAshfall:
                    ApplyAshfall();
                    return true;
                case ModeWhiteout:
                    ApplyWhiteout();
                    return true;
                case ModeVeryDenseFog:
                    ApplyVeryDenseFog();
                    return true;
                case ModeVeryHeavySnow:
                    ApplyVeryHeavySnow();
                    return true;
                case ModeWindyLightSnow:
                    ApplyWindyLightSnow();
                    return true;
                case ModeViolentBlizzard:
                    ApplyViolentBlizzard();
                    return true;
                case ModeLowOvercast:
                    ApplyLowOvercast();
                    return true;
                case ModeFreezingFog:
                    ApplyFreezingFog();
                    return true;
                case ModeHeavyOvercast:
                    ApplyHeavyOvercast();
                    return true;
                case ModeCloudyAurora:
                    ApplyCloudyAurora();
                    return true;
                case ModeFoggyAurora:
                    ApplyFoggyAurora();
                    return true;
                case ModeSnowyAurora:
                    ApplySnowyAurora();
                    return true;
                case ModeBloodMoon:
                    ApplyBloodMoon();
                    return true;
                case ModeLightSnowBloodMoon:
                    ApplyLightSnowBloodMoon();
                    return true;
                default:
                    return false;
            }
        }

        private static void ApplyAshfall()
        {
            if (!TryForceBaseStage(WeatherStage.HeavySnow)) return;
            ApplyFogScaleTarget(0.8f);
            ApplyFogColorBlended(new(0.17f, 0.15f, 0.12f, 1f));
            ApplyMasterAmbientBlended(0.5f);
            ApplySnowPresetAndParticles(WeatherStage.HeavySnow, 1.35f, new Color(0.18f, 0.17f, 0.15f, 0.95f));
            ApplyWindRangeBlended(4f, 8f);
            ApplyTemperatureOffsetBlended(0f);
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Ashfall", "Base=HeavySnow | Snow=1.35x | FogScale=0.8 | MasterAmbient=0.5 | FogColor=dark ash | Wind=4-8mph");
        }

        private static void ApplyWhiteout()
        {
            if (!TryForceBaseStage(WeatherStage.HeavySnow)) return;
            ApplyFogScaleTarget(2f);
            ApplyFogColorBlended(new(0.92f, 0.94f, 0.96f, 1f));
            ApplyMasterAmbientBlended(2f);
            ApplySnowPresetAndParticles(WeatherStage.HeavySnow, 1.5f, new Color(0.72f, 0.74f, 0.74f, 0.92f));
            ApplyWindRangeBlended(6f, 10f);
            ApplyTemperatureOffsetBlended(0f);
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Whiteout", "Base=HeavySnow | Snow=1.5x | FogScale=2 | MasterAmbient=2 | FogColor=bright white | Wind=6-10mph");
        }

        private static void ApplyHeavyOvercast()
        {
            if (!TryForceBaseStage(WeatherStage.HeavySnow)) return;
            ApplyFogScaleTarget(FogScaleNone);
            RestoreFogColorTowardDefault();
            RestoreMasterAmbientTowardDefault();
            SuppressFallingSnowBlended(WeatherStage.HeavySnow);
            RestoreSnowColorTowardDefault();
            RestoreWindLockTowardDefault(WeatherStage.HeavySnow);
            ApplyTemperatureOffsetBlended(0f);
            RestoreVisualAuroraAlphaTowardDefault();
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Heavy Overcast", "Base=HeavySnow | Falling snow suppressed | HeavySnow atmosphere, wind and blowing snow retained");
        }

        private static void ApplyVeryDenseFog()
        {
            if (!TryForceBaseStage(WeatherStage.DenseFog)) return;
            ApplyFogScaleTarget(3f);
            ApplyFogColorBlended(new(0.08f, 0.09f, 0.1f, 1f));
            ApplyMasterAmbientBlended(0.1f);
            RestoreSnowPresetOverrideTowardStage(WeatherStage.DenseFog);
            RestoreSnowColorTowardDefault();
            ApplyWindRangeBlended(0f, 3f);
            ApplyTemperatureOffsetBlended(-8f);
            RestoreVisualAuroraAlphaTowardDefault();
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Very Dense Fog", "Base=DenseFog | FogScale=3 | MasterAmbient=0.1 | FogColor=very dark | Wind=0-3mph | TemperatureOffset=-8C");
        }

        private static void ApplyCloudyAurora()
        {
            if (!TryForceAuroraBaseStage()) return;
            if (ShouldDeferOutdoorNightEventVisuals(ModeCloudyAurora)) return;
            ApplyFogScaleTarget(0.6f);
            ApplyFogColorBlended(new(0.05f, 0.065f, 0.06f, 0.035f));
            ApplyDirectFogDensityBlended(0.00004f);
            ApplyMasterAmbientBlended(0.62f);
            RestoreSnowPresetOverrideTowardStage(WeatherStage.ClearAurora);
            RestoreSnowColorTowardDefault();
            ApplyWindRangeBlended(1f, 7f);
            ApplyTemperatureOffsetBlended(0f);
            ApplyVisualAuroraAlphaBlended(1f);
            ApplyAuroraCloudLayer(0.10f, 0.28f, 0.25f, 0.18f, 0.11f, 0.04f, new(0.13f, 0.20f, 0.17f, 1f));
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Cloudy Aurora", "Base=ClearAurora | Aurora gameplay functional | dark muted cyan-green cloud overlay | no aurora lighting override | FogScale=0.6 | DirectFogDensity=0.00004 | SkyboxFogAlpha=0.035 | MasterAmbient=0.62 | CloudCoverage=0.28 | Wind=1-7mph | AuroraAlpha=1");
        }

        private static void ApplyFoggyAurora()
        {
            if (!TryForceBaseStage(WeatherStage.HeavySnow)) return;
            if (ShouldDeferOutdoorNightEventVisuals(ModeFoggyAurora)) return;

            ApplyFogScaleTarget(FogScaleNone);
            RestoreFogColorTowardDefault();
            RestoreDirectFogDensity();
            RestoreMasterAmbientTowardDefault();
            SuppressFallingSnowBlended(WeatherStage.HeavySnow);
            RestoreSnowColorTowardDefault();
            RestoreWindLockTowardDefault(WeatherStage.HeavySnow);
            ApplyTemperatureOffsetBlended(0f);
            ApplyVisualAuroraAlphaBlended(1f);
            RestoreBloodMoonVisualsTowardDefault();

            LogApplied("Foggy Aurora", "Base=HeavySnow | exact Heavy Overcast atmosphere, wind and blowing snow retained | falling snow suppressed | aurora active | no additional fog, color, cloud, ambient, wind or temperature adaptation");
        }

        private static void ApplySnowyAurora()
        {
            if (!TryForceAuroraBaseStage()) return;
            if (ShouldDeferOutdoorNightEventVisuals(ModeSnowyAurora)) return;

            ApplyFogScaleTarget(0.6f);
            ApplyFogColorBlended(new(0.05f, 0.065f, 0.06f, 0.035f));
            ApplyDirectFogDensityBlended(0.00004f);
            ApplyMasterAmbientBlended(0.62f);
            ApplySparseSnowyAuroraSnow(0.10f);
            ApplySnowColor(BlendSnowColor(SnowyAuroraSnowColor));
            ApplyWindRangeBlended(1f, 7f);
            ApplyTemperatureOffsetBlended(0f);
            ApplyVisualAuroraAlphaBlended(1f);
            ApplyAuroraCloudLayer(0.10f, 0.28f, 0.25f, 0.18f, 0.11f, 0.04f, new(0.13f, 0.20f, 0.17f, 1f));
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Snowy Aurora", "Base=ClearAurora | original Cloudy Aurora atmosphere and aurora visuals restored | sparse cyan-green LightSnow emission=0.10x | FogScale=0.6 | DirectFogDensity=0.00004 | MasterAmbient=0.62 | CloudCoverage=0.28 | Wind=1-7mph | AuroraAlpha=1");
        }

        private static void ApplyBloodMoon()
        {
            if (!TryForceBaseStage(WeatherStage.Clear)) return;
            if (ShouldDeferOutdoorNightEventVisuals(ModeBloodMoon)) return;
            ApplyFogScaleTarget(FogScaleNone);
            RestoreFogColorTowardDefault();
            ApplyMasterAmbientBlended(BloodMoonMasterAmbient);
            RestoreSnowPresetOverrideTowardStage(WeatherStage.Clear);
            RestoreSnowColorTowardDefault();
            RestoreWindLockTowardDefault(WeatherStage.Clear);
            ApplyTemperatureOffsetBlended(0f);
            RestoreVisualAuroraAlphaTowardDefault();
            ApplyBloodMoonVisualsBlended(BloodMoonMoonColor, BloodMoonMoonLightColor, BloodMoonStarColor, BloodMoonMoonGlowColor);
            LogApplied("Blood Moon", "Base=Clear | FogScale/fog color transition restored from previous weather | MasterAmbient=0.26 | MoonColor=immediate muted red | MoonLight=muted red | No moon color transition");
        }

        private static void ApplyLightSnowBloodMoon()
        {
            if (!TryForceBaseStage(WeatherStage.PartlyCloudy)) return;
            if (ShouldDeferOutdoorNightEventVisuals(ModeLightSnowBloodMoon)) return;
            ApplyFogScaleTarget(FogScaleNone);
            RestoreFogColorTowardDefault();
            ApplyMasterAmbientBlended(BloodMoonMasterAmbient);
            ApplySnowBloodMoonParticles(SnowBloodMoonSnowIntensity, SnowBloodMoonSnowColor);
            RestoreWindLockTowardDefault(WeatherStage.PartlyCloudy);
            ApplyTemperatureOffsetBlended(0f);
            RestoreVisualAuroraAlphaTowardDefault();
            ApplyBloodMoonVisualsBlended(BloodMoonMoonColor, BloodMoonMoonLightColor, BloodMoonStarColor, BloodMoonMoonGlowColor);
            LogApplied("Snow Blood Moon", "Base=PartlyCloudy | FogScale/fog color transition restored from previous weather | LightSnow particle preset low intensity | SnowColor=transition to muted red | Blowing snow emission disabled | MoonColor=immediate muted red");
        }


        private static void ApplyVeryHeavySnow()
        {
            if (!TryForceBaseStage(WeatherStage.HeavySnow)) return;
            ApplyFogScaleTarget(2f);
            ApplySnowPresetAndParticles(WeatherStage.Blizzard, 2f);
            ApplyWindRangeBlended(7f, 13f);
            ApplyTemperatureOffsetBlended(0f);
            RestoreMasterAmbientTowardDefault();
            RestoreFogColor();
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Very Heavy Snow", "Base=HeavySnow | Snow=2x | FogScale=2 | Wind=7-13mph");
        }

        private static void ApplyWindyLightSnow()
        {
            if (!TryForceBaseStage(WeatherStage.LightSnow)) return;
            ApplyFogScaleTarget(FogScaleNone);
            ApplySnowPresetAndParticles(WeatherStage.LightSnow, 1.1f);
            ApplyWindRangeBlended(55f, 68f);
            ApplyTemperatureOffsetBlended(0f);
            RestoreMasterAmbientTowardDefault();
            RestoreFogColor();
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Windy Light Snow", "Base=LightSnow | Snow=1.1x | Wind=55-68mph (~90-110km/h)");
        }

        private static void ApplyViolentBlizzard()
        {
            if (!TryForceBaseStage(WeatherStage.Blizzard)) return;
            ApplyFogScaleTarget(2.5f);
            ApplySnowPresetAndParticles(WeatherStage.Blizzard, 3f);
            ApplyWindRangeBlended(92f, 100f);
            ApplyTemperatureOffsetBlended(-15f);
            RestoreMasterAmbientTowardDefault();
            RestoreFogColor();
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Violent Blizzard", "Base=Blizzard | Snow=3x | FogScale=2.5 | Wind=92-100mph (~148-161km/h) | TemperatureOffset=-15C");
        }

        private static void ApplyLowOvercast()
        {
            if (!TryForceBaseStage(WeatherStage.Cloudy)) return;
            ApplyFogScaleTarget(FogScaleNone);
            ApplySnowPresetAndParticles(WeatherStage.LightSnow, 0.5f);
            ApplyWindRangeBlended(3f, 8f);
            ApplyTemperatureOffsetBlended(0f);
            RestoreMasterAmbientTowardDefault();
            RestoreFogColor();
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Low Overcast", "Base=Cloudy | LightSnow particles=0.5x | Wind=3-8mph");
        }

        private static void ApplyFreezingFog()
        {
            if (!TryForceBaseStage(WeatherStage.LightFog)) return;
            ApplyFogScaleTarget(1.25f);
            ApplyFogColorBlended(new(0.72f, 0.78f, 0.82f, 1f));
            ApplyMasterAmbientBlended(0.62f);
            RestoreSnowPresetOverrideTowardStage(WeatherStage.LightFog);
            RestoreSnowColorTowardDefault();
            ApplyWindRangeBlended(0f, 0f);
            ApplyTemperatureOffsetBlended(-40f);
            RestoreVisualAuroraAlphaTowardDefault();
            RestoreBloodMoonVisualsTowardDefault();
            LogApplied("Freezing Fog", "Base=LightFog | FogScale=1.25 | Wind=0mph | TemperatureOffset=-40C | no snow");
        }

        private static bool TryForceAuroraBaseStage()
        {
            var weather = GameManager.GetWeatherComponent();
            WeatherTransition transition = GameManager.GetWeatherTransitionComponent();
            if (weather == null || transition == null) return false;
            if (s_LastForcedBaseStage == WeatherStage.ClearAurora) return true;

            WeatherStage currentStage = weather.GetWeatherStage();
            if (currentStage == WeatherStage.ClearAurora)
            {
                s_LastForcedBaseStage = WeatherStage.ClearAurora;
                return true;
            }

            try
            {
                s_IsApplyingCustomBaseStage = true;

                transition.ChooseNextWeatherSet(null, true, true);
                currentStage = weather.GetWeatherStage();

                if (currentStage != WeatherStage.ClearAurora)
                {
                    transition.ActivateWeatherSetAtFrac(WeatherStage.ClearAurora, 0f);
                    currentStage = weather.GetWeatherStage();
                }

                if (currentStage != WeatherStage.ClearAurora)
                {
                    transition.ForceTransitionToWeatherStage((int)WeatherStage.ClearAurora);
                    currentStage = weather.GetWeatherStage();
                }

                if (currentStage != WeatherStage.ClearAurora)
                {
                    transition.ForceUnmanagedWeatherStage(WeatherStage.ClearAurora, ForceTransitionSeconds);
                    currentStage = weather.GetWeatherStage();
                }
            }
            finally
            {
                s_IsApplyingCustomBaseStage = false;
            }

            if (currentStage == WeatherStage.ClearAurora)
            {
                s_LastForcedBaseStage = WeatherStage.ClearAurora;
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log("[CustomWeatherStage] Forced base stage Clear aurora.");
                return true;
            }

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[CustomWeatherStage] Could not force base stage Clear aurora; current stage is {WeatherStageFormatter.ToDisplayName(currentStage)}.");
            return false;
        }

        private static bool TryForceBaseStage(WeatherStage baseStage)
        {
            var weather = GameManager.GetWeatherComponent();
            WeatherTransition transition = GameManager.GetWeatherTransitionComponent();
            if (weather == null || transition == null) return false;
            if (s_LastForcedBaseStage == baseStage) return true;

            WeatherStage currentStage = weather.GetWeatherStage();
            if (currentStage == baseStage)
            {
                s_LastForcedBaseStage = baseStage;
                return true;
            }

            try
            {
                s_IsApplyingCustomBaseStage = true;

                transition.ActivateWeatherSetImmediate(baseStage);
                currentStage = weather.GetWeatherStage();
                if (currentStage != baseStage)
                {
                    transition.ForceTransitionToWeatherStage((int)baseStage);
                    currentStage = weather.GetWeatherStage();
                }

                if (currentStage != baseStage)
                {
                    transition.ForceUnmanagedWeatherStage(baseStage, ForceTransitionSeconds);
                    currentStage = weather.GetWeatherStage();
                }
            }
            finally
            {
                s_IsApplyingCustomBaseStage = false;
            }

            if (currentStage == baseStage)
            {
                s_LastForcedBaseStage = baseStage;
                if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[CustomWeatherStage] Forced base stage {WeatherStageFormatter.ToDisplayName(baseStage)}.");
                return true;
            }

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[CustomWeatherStage] Could not force base stage {WeatherStageFormatter.ToDisplayName(baseStage)}; current stage is {WeatherStageFormatter.ToDisplayName(currentStage)}.");
            return false;
        }

        private static void ApplyFogScaleTarget(float fogScale)
        {
            if (fogScale < 0f)
            {
                if (!s_HasFogScaleBackup && !s_CustomFogScaleActive) return;
                float restoredFogScale = BlendFogScale(1f);
                if (s_CurrentEffectBlend >= 0.999f)
                {
                    RestoreFogScale();
                    return;
                }

                ApplyFogScaleImmediate(restoredFogScale, customOverrideActive: true);
                return;
            }

            ApplyFogScaleImmediate(BlendFogScale(Math.Max(0f, fogScale)), customOverrideActive: true);
        }

        private static void ApplyFogScaleImmediate(float fogScale, bool customOverrideActive)
        {
            BackupFogScale();
            float clampedFogScale = Math.Max(0f, fogScale);
            UniStormWeatherSystem.m_FogScale = clampedFogScale;
            s_CustomFogScaleActive = customOverrideActive;
            s_CurrentCustomFogScale = customOverrideActive ? clampedFogScale : 1f;
        }

        private static void ApplySparseSnowyAuroraSnow(float intensity)
        {
            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;
            if (s_SuppressForecastPrecipitationVisuals)
            {
                SuppressInteriorPrecipitationVisuals();
                return;
            }

            float effectiveIntensity = Math.Max(0f, Math.Min(0.5f, BlendSnowIntensity(intensity)));
            if (!TrySetSnowPresetBlend(weather, WeatherStage.LightSnow)) return;

            s_CurrentSnowIntensity = effectiveIntensity;
            RefreshFallingSnowParticleCache(weather);
            for (int i = 0; i < s_CachedFallingSnowSystems.Length; i++)
            {
                ParticleSystem particleSystem = s_CachedFallingSnowSystems[i];
                if (particleSystem == null) continue;
                ApplySparseSnowyAuroraParticleSystem(particleSystem, GetParticleSystemRenderer(particleSystem), effectiveIntensity);
            }
        }

        private static void ApplySparseSnowyAuroraParticleSystem(ParticleSystem particleSystem, ParticleSystemRenderer renderer, float intensity)
        {
            if (particleSystem == null) return;

            float clampedIntensity = Math.Max(0f, Math.Min(0.5f, intensity));
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            if (clampedIntensity <= 0.001f)
            {
                emission.enabled = false;
                if (particleSystem.isPlaying) particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (particleSystem.gameObject != null && !particleSystem.gameObject.activeSelf) particleSystem.gameObject.SetActive(true);

            float rateOverTime = 5000f * clampedIntensity;
            emission.enabled = true;
            emission.rateOverTime = new(rateOverTime);

            ParticleSystem.MainModule main = particleSystem.main;
            const float lifetimeSeconds = 7.5f;
            main.startLifetime = lifetimeSeconds;
            main.maxParticles = Math.Min(10000, Math.Max(750, (int)Math.Ceiling(rateOverTime * lifetimeSeconds * 1.35f)));
            main.gravityModifier = 0.35f;

            if (renderer != null) renderer.velocityScale = 0.012f;
            if (!particleSystem.isPlaying) particleSystem.Play();
        }

        private static void ApplySnowPresetAndParticles(WeatherStage snowPresetStage, float intensity)
        {
            ApplySnowPresetAndParticles(snowPresetStage, intensity, null);
        }

        private static void ApplySnowPresetAndParticles(WeatherStage snowPresetStage, float intensity, Color? color)
        {
            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;
            if (s_SuppressForecastPrecipitationVisuals)
            {
                SuppressInteriorPrecipitationVisuals();
                if (color.HasValue) RestoreSnowColorTowardDefault();
                return;
            }

            float effectiveIntensity = BlendSnowIntensity(intensity);
            if (TrySetSnowPresetBlend(weather, snowPresetStage))
            {
                BoostFallingSnowParticles(weather.m_FallingSnowParticleSystem, weather.m_FallingSnowParticleSystemRenderer, effectiveIntensity);
            }

            if (color.HasValue) ApplySnowColor(BlendSnowColor(color.Value));
            else RestoreSnowColorTowardDefault();
        }

        private static void SuppressFallingSnowBlended(WeatherStage snowPresetStage)
        {
            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;
            if (s_SuppressForecastPrecipitationVisuals)
            {
                SuppressInteriorPrecipitationVisuals();
                return;
            }

            if (!TrySetSnowPresetBlend(weather, snowPresetStage, activateFallingSnowRoot: false)) return;
            ApplyFallingSnowIntensity(weather, BlendSnowIntensity(0f));
            s_FallingSnowSuppressedByCustomStage = true;
        }

        private static void MaintainHeavyOvercastFallingSnowSuppression()
        {
            if (!s_FallingSnowSuppressedByCustomStage) return;
            if (s_ActiveMode != ModeHeavyOvercast && s_RequestedMode != ModeHeavyOvercast && s_ActiveMode != ModeFoggyAurora && s_RequestedMode != ModeFoggyAurora) return;

            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;
            if (s_CurrentSnowIntensity <= 0.001f && weather.m_FallingSnow != null && !weather.m_FallingSnow.activeSelf) return;
            ApplyFallingSnowIntensity(weather, s_CurrentSnowIntensity);
        }

        private static void ApplyFallingSnowIntensity(Il2Cpp.Weather weather, float intensity)
        {
            if (weather == null) return;

            float clampedIntensity = Math.Max(0f, Math.Min(4f, intensity));
            s_CurrentSnowIntensity = clampedIntensity;
            RefreshFallingSnowParticleCache(weather);
            SetGameObjectActive(weather.m_FallingSnow, true);

            for (int i = 0; i < s_CachedFallingSnowSystems.Length; i++)
            {
                ParticleSystem particleSystem = s_CachedFallingSnowSystems[i];
                if (particleSystem == null) continue;
                ApplySingleFallingSnowTransitionIntensity(particleSystem, GetParticleSystemRenderer(particleSystem), clampedIntensity);
            }
        }

        private static void ApplySingleFallingSnowTransitionIntensity(ParticleSystem particleSystem, ParticleSystemRenderer renderer, float intensity)
        {
            if (particleSystem == null) return;

            float clampedIntensity = Math.Max(0f, Math.Min(4f, intensity));
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            if (clampedIntensity <= 0.001f)
            {
                emission.enabled = false;
                if (particleSystem.isPlaying) particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (particleSystem.gameObject != null && !particleSystem.gameObject.activeSelf) particleSystem.gameObject.SetActive(true);
            emission.enabled = true;
            emission.rateOverTime = new(5000f * clampedIntensity);

            if (renderer != null) renderer.velocityScale = Math.Min(0.025f, 0.01f + 0.004f * clampedIntensity);
            if (!particleSystem.isPlaying) particleSystem.Play();
        }

        private static void RefreshFallingSnowParticleCache(Il2Cpp.Weather weather)
        {
            GameObject root = weather.m_FallingSnow;
            if (weather == s_CachedFallingSnowWeather && root == s_CachedFallingSnowRoot && s_CachedFallingSnowSystems.Length > 0) return;

            s_CachedFallingSnowWeather = weather;
            s_CachedFallingSnowRoot = root;
            if (root == null)
            {
                s_CachedFallingSnowSystems = weather.m_FallingSnowParticleSystem != null ? new[] { weather.m_FallingSnowParticleSystem } : Array.Empty<ParticleSystem>();
                return;
            }

            try
            {
                s_CachedFallingSnowSystems = root.GetComponentsInChildren<ParticleSystem>(true) ?? Array.Empty<ParticleSystem>();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.RefreshFallingSnowParticleCache.1", "CustomWeatherStageRuntime.RefreshFallingSnowParticleCache failed.", caughtException);
                s_CachedFallingSnowSystems = weather.m_FallingSnowParticleSystem != null ? new[] { weather.m_FallingSnowParticleSystem } : Array.Empty<ParticleSystem>();
            }
        }

        private static void ClearFallingSnowParticleCache()
        {
            s_CachedFallingSnowWeather = null;
            s_CachedFallingSnowRoot = null;
            s_CachedFallingSnowSystems = Array.Empty<ParticleSystem>();
        }

        private static void ApplySnowBloodMoonParticles(float intensity, Color color)
        {
            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;
            if (s_SuppressForecastPrecipitationVisuals)
            {
                SuppressInteriorPrecipitationVisuals();
                RestoreSnowColorTowardDefault();
                return;
            }

            float effectiveIntensity = Math.Max(0.05f, Math.Min(0.45f, BlendSnowIntensity(intensity)));
            Color blendedColor = BlendSnowColor(color);
            if (TrySetSnowPresetBlend(weather, WeatherStage.LightSnow)) BoostFallingSnowParticles(weather.m_FallingSnowParticleSystem, weather.m_FallingSnowParticleSystemRenderer, effectiveIntensity);
            DisableBlowingSnowEmission(weather, blendedColor);
            ApplySnowColor(blendedColor);
        }

        private static void SuppressInteriorPrecipitationVisuals()
        {
            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;

            StopParticleSystem(weather.m_FallingSnowParticleSystem);
            StopParticleSystem(weather.m_BlowingSnowParticleSystem);
            s_InteriorPrecipitationVisualsSuppressed = true;
            s_InteriorPrecipitationRestorePending = false;
        }

        private static void RequestInteriorPrecipitationVisualRestore()
        {
            if (!s_InteriorPrecipitationVisualsSuppressed) return;
            s_InteriorPrecipitationRestorePending = true;
            s_NextPrecipitationRestoreAttemptRealtime = Math.Max(s_NextPrecipitationRestoreAttemptRealtime, s_PrecipitationRestoreNotBeforeRealtime);
        }

        private static void TryRestoreInteriorPrecipitationVisuals(WeatherSnapshot snapshot)
        {
            if (!s_InteriorPrecipitationVisualsSuppressed)
            {
                s_InteriorPrecipitationRestorePending = false;
                return;
            }

            if (!snapshot.IsValid || snapshot.IsIndoorEnvironment || IsSaveBoundarySceneName(snapshot.SceneName) || IsAdditiveWeatherSubsceneName(snapshot.SceneName)) return;

            RequestInteriorPrecipitationVisualRestore();
            float now = Time.realtimeSinceStartup;
            if (!s_InteriorPrecipitationRestorePending || now < s_PrecipitationRestoreNotBeforeRealtime || now < s_NextPrecipitationRestoreAttemptRealtime) return;

            s_NextPrecipitationRestoreAttemptRealtime = now + PrecipitationRestoreRetrySeconds;
            var weather = GameManager.GetWeatherComponent();
            if (!IsPrecipitationRuntimeReady(weather)) return;

            try
            {
                SetGameObjectActive(weather.m_FallingSnow, true);
                SetGameObjectActive(weather.m_BlowingSnow, true);
                RestoreParticleSystemEmission(weather.m_FallingSnowParticleSystem);
                RestoreParticleSystemEmission(weather.m_BlowingSnowParticleSystem);
                weather.UpdateFallingSnowPreset();
                s_InteriorPrecipitationVisualsSuppressed = false;
                s_InteriorPrecipitationRestorePending = false;
                if (s_ActiveMode == ModeHeavyOvercast || s_ActiveMode == ModeFoggyAurora) MaintainHeavyOvercastFallingSnowSuppression();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.TryRestoreInteriorPrecipitationVisuals.1", "Deferred precipitation visual restoration failed and will be retried after the weather runtime is ready.", caughtException);
            }
        }

        private static bool IsPrecipitationRuntimeReady(Il2Cpp.Weather? weather)
        {
            if (weather == null) return false;
            if (GameManager.GetUniStorm() == null) return false;
            if (weather.m_FallingSnow == null || weather.m_FallingSnowParticleSystem == null) return false;
            if (weather.m_FallingSnowCurrentPreset == null) return false;
            return true;
        }

        private static void StopParticleSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null) return;

            try
            {
                ParticleSystem.EmissionModule emission = particleSystem.emission;
                emission.enabled = false;
                if (particleSystem.isPlaying) particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.StopParticleSystem.1", "CustomWeatherStageRuntime.StopParticleSystem failed.", caughtException);
            }
        }

        private static void SetGameObjectActive(GameObject gameObject, bool active)
        {
            if (gameObject == null) return;
            if (gameObject.activeSelf != active) gameObject.SetActive(active);
        }

        private static void RestoreParticleSystemEmission(ParticleSystem particleSystem)
        {
            if (particleSystem == null) return;

            try
            {
                ParticleSystem.EmissionModule emission = particleSystem.emission;
                emission.enabled = true;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreParticleSystemEmission.1", "CustomWeatherStageRuntime.RestoreParticleSystemEmission failed.", caughtException);
            }
        }

        private static void DisableBlowingSnowEmission(Il2Cpp.Weather weather, Color color)
        {
            if (weather == null) return;

            try
            {
                ParticleSystem particleSystem = weather.m_BlowingSnowParticleSystem;
                if (particleSystem != null)
                {
                    ParticleSystem.EmissionModule emission = particleSystem.emission;
                    emission.enabled = false;
                    if (particleSystem.isPlaying) particleSystem.Stop();
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.DisableBlowingSnowEmission.1", "CustomWeatherStageRuntime.DisableBlowingSnowEmission failed.", caughtException);
            }

            weather.SetBlowingSnowColor(color);
        }

        private static bool TrySetSnowPresetBlend(Il2Cpp.Weather weather, WeatherStage snowPresetStage, bool activateFallingSnowRoot = true)
        {
            if (weather == null) return false;
            if (weather.m_FallingSnowParticleSystem == null || weather.m_FallingSnowParticleSystemRenderer == null) return false;

            try
            {
                bool presetChanged = !s_SnowPresetOverridden || s_CurrentSnowPresetStage != snowPresetStage || s_CurrentSnowPresetWeather != weather;
                if (presetChanged)
                {
                    weather.SetSnowPresetBlend(snowPresetStage, snowPresetStage, 1f, 0f);
                    weather.UpdateFallingSnowPreset();
                }

                if (activateFallingSnowRoot && weather.m_FallingSnow != null && !weather.m_FallingSnow.activeSelf) weather.m_FallingSnow.SetActive(true);
                s_SnowPresetOverridden = true;
                s_CurrentSnowPresetStage = snowPresetStage;
                s_CurrentSnowPresetWeather = weather;
                return true;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.TrySetSnowPresetBlend.1", "CustomWeatherStageRuntime.TrySetSnowPresetBlend failed.", caughtException);
                return false;
            }
        }

        private static void BoostFallingSnowParticles(ParticleSystem particleSystem, ParticleSystemRenderer renderer, float intensity)
        {
            if (particleSystem == null) return;

            float clampedIntensity = Math.Max(0.1f, Math.Min(4f, intensity));
            s_CurrentSnowIntensity = clampedIntensity;
            BoostSingleSnowParticleSystem(particleSystem, renderer, clampedIntensity);

            var weather = GameManager.GetWeatherComponent();
            if (weather == null || weather.m_FallingSnow == null) return;

            try
            {
                ParticleSystem[] childSystems = weather.m_FallingSnow.GetComponentsInChildren<ParticleSystem>(true);
                if (childSystems == null) return;

                for (int i = 0; i < childSystems.Length; i++)
                {
                    ParticleSystem childSystem = childSystems[i];
                    if (childSystem == null || childSystem == particleSystem) continue;
                    BoostSingleSnowParticleSystem(childSystem, GetParticleSystemRenderer(childSystem), clampedIntensity);
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.BoostFallingSnowParticles.1", "CustomWeatherStageRuntime.BoostFallingSnowParticles failed.", caughtException);
            }
        }

        private static void BoostSingleSnowParticleSystem(ParticleSystem particleSystem, ParticleSystemRenderer renderer, float intensity)
        {
            if (particleSystem == null) return;

            float clampedIntensity = Math.Max(0.1f, Math.Min(4f, intensity));
            if (particleSystem.gameObject != null && !particleSystem.gameObject.activeSelf) particleSystem.gameObject.SetActive(true);
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            float rateOverTime = 5000f * clampedIntensity;
            emission.rateOverTime = new(rateOverTime);

            ParticleSystem.MainModule main = particleSystem.main;
            float startLifetime = Math.Max(2.5f, 7.5f / Math.Min(3f, clampedIntensity));
            main.startLifetime = startLifetime;
            main.maxParticles = Math.Min(50000, Math.Max(1000, (int)Math.Ceiling(rateOverTime * startLifetime * 1.35f)));
            main.gravityModifier = Math.Min(1.6f, 0.55f * clampedIntensity);

            if (renderer != null) renderer.velocityScale = Math.Min(0.025f, 0.01f + 0.004f * clampedIntensity);
            if (!particleSystem.isPlaying) particleSystem.Play();
        }

        private static void ApplySnowColor(Color color)
        {
            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;

            TrySetWeatherSnowColors(weather, color);
            ApplyDirectSnowTint(weather, color);
            s_CurrentSnowColor = color;
            s_SnowColorOverridden = true;
        }

        private static bool TrySetWeatherSnowColors(Il2Cpp.Weather weather, Color color)
        {
            if (!IsWeatherSnowColorContextUsable(weather)) return false;

            try
            {
                weather.SetSnowColor(color);
                weather.SetBlowingSnowColor(color);
                return true;
            }
            catch (Exception caughtException)
            {
                if (IsGameplayWeatherSceneActive())
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.TrySetWeatherSnowColors.1", "CustomWeatherStageRuntime.TrySetWeatherSnowColors failed.", caughtException);
                }

                return false;
            }
        }

        private static bool IsGameplayWeatherSceneActive()
        {
            try
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                return !IsSaveBoundarySceneName(sceneName);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWeatherSnowColorContextUsable(Il2Cpp.Weather weather)
        {
            if (weather == null || !IsGameplayWeatherSceneActive()) return false;

            try
            {
                if (GameManager.GetWeatherComponent() != weather) return false;
                return weather.m_FallingSnowParticleSystem != null && weather.m_BlowingSnowParticleSystem != null;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyDirectSnowTint(Il2Cpp.Weather weather, Color color)
        {
            if (weather == null) return;

            ApplyDirectParticleSnowTint(weather.m_FallingSnowParticleSystem, weather.m_FallingSnowParticleSystemRenderer, color, backupPrimary: true);
            ApplyDirectSnowTintUnderRoot(weather.m_FallingSnow, weather.m_FallingSnowParticleSystem, color);
            ApplyDirectParticleSnowTint(weather.m_BlowingSnowParticleSystem, GetParticleSystemRenderer(weather.m_BlowingSnowParticleSystem), color, backupPrimary: false);
            ApplyDirectSnowTintUnderRoot(weather.m_BlowingSnow, weather.m_BlowingSnowParticleSystem, color);
        }

        private static void ApplyDirectSnowTintUnderRoot(GameObject root, ParticleSystem primaryParticleSystem, Color color)
        {
            if (root == null) return;

            try
            {
                ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
                if (particleSystems != null)
                {
                    for (int i = 0; i < particleSystems.Length; i++)
                    {
                        ParticleSystem particleSystem = particleSystems[i];
                        if (particleSystem == null || particleSystem == primaryParticleSystem) continue;
                        ApplyDirectParticleSnowTint(particleSystem, GetParticleSystemRenderer(particleSystem), color, backupPrimary: false);
                    }
                }

            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyDirectSnowTintUnderRoot.1", "CustomWeatherStageRuntime.ApplyDirectSnowTintUnderRoot failed.", caughtException);
            }
        }

        private static void ApplyDirectParticleSnowTint(ParticleSystem particleSystem, ParticleSystemRenderer renderer, Color color, bool backupPrimary)
        {
            try
            {
                if (particleSystem != null)
                {
                    ParticleSystem.MainModule main = particleSystem.main;
                    if (backupPrimary && !s_HasParticleStartColorBackup)
                    {
                        s_OriginalParticleStartColor = main.startColor;
                        s_HasParticleStartColorBackup = true;
                    }
                    main.startColor = new(color);
                }

                ApplyDirectRendererSnowTint(renderer, color, backupPrimary);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyDirectParticleSnowTint.1", "CustomWeatherStageRuntime.ApplyDirectParticleSnowTint failed.", caughtException);
            }
        }

        private static void ApplyDirectRendererSnowTint(ParticleSystemRenderer renderer, Color color, bool backupPrimary)
        {
            try
            {
                if (renderer == null) return;
                Material material = renderer.material;
                if (material == null) return;

                if (material.HasProperty("_Color"))
                {
                    if (backupPrimary && !s_HasSnowMaterialColorBackup)
                    {
                        s_OriginalSnowMaterialColor = material.GetColor("_Color");
                        s_HasSnowMaterialColorBackup = true;
                    }
                    material.SetColor("_Color", color);
                }
                else if (material.HasProperty("_BaseColor"))
                {
                    if (backupPrimary && !s_HasSnowMaterialColorBackup)
                    {
                        s_OriginalSnowMaterialColor = material.GetColor("_BaseColor");
                        s_HasSnowMaterialColorBackup = true;
                    }
                    material.SetColor("_BaseColor", color);
                }
                else if (material.HasProperty("_TintColor"))
                {
                    if (backupPrimary && !s_HasSnowMaterialColorBackup)
                    {
                        s_OriginalSnowMaterialColor = material.GetColor("_TintColor");
                        s_HasSnowMaterialColorBackup = true;
                    }
                    material.SetColor("_TintColor", color);
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyDirectRendererSnowTint.1", "CustomWeatherStageRuntime.ApplyDirectRendererSnowTint failed.", caughtException);
            }
        }


        private static ParticleSystemRenderer? GetParticleSystemRenderer(ParticleSystem particleSystem)
        {
            if (particleSystem == null) return null;

            try
            {
                return particleSystem.GetComponent<ParticleSystemRenderer>();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetParticleSystemRenderer.1", "CustomWeatherStageRuntime.GetParticleSystemRenderer failed.", caughtException);
                return null;
            }
        }

        private static void RestoreSnowColorTowardDefault()
        {
            if (!s_SnowColorOverridden)
            {
                s_CurrentSnowColor = Color.white;
                return;
            }

            if (s_CurrentEffectBlend >= 0.999f)
            {
                RestoreSnowColor();
                return;
            }

            ApplySnowColor(BlendSnowColor(Color.white));
        }

        private static void RestoreSnowColor()
        {
            if (!s_SnowColorOverridden) return;
            var weather = GameManager.GetWeatherComponent();
            if (IsWeatherSnowColorContextUsable(weather))
            {
                try
                {
                    TrySetWeatherSnowColors(weather, Color.white);

                    ParticleSystem particleSystem = weather.m_FallingSnowParticleSystem;
                    if (particleSystem != null && s_HasParticleStartColorBackup)
                    {
                        ParticleSystem.MainModule main = particleSystem.main;
                        main.startColor = s_OriginalParticleStartColor;
                    }

                    ParticleSystemRenderer renderer = weather.m_FallingSnowParticleSystemRenderer;
                    Material material = renderer != null ? renderer.material : null;
                    if (material != null && s_HasSnowMaterialColorBackup)
                    {
                        if (material.HasProperty("_Color")) material.SetColor("_Color", s_OriginalSnowMaterialColor);
                        else if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", s_OriginalSnowMaterialColor);
                        else if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", s_OriginalSnowMaterialColor);
                    }

                    ApplyDirectSnowTintUnderRoot(weather.m_FallingSnow, weather.m_FallingSnowParticleSystem, Color.white);
                    ApplyDirectParticleSnowTint(weather.m_BlowingSnowParticleSystem, GetParticleSystemRenderer(weather.m_BlowingSnowParticleSystem), Color.white, backupPrimary: false);
                    ApplyDirectSnowTintUnderRoot(weather.m_BlowingSnow, weather.m_BlowingSnowParticleSystem, Color.white);
                }
                catch (Exception caughtException)
                {
                    if (IsWeatherSnowColorContextUsable(weather)) Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreSnowColor.1", "CustomWeatherStageRuntime.RestoreSnowColor failed.", caughtException);
                }
            }

            s_CurrentSnowColor = Color.white;
            s_SnowColorOverridden = false;
            s_HasParticleStartColorBackup = false;
            s_HasSnowMaterialColorBackup = false;
        }

        private static float GetCurrentAuroraAlpha()
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return s_CurrentAuroraAlpha;

            try
            {
                return Mathf.Clamp01(uniStorm.m_NormalizedAuroraAlpha);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentAuroraAlpha.1", "CustomWeatherStageRuntime.GetCurrentAuroraAlpha failed.", caughtException);
                return s_CurrentAuroraAlpha;
            }
        }

        private static void ApplyVisualAuroraAlpha(float alpha)
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return;

            BackupAuroraAlpha(uniStorm);
            float clampedAlpha = Mathf.Clamp01(alpha);
            try
            {
                uniStorm.SetAuroraAlpha(clampedAlpha);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyVisualAuroraAlpha.1", "CustomWeatherStageRuntime.ApplyVisualAuroraAlpha failed.", caughtException);
                try
                {
                    uniStorm.m_NormalizedAuroraAlpha = clampedAlpha;
                }
                catch (Exception fallbackException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyVisualAuroraAlpha.2", "The fallback aurora alpha assignment failed.", fallbackException);
                    return;
                }
            }

            s_CurrentAuroraAlpha = clampedAlpha;
            s_AuroraAlphaOverridden = true;
        }

        private static void BackupAuroraAlpha(UniStormWeatherSystem uniStorm)
        {
            if (s_HasAuroraAlphaBackup || uniStorm == null) return;

            try
            {
                s_OriginalAuroraAlpha = Mathf.Clamp01(uniStorm.m_NormalizedAuroraAlpha);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupAuroraAlpha.1", "CustomWeatherStageRuntime.BackupAuroraAlpha failed.", caughtException);
                s_OriginalAuroraAlpha = 0f;
            }

            s_HasAuroraAlphaBackup = true;
        }

        private static void RestoreVisualAuroraAlpha()
        {
            if (!s_AuroraAlphaOverridden) return;
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm != null)
            {
                try
                {
                    uniStorm.SetAuroraAlpha(s_OriginalAuroraAlpha);
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreVisualAuroraAlpha.1", "CustomWeatherStageRuntime.RestoreVisualAuroraAlpha failed.", caughtException);
                    uniStorm.m_NormalizedAuroraAlpha = s_OriginalAuroraAlpha;
                }
            }

            s_CurrentAuroraAlpha = s_OriginalAuroraAlpha;
            s_AuroraAlphaOverridden = false;
            s_HasAuroraAlphaBackup = false;
            s_OriginalAuroraAlpha = 0f;
        }

        private static float GetCurrentMasterAmbient()
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return s_CurrentMasterAmbient;
            try
            {
                return uniStorm.m_MasterAmbientIntensityScalar;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentMasterAmbient.1", "CustomWeatherStageRuntime.GetCurrentMasterAmbient failed.", caughtException);
                return s_CurrentMasterAmbient;
            }
        }

        private static void ApplyMasterAmbient(float intensity)
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return;
            BackupMasterAmbient(uniStorm);
            float clampedIntensity = Math.Max(0f, intensity);
            try
            {
                uniStorm.m_MasterAmbientIntensityScalar = clampedIntensity;
                s_CurrentMasterAmbient = clampedIntensity;
                s_MasterAmbientOverridden = true;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyMasterAmbient.1", "CustomWeatherStageRuntime.ApplyMasterAmbient failed.", caughtException);
            }
        }

        private static void BackupMasterAmbient(UniStormWeatherSystem uniStorm)
        {
            if (s_HasMasterAmbientBackup || uniStorm == null) return;
            try
            {
                s_OriginalMasterAmbient = uniStorm.m_MasterAmbientIntensityScalar;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupMasterAmbient.1", "CustomWeatherStageRuntime.BackupMasterAmbient failed.", caughtException);
                s_OriginalMasterAmbient = 1f;
            }
            s_CurrentMasterAmbient = s_OriginalMasterAmbient;
            s_HasMasterAmbientBackup = true;
        }

        private static void RestoreMasterAmbient()
        {
            if (!s_MasterAmbientOverridden && !s_HasMasterAmbientBackup) return;
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm != null)
            {
                uniStorm.m_MasterAmbientIntensityScalar = s_OriginalMasterAmbient;
            }

            s_CurrentMasterAmbient = s_OriginalMasterAmbient;
            s_MasterAmbientOverridden = false;
            s_HasMasterAmbientBackup = false;
            s_OriginalMasterAmbient = 1f;
        }

        private static float GetCurrentDirectFogDensity()
        {
            if (s_DirectFogDensityOverridden) return s_CurrentDirectFogDensity;

            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm != null) return Math.Max(0f, uniStorm.m_FogDensity);

            try

            {

                return Math.Max(0f, RenderSettings.fogDensity);

            }

            catch (Exception caughtException)

            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentDirectFogDensity.1", "CustomWeatherStageRuntime.GetCurrentDirectFogDensity failed.", caughtException);
                return 0f;
            }
        }

        private static void ApplyDirectFogDensity(float density)
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            BackupDirectFogDensity(uniStorm);

            float clampedDensity = Math.Max(0f, density);
            try
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = clampedDensity;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyDirectFogDensity.1", "CustomWeatherStageRuntime.ApplyDirectFogDensity failed.", caughtException);
            }

            if (uniStorm != null)
            {
                uniStorm.m_FogDensity = clampedDensity;
                ApplyDirectHeightFogDensity(uniStorm, clampedDensity);
            }

            s_CurrentDirectFogDensity = clampedDensity;
            s_DirectFogDensityOverridden = true;
        }

        private static void BackupDirectFogDensity(UniStormWeatherSystem uniStorm)
        {
            if (s_HasDirectFogDensityBackup) return;

            try

            {

                s_OriginalRenderSettingsFogDensity = Math.Max(0f, RenderSettings.fogDensity);
                s_OriginalRenderSettingsFogEnabled = RenderSettings.fog;
                s_OriginalRenderSettingsFogMode = RenderSettings.fogMode;

            }

            catch (Exception caughtException)

            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupDirectFogDensity.1", "CustomWeatherStageRuntime.BackupDirectFogDensity failed.", caughtException);
                s_OriginalRenderSettingsFogDensity = 0f;
            }
            try
            {
                s_OriginalUniStormFogDensity = uniStorm != null ? Math.Max(0f, uniStorm.m_FogDensity) : s_OriginalRenderSettingsFogDensity;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupDirectFogDensity.2", "CustomWeatherStageRuntime.BackupDirectFogDensity failed.", caughtException);
                s_OriginalUniStormFogDensity = s_OriginalRenderSettingsFogDensity;
            }

            HeightFogManager? heightFogManager = GetHeightFogManager(uniStorm);
            if (!s_HasHeightFogActivationBackup && heightFogManager != null)
            {
                s_OriginalHeightFogObjectActive = heightFogManager.gameObject != null && heightFogManager.gameObject.activeSelf;
                s_OriginalHeightFogRendererEnabled = heightFogManager.m_Renderer != null && heightFogManager.m_Renderer.enabled;
                s_HasHeightFogActivationBackup = true;
            }

            if (!s_HasHeightFogSettingsBackup && heightFogManager != null)
            {
                try
                {
                    s_OriginalHeightFogSettings = heightFogManager.m_CurrentSettings;
                    s_HasHeightFogSettingsBackup = true;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupDirectFogDensity.3", "CustomWeatherStageRuntime.BackupDirectFogDensity failed.", caughtException);
                }
            }

            s_CurrentDirectFogDensity = s_OriginalRenderSettingsFogDensity;
            s_HasDirectFogDensityBackup = true;
        }

        private static void ApplyDirectHeightFogDensity(UniStormWeatherSystem uniStorm, float renderFogDensity)
        {
            HeightFogManager? heightFogManager = GetHeightFogManager(uniStorm);
            if (heightFogManager == null) return;

            try
            {
                HeightFogSettings settings = heightFogManager.m_CurrentSettings;
                float heightDensity = Mathf.Clamp(renderFogDensity * 320f, 0f, 0.95f);
                settings.m_FogDensity = heightDensity;
                if (renderFogDensity >= 0.004f)
                {
                    settings.m_FogPower = 0.65f;
                    settings.m_FogDistanceStart = -2f;
                    settings.m_FogDistanceEnd = 20f;
                    settings.m_FogDistanceFalloff = 0.75f;
                    settings.m_FogHeightStart = -3f;
                    settings.m_FogHeightEnd = 11f;
                    settings.m_FogHeightFalloff = 0.85f;
                    settings.m_NoiseStrength = Math.Max(settings.m_NoiseStrength, 0.06f);
                    settings.m_NoiseScale = Math.Max(settings.m_NoiseScale, 0.02f);
                }
                else
                {
                    settings.m_FogPower = Mathf.Clamp(settings.m_FogPower, 0.85f, 2f);
                    settings.m_FogDistanceStart = Math.Min(settings.m_FogDistanceStart, -60f);
                    settings.m_FogDistanceEnd = Math.Max(settings.m_FogDistanceEnd, 140f);
                    settings.m_FogDistanceFalloff = Mathf.Clamp(settings.m_FogDistanceFalloff, 0.8f, 2.2f);
                    settings.m_FogHeightStart = Math.Min(settings.m_FogHeightStart, -12f);
                    settings.m_FogHeightEnd = Math.Max(settings.m_FogHeightEnd, 35f);
                    settings.m_FogHeightFalloff = Mathf.Clamp(settings.m_FogHeightFalloff, 0.8f, 2.2f);
                    settings.m_NoiseStrength = Math.Max(settings.m_NoiseStrength, 0.035f);
                    settings.m_NoiseScale = Math.Max(settings.m_NoiseScale, 0.015f);
                }
                if (settings.m_NoiseSpeed == Vector3.zero) settings.m_NoiseSpeed = new(0.005f, 0f, 0.002f);
                heightFogManager.m_CurrentSettings = settings;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyDirectHeightFogDensity.1", "CustomWeatherStageRuntime.ApplyDirectHeightFogDensity failed.", caughtException);
            }

            try
            {
                MaterialPropertyBlock block = GetAuroraVariantPropertyBlock();
                MeshRenderer renderer = heightFogManager.m_Renderer;
                if (renderer == null) return;
                renderer.GetPropertyBlock(block);
                block.SetFloat(HeightFogManager.s_FogDensityID, Mathf.Clamp(renderFogDensity * 320f, 0f, 0.95f));
                renderer.SetPropertyBlock(block);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyDirectHeightFogDensity.2", "CustomWeatherStageRuntime.ApplyDirectHeightFogDensity failed.", caughtException);
            }
        }

        private static void RestoreDirectFogDensity()
        {
            if (!s_DirectFogDensityOverridden && !s_HasDirectFogDensityBackup) return;

            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            try
            {
                RenderSettings.fog = s_OriginalRenderSettingsFogEnabled;
                RenderSettings.fogMode = s_OriginalRenderSettingsFogMode;
                RenderSettings.fogDensity = s_OriginalRenderSettingsFogDensity;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreDirectFogDensity.1", "CustomWeatherStageRuntime.RestoreDirectFogDensity failed.", caughtException);
            }

            if (uniStorm != null)
            {
                uniStorm.m_FogDensity = s_OriginalUniStormFogDensity;
                HeightFogManager? heightFogManager = GetHeightFogManager(uniStorm);
                if (heightFogManager != null)
                {
                    if (s_HasHeightFogSettingsBackup) heightFogManager.m_CurrentSettings = s_OriginalHeightFogSettings;
                    if (s_HasHeightFogActivationBackup)
                    {
                        if (heightFogManager.gameObject != null && heightFogManager.gameObject.activeSelf != s_OriginalHeightFogObjectActive) heightFogManager.gameObject.SetActive(s_OriginalHeightFogObjectActive);
                        if (heightFogManager.m_Renderer != null) heightFogManager.m_Renderer.enabled = s_OriginalHeightFogRendererEnabled;
                    }
                }
            }

            s_DirectFogDensityOverridden = false;
            s_HasDirectFogDensityBackup = false;
            s_HasHeightFogActivationBackup = false;
            s_OriginalRenderSettingsFogDensity = 0f;
            s_OriginalRenderSettingsFogEnabled = false;
            s_OriginalRenderSettingsFogMode = FogMode.Linear;
            s_OriginalUniStormFogDensity = 0f;
            s_CurrentDirectFogDensity = 0f;
        }

        private static void BackupAuroraCloudState(UniStormWeatherSystem uniStorm)
        {
            if (s_HasAuroraCloudStateBackup || uniStorm == null) return;
            s_OriginalCloudCoverage = uniStorm.m_CloudCoverage;
            s_OriginalCloudParentActive = uniStorm.m_CloudParentGameObject != null && uniStorm.m_CloudParentGameObject.activeSelf;
            try
            {
                TODStateData state = uniStorm.GetActiveTODState();
                if (state != null)
                {
                    s_OriginalAuroraCloudTodState = state;
                    s_OriginalHorizonCloudColor1 = state.m_HorizonCloudsColor1;
                    s_OriginalHorizonCloudColor2 = state.m_HorizonCloudsColor2;
                    s_OriginalHorizonCloudColor3 = state.m_HorizonCloudsColor3;
                    s_OriginalCloudAlphas = state.m_CloudAlphas;
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupAuroraCloudState.1", "CustomWeatherStageRuntime.BackupAuroraCloudState failed.", caughtException);
            }
            s_HasAuroraCloudStateBackup = true;
        }

        private static void BackupAuroraCloudRenderer(Renderer renderer)
        {
            if (renderer == null || s_AuroraCloudRendererBackups.ContainsKey(renderer)) return;
            AuroraCloudRendererBackup backup = new()
            {
                RendererEnabled = renderer.enabled,
                GameObjectActive = renderer.gameObject != null && renderer.gameObject.activeSelf,
                Material = renderer.material,
                SharedMaterial = renderer.sharedMaterial
            };
            if (backup.Material != null) backup.MaterialColor = backup.Material.color;
            if (backup.SharedMaterial != null) backup.SharedMaterialColor = backup.SharedMaterial.color;
            renderer.GetPropertyBlock(backup.PropertyBlock);
            s_AuroraCloudRendererBackups[renderer] = backup;
        }

        private static void RestoreAuroraCloudState()
        {
            foreach (KeyValuePair<Renderer, AuroraCloudRendererBackup> pair in s_AuroraCloudRendererBackups)
            {
                Renderer renderer = pair.Key;
                AuroraCloudRendererBackup backup = pair.Value;
                if (renderer == null) continue;
                if (backup.Material != null) backup.Material.color = backup.MaterialColor;
                if (backup.SharedMaterial != null) backup.SharedMaterial.color = backup.SharedMaterialColor;
                renderer.SetPropertyBlock(backup.PropertyBlock);
                renderer.enabled = backup.RendererEnabled;
                if (renderer.gameObject != null && renderer.gameObject.activeSelf != backup.GameObjectActive) renderer.gameObject.SetActive(backup.GameObjectActive);
            }
            s_AuroraCloudRendererBackups.Clear();

            if (s_HasAuroraCloudStateBackup)
            {
                UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
                if (uniStorm != null)
                {
                    uniStorm.m_CloudCoverage = s_OriginalCloudCoverage;
                    if (uniStorm.m_CloudParentGameObject != null && uniStorm.m_CloudParentGameObject.activeSelf != s_OriginalCloudParentActive) uniStorm.m_CloudParentGameObject.SetActive(s_OriginalCloudParentActive);
                }

                if (s_OriginalAuroraCloudTodState != null)
                {
                    s_OriginalAuroraCloudTodState.m_HorizonCloudsColor1 = s_OriginalHorizonCloudColor1;
                    s_OriginalAuroraCloudTodState.m_HorizonCloudsColor2 = s_OriginalHorizonCloudColor2;
                    s_OriginalAuroraCloudTodState.m_HorizonCloudsColor3 = s_OriginalHorizonCloudColor3;
                    s_OriginalAuroraCloudTodState.m_CloudAlphas = s_OriginalCloudAlphas;
                }
            }

            s_HasAuroraCloudStateBackup = false;
            s_OriginalAuroraCloudTodState = null;
        }

        private static void ApplyAuroraCloudLayer(float highAlpha, float light1Alpha, float light2Alpha, float mostlyAlpha, float horizonAlpha, float edgeAlpha, Color color)
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            ApplyAuroraCloudLayer(uniStorm, highAlpha, light1Alpha, light2Alpha, mostlyAlpha, horizonAlpha, edgeAlpha, color);
        }

        private static void ApplyAuroraCloudLayer(UniStormWeatherSystem uniStorm, float highAlpha, float light1Alpha, float light2Alpha, float mostlyAlpha, float horizonAlpha, float edgeAlpha, Color color)
        {
            if (uniStorm == null) return;

            BackupAuroraCloudState(uniStorm);
            uniStorm.EnableCloudParent(true);
            if (uniStorm.m_CloudParentGameObject != null && !uniStorm.m_CloudParentGameObject.activeSelf) uniStorm.m_CloudParentGameObject.SetActive(true);

            float blend = Mathf.Clamp01(s_CurrentEffectBlend);
            Color baseColor = WithAlphaOne(color);
            ApplyAuroraCloudObject(SafeGetGameObject(() => uniStorm.m_HighClouds1), SafeGetRenderer(() => uniStorm.m_HighClouds1_Renderer), baseColor, highAlpha * blend);
            ApplyAuroraCloudObject(SafeGetGameObject(() => uniStorm.m_LightClouds1), SafeGetRenderer(() => uniStorm.m_LightClouds1_Renderer), baseColor, light1Alpha * blend);
            ApplyAuroraCloudObject(SafeGetGameObject(() => uniStorm.m_LightClouds2), SafeGetRenderer(() => uniStorm.m_LightClouds2_Renderer), baseColor, light2Alpha * blend);
            ApplyAuroraCloudObject(SafeGetGameObject(() => uniStorm.m_MostlyCloudyClouds), SafeGetRenderer(() => uniStorm.m_MostlyCloudyClouds_Renderer), baseColor, mostlyAlpha * blend);

            Color horizon = baseColor;
            horizon.a = Mathf.Clamp01(horizonAlpha * blend);
            ApplyAuroraCloudPropertyBlock(SafeGetRenderer(() => uniStorm.m_HorizonCloudsBand1_Renderer), horizon);
            ApplyAuroraCloudPropertyBlock(SafeGetRenderer(() => uniStorm.m_HorizonCloudsBand2_Renderer), horizon);
            ApplyAuroraCloudPropertyBlock(SafeGetRenderer(() => uniStorm.m_HorizonCloudsBand3_Renderer), horizon);

            Color edge = baseColor;
            edge.a = Mathf.Clamp01(edgeAlpha * blend);
            ApplyAuroraCloudPropertyBlock(SafeGetRenderer(() => uniStorm.m_SkyCloudEdge1_Renderer), edge);
            ApplyAuroraCloudPropertyBlock(SafeGetRenderer(() => uniStorm.m_SkyCloudEdge2_Renderer), edge);

            float coverage = Mathf.Clamp01(Mathf.Max(Mathf.Max(highAlpha, light1Alpha), Mathf.Max(light2Alpha, mostlyAlpha)) * blend);
            uniStorm.m_CloudCoverage = coverage;
            ApplyAuroraTODCloudState(uniStorm, baseColor, highAlpha * blend, light1Alpha * blend, light2Alpha * blend, mostlyAlpha * blend, horizonAlpha * blend);
        }

        private static void ApplyAuroraTODCloudState(UniStormWeatherSystem uniStorm, Color color, float highAlpha, float light1Alpha, float light2Alpha, float mostlyAlpha, float horizonAlpha)
        {
            if (uniStorm == null) return;

            try
            {
                TODStateData state = uniStorm.GetActiveTODState();
                if (state == null) return;

                Color horizon1 = color;
                horizon1.a = Mathf.Clamp01(horizonAlpha);
                Color horizon2 = color;
                horizon2.a = Mathf.Clamp01(horizonAlpha * 0.85f);
                Color horizon3 = color;
                horizon3.a = Mathf.Clamp01(horizonAlpha * 0.7f);

                state.m_HorizonCloudsColor1 = horizon1;
                state.m_HorizonCloudsColor2 = horizon2;
                state.m_HorizonCloudsColor3 = horizon3;
                state.m_CloudAlphas = new(Mathf.Clamp01(light1Alpha), Mathf.Clamp01(light2Alpha), Mathf.Clamp01(highAlpha), Mathf.Clamp01(mostlyAlpha));
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyAuroraTODCloudState.1", "CustomWeatherStageRuntime.ApplyAuroraTODCloudState failed.", caughtException);
            }
        }

        private static void ApplyAuroraCloudObject(GameObject? gameObject, Renderer? renderer, Color color, float alpha)
        {
            if (renderer != null) BackupAuroraCloudRenderer(renderer);
            if (gameObject != null && !gameObject.activeSelf) gameObject.SetActive(true);
            if (renderer == null) return;

            renderer.enabled = true;

            try
            {
                Material material = renderer.material;
                if (material != null)
                {
                    Color cloudColor = color;
                    cloudColor.a = Mathf.Clamp01(alpha);
                    material.color = cloudColor;
                    if (material.HasProperty("_Color")) material.SetColor("_Color", cloudColor);
                    if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", cloudColor);
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyAuroraCloudObject.1", "CustomWeatherStageRuntime.ApplyAuroraCloudObject failed.", caughtException);
            }

            try
            {
                Material sharedMaterial = renderer.sharedMaterial;
                if (sharedMaterial != null)
                {
                    Color cloudColor = color;
                    cloudColor.a = Mathf.Clamp01(alpha);
                    sharedMaterial.color = cloudColor;
                    if (sharedMaterial.HasProperty("_Color")) sharedMaterial.SetColor("_Color", cloudColor);
                    if (sharedMaterial.HasProperty("_TintColor")) sharedMaterial.SetColor("_TintColor", cloudColor);
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyAuroraCloudObject.2", "CustomWeatherStageRuntime.ApplyAuroraCloudObject failed.", caughtException);
            }

            try
            {
                MaterialPropertyBlock block = GetAuroraVariantPropertyBlock();
                Color cloudColor = color;
                cloudColor.a = Mathf.Clamp01(alpha);
                renderer.GetPropertyBlock(block);
                int colorId = SafeShaderId(() => UniStormWeatherSystem.s_ColorShaderID);
                if (colorId >= 0) block.SetColor(colorId, cloudColor);
                else block.SetColor("_Color", cloudColor);
                block.SetColor("_Color", cloudColor);
                block.SetColor("_TintColor", cloudColor);
                renderer.SetPropertyBlock(block);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyAuroraCloudObject.3", "CustomWeatherStageRuntime.ApplyAuroraCloudObject failed.", caughtException);
            }
        }

        private static void ApplyAuroraCloudPropertyBlock(Renderer? renderer, Color color)
        {
            if (renderer == null) return;
            BackupAuroraCloudRenderer(renderer);

            if (renderer.gameObject != null && !renderer.gameObject.activeSelf) renderer.gameObject.SetActive(true);
            renderer.enabled = true;
            try
            {
                Material material = renderer.material;
                if (material != null)
                {
                    material.color = color;
                    if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                    if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", color);
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyAuroraCloudPropertyBlock.1", "CustomWeatherStageRuntime.ApplyAuroraCloudPropertyBlock failed.", caughtException);
            }

            try
            {
                MaterialPropertyBlock block = GetAuroraVariantPropertyBlock();
                renderer.GetPropertyBlock(block);
                int colorId = SafeShaderId(() => UniStormWeatherSystem.s_ColorShaderID);
                if (colorId >= 0) block.SetColor(colorId, color);
                else block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyAuroraCloudPropertyBlock.2", "CustomWeatherStageRuntime.ApplyAuroraCloudPropertyBlock failed.", caughtException);
            }
        }

        private static MaterialPropertyBlock GetAuroraVariantPropertyBlock()
        {
            s_AuroraVariantPropertyBlock ??= new();
            s_AuroraVariantPropertyBlock.Clear();
            return s_AuroraVariantPropertyBlock!;
        }

        private static bool IsAuroraWeatherVariantMode(int mode)
        {
            return mode == ModeCloudyAurora || mode == ModeFoggyAurora || mode == ModeSnowyAurora;
        }


        private static Color GetCurrentFogColor()
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            Material? skybox = uniStorm != null ? GetSkyboxMaterial(uniStorm) : null;
            int fogColorId = SafeShaderId(() => UniStormWeatherSystem.s_FogColorShaderID);
            if (skybox != null && fogColorId >= 0 && SafeMaterialHasProperty(skybox, fogColorId)) return SafeGetMaterialColor(skybox, fogColorId, RenderSettings.fogColor);
            return RenderSettings.fogColor;
        }

        private static void ApplyFogColor(Color color, bool applySkyboxFogColor = true)
        {
            BackupFogColor(applySkyboxFogColor);
            Color renderFogColor = WithAlphaOne(color);
            Color skyboxFogColor = color;
            skyboxFogColor.a = Mathf.Clamp01(color.a);
            RenderSettings.fogColor = renderFogColor;

            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            Material? skybox = uniStorm != null ? GetSkyboxMaterial(uniStorm) : null;
            int fogColorId = SafeShaderId(() => UniStormWeatherSystem.s_FogColorShaderID);
            if (applySkyboxFogColor && skybox != null && fogColorId >= 0) TrySetMaterialColor(skybox, fogColorId, skyboxFogColor);
            ApplyHeightFogColor(uniStorm, renderFogColor);

            s_CurrentFogColor = color;
            s_FogColorOverridden = true;
        }

        private static void BackupFogColor(bool includeSkyboxFogColor = true)
        {
            if (!s_HasFogColorBackup)
            {
                try
                {
                    s_OriginalRenderSettingsFogColor = RenderSettings.fogColor;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupFogColor.1", "CustomWeatherStageRuntime.BackupFogColor failed.", caughtException);
                    s_OriginalRenderSettingsFogColor = Color.white;
                }
                s_HasFogColorBackup = true;
            }

            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            Material? skybox = uniStorm != null ? GetSkyboxMaterial(uniStorm) : null;
            int fogColorId = SafeShaderId(() => UniStormWeatherSystem.s_FogColorShaderID);
            if (includeSkyboxFogColor && !s_HasSkyboxFogColorBackup && skybox != null && fogColorId >= 0 && SafeMaterialHasProperty(skybox, fogColorId))
            {
                s_OriginalSkyboxFogColor = SafeGetMaterialColor(skybox, fogColorId, Color.white);
                s_HasSkyboxFogColorBackup = true;
            }

            HeightFogManager? heightFogManager = GetHeightFogManager(uniStorm);
            if (!s_HasHeightFogSettingsBackup && heightFogManager != null)
            {
                try
                {
                    s_OriginalHeightFogSettings = heightFogManager.m_CurrentSettings;
                    s_HasHeightFogSettingsBackup = true;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupFogColor.2", "CustomWeatherStageRuntime.BackupFogColor failed.", caughtException);
                }
            }
        }

        private static void ApplyHeightFogColor(UniStormWeatherSystem uniStorm, Color color)
        {
            HeightFogManager? heightFogManager = GetHeightFogManager(uniStorm);
            if (heightFogManager == null) return;

            try
            {
                HeightFogSettings settings = heightFogManager.m_CurrentSettings;
                settings.m_FogColor = color;
                heightFogManager.m_CurrentSettings = settings;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyHeightFogColor.1", "CustomWeatherStageRuntime.ApplyHeightFogColor failed.", caughtException);
            }

            try
            {
                MaterialPropertyBlock block = heightFogManager.m_PropertyBlock;
                MeshRenderer renderer = heightFogManager.m_Renderer;
                if (block == null || renderer == null) return;
                block.SetColor(HeightFogManager.s_FogColorID, color);
                renderer.SetPropertyBlock(block);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyHeightFogColor.2", "CustomWeatherStageRuntime.ApplyHeightFogColor failed.", caughtException);
            }
        }

        private static void RestoreFogColorTowardDefault()
        {
            if (!s_FogColorOverridden && !s_HasFogColorBackup && !s_HasSkyboxFogColorBackup && !s_HasHeightFogSettingsBackup)
            {
                s_CurrentFogColor = GetCurrentFogColor();
                return;
            }

            if (s_CurrentEffectBlend >= 0.999f)
            {
                RestoreFogColor();
                return;
            }

            Color targetColor = s_HasFogColorBackup ? s_OriginalRenderSettingsFogColor : GetCurrentFogColor();
            Color startColor = s_EffectTransitionMode == s_ActiveMode ? s_EffectStartFogColor : s_CurrentFogColor;
            ApplyFogColor(Color.Lerp(startColor, targetColor, s_CurrentEffectBlend));
        }

        private static void RestoreFogColor()
        {
            if (!s_FogColorOverridden && !s_HasFogColorBackup && !s_HasSkyboxFogColorBackup && !s_HasHeightFogSettingsBackup) return;

            RenderSettings.fogColor = s_OriginalRenderSettingsFogColor;

            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            Material? skybox = uniStorm != null ? GetSkyboxMaterial(uniStorm) : null;
            int fogColorId = SafeShaderId(() => UniStormWeatherSystem.s_FogColorShaderID);
            if (s_HasSkyboxFogColorBackup && skybox != null && fogColorId >= 0) TrySetMaterialColor(skybox, fogColorId, s_OriginalSkyboxFogColor);

            HeightFogManager? heightFogManager = GetHeightFogManager(uniStorm);
            if (s_HasHeightFogSettingsBackup && heightFogManager != null)
            {
                heightFogManager.m_CurrentSettings = s_OriginalHeightFogSettings;
                try
                {
                    MaterialPropertyBlock block = heightFogManager.m_PropertyBlock;
                    MeshRenderer renderer = heightFogManager.m_Renderer;
                    if (block != null && renderer != null)
                    {
                        block.SetColor(HeightFogManager.s_FogColorID, s_OriginalHeightFogSettings.m_FogColor);
                        renderer.SetPropertyBlock(block);
                    }
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreFogColor.1", "CustomWeatherStageRuntime.RestoreFogColor failed.", caughtException);
                }
            }

            s_CurrentFogColor = s_OriginalRenderSettingsFogColor;
            s_FogColorOverridden = false;
            s_HasFogColorBackup = false;
            s_HasSkyboxFogColorBackup = false;
            s_HasHeightFogSettingsBackup = false;
            s_OriginalRenderSettingsFogColor = Color.white;
            s_OriginalSkyboxFogColor = Color.white;
        }

        private static Color GetCurrentMoonColourOverride()
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return s_CurrentMoonColour;
            try
            {
                return uniStorm.m_MoonLightColorOverride;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentMoonColourOverride.1", "CustomWeatherStageRuntime.GetCurrentMoonColourOverride failed.", caughtException);
                return s_CurrentMoonColour;
            }
        }

        private static Color GetCurrentMoonLightColor()
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return s_CurrentMoonLightColour;
            try
            {
                return uniStorm.m_MoonLightColor;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentMoonLightColor.1", "CustomWeatherStageRuntime.GetCurrentMoonLightColor failed.", caughtException);
                return s_CurrentMoonLightColour;
            }
        }

        private static Color GetCurrentStarColor()
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return s_CurrentStarColour;
            try
            {
                return uniStorm.m_StarColor;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetCurrentStarColor.1", "CustomWeatherStageRuntime.GetCurrentStarColor failed.", caughtException);
                return s_CurrentStarColour;
            }
        }

        private static Color GetCurrentMoonGlowColor()
        {
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            Material? skybox = uniStorm != null ? GetSkyboxMaterial(uniStorm) : null;
            int moonGlowId = SafeShaderId(() => UniStormWeatherSystem.s_MoonGlowShaderID);
            if (skybox != null && moonGlowId >= 0 && SafeMaterialHasProperty(skybox, moonGlowId)) return SafeGetMaterialColor(skybox, moonGlowId, s_CurrentMoonGlowColour);
            return s_CurrentMoonGlowColour;
        }

        private static void ApplyBloodMoonVisuals(Color moonColor, Color moonLightColor, Color starColor, Color moonGlowColor)
        {
            if (s_SuppressOutdoorNightEventVisuals) return;

            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return;

            BackupBloodMoonVisuals(uniStorm);
            Color normalizedMoon = WithAlphaOne(moonColor);
            Color normalizedMoonLight = WithAlphaOne(moonLightColor);
            Color normalizedStar = WithAlphaOne(starColor);
            Color normalizedGlow = WithAlphaOne(moonGlowColor);

            ApplyBloodMoonVisualState(uniStorm, normalizedMoon, normalizedMoonLight, normalizedStar, suppressSolarHalo: true);

            if (!s_MoonTextureOverridden) ApplyBloodMoonMoonTexture(uniStorm, normalizedMoon);

            s_CurrentMoonColour = normalizedMoon;
            s_CurrentMoonLightColour = normalizedMoonLight;
            s_CurrentStarColour = normalizedStar;
            s_CurrentMoonGlowColour = normalizedGlow;
            s_BloodMoonVisualsOverridden = true;
        }

        internal static void NotifyUniStormUpdated(UniStormWeatherSystem uniStorm)
        {
            MaintainHeavyOvercastFallingSnowSuppression();
            if (ShouldMaintainAuroraVariantVisuals()) MaintainAuroraVariantVisualState(uniStorm);
            if (ShouldMaintainBloodMoonVisuals()) ApplyBloodMoonVisualState(uniStorm, s_CurrentMoonColour, s_CurrentMoonLightColour, s_CurrentStarColour, suppressSolarHalo: true);
        }

        private static void MaintainForecastVisualOverrides()
        {
            if (s_SuppressOutdoorNightEventVisuals && IsNightEventVisualMode(s_ActiveMode)) return;

            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm == null) return;

            if (ShouldMaintainAuroraVariantVisuals()) MaintainAuroraVariantVisualState(uniStorm);
            if (ShouldMaintainBloodMoonVisuals()) ApplyBloodMoonVisualState(uniStorm, s_CurrentMoonColour, s_CurrentMoonLightColour, s_CurrentStarColour, suppressSolarHalo: true);
        }

        private static bool ShouldMaintainAuroraVariantVisuals()
        {
            if (s_SuppressOutdoorNightEventVisuals) return false;
            if (!IsAuroraWeatherVariantMode(s_ActiveMode)) return false;
            if (s_RequestedMode == 0 && !s_ForecastOverrideActive) return false;
            return true;
        }

        private static void MaintainAuroraVariantVisualState(UniStormWeatherSystem uniStorm)
        {
            if (uniStorm == null) return;

            switch (s_ActiveMode)
            {
                case ModeCloudyAurora:
                    ApplyFogColor(new(0.05f, 0.065f, 0.06f, 0.035f));
                    ApplyFogScaleImmediate(0.6f, customOverrideActive: true);
                    ApplyDirectFogDensity(0.00004f);
                    ApplyAuroraCloudLayer(uniStorm, 0.10f, 0.28f, 0.25f, 0.18f, 0.11f, 0.04f, new(0.13f, 0.20f, 0.17f, 1f));
                    break;
                case ModeFoggyAurora:
                    ApplyVisualAuroraAlpha(1f);
                    MaintainHeavyOvercastFallingSnowSuppression();
                    break;
                case ModeSnowyAurora:
                    ApplyFogColor(new(0.05f, 0.065f, 0.06f, 0.035f));
                    ApplyFogScaleImmediate(0.6f, customOverrideActive: true);
                    ApplyDirectFogDensity(0.00004f);
                    ApplyAuroraCloudLayer(uniStorm, 0.10f, 0.28f, 0.25f, 0.18f, 0.11f, 0.04f, new(0.13f, 0.20f, 0.17f, 1f));
                    ApplySparseSnowyAuroraSnow(0.10f);
                    ApplySnowColor(BlendSnowColor(SnowyAuroraSnowColor));
                    break;
            }
        }

        private static bool ShouldMaintainBloodMoonVisuals()
        {
            if (s_SuppressOutdoorNightEventVisuals) return false;
            if (s_RequestedMode == 0 && !s_ForecastOverrideActive) return false;
            if (s_ActiveMode != ModeBloodMoon && s_ActiveMode != ModeLightSnowBloodMoon) return false;
            return s_BloodMoonVisualsOverridden;
        }


        private static void ApplyBloodMoonVisualState(UniStormWeatherSystem uniStorm, Color moonColor, Color moonLightColor, Color starColor, bool suppressSolarHalo)
        {
            if (uniStorm == null) return;

            try

            {

                uniStorm.SetMoonColourOverride(true);

            }

            catch (Exception caughtException)

            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyBloodMoonVisualState.1", "CustomWeatherStageRuntime.ApplyBloodMoonVisualState failed.", caughtException);
                uniStorm.m_UseMoonColourColorOverride = true;
            }
            try
            {
                uniStorm.SetMoonColourOverrideColour(WithAlphaOne(moonColor));
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyBloodMoonVisualState.2", "CustomWeatherStageRuntime.ApplyBloodMoonVisualState failed.", caughtException);
                uniStorm.m_MoonLightColorOverride = WithAlphaOne(moonColor);
            }
            uniStorm.m_MoonLightColor = WithAlphaOne(moonLightColor);
            uniStorm.m_StarColor = WithAlphaOne(starColor);

            ApplyMoonColorMaterial(uniStorm, moonColor);
            ApplyBloodMoonMoonAndStarsPropertyBlock(uniStorm, moonColor, starColor);

            if (suppressSolarHalo) SuppressBloodMoonSolarHalo(uniStorm);
        }

        private static void SuppressBloodMoonSolarHalo(UniStormWeatherSystem uniStorm)
        {
            if (uniStorm == null) return;
            Material? skybox = GetSkyboxMaterial(uniStorm);
            if (skybox == null) return;

            TrySetMaterialFloat(skybox, SafeShaderId(() => UniStormWeatherSystem.s_SunSizeShaderID), 0f);
            TrySetMaterialFloat(skybox, SafeShaderId(() => UniStormWeatherSystem.s_SunDiffusionShaderID), 0f);
        }

        private static Renderer? SafeGetRenderer(Func<Renderer?> getter)
        {
            try
            {
                return getter();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.SafeGetRenderer.1", "CustomWeatherStageRuntime.SafeGetRenderer failed.", caughtException);
                return null;
            }
        }

        private static GameObject? SafeGetGameObject(Func<GameObject?> getter)
        {
            try
            {
                return getter();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.SafeGetGameObject.1", "CustomWeatherStageRuntime.SafeGetGameObject failed.", caughtException);
                return null;
            }
        }

        private static MaterialPropertyBlock GetSharedSkyPropertyBlock()
        {
            s_SharedSkyPropertyBlock ??= new MaterialPropertyBlock();
            return s_SharedSkyPropertyBlock;
        }

        private static void ClearRendererPropertyBlock(Renderer? renderer)
        {
            if (renderer == null) return;
            MaterialPropertyBlock block = GetSharedSkyPropertyBlock();
            block.Clear();
            renderer.SetPropertyBlock(block);
        }

        private static void ApplyBloodMoonMoonAndStarsPropertyBlock(UniStormWeatherSystem uniStorm, Color moonColor, Color starColor)
        {
            if (uniStorm == null) return;

            Renderer? starSphere = SafeGetRenderer(() => uniStorm.m_StarSphereRenderer);
            if (starSphere == null) return;

            MaterialPropertyBlock block = GetSharedSkyPropertyBlock();
            try
            {
                starSphere.GetPropertyBlock(block);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyBloodMoonMoonAndStarsPropertyBlock.1", "CustomWeatherStageRuntime.ApplyBloodMoonMoonAndStarsPropertyBlock failed.", caughtException);
                block.Clear();
            }
            block.SetColor(s_TintColorShaderId, WithAlphaOne(starColor));
            block.SetColor(s_MoonColorShaderId, WithAlphaOne(moonColor));
            starSphere.SetPropertyBlock(block);
        }

        private static void RestoreBloodMoonMoonAndStarsPropertyBlock(UniStormWeatherSystem uniStorm)
        {
            if (uniStorm == null) return;
            Renderer? starSphere = SafeGetRenderer(() => uniStorm.m_StarSphereRenderer);
            if (starSphere == null) return;

            if (s_HasBloodMoonStarSpherePropertyBlockBackup && s_OriginalBloodMoonStarSpherePropertyBlock != null)
            {
                starSphere.SetPropertyBlock(s_OriginalBloodMoonStarSpherePropertyBlock);
                return;
            }

            ClearRendererPropertyBlock(starSphere);
        }

        private static void ApplyMoonColorMaterial(UniStormWeatherSystem uniStorm, Color moonColor)
        {
            if (uniStorm == null) return;
            Material? skybox = GetSkyboxMaterial(uniStorm);
            int moonColorId = SafeShaderId(() => UniStormWeatherSystem.s_MoonColorShaderID);
            if (skybox != null && moonColorId >= 0) TrySetMaterialColor(skybox, moonColorId, WithAlphaOne(moonColor));
        }

        private static void ApplyBloodMoonMoonTexture(UniStormWeatherSystem uniStorm, Color tintColor)
        {
            if (uniStorm == null) return;
            BackupMoonTexture(uniStorm);

            Texture2D? generatedTexture = EnsureBloodMoonTexture(uniStorm, tintColor);
            if (generatedTexture == null) return;

            int phaseIndex = GetSafeMoonPhaseIndex(uniStorm);
            try
            {
                var phases = uniStorm.m_MoonPhases;
                if (phases != null && phaseIndex >= 0 && phaseIndex < phases.Length) phases[phaseIndex] = generatedTexture;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.ApplyBloodMoonMoonTexture.1", "CustomWeatherStageRuntime.ApplyBloodMoonMoonTexture failed.", caughtException);
            }

            Material? skybox = GetSkyboxMaterial(uniStorm);
            int moonTexId = SafeShaderId(() => UniStormWeatherSystem.s_MoonTexShaderID);
            if (skybox != null && moonTexId >= 0) TrySetMaterialTexture(skybox, moonTexId, generatedTexture);
            RefreshMoonPhase(uniStorm, phaseIndex);
            s_MoonTextureOverridden = true;
        }

        private static void BackupMoonTexture(UniStormWeatherSystem uniStorm)
        {
            if (uniStorm == null) return;
            int phaseIndex = GetSafeMoonPhaseIndex(uniStorm);

            if (!s_HasMoonTextureBackup)
            {
                s_OriginalMoonTextureIndex = phaseIndex;
                try
                {
                    var phases = uniStorm.m_MoonPhases;
                    if (phases != null && phaseIndex >= 0 && phaseIndex < phases.Length) s_OriginalMoonPhaseTexture = phases[phaseIndex];
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupMoonTexture.1", "CustomWeatherStageRuntime.BackupMoonTexture failed.", caughtException);
                    s_OriginalMoonPhaseTexture = null;
                }

                s_HasMoonTextureBackup = true;
            }

            Material? skybox = GetSkyboxMaterial(uniStorm);
            int moonTexId = SafeShaderId(() => UniStormWeatherSystem.s_MoonTexShaderID);
            if (!s_HasSkyboxMoonTextureBackup && skybox != null && moonTexId >= 0 && SafeMaterialHasProperty(skybox, moonTexId))
            {
                Texture? originalTexture = SafeGetMaterialTexture(skybox, moonTexId, null);
                if (originalTexture != null)
                {
                    s_OriginalSkyboxMoonTexture = originalTexture;
                    s_HasSkyboxMoonTextureBackup = true;
                }
            }
        }

        private static Texture2D? EnsureBloodMoonTexture(UniStormWeatherSystem uniStorm, Color tintColor)
        {
            if (s_GeneratedBloodMoonTexture != null) return s_GeneratedBloodMoonTexture;

            Texture2D? source = null;
            int phaseIndex = GetSafeMoonPhaseIndex(uniStorm);
            if (s_HasMoonTextureBackup && s_OriginalMoonPhaseTexture != null) source = s_OriginalMoonPhaseTexture;
            if (source == null)
            {
                try
                {
                    var phases = uniStorm.m_MoonPhases;
                    if (phases != null && phaseIndex >= 0 && phaseIndex < phases.Length) source = phases[phaseIndex];
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.EnsureBloodMoonTexture.1", "CustomWeatherStageRuntime.EnsureBloodMoonTexture failed.", caughtException);
                }
            }

            Texture2D? readableSource = CreateReadableMoonTextureCopy(source);
            if (readableSource == null) return null;

            Texture2D? texture = null;
            try
            {
                int width = readableSource.width;
                int height = readableSource.height;
                texture = new(width, height, TextureFormat.RGBA32, false)
                {
                    name = "WeatherOverhaul_BloodMoonTintedMoonPhase"
                };

                Color core = WithAlphaOne(tintColor);
                Color shadow = new(Mathf.Clamp01(core.r * 0.18f), Mathf.Clamp01(core.g * 0.08f), Mathf.Clamp01(core.b * 0.08f), 1f);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color sourcePixel = readableSource.GetPixel(x, y);
                        float luminance = Mathf.Clamp01((sourcePixel.r * 0.299f) + (sourcePixel.g * 0.587f) + (sourcePixel.b * 0.114f));
                        float detail = Mathf.Clamp01(luminance * 1.25f);
                        Color pixel = Color.Lerp(shadow, core, detail);
                        pixel.r *= Mathf.Lerp(0.45f, 1.15f, luminance);
                        pixel.g *= Mathf.Lerp(0.18f, 0.65f, luminance);
                        pixel.b *= Mathf.Lerp(0.12f, 0.45f, luminance);
                        pixel.a = sourcePixel.a;
                        texture.SetPixel(x, y, pixel);
                    }
                }

                texture.Apply(false, false);
                s_GeneratedBloodMoonTexture = texture;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.EnsureBloodMoonTexture.2", "CustomWeatherStageRuntime.EnsureBloodMoonTexture failed.", caughtException);
                if (texture != null) UnityEngine.Object.Destroy(texture);
                return null;
            }
            finally
            {
                if (readableSource != null) UnityEngine.Object.Destroy(readableSource);
            }

            return s_GeneratedBloodMoonTexture;
        }

        private static Texture2D? CreateReadableMoonTextureCopy(Texture2D? source)
        {
            if (source == null) return null;

            int width;
            int height;
            try
            {
                width = Mathf.Clamp(source.width, 64, 512);
                height = Mathf.Clamp(source.height, 64, 512);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.CreateReadableMoonTextureCopy.1", "CustomWeatherStageRuntime.CreateReadableMoonTextureCopy failed.", caughtException);
                return null;
            }

            RenderTexture? previousActive = RenderTexture.active;
            RenderTexture? temporary = null;
            Texture2D? readable = null;

            try
            {
                temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;

                readable = new(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                readable.Apply(false, false);
                return readable;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.CreateReadableMoonTextureCopy.2", "CustomWeatherStageRuntime.CreateReadableMoonTextureCopy failed.", caughtException);
                if (readable != null) UnityEngine.Object.Destroy(readable);
                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (temporary != null) RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static int GetSafeMoonPhaseIndex(UniStormWeatherSystem uniStorm)
        {
            if (uniStorm == null) return 0;
            try
            {
                int phaseIndex = Math.Max(0, uniStorm.m_CurrentMoonPhaseIndex);
                var phases = uniStorm.m_MoonPhases;
                if (phases != null && phases.Length > 0) return Mathf.Clamp(phaseIndex, 0, phases.Length - 1);
                return phaseIndex;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetSafeMoonPhaseIndex.1", "CustomWeatherStageRuntime.GetSafeMoonPhaseIndex failed.", caughtException);
                return 0;
            }
        }

        private static void RefreshMoonPhase(UniStormWeatherSystem uniStorm, int phaseIndex)
        {
            if (uniStorm == null) return;
            uniStorm.m_MoonPhaseSet = false;
            uniStorm.SetMoonPhaseIndex(Math.Max(0, phaseIndex));
            uniStorm.SetMoonPhase();
        }

        private static void BackupBloodMoonVisuals(UniStormWeatherSystem uniStorm)
        {
            if (!s_HasBloodMoonVisualsBackup)
            {
                try
                {
                    s_OriginalUseMoonColourOverride = uniStorm.m_UseMoonColourColorOverride;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupBloodMoonVisuals.1", "CustomWeatherStageRuntime.BackupBloodMoonVisuals failed.", caughtException);
                    s_OriginalUseMoonColourOverride = false;
                }
                try
                {
                    s_OriginalMoonColourOverride = uniStorm.m_MoonLightColorOverride;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupBloodMoonVisuals.2", "CustomWeatherStageRuntime.BackupBloodMoonVisuals failed.", caughtException);
                    s_OriginalMoonColourOverride = Color.white;
                }
                try
                {
                    s_OriginalMoonLightColor = uniStorm.m_MoonLightColor;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupBloodMoonVisuals.3", "CustomWeatherStageRuntime.BackupBloodMoonVisuals failed.", caughtException);
                    s_OriginalMoonLightColor = Color.white;
                }
                try
                {
                    s_OriginalStarColor = uniStorm.m_StarColor;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupBloodMoonVisuals.4", "CustomWeatherStageRuntime.BackupBloodMoonVisuals failed.", caughtException);
                    s_OriginalStarColor = Color.white;
                }
                s_HasBloodMoonVisualsBackup = true;
            }

            if (!s_HasBloodMoonStarSpherePropertyBlockBackup)
            {
                Renderer? starSphere = SafeGetRenderer(() => uniStorm.m_StarSphereRenderer);
                if (starSphere != null)
                {
                    try
                    {
                        s_OriginalBloodMoonStarSpherePropertyBlock ??= new MaterialPropertyBlock();
                        starSphere.GetPropertyBlock(s_OriginalBloodMoonStarSpherePropertyBlock);
                        s_HasBloodMoonStarSpherePropertyBlockBackup = true;
                    }
                    catch (Exception caughtException)
                    {
                        Core.LogExceptionOnce("CustomWeatherStageRuntime.BackupBloodMoonVisuals.PropertyBlock", "CustomWeatherStageRuntime failed to back up the MoonAndStars property block.", caughtException);
                    }
                }
            }

            Material? skybox = GetSkyboxMaterial(uniStorm);
            int moonColorId = SafeShaderId(() => UniStormWeatherSystem.s_MoonColorShaderID);
            int moonGlowId = SafeShaderId(() => UniStormWeatherSystem.s_MoonGlowShaderID);
            if (!s_HasSkyboxMoonColorBackup && skybox != null && moonColorId >= 0 && SafeMaterialHasProperty(skybox, moonColorId))
            {
                s_OriginalSkyboxMoonColor = SafeGetMaterialColor(skybox, moonColorId, Color.white);
                s_HasSkyboxMoonColorBackup = true;
            }
            if (!s_HasSkyboxMoonGlowBackup && skybox != null && moonGlowId >= 0 && SafeMaterialHasProperty(skybox, moonGlowId))
            {
                s_OriginalSkyboxMoonGlow = SafeGetMaterialColor(skybox, moonGlowId, Color.white);
                s_HasSkyboxMoonGlowBackup = true;
            }
            if (!s_HasSkyboxSunHaloBackup && skybox != null)
            {
                int sunSizeId = SafeShaderId(() => UniStormWeatherSystem.s_SunSizeShaderID);
                int sunDiffusionId = SafeShaderId(() => UniStormWeatherSystem.s_SunDiffusionShaderID);
                if (sunSizeId >= 0 && sunDiffusionId >= 0 && SafeMaterialHasProperty(skybox, sunSizeId) && SafeMaterialHasProperty(skybox, sunDiffusionId))
                {
                    s_OriginalSkyboxSunSize = skybox.GetFloat(sunSizeId);
                    s_OriginalSkyboxSunDiffusion = skybox.GetFloat(sunDiffusionId);
                    s_HasSkyboxSunHaloBackup = true;
                }
            }
        }

        private static void RestoreBloodMoonVisuals()
        {
            if (!s_BloodMoonVisualsOverridden && !s_HasBloodMoonVisualsBackup && !s_HasSkyboxMoonColorBackup && !s_HasSkyboxMoonGlowBackup && !s_HasSkyboxSunHaloBackup && !s_MoonTextureOverridden && !s_HasMoonTextureBackup && !s_HasSkyboxMoonTextureBackup) return;
            UniStormWeatherSystem uniStorm = GameManager.GetUniStorm();
            if (uniStorm != null)
            {
                RestoreBloodMoonMoonTexture(uniStorm);
                RestoreBloodMoonMoonAndStarsPropertyBlock(uniStorm);
                try
                {
                    uniStorm.SetMoonColourOverride(s_OriginalUseMoonColourOverride);
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreBloodMoonVisuals.1", "CustomWeatherStageRuntime.RestoreBloodMoonVisuals failed.", caughtException);
                    uniStorm.m_UseMoonColourColorOverride = s_OriginalUseMoonColourOverride;
                }
                try
                {
                    uniStorm.SetMoonColourOverrideColour(s_OriginalMoonColourOverride);
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreBloodMoonVisuals.2", "CustomWeatherStageRuntime.RestoreBloodMoonVisuals failed.", caughtException);
                    uniStorm.m_MoonLightColorOverride = s_OriginalMoonColourOverride;
                }
                uniStorm.m_MoonLightColor = s_OriginalMoonLightColor;
                uniStorm.m_StarColor = s_OriginalStarColor;

                Material? skybox = GetSkyboxMaterial(uniStorm);
                if (skybox != null)
                {
                    if (s_HasSkyboxMoonColorBackup) TrySetMaterialColor(skybox, SafeShaderId(() => UniStormWeatherSystem.s_MoonColorShaderID), s_OriginalSkyboxMoonColor);
                    if (s_HasSkyboxMoonGlowBackup) TrySetMaterialColor(skybox, SafeShaderId(() => UniStormWeatherSystem.s_MoonGlowShaderID), s_OriginalSkyboxMoonGlow);
                    if (s_HasSkyboxSunHaloBackup)
                    {
                        TrySetMaterialFloat(skybox, SafeShaderId(() => UniStormWeatherSystem.s_SunSizeShaderID), s_OriginalSkyboxSunSize);
                        TrySetMaterialFloat(skybox, SafeShaderId(() => UniStormWeatherSystem.s_SunDiffusionShaderID), s_OriginalSkyboxSunDiffusion);
                    }
                }
            }

            s_CurrentMoonColour = s_OriginalMoonColourOverride;
            s_CurrentMoonLightColour = s_OriginalMoonLightColor;
            s_CurrentStarColour = s_OriginalStarColor;
            s_CurrentMoonGlowColour = s_OriginalSkyboxMoonGlow;
            s_BloodMoonVisualsOverridden = false;
            s_HasBloodMoonVisualsBackup = false;
            s_HasSkyboxMoonColorBackup = false;
            s_HasSkyboxMoonGlowBackup = false;
            s_HasSkyboxSunHaloBackup = false;
            s_HasBloodMoonStarSpherePropertyBlockBackup = false;
            s_OriginalBloodMoonStarSpherePropertyBlock?.Clear();
            s_OriginalSkyboxSunSize = 0f;
            s_OriginalSkyboxSunDiffusion = 0f;
            s_OriginalUseMoonColourOverride = false;
            s_OriginalMoonColourOverride = Color.white;
            s_OriginalMoonLightColor = Color.white;
            s_OriginalStarColor = Color.white;
            s_OriginalSkyboxMoonColor = Color.white;
            s_OriginalSkyboxMoonGlow = Color.white;
            ClearBloodMoonTextureState();
        }

        private static void RestoreBloodMoonMoonTexture(UniStormWeatherSystem uniStorm)
        {
            if (uniStorm == null) return;
            int phaseIndex = s_OriginalMoonTextureIndex >= 0 ? s_OriginalMoonTextureIndex : GetSafeMoonPhaseIndex(uniStorm);

            if (s_HasMoonTextureBackup && s_OriginalMoonPhaseTexture != null)
            {
                try
                {
                    var phases = uniStorm.m_MoonPhases;
                    if (phases != null && phaseIndex >= 0 && phaseIndex < phases.Length) phases[phaseIndex] = s_OriginalMoonPhaseTexture;
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("CustomWeatherStageRuntime.RestoreBloodMoonMoonTexture.1", "CustomWeatherStageRuntime.RestoreBloodMoonMoonTexture failed.", caughtException);
                }
            }

            Material? skybox = GetSkyboxMaterial(uniStorm);
            int moonTexId = SafeShaderId(() => UniStormWeatherSystem.s_MoonTexShaderID);
            if (skybox != null && moonTexId >= 0)
            {
                if (s_OriginalMoonPhaseTexture != null) TrySetMaterialTexture(skybox, moonTexId, s_OriginalMoonPhaseTexture);
                else if (s_HasSkyboxMoonTextureBackup && s_OriginalSkyboxMoonTexture != null) TrySetMaterialTexture(skybox, moonTexId, s_OriginalSkyboxMoonTexture);
            }

            RefreshMoonPhase(uniStorm, phaseIndex);
        }

        private static void ClearBloodMoonTextureState()
        {
            s_MoonTextureOverridden = false;
            s_HasMoonTextureBackup = false;
            s_OriginalMoonTextureIndex = -1;
            s_OriginalMoonPhaseTexture = null;
            s_HasSkyboxMoonTextureBackup = false;
            s_OriginalSkyboxMoonTexture = null;
            if (s_GeneratedBloodMoonTexture != null)
            {
                UnityEngine.Object.Destroy(s_GeneratedBloodMoonTexture);
                s_GeneratedBloodMoonTexture = null;
            }
        }

        private static HeightFogManager? GetHeightFogManager(UniStormWeatherSystem? uniStorm)
        {
            if (uniStorm == null) return null;
            try
            {
                return uniStorm.m_HeightFogManager;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetHeightFogManager.1", "CustomWeatherStageRuntime.GetHeightFogManager failed.", caughtException);
                return null;
            }
        }

        private static Material? GetSkyboxMaterial(UniStormWeatherSystem? uniStorm)
        {
            if (uniStorm == null) return null;
            try
            {
                if (uniStorm.m_SkyboxMaterialCopy != null) return uniStorm.m_SkyboxMaterialCopy;
                return uniStorm.m_SkyboxMaterial;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.GetSkyboxMaterial.1", "CustomWeatherStageRuntime.GetSkyboxMaterial failed.", caughtException);
                return null;
            }
        }

        private static int SafeShaderId(Func<int> getter)
        {
            try
            {
                return getter();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.SafeShaderId.1", "CustomWeatherStageRuntime.SafeShaderId failed.", caughtException);
                return -1;
            }
        }

        private static bool SafeMaterialHasProperty(Material? material, int shaderId)
        {
            try
            {
                return material != null && shaderId >= 0 && material.HasProperty(shaderId);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.SafeMaterialHasProperty.1", "CustomWeatherStageRuntime.SafeMaterialHasProperty failed.", caughtException);
                return false;
            }
        }

        private static Color SafeGetMaterialColor(Material? material, int shaderId, Color fallback)
        {
            try
            {
                if (material == null || shaderId < 0 || !material.HasProperty(shaderId)) return fallback;
                return material.GetColor(shaderId);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.SafeGetMaterialColor.1", "CustomWeatherStageRuntime.SafeGetMaterialColor failed.", caughtException);
                return fallback;
            }
        }

        private static void TrySetMaterialColor(Material? material, int shaderId, Color value)
        {
            try
            {
                if (material == null || shaderId < 0 || !material.HasProperty(shaderId)) return;
                material.SetColor(shaderId, value);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.TrySetMaterialColor.1", "CustomWeatherStageRuntime.TrySetMaterialColor failed.", caughtException);
            }
        }

        private static void TrySetMaterialFloat(Material? material, int shaderId, float value)
        {
            try
            {
                if (material == null || shaderId < 0 || !material.HasProperty(shaderId)) return;
                material.SetFloat(shaderId, value);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.TrySetMaterialFloat.2", "CustomWeatherStageRuntime.TrySetMaterialFloat failed.", caughtException);
            }
        }

        private static Texture? SafeGetMaterialTexture(Material? material, int shaderId, Texture? fallback)
        {
            try
            {
                if (material == null || shaderId < 0 || !material.HasProperty(shaderId)) return fallback;
                return material.GetTexture(shaderId);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.SafeGetMaterialTexture.1", "CustomWeatherStageRuntime.SafeGetMaterialTexture failed.", caughtException);
                return fallback;
            }
        }

        private static void TrySetMaterialTexture(Material? material, int shaderId, Texture? value)
        {
            try
            {
                if (material == null || shaderId < 0 || !material.HasProperty(shaderId)) return;
                material.SetTexture(shaderId, value);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("CustomWeatherStageRuntime.TrySetMaterialTexture.1", "CustomWeatherStageRuntime.TrySetMaterialTexture failed.", caughtException);
            }
        }

        private static Color WithAlphaOne(Color color)
        {
            color.a = 1f;
            return color;
        }

        private static void ApplyWindLockValue(float mph)
        {
            Wind wind = GameManager.GetWindComponent();
            if (wind == null) return;

            float clamped = Math.Max(0f, mph);
            wind.LockWindInstant(clamped);
            s_WindLockedByCustomStage = true;
            s_CurrentWindLockMPH = clamped;
            s_TargetWindLockMPH = clamped;
            s_WindRangeMinMPH = clamped;
            s_WindRangeMaxMPH = clamped;
            s_LastWindUpdateRealtime = Time.realtimeSinceStartup;
        }

        private static void ApplyWindRange(float minMPH, float maxMPH)
        {
            Wind wind = GameManager.GetWindComponent();
            if (wind == null) return;

            if (minMPH < 0f || maxMPH < 0f)
            {
                RestoreWindLock();
                return;
            }

            minMPH = Math.Max(0f, minMPH);
            maxMPH = Math.Max(minMPH, maxMPH);
            float now = Time.realtimeSinceStartup;
            bool rangeChanged = Math.Abs(s_WindRangeMinMPH - minMPH) > 0.01f || Math.Abs(s_WindRangeMaxMPH - maxMPH) > 0.01f;

            if (!s_WindLockedByCustomStage || s_CurrentWindLockMPH < 0f)
            {
                s_WindRangeMinMPH = minMPH;
                s_WindRangeMaxMPH = maxMPH;
                float currentWind = Math.Max(0f, wind.GetSpeedMPH());
                s_CurrentWindLockMPH = Mathf.Clamp(currentWind, minMPH, maxMPH);
                s_TargetWindLockMPH = s_CurrentWindLockMPH;
                s_NextWindTargetRealtime = now + UnityEngine.Random.Range(WindTargetIntervalMinSeconds, WindTargetIntervalMaxSeconds);
                s_LastWindUpdateRealtime = now;
            }
            else if (rangeChanged)
            {
                s_WindRangeMinMPH = minMPH;
                s_WindRangeMaxMPH = maxMPH;
                s_CurrentWindLockMPH = Mathf.Clamp(s_CurrentWindLockMPH, minMPH, maxMPH);
                s_TargetWindLockMPH = Mathf.Clamp(s_TargetWindLockMPH, minMPH, maxMPH);
            }

            if (now >= s_NextWindTargetRealtime)
            {
                s_TargetWindLockMPH = UnityEngine.Random.Range(minMPH, maxMPH);
                s_NextWindTargetRealtime = now + UnityEngine.Random.Range(WindTargetIntervalMinSeconds, WindTargetIntervalMaxSeconds);
            }

            float deltaSeconds = Math.Max(0.01f, now - s_LastWindUpdateRealtime);
            s_LastWindUpdateRealtime = now;
            float windWidth = Math.Max(1f, maxMPH - minMPH);
            float maxDelta = Math.Max(0.5f, windWidth * 0.65f) * deltaSeconds;
            s_CurrentWindLockMPH = Mathf.MoveTowards(s_CurrentWindLockMPH, s_TargetWindLockMPH, maxDelta);
            wind.LockWindInstant(s_CurrentWindLockMPH);
            s_WindLockedByCustomStage = true;
        }

        private static void RestoreWindLock()
        {
            if (!s_WindLockedByCustomStage) return;
            Wind wind = GameManager.GetWindComponent();
            wind?.LockWindInstant(WindUnlockedMPH);
            s_WindLockedByCustomStage = false;
            s_WindRangeMinMPH = -1f;
            s_WindRangeMaxMPH = -1f;
            s_CurrentWindLockMPH = -1f;
            s_TargetWindLockMPH = -1f;
            s_NextWindTargetRealtime = 0f;
            s_LastWindUpdateRealtime = 0f;
        }

        private static void RestoreTemperatureLock()
        {
            s_CalculatedTemperatureOffsetCelsius = 0f;
        }

        private static void BackupFogScale()
        {
            if (s_HasFogScaleBackup) return;
            s_OriginalFogScale = UniStormWeatherSystem.m_FogScale;
            s_HasFogScaleBackup = true;
        }

        private static void RestoreFogScale()
        {
            if (s_HasFogScaleBackup)
            {
                UniStormWeatherSystem.m_FogScale = s_OriginalFogScale;
                s_HasFogScaleBackup = false;
            }

            s_CustomFogScaleActive = false;
            s_CurrentCustomFogScale = 1f;
        }

        private static void RestoreSnowPresetOverrideTowardStage(WeatherStage targetStage)
        {
            if (!s_SnowPresetOverridden) return;

            if (s_CurrentEffectBlend >= 0.999f)
            {
                RestoreSnowPresetOverrideToStage(targetStage);
                return;
            }

            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;

            WeatherStage fadeStage = s_CurrentSnowPresetStage != WeatherStage.Undefined ? s_CurrentSnowPresetStage : WeatherStage.LightSnow;
            float targetIntensity = targetStage == WeatherStage.LightSnow || targetStage == WeatherStage.HeavySnow || targetStage == WeatherStage.Blizzard ? 1f : 0f;
            float intensity = Mathf.Lerp(Math.Max(0f, s_EffectStartSnowIntensity), targetIntensity, s_CurrentEffectBlend);
            if (TrySetSnowPresetBlend(weather, fadeStage)) BoostFallingSnowParticles(weather.m_FallingSnowParticleSystem, weather.m_FallingSnowParticleSystemRenderer, Math.Max(0.1f, intensity));
        }


        private static void DiscardSnowPresetOverrideState()
        {
            s_SnowPresetOverridden = false;
            s_FallingSnowSuppressedByCustomStage = false;
            ClearFallingSnowParticleCache();
            s_CurrentSnowPresetStage = WeatherStage.Undefined;
            s_CurrentSnowPresetWeather = null;
            s_CurrentSnowIntensity = 1f;
        }

        private static void RestoreSnowPresetOverrideToStage(WeatherStage targetStage)
        {
            if (!s_SnowPresetOverridden) return;
            var weather = GameManager.GetWeatherComponent();
            if (weather == null) return;

            TrySetSnowPresetBlend(weather, targetStage == WeatherStage.Undefined ? WeatherStage.Clear : targetStage);
            s_SnowPresetOverridden = false;
            s_FallingSnowSuppressedByCustomStage = false;
            ClearFallingSnowParticleCache();
            s_CurrentSnowPresetStage = WeatherStage.Undefined;
            s_CurrentSnowPresetWeather = null;
            s_CurrentSnowIntensity = 1f;
        }

        private static void ClearRuntimeOverrides(bool clearRequestedMode, bool log)
        {
            bool hadActiveCustomStage = s_RequestedMode > 0 || s_ActiveMode > 0 || s_ForecastOverrideActive;
            RestoreFogScale();
            RestoreFogColor();
            RestoreDirectFogDensity();
            RestoreMasterAmbient();
            RestoreBloodMoonVisuals();
            RestoreSnowColor();
            RestoreVisualAuroraAlpha();
            RestoreWindLock();
            RestoreTemperatureLock();
            if (s_SuppressForecastPrecipitationVisuals || s_SuppressOutdoorNightEventVisuals) DiscardSnowPresetOverrideState();
            else RestoreSnowPresetOverrideToStage(WeatherStage.Clear);
            s_SuppressForecastPrecipitationVisuals = false;
            s_ActiveMode = 0;
            s_FoggyAuroraStageSpoofActive = false;
            s_ForecastOverrideActive = false;
            ClearImmediateForecastPlanLatch();
            s_LastAppliedLogMode = 0;
            s_LastForcedBaseStage = WeatherStage.Undefined;
            s_NextMaintenanceRealtime = 0f;
            s_CurrentEffectBlend = 1f;
            s_EffectTransitionMode = 0;
            s_ForecastFadeOutTransitionActive = false;
            s_ForecastFadeOutTargetStageId = WeatherStageId.Undefined;
            s_CurrentSnowPresetStage = WeatherStage.Undefined;
            s_CurrentSnowPresetWeather = null;
            s_FallingSnowSuppressedByCustomStage = false;
            ClearFallingSnowParticleCache();

            if (clearRequestedMode) s_RequestedMode = 0;
            if (!log || !hadActiveCustomStage) return;

            Core.Log("[CustomWeatherStage] Custom weather override ended; vanilla weather control restored.", false);
            Core.Log("[CustomWeatherStage] Cleared custom WeatherStage overrides.");
        }

        private static void LogApplied(string displayName, string details)
        {
            int mode = s_RequestedMode > 0 ? s_RequestedMode : s_ActiveMode;
            if (mode != 0 && mode == s_LastAppliedLogMode) return;

            s_LastAppliedLogMode = mode;
            Core.Log($"[CustomWeatherStage] Active: {displayName}.", false);
            Core.Log($"[CustomWeatherStage] Applied {displayName} | {details}.");
        }

        internal static bool TryGetCalculatedTemperatureOffset(out float offsetCelsius)
        {
            if (!GameplaySceneState.IsGameplaySceneActive())
            {
                offsetCelsius = 0f;
                return false;
            }

            offsetCelsius = s_CalculatedTemperatureOffsetCelsius;
            if (Math.Abs(offsetCelsius) <= 0.01f) return false;
            return s_RequestedMode > 0 || s_ForecastOverrideActive;
        }

        internal static bool TryGetActiveStageId(out WeatherStageId stageId)
        {
            if (!GameplaySceneState.IsGameplaySceneActive())
            {
                stageId = WeatherStageId.Undefined;
                return false;
            }

            int mode = s_RequestedMode > 0 ? s_RequestedMode : (s_ForecastOverrideActive ? s_ActiveMode : 0);
            stageId = GetStageIdFromMode(mode);
            return stageId != WeatherStageId.Undefined;
        }

        private static int GetModeFromStageId(WeatherStageId stageId)
        {
            return stageId switch
            {
                WeatherStageId.Ashfall => ModeAshfall,
                WeatherStageId.Whiteout => ModeWhiteout,
                WeatherStageId.VeryDenseFog => ModeVeryDenseFog,
                WeatherStageId.FreezingFog => ModeFreezingFog,
                WeatherStageId.VeryHeavySnow => ModeVeryHeavySnow,
                WeatherStageId.WindyLightSnow => ModeWindyLightSnow,
                WeatherStageId.ViolentBlizzard => ModeViolentBlizzard,
                WeatherStageId.LowOvercast => ModeLowOvercast,
                WeatherStageId.HeavyOvercast => ModeHeavyOvercast,
                WeatherStageId.CloudyAurora => ModeCloudyAurora,
                WeatherStageId.FoggyAurora => ModeFoggyAurora,
                WeatherStageId.SnowyAurora => ModeSnowyAurora,
                WeatherStageId.ClearBloodMoon => ModeBloodMoon,
                WeatherStageId.SnowBloodMoon => ModeLightSnowBloodMoon,
                _ => 0
            };
        }

        private static WeatherStageId GetStageIdFromMode(int mode)
        {
            return mode switch
            {
                ModeAshfall => WeatherStageId.Ashfall,
                ModeWhiteout => WeatherStageId.Whiteout,
                ModeVeryDenseFog => WeatherStageId.VeryDenseFog,
                ModeFreezingFog => WeatherStageId.FreezingFog,
                ModeVeryHeavySnow => WeatherStageId.VeryHeavySnow,
                ModeWindyLightSnow => WeatherStageId.WindyLightSnow,
                ModeViolentBlizzard => WeatherStageId.ViolentBlizzard,
                ModeLowOvercast => WeatherStageId.LowOvercast,
                ModeHeavyOvercast => WeatherStageId.HeavyOvercast,
                ModeCloudyAurora => WeatherStageId.CloudyAurora,
                ModeFoggyAurora => WeatherStageId.FoggyAurora,
                ModeSnowyAurora => WeatherStageId.SnowyAurora,
                ModeBloodMoon => WeatherStageId.ClearBloodMoon,
                ModeLightSnowBloodMoon => WeatherStageId.SnowBloodMoon,
                _ => WeatherStageId.Undefined
            };
        }
    }
}