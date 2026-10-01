namespace WeatherOverhaul.Weather
{
    internal enum BloodMoonInfluenceState
    {
        None,
        ClearBloodMoon,
        SnowBloodMoon
    }

    internal static class BloodMoonInfluenceRuntime
    {
        private const float AnimalRegistryCleanupIntervalSeconds = 10f;
        private const float DamageBleedRestoreGuardSeconds = 2f;
        private const float BloodMoonExitRefreshDurationSeconds = 2f;
        private const float BloodMoonExitRefreshIntervalSeconds = 0.2f;

        private static readonly Dictionary<BaseAi, AnimalInfluenceState> s_Animals = [];
        private static readonly Color s_BloodMoonAnimalAuroraColor = new(0.42f, 0.015f, 0.01f, 1f);
        private static readonly int[] s_BloodMoonAnimalColorIds =
        [
            Shader.PropertyToID("_TintColor"),
            Shader.PropertyToID("_Color"),
            Shader.PropertyToID("_EmissionColor"),
            Shader.PropertyToID("_AuroraColor"),
            Shader.PropertyToID("_AuroraColour"),
            Shader.PropertyToID("_GlowColor"),
            Shader.PropertyToID("_RimColor"),
            Shader.PropertyToID("_FresnelColor"),
            Shader.PropertyToID("_BaseColor"),
            Shader.PropertyToID("_MainColor"),
            Shader.PropertyToID("_OverlayColor")
        ];


        private static Material? s_BloodMoonEyesAuroraMaterial;
        private static Material? s_BloodMoonWildlifeAuroraTemplate;
        private static Texture2D? s_BloodMoonAuroraColoursTexture;
        private static float s_BloodMoonAuroraColoursTextureBlend = -1f;
        private static float s_LastAppliedAnimalAuraMaterialBlend = -1f;
        private static bool s_AuroraMaterialLookupAttempted;
        private static float s_NextAuroraMaterialLookupRealtime;
        private static bool s_AuroraMaterialLookupWarningLogged;
        private static float s_NextAnimalRegistryCleanupRealtime;
        private static float s_BloodMoonExitRefreshUntilRealtime;
        private static float s_NextBloodMoonExitRefreshRealtime;
        private static BloodMoonInfluenceState s_LastLoggedState = BloodMoonInfluenceState.None;
        private static float s_LastLoggedBleedMultiplier = -1f;

        internal struct BleedState
        {
            internal bool ShouldRestore;
            internal bool WasBleedingOut;
            internal float DeathAfterBleedingOutMinutes;
            internal float ElapsedBleedingOutMinutes;
        }

        internal static BloodMoonInfluenceState CurrentState { get; private set; } = BloodMoonInfluenceState.None;
        internal static float CurrentMoonIllumination01 { get; private set; }
        internal static float CurrentAnimalBleedSpeedMultiplier { get; private set; } = 1f;

        internal static bool IsAnyBloodMoonActive => GameplaySceneState.IsGameplaySceneActive() && CurrentState != BloodMoonInfluenceState.None;
        internal static bool IsClearBloodMoonActive => GameplaySceneState.IsGameplaySceneActive() && CurrentState == BloodMoonInfluenceState.ClearBloodMoon;
        internal static bool IsSnowBloodMoonActive => GameplaySceneState.IsGameplaySceneActive() && CurrentState == BloodMoonInfluenceState.SnowBloodMoon;

        internal static void Update(WeatherSnapshot snapshot)
        {
            BloodMoonInfluenceState previousState = CurrentState;
            CurrentState = ResolveState(snapshot);
            CurrentMoonIllumination01 = CurrentState == BloodMoonInfluenceState.None ? 0f : GetCurrentMoonIllumination01();
            CurrentAnimalBleedSpeedMultiplier = CurrentState == BloodMoonInfluenceState.None ? 1f : Mathf.Clamp01(1f - CurrentMoonIllumination01);

            MaybeLogState();

            if (CurrentState != previousState)
            {
                if (CurrentState == BloodMoonInfluenceState.None)
                {
                    RestoreAllLoadedAnimalsAfterBloodMoon();
                    s_LastAppliedAnimalAuraMaterialBlend = -1f;
                    s_BloodMoonExitRefreshUntilRealtime = Time.realtimeSinceStartup + BloodMoonExitRefreshDurationSeconds;
                    s_NextBloodMoonExitRefreshRealtime = Time.realtimeSinceStartup + BloodMoonExitRefreshIntervalSeconds;
                }
                else
                {
                    s_BloodMoonExitRefreshUntilRealtime = 0f;
                    s_NextBloodMoonExitRefreshRealtime = 0f;
                    ApplyToRegisteredAnimals();
                }
            }

            if (CurrentState == BloodMoonInfluenceState.None &&
                Time.realtimeSinceStartup < s_BloodMoonExitRefreshUntilRealtime &&
                Time.realtimeSinceStartup >= s_NextBloodMoonExitRefreshRealtime)
            {
                s_NextBloodMoonExitRefreshRealtime = Time.realtimeSinceStartup + BloodMoonExitRefreshIntervalSeconds;
                RestoreRegisteredAnimalsAfterBloodMoon();
            }

            if (Time.realtimeSinceStartup >= s_NextAnimalRegistryCleanupRealtime)
            {
                s_NextAnimalRegistryCleanupRealtime = Time.realtimeSinceStartup + AnimalRegistryCleanupIntervalSeconds;
                CleanupInvalidStates();
            }
        }

        internal static void NotifyBaseAiAwake(BaseAi ai)
        {
            if (!GameplaySceneState.IsGameplaySceneActive()) return;
            if (ai == null) return;
            EnsureState(ai);
            ApplyToAnimal(ai);
        }

        internal static void NotifyBaseAiUpdate(BaseAi ai)
        {
            if (!GameplaySceneState.IsGameplaySceneActive()) return;
            if (ai == null) return;
            ApplyToAnimal(ai);
        }

        internal static void ScaleUpdateWoundsRealtime(BaseAi ai, ref float realtimeSeconds)
        {
            if (ai == null) return;
            if (!IsAnyBloodMoonActive) return;

            float multiplier = CurrentAnimalBleedSpeedMultiplier;
            if (multiplier >= 0.999f) return;

            realtimeSeconds *= Mathf.Clamp01(multiplier);
        }

        internal static void CaptureBleedStateBeforeApplyDamage(BaseAi ai, out BleedState state)
        {
            state = default;
            if (ai == null) return;
            if (!IsAnyBloodMoonActive) return;
            if (CurrentAnimalBleedSpeedMultiplier > 0.01f) return;

            state.ShouldRestore = true;
            state.WasBleedingOut = SafeIsBleedingOut(ai);
            state.DeathAfterBleedingOutMinutes = SafeGet(() => ai.m_DeathAfterBleeingOutMinutes, 0f);
            state.ElapsedBleedingOutMinutes = SafeGet(() => ai.m_ElapsedBleedingOutMinutes, 0f);
        }

        internal static void RestoreBleedStateAfterApplyDamage(BaseAi ai, BleedState state)
        {
            if (ai == null || !state.ShouldRestore) return;
            RestoreBleedState(ai, state);

            AnimalInfluenceState animalState = EnsureState(ai);
            animalState.PendingBleedRestore = state;
            animalState.HasPendingBleedRestore = true;
            animalState.PendingBleedRestoreUntilRealtime = Time.realtimeSinceStartup + DamageBleedRestoreGuardSeconds;
        }

        private static void ApplyToRegisteredAnimals()
        {
            CleanupInvalidStates();
            if (s_Animals.Count <= 0) return;

            try
            {
                foreach (KeyValuePair<BaseAi, AnimalInfluenceState> pair in s_Animals)
                {
                    if (pair.Key == null) continue;
                    ApplyToAnimal(pair.Key);
                }
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("blood-moon-animal-registry-apply", "[BloodMoonInfluence] Failed to update registered animals.", e);
            }
        }

        private static void RestoreAllLoadedAnimalsAfterBloodMoon()
        {
            int restored = 0;

            try
            {
                var animals = Resources.FindObjectsOfTypeAll<BaseAi>();
                if (animals != null)
                {
                    for (int i = 0; i < animals.Length; i++)
                    {
                        BaseAi ai = animals[i];
                        if (!IsLoadedSceneAnimal(ai)) continue;

                        AnimalInfluenceState state = EnsureState(ai);
                        RestorePostStruggleIgnoreInfluence(ai, state);
                        RestoreAnimalAuroraInfluence(ai, state);
                        SynchronizeVanillaAuroraMaterials(ai);
                        restored++;
                    }
                }
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("blood-moon-loaded-animal-restore", "[BloodMoonInfluence] Failed to restore loaded animals after Blood Moon.", e);
            }

            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log("[BloodMoonInfluence] Blood Moon ended; restored " + restored + " loaded animal(s).");
        }

        private static void RestoreRegisteredAnimalsAfterBloodMoon()
        {
            CleanupInvalidStates();
            if (s_Animals.Count <= 0) return;

            try
            {
                foreach (KeyValuePair<BaseAi, AnimalInfluenceState> pair in s_Animals)
                {
                    BaseAi ai = pair.Key;
                    if (!IsLoadedSceneAnimal(ai)) continue;

                    RestorePostStruggleIgnoreInfluence(ai, pair.Value);
                    RestoreAnimalAuroraInfluence(ai, pair.Value);
                    SynchronizeVanillaAuroraMaterials(ai);
                }
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("blood-moon-registered-animal-restore", "[BloodMoonInfluence] Failed to finish restoring registered animals after Blood Moon.", e);
            }
        }

        private static bool IsLoadedSceneAnimal(BaseAi ai)
        {
            if (ai == null || ai.gameObject == null) return false;

            try
            {
                return ai.gameObject.scene.IsValid() && ai.gameObject.activeInHierarchy;
            }
            catch
            {
                return false;
            }
        }

        private static void SynchronizeVanillaAuroraMaterials(BaseAi ai)
        {
            if (ai == null) return;

            try
            {
                ai.EnableAuroraMaterials(IsVanillaAuroraActive());
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.SynchronizeVanillaAuroraMaterials.1", "BloodMoonInfluenceRuntime.SynchronizeVanillaAuroraMaterials failed.", caughtException);
            }
        }

        private static bool IsVanillaAuroraActive()
        {
            try
            {
                AuroraManager manager = GameManager.GetAuroraManager();
                return manager != null && manager.AuroraIsActive();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.IsVanillaAuroraActive.1", "BloodMoonInfluenceRuntime.IsVanillaAuroraActive failed.", caughtException);
                return false;
            }
        }

        private static void ApplyToAnimal(BaseAi ai)
        {
            if (ai == null) return;

            AnimalInfluenceState state = EnsureState(ai);
            if (state.HasPendingBleedRestore)
            {
                if (Time.realtimeSinceStartup <= state.PendingBleedRestoreUntilRealtime && CurrentState != BloodMoonInfluenceState.None && CurrentAnimalBleedSpeedMultiplier <= 0.01f) RestoreBleedState(ai, state.PendingBleedRestore);
                else state.HasPendingBleedRestore = false;
            }

            if (IsBloodMoonPredator(ai)) ApplyPostStruggleIgnoreInfluence(ai, state);
            else RestorePostStruggleIgnoreInfluence(ai, state);

            ApplyAnimalAuroraInfluence(ai, state);
        }

        private static void ApplyPostStruggleIgnoreInfluence(BaseAi ai, AnimalInfluenceState state)
        {
            if (CurrentState != BloodMoonInfluenceState.ClearBloodMoon && CurrentState != BloodMoonInfluenceState.SnowBloodMoon)
            {
                RestorePostStruggleIgnoreInfluence(ai, state);
                return;
            }

            if (!state.HasPostStruggleIgnoreBackup)
            {
                state.OriginalIgnoreFootStepsAndSmellsAfterStruggleSeconds = ai.m_IgnoreFootStepsAndSmellsAfterStruggleSeconds;
                state.HasPostStruggleIgnoreBackup = true;
            }

            ai.m_IgnoreFootStepsAndSmellsAfterStruggleSeconds = Mathf.Max(0f, state.OriginalIgnoreFootStepsAndSmellsAfterStruggleSeconds * 0.75f);
        }

        private static void RestorePostStruggleIgnoreInfluence(BaseAi ai, AnimalInfluenceState state)
        {
            if (!state.HasPostStruggleIgnoreBackup) return;
            ai.m_IgnoreFootStepsAndSmellsAfterStruggleSeconds = state.OriginalIgnoreFootStepsAndSmellsAfterStruggleSeconds;
            state.HasPostStruggleIgnoreBackup = false;
        }


        private static void ApplyAnimalAuroraInfluence(BaseAi ai, AnimalInfluenceState state)
        {
            int desiredAuraMode = GetDesiredAnimalAuraMode();
            float auraBlend = GetDesiredAnimalAuraBlend(desiredAuraMode);
            if (desiredAuraMode == 0 || auraBlend <= 0.001f)
            {
                RestoreAnimalAuroraInfluence(ai, state);
                return;
            }

            if (state.AppliedAnimalAuraMode != 0 && state.AppliedAnimalAuraMode != desiredAuraMode) RestoreAnimalAuroraInfluence(ai, state);
            state.AppliedAnimalAuraMode = desiredAuraMode;

            try
            {
                UpdateAnimalAuraMaterials(desiredAuraMode, auraBlend);
                if (desiredAuraMode == 1) ApplyBloodMoonAuroraMaterialSwap(ai, state, auraBlend);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.ApplyAnimalAuroraInfluence.1", "BloodMoonInfluenceRuntime.ApplyAnimalAuroraInfluence failed.", caughtException);
            }
        }

        private static int GetDesiredAnimalAuraMode()
        {
            if (CurrentState == BloodMoonInfluenceState.ClearBloodMoon || CurrentState == BloodMoonInfluenceState.SnowBloodMoon) return 1;

            return 0;
        }

        private static float GetDesiredAnimalAuraBlend(int desiredAuraMode)
        {
            if (desiredAuraMode == 0) return 0f;
            try
            {
                return Mathf.Clamp01(CustomWeatherStageRuntime.CurrentEffectBlend);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.GetDesiredAnimalAuraBlend.1", "BloodMoonInfluenceRuntime.GetDesiredAnimalAuraBlend failed.", caughtException);
                return 1f;
            }
        }


        private static void ApplyBloodMoonAuroraMaterialSwap(BaseAi ai, AnimalInfluenceState state, float auraBlend)
        {
            if (Time.realtimeSinceStartup < state.NextBloodMoonRendererTintRealtime && Math.Abs(state.LastAnimalAuraBlend - auraBlend) < 0.025f) return;
            state.NextBloodMoonRendererTintRealtime = Time.realtimeSinceStartup + 0.1f;
            state.LastAnimalAuraBlend = auraBlend;

            Material? eyesMaterial = GetBloodMoonEyesAuroraMaterial();
            Material? bodyTemplate = GetBloodMoonWildlifeAuroraTemplate();
            if (eyesMaterial == null && bodyTemplate == null) return;

            Renderer[] renderers = state.GetRenderers(ai);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;

                string rendererName = SafeLowerName(renderer);

                if (IsEyeRendererName(rendererName))
                {
                    ApplyBloodMoonSingleMaterial(renderer, state, eyesMaterial);
                    continue;
                }

                if (IsBodyRendererName(rendererName))
                {
                    ApplyBloodMoonBodyAuroraMaterial(renderer, state, bodyTemplate);
                }
            }
        }

        private static void ApplyBloodMoonSingleMaterial(Renderer renderer, AnimalInfluenceState state, Material material)
        {
            if (renderer == null || material == null) return;

            RendererVisualState visualState = state.GetOrCreateRendererVisualState(renderer);
            visualState.ApplyTemplate(material, preserveMainTexture: false);
        }

        private static void ApplyBloodMoonBodyAuroraMaterial(Renderer renderer, AnimalInfluenceState state, Material bodyTemplate)
        {
            if (renderer == null || bodyTemplate == null) return;

            RendererVisualState visualState = state.GetOrCreateRendererVisualState(renderer);
            visualState.ApplyTemplate(bodyTemplate, preserveMainTexture: true);
        }

        private static bool IsBodyRendererName(string rendererName)
        {
            if (string.IsNullOrEmpty(rendererName)) return false;
            if (IsEyeRendererName(rendererName)) return false;
            if (rendererName.Contains("crps")) return false;
            if (rendererName.Contains("corpse")) return false;
            if (rendererName.Contains("carcass")) return false;
            if (rendererName.Contains("breath")) return false;
            if (rendererName.Contains("shadow")) return false;

            if (rendererName.Contains("mesh_body")) return true;
            if (rendererName.Contains("mesh_bear")) return true;
            if (rendererName.Contains("mesh_moose")) return true;
            if (rendererName.Contains("mesh_stag")) return true;
            if (rendererName.Contains("mesh_deer")) return true;
            if (rendererName.Contains("mesh_rabbit")) return true;
            if (rendererName.Contains("mesh_wolf")) return true;
            if (rendererName.Contains(":mesh_")) return true;
            if (rendererName.Contains("_body")) return true;
            if (rendererName.Contains(" body")) return true;

            return false;
        }

        private static bool IsEyeRendererName(string rendererName)
        {
            if (string.IsNullOrEmpty(rendererName)) return false;
            if (rendererName.Contains("mesh_eyes")) return true;
            if (rendererName.Contains("_eyes")) return true;
            if (rendererName.Contains("eyes")) return true;
            if (rendererName.Contains("_eye")) return true;
            if (rendererName.Contains(" eye")) return true;
            return rendererName == "eye" || rendererName == "eyes";
        }

        private static void UpdateAnimalAuraMaterials(int auraMode, float blend)
        {
            blend = Mathf.Clamp01(blend);
            if (Math.Abs(s_LastAppliedAnimalAuraMaterialBlend - blend) < 0.01f) return;
            s_LastAppliedAnimalAuraMaterialBlend = blend;

            EnsureBloodMoonAuroraMaterials();

            if (auraMode == 1)
            {
                ConfigureAnimalEyeAuroraMaterial(s_BloodMoonEyesAuroraMaterial, ScaleColor(s_BloodMoonAnimalAuroraColor, blend));
                ConfigureBloodMoonWildlifeAuroraMaterial(s_BloodMoonWildlifeAuroraTemplate, blend);
            }
        }

        private static Color ScaleColor(Color color, float blend)
        {
            blend = Mathf.Clamp01(blend);
            return new(color.r * blend, color.g * blend, color.b * blend, color.a);
        }

        private static Material? GetBloodMoonEyesAuroraMaterial()
        {
            EnsureBloodMoonAuroraMaterials();
            return s_BloodMoonEyesAuroraMaterial;
        }

        private static Material? GetBloodMoonWildlifeAuroraTemplate()
        {
            EnsureBloodMoonAuroraMaterials();
            return s_BloodMoonWildlifeAuroraTemplate;
        }

        private static void EnsureBloodMoonAuroraMaterials()
        {
            if (s_BloodMoonEyesAuroraMaterial != null && s_BloodMoonWildlifeAuroraTemplate != null) return;
            if (s_AuroraMaterialLookupAttempted && Time.realtimeSinceStartup < s_NextAuroraMaterialLookupRealtime) return;

            s_AuroraMaterialLookupAttempted = true;
            s_NextAuroraMaterialLookupRealtime = Time.realtimeSinceStartup + 5f;

            if (s_BloodMoonEyesAuroraMaterial == null) s_BloodMoonEyesAuroraMaterial = CreateAnimalEyeAuroraMaterial("WO_BloodMoon_Aurora_Eyes_MAT", s_BloodMoonAnimalAuroraColor);
            if (s_BloodMoonWildlifeAuroraTemplate == null) s_BloodMoonWildlifeAuroraTemplate = CreateBloodMoonWildlifeAuroraTemplate();

            if ((s_BloodMoonEyesAuroraMaterial == null || s_BloodMoonWildlifeAuroraTemplate == null) && WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation && !s_AuroraMaterialLookupWarningLogged)
            {
                s_AuroraMaterialLookupWarningLogged = true;
                Core.Warn("[BloodMoonInfluence] Aurora materials are not available yet; WeatherOverhaul will retry. BloodEyes=" + (s_BloodMoonEyesAuroraMaterial != null) + " | BodyTemplate=" + (s_BloodMoonWildlifeAuroraTemplate != null));
            }
        }

        private static Material? CreateBloodMoonWildlifeAuroraTemplate()
        {
            Material? template = FindLoadedMaterial("Aurora_Wildlife", "Shader Forge/Wildlife_Aurora");
            Material? material = null;

            try
            {
                if (template != null) material = new(template);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.CreateBloodMoonWildlifeAuroraTemplate.1", "BloodMoonInfluenceRuntime.CreateBloodMoonWildlifeAuroraTemplate failed.", caughtException);
                material = null;
            }

            if (material == null)
            {
                try
                {
                    Shader shader = Shader.Find("Shader Forge/Wildlife_Aurora");
                    if (shader != null) material = new(shader);
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("BloodMoonInfluenceRuntime.CreateBloodMoonWildlifeAuroraTemplate.2", "BloodMoonInfluenceRuntime.CreateBloodMoonWildlifeAuroraTemplate failed.", caughtException);
                    material = null;
                }
            }

            if (material == null) return null;

            material.name = "WO_BloodMoon_Aurora_Wildlife_Template_MAT";
            ConfigureBloodMoonWildlifeAuroraMaterial(material, 1f);
            return material;
        }


        private static void ConfigureBloodMoonWildlifeAuroraMaterial(Material material, float blend)
        {
            if (material == null) return;

            blend = Mathf.Clamp01(blend);
            Texture2D? redAuroraColours = GetBloodMoonAuroraColoursTexture(blend);

            if (redAuroraColours != null && material.HasProperty("_Aurora_Colours")) material.SetTexture("_Aurora_Colours", redAuroraColours);

            TrySetFloat(material, "_Intensity", 1.35f * blend);
            TrySetFloat(material, "_intensity_far", 1.15f * blend);
            TrySetFloat(material, "_Intensity2", 0.55f * blend);
            TrySetFloat(material, "_Intensity_3", 0.045f * blend);

            material.EnableKeyword("_EMISSION");
        }

        private static Texture2D? GetBloodMoonAuroraColoursTexture(float blend)
        {
            blend = Mathf.Clamp01(blend);
            if (s_BloodMoonAuroraColoursTexture != null && Math.Abs(s_BloodMoonAuroraColoursTextureBlend - blend) < 0.01f) return s_BloodMoonAuroraColoursTexture;

            try
            {
                Texture2D? texture = s_BloodMoonAuroraColoursTexture;
                if (texture == null)
                {
                    texture = new(16, 1, TextureFormat.RGBA32, false)
                    {
                        name = "WO_BloodMoon_AuroraColours_Red",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    s_BloodMoonAuroraColoursTexture = texture;
                }

                for (int i = 0; i < 16; i++)
                {
                    float t = i / 15f;
                    float r = (0.035f + 0.36f * t) * blend;
                    float g = (0.004f + 0.026f * t) * blend;
                    float b = (0.003f + 0.016f * t) * blend;
                    texture.SetPixel(i, 0, new(r, g, b, 1f));
                }

                texture.Apply(false, false);
                s_BloodMoonAuroraColoursTextureBlend = blend;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.GetBloodMoonAuroraColoursTexture.1", "BloodMoonInfluenceRuntime.GetBloodMoonAuroraColoursTexture failed.", caughtException);
                s_BloodMoonAuroraColoursTexture = null;
                s_BloodMoonAuroraColoursTextureBlend = -1f;
            }

            return s_BloodMoonAuroraColoursTexture;
        }

        private static void TrySetFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName)) material.SetFloat(propertyName, value);
        }

        private static Material? CreateAnimalEyeAuroraMaterial(string cloneName, Color color)
        {
            Material? template = FindLoadedMaterial("Aurora_Eyes_MAT", "Shader Forge/Wildlife_Aurora_Eyes");
            Material? material = null;

            try
            {
                if (template != null) material = new(template);
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.CreateAnimalEyeAuroraMaterial.1", "BloodMoonInfluenceRuntime.CreateAnimalEyeAuroraMaterial failed.", caughtException);
                material = null;
            }

            if (material == null)
            {
                try
                {
                    Shader shader = Shader.Find("Shader Forge/Wildlife_Aurora_Eyes");
                    if (shader != null) material = new(shader);
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("BloodMoonInfluenceRuntime.CreateAnimalEyeAuroraMaterial.2", "BloodMoonInfluenceRuntime.CreateAnimalEyeAuroraMaterial failed.", caughtException);
                    material = null;
                }
            }

            if (material == null) return null;

            material.name = cloneName;
            ConfigureAnimalEyeAuroraMaterial(material, color);
            return material;
        }

        private static Material? FindLoadedMaterial(string materialName, string shaderName)
        {
            try
            {
                Material[]? materials = Resources.FindObjectsOfTypeAll<Material>();
                if (materials == null) return null;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null) continue;

                    string name = SafeLowerName(material);
                    if (!name.Contains(materialName.ToLowerInvariant())) continue;

                    string shader = "?";
                    shader = material.shader != null ? material.shader.name : "?";
                    if (!string.IsNullOrEmpty(shaderName) && shader != shaderName) continue;

                    return material;
                }
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.FindLoadedMaterial.1", "BloodMoonInfluenceRuntime.FindLoadedMaterial failed.", caughtException);
            }

            return null;
        }

        private static void ConfigureAnimalEyeAuroraMaterial(Material material, Color color)
        {
            if (material == null) return;

            material.color = color;

            for (int i = 0; i < s_BloodMoonAnimalColorIds.Length; i++)
            {
                try
                {
                    int id = s_BloodMoonAnimalColorIds[i];
                    if (material.HasProperty(id)) material.SetColor(id, color);
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("BloodMoonInfluenceRuntime.ConfigureAnimalEyeAuroraMaterial.1", "BloodMoonInfluenceRuntime.ConfigureAnimalEyeAuroraMaterial failed.", caughtException);
                }
            }

            material.EnableKeyword("_EMISSION");
        }

        private static void RestoreAnimalAuroraInfluence(BaseAi ai, AnimalInfluenceState state)
        {
            if (state.HasRendererVisualStates) state.RestoreRendererVisuals();
            state.AppliedAnimalAuraMode = 0;
            state.LastAnimalAuraBlend = -1f;
        }

        private static string SafeLowerName(UnityEngine.Object target)
        {
            try
            {
                return target != null && !string.IsNullOrEmpty(target.name) ? target.name.ToLowerInvariant() : string.Empty;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.SafeLowerName.1", "BloodMoonInfluenceRuntime.SafeLowerName failed.", caughtException);
                return string.Empty;
            }
        }

        private static void RestoreBleedState(BaseAi ai, BleedState state)
        {
            try
            {
                ai.m_BleedingOut = state.WasBleedingOut;
                ai.m_DeathAfterBleeingOutMinutes = state.DeathAfterBleedingOutMinutes;
                ai.m_ElapsedBleedingOutMinutes = state.ElapsedBleedingOutMinutes;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.RestoreBleedState.1", "BloodMoonInfluenceRuntime.RestoreBleedState failed.", caughtException);
            }
        }

        private static AnimalInfluenceState EnsureState(BaseAi ai)
        {
            if (!s_Animals.TryGetValue(ai, out AnimalInfluenceState state))
            {
                state = new();
                s_Animals[ai] = state;
            }

            return state;
        }

        private static void CleanupInvalidStates()
        {
            if (s_Animals.Count <= 0) return;

            List<BaseAi>? remove = null;
            foreach (KeyValuePair<BaseAi, AnimalInfluenceState> pair in s_Animals)
            {
                if (pair.Key != null) continue;
                remove ??= [];
                remove.Add(pair.Key);
            }

            if (remove == null) return;
            for (int i = 0; i < remove.Count; i++)
            {
                BaseAi ai = remove[i];
                if (s_Animals.TryGetValue(ai, out AnimalInfluenceState state)) state.DiscardRendererVisuals();
                s_Animals.Remove(ai);
            }
        }

        private static bool IsWolf(BaseAi ai)
        {
            try
            {
                return ai != null && ai.m_AiSubType == AiSubType.Wolf;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.IsWolf.1", "BloodMoonInfluenceRuntime.IsWolf failed.", caughtException);
                return false;
            }
        }

        private static bool IsBloodMoonPredator(BaseAi ai)
        {
            if (ai == null) return false;

            try
            {
                string subtype = ai.m_AiSubType.ToString();
                return subtype.Contains("Wolf") || subtype.Contains("Bear") || subtype.Contains("Cougar");
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.IsBloodMoonPredator.1", "BloodMoonInfluenceRuntime.IsBloodMoonPredator failed.", caughtException);
                return IsWolf(ai);
            }
        }

        private static bool SafeIsBleedingOut(BaseAi ai)
        {
            try
            {
                return ai != null && ai.IsBleedingOut();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.SafeIsBleedingOut.1", "BloodMoonInfluenceRuntime.SafeIsBleedingOut failed.", caughtException);
                return false;
            }
        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try
            {
                return getter();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.SafeIsBleedingOut.2", "BloodMoonInfluenceRuntime.SafeIsBleedingOut failed.", caughtException);
                return fallback;
            }
        }

        private static BloodMoonInfluenceState ResolveState(WeatherSnapshot snapshot)
        {
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul) return BloodMoonInfluenceState.None;
            if (WeatherOverhaulSettingsManager.BloodMoonRequiresFullMoon && !GlobalWeatherSimulation.IsRuntimeFullMoonAuthoritative(snapshot.WorldHour)) return BloodMoonInfluenceState.None;

            if (CustomWeatherStageRuntime.IsActive)
            {
                WeatherStageId forcedStageId = CustomWeatherStageRuntime.DebugRequestedStageId;
                if (forcedStageId == WeatherStageId.ClearBloodMoon) return BloodMoonInfluenceState.ClearBloodMoon;
                if (forcedStageId == WeatherStageId.SnowBloodMoon) return BloodMoonInfluenceState.SnowBloodMoon;
                return BloodMoonInfluenceState.None;
            }

            if (!WeatherDirector.Enabled) return BloodMoonInfluenceState.None;
            if (!snapshot.IsValid || snapshot.RegionId == WeatherRegionId.Unknown) return BloodMoonInfluenceState.None;

            try
            {
                if (!GlobalWeatherSimulation.TryGetActivationPlan(snapshot.RegionId, snapshot.WorldHour, out WeatherActivationPlan plan)) return BloodMoonInfluenceState.None;
                if (plan.Family == WeatherFamily.ClearBloodMoon) return BloodMoonInfluenceState.ClearBloodMoon;
                if (plan.Family == WeatherFamily.LightSnowBloodMoon) return BloodMoonInfluenceState.SnowBloodMoon;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("BloodMoonInfluenceRuntime.ResolveState.1", "BloodMoonInfluenceRuntime.ResolveState failed.", caughtException);
            }

            return BloodMoonInfluenceState.None;
        }

        private static float GetCurrentMoonIllumination01()
        {
            return MoonPhaseRuntime.GetCurrentIllumination01();
        }

        private static void MaybeLogState()
        {
            if (!WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) return;
            if (CurrentState == s_LastLoggedState && Math.Abs(CurrentAnimalBleedSpeedMultiplier - s_LastLoggedBleedMultiplier) < 0.01f) return;

            s_LastLoggedState = CurrentState;
            s_LastLoggedBleedMultiplier = CurrentAnimalBleedSpeedMultiplier;

            if (CurrentState == BloodMoonInfluenceState.None)
            {
                Core.Log("[BloodMoonInfluence] inactive.");
                return;
            }

            Core.Log("[BloodMoonInfluence] active | State=" + CurrentState + " | MoonIllumination=" + CurrentMoonIllumination01.ToString("0.###") + " | AnimalBleedSpeed=x" + CurrentAnimalBleedSpeedMultiplier.ToString("0.###") + " | PostStruggleIgnore=-25% | AnimalAuroraShader=BloodMoon red body/eyes");
        }

        private sealed class AnimalInfluenceState
        {
            internal int AppliedAnimalAuraMode;
            internal bool HasPostStruggleIgnoreBackup;
            internal float OriginalIgnoreFootStepsAndSmellsAfterStruggleSeconds;
            internal float NextBloodMoonRendererTintRealtime;
            internal float LastAnimalAuraBlend = -1f;
            internal BleedState PendingBleedRestore;
            internal bool HasPendingBleedRestore;
            internal float PendingBleedRestoreUntilRealtime;
            private Renderer[]? m_CachedRenderers;
            private readonly List<RendererVisualState> m_RendererVisualStates = [];

            internal bool HasRendererVisualStates => m_RendererVisualStates.Count > 0;

            internal Renderer[] GetRenderers(BaseAi ai)
            {
                if (m_CachedRenderers != null && m_CachedRenderers.Length > 0) return m_CachedRenderers;
                m_CachedRenderers = ai != null ? ai.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
                return m_CachedRenderers;
            }

            internal RendererVisualState GetOrCreateRendererVisualState(Renderer renderer)
            {
                for (int i = 0; i < m_RendererVisualStates.Count; i++)
                {
                    RendererVisualState state = m_RendererVisualStates[i];
                    if (state.Renderer == renderer) return state;
                }

                RendererVisualState created = new(renderer);
                m_RendererVisualStates.Add(created);
                return created;
            }

            internal void RestoreRendererVisuals()
            {
                for (int i = 0; i < m_RendererVisualStates.Count; i++) m_RendererVisualStates[i].Restore();
                m_RendererVisualStates.Clear();
            }

            internal void DiscardRendererVisuals()
            {
                for (int i = 0; i < m_RendererVisualStates.Count; i++) m_RendererVisualStates[i].Discard();
                m_RendererVisualStates.Clear();
            }
        }

        private sealed class RendererVisualState
        {
            internal readonly Renderer Renderer;
            private readonly List<MaterialVisualState> m_Materials = [];

            internal RendererVisualState(Renderer renderer)
            {
                Renderer = renderer;
                if (renderer == null) return;

                try
                {
                    Material[] materials = renderer.materials;
                    if (materials == null) return;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];
                        if (material != null) m_Materials.Add(new(material));
                    }
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("BloodMoonInfluenceRuntime.RendererVisualState.1", "BloodMoonInfluenceRuntime.RendererVisualState failed.", caughtException);
                }
            }

            internal void ApplyTemplate(Material template, bool preserveMainTexture)
            {
                if (template == null) return;

                for (int i = 0; i < m_Materials.Count; i++) m_Materials[i].ApplyTemplate(template, preserveMainTexture);
            }

            internal void Restore()
            {
                for (int i = 0; i < m_Materials.Count; i++) m_Materials[i].Restore();
            }

            internal void Discard()
            {
                for (int i = 0; i < m_Materials.Count; i++) m_Materials[i].Discard();
            }
        }

        private sealed class MaterialVisualState
        {
            private readonly Material m_Material;
            private readonly Material m_Original;
            private readonly Texture? m_OriginalMainTexture;

            internal MaterialVisualState(Material material)
            {
                m_Material = material;
                m_Original = new(material);
                m_OriginalMainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            }

            internal void ApplyTemplate(Material template, bool preserveMainTexture)
            {
                if (m_Material == null || template == null) return;

                try
                {
                    Shader shader = template.shader;
                    if (shader != null) m_Material.shader = shader;
                    m_Material.CopyPropertiesFromMaterial(template);
                    if (preserveMainTexture && m_OriginalMainTexture != null && m_Material.HasProperty("_MainTex")) m_Material.SetTexture("_MainTex", m_OriginalMainTexture);
                }
                catch (Exception caughtException)
                {
                    Core.LogExceptionOnce("BloodMoonInfluenceRuntime.MaterialVisualState.ApplyTemplate.1", "BloodMoonInfluenceRuntime.MaterialVisualState.ApplyTemplate failed.", caughtException);
                }
            }

            internal void Restore()
            {
                if (m_Material != null && m_Original != null)
                {
                    try
                    {
                        Shader shader = m_Original.shader;
                        if (shader != null) m_Material.shader = shader;
                        m_Material.CopyPropertiesFromMaterial(m_Original);
                    }
                    catch (Exception caughtException)
                    {
                        Core.LogExceptionOnce("BloodMoonInfluenceRuntime.MaterialVisualState.Restore.1", "BloodMoonInfluenceRuntime.MaterialVisualState.Restore failed.", caughtException);
                    }
                }

                Discard();
            }

            internal void Discard()
            {
                if (m_Original != null) UnityEngine.Object.Destroy(m_Original);
            }
        }
    }
}