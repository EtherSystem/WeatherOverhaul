using WeatherOverhaul.Weather;

namespace WeatherOverhaul.UI
{
    internal static class GlobalWeatherDebugUi
    {
        private static readonly Rect s_PanelRect = new Rect(25f, 80f, 1080f, 720f);
        private static Vector2 s_Scroll;
        private static GUIStyle s_HeaderStyle = null!;
        private static GUIStyle s_SubHeaderStyle = null!;
        private static GUIStyle s_LabelStyle = null!;
        private static GUIStyle s_MutedStyle = null!;
        private static GUIStyle s_CurrentStyle = null!;
        private static GUIStyle s_CellStyle = null!;
        private static WeatherRegionId s_SelectedRegion = WeatherRegionId.Unknown;
        private const int DebugForecastMaxPlans = 512;
        private const int RegionButtonColumns = 2;
        private const int CustomStageButtonColumns = 3;
        private static readonly WeatherStageId[] s_CustomDayStages =
        {
            WeatherStageId.LowOvercast,
            WeatherStageId.HeavyOvercast,
            WeatherStageId.VeryHeavySnow,
            WeatherStageId.Whiteout,
            WeatherStageId.WindyLightSnow,
            WeatherStageId.Ashfall,
            WeatherStageId.VeryDenseFog,
            WeatherStageId.FreezingFog,
            WeatherStageId.ViolentBlizzard
        };
        private static readonly WeatherStageId[] s_CustomNightStages =
        {
            WeatherStageId.CloudyAurora,
            WeatherStageId.SnowyAurora,
            WeatherStageId.FoggyAurora,
            WeatherStageId.ClearBloodMoon,
            WeatherStageId.SnowBloodMoon
        };
        private static string s_CustomStageDebugMessage = string.Empty;

        private static bool s_Show;
        private static bool s_HasCursorBackup;
        private static bool s_PreviousCursorVisible;
        private static CursorLockMode s_PreviousCursorLockMode;
        private static bool s_HasControlModeBackup;
        private static PlayerControlMode s_PreviousControlMode;

        internal static bool Show
        {
            get => s_Show;
            set
            {
                if (s_Show == value) return;
                s_Show = value;
                if (value)
                {
                    if (!s_HasCursorBackup)
                    {
                        s_PreviousCursorVisible = Cursor.visible;
                        s_PreviousCursorLockMode = Cursor.lockState;
                        s_HasCursorBackup = true;
                    }

                    LockPlayerControl();
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    RestorePlayerControl();

                    if (s_HasCursorBackup)
                    {
                        Cursor.visible = s_PreviousCursorVisible;
                        Cursor.lockState = s_PreviousCursorLockMode;
                        s_HasCursorBackup = false;
                    }
                }
            }
        }

        internal static void Draw()
        {
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul || !WeatherOverhaulSettingsManager.EnableGlobalSimulation || !WeatherOverhaulSettingsManager.Debug || !Show) return;

            try
            {
                LockPlayerControl();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                EnsureStyles();
                GUILayout.BeginArea(s_PanelRect, "WeatherOverhaul 1.0.0 - Global Weather Forecast", GUI.skin.window);
                DrawHeader();
                s_Scroll = GUILayout.BeginScrollView(s_Scroll, false, true);
                DrawBody();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("global-weather-ui-draw", "Global weather UI draw failed.", e);
            }
        }

        private static void LockPlayerControl()
        {
            var playerManager = GameManager.GetPlayerManagerComponent();
            if (playerManager == null) return;

            if (!s_HasControlModeBackup)
            {
                s_PreviousControlMode = playerManager.GetControlMode();
                s_HasControlModeBackup = true;
            }

            if (playerManager.GetControlMode() != PlayerControlMode.Locked) playerManager.SetControlMode(PlayerControlMode.Locked);
        }

        private static void RestorePlayerControl()
        {
            if (!s_HasControlModeBackup) return;

            var playerManager = GameManager.GetPlayerManagerComponent();
            if (playerManager != null && playerManager.GetControlMode() == PlayerControlMode.Locked) playerManager.SetControlMode(s_PreviousControlMode);
            s_HasControlModeBackup = false;
        }

        private static void DrawHeader()
        {
            GUILayout.Space(6f);
            GUILayout.Label("Global Weather Forecast", s_HeaderStyle);
            GUILayout.Label("Numpad 1: toggle this window     Numpad 2: rebuild simulation     Numpad 3: log current region + neighbors", s_MutedStyle);
            GUILayout.Label("Panel_Map overlay is automatic when the map is open and the map overlay setting is enabled.", s_MutedStyle);
            GUILayout.Space(6f);
        }

        private static void DrawBody()
        {
            WeatherSnapshot snapshot = WeatherOverhaulRuntime.CurrentSnapshot;
            GlobalWeatherState state = GlobalWeatherSimulation.Current;

            if (!snapshot.IsValid)
            {
                GUILayout.Label("Waiting for an active game scene and weather components...", s_LabelStyle);
                return;
            }

            GUILayout.Label($"Now: {snapshot.GetClockText()} | Vanilla: {snapshot.StageLabel} | Temp={snapshot.TemperatureCelsius:0.#}C | Wind={snapshot.WindStrength} {snapshot.WindSpeedMph:0.#}mph", s_LabelStyle);
            GUILayout.Label($"Scene: {snapshot.SceneName} -> {snapshot.RegionDisplayName} | {(snapshot.IsIndoorEnvironment ? "Indoor environment" : "Outdoor environment")}", snapshot.RegionId == WeatherRegionId.Unknown ? s_MutedStyle : s_LabelStyle);
            GUILayout.Space(8f);
            DrawCustomStageShowcase(snapshot);
            GUILayout.Space(8f);

            if (!state.IsValid)
            {
                GUILayout.Label("No global weather simulation generated yet.", s_LabelStyle);
                return;
            }

            WeatherRegionDefinition anchor = RegionWeatherGraph.Get(state.AnchorRegion);
            GUILayout.Label($"Source: {anchor.DisplayName} | Night event: {state.NightEventSchedule.BuildSummary(snapshot.WorldHour)} | Glimmer: {state.GlimmerFogSchedule.BuildSummary(snapshot.WorldHour)}", s_LabelStyle);
            GUILayout.Label("Forecast display: planned stage windows, not +hour samples. This is the real timeline WeatherOverhaul is trying to apply.", s_MutedStyle);
            GUILayout.Label("Summary: " + state.Summary, s_MutedStyle);
            GUILayout.Space(8f);
            DrawRegionButtons(snapshot, state);
            GUILayout.Space(8f);

            if (s_SelectedRegion != WeatherRegionId.Unknown)
            {
                DrawSelectedRegionForecast(snapshot, state);
                GUILayout.Space(8f);
                GUILayout.Label("Weather stages are displayed with full names.", s_MutedStyle);
            }

            GUILayout.Label("Markers: > current scene | ~ simulation source | [Electrostatic Fog] electrostatic-capable region", s_MutedStyle);
        }

        private static void DrawCustomStageShowcase(WeatherSnapshot snapshot)
        {
            GUILayout.Label("Custom stage showcase", s_SubHeaderStyle);
            GUILayout.Label("Immediate local override for testing/capture. Forecast authority is paused while a showcase stage is forced.", s_MutedStyle);

            DrawCustomStageButtonGrid(s_CustomDayStages);

            GUILayout.Space(3f);
            GUILayout.Label("Aurora / Blood Moon variants (best viewed at night):", s_MutedStyle);
            DrawCustomStageButtonGrid(s_CustomNightStages);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore forecast", GUILayout.Width(180f), GUILayout.Height(26f)))
            {
                WeatherOverhaulRuntime.RestoreForecastNow(out s_CustomStageDebugMessage);
            }

            WeatherStageId activeStageId = CustomWeatherStageRuntime.DebugRequestedStageId;
            string activeText = activeStageId == WeatherStageId.Undefined
                ? "Showcase override: none"
                : "Showcase override: " + WeatherStageCatalog.Get(activeStageId).DisplayName;
            GUILayout.Label(activeText, activeStageId == WeatherStageId.Undefined ? s_MutedStyle : s_CurrentStyle, GUILayout.Width(420f));
            GUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(s_CustomStageDebugMessage)) GUILayout.Label(s_CustomStageDebugMessage, s_MutedStyle);
            if (snapshot.IsIndoorEnvironment) GUILayout.Label("Move outdoors before forcing a custom weather stage.", s_MutedStyle);
        }

        private static void DrawCustomStageButtonGrid(WeatherStageId[] stageIds)
        {
            for (int i = 0; i < stageIds.Length; i++)
            {
                if (i % CustomStageButtonColumns == 0) GUILayout.BeginHorizontal();

                WeatherStageId stageId = stageIds[i];
                WeatherStageDefinition definition = WeatherStageCatalog.Get(stageId);
                bool active = CustomWeatherStageRuntime.DebugRequestedStageId == stageId;
                string label = active ? $"> {definition.DisplayName}" : definition.DisplayName;

                if (GUILayout.Button(label, GUILayout.Width(300f), GUILayout.Height(26f)))
                {
                    CustomWeatherStageRuntime.TryForceDebugStage(stageId, out s_CustomStageDebugMessage);
                }

                if (i % CustomStageButtonColumns == CustomStageButtonColumns - 1 || i == stageIds.Length - 1) GUILayout.EndHorizontal();
            }
        }

        private static void DrawRegionButtons(WeatherSnapshot snapshot, GlobalWeatherState state)
        {
            GUILayout.Label("Select a region to display its full 336h planned forecast:", s_SubHeaderStyle);

            for (int i = 0; i < RegionWeatherGraph.Regions.Count; i++)
            {
                if (i % RegionButtonColumns == 0) GUILayout.BeginHorizontal();

                WeatherRegionDefinition region = RegionWeatherGraph.Regions[i];
                bool isCurrent = region.Id == snapshot.RegionId;
                bool isAnchor = region.Id == state.AnchorRegion;
                string marker = BuildMarker(isCurrent, isAnchor);
                string electrostatic = region.AllowsElectrostaticFog ? " [Electrostatic Fog]" : string.Empty;
                string selected = region.Id == s_SelectedRegion ? " *" : string.Empty;
                string label = $"{marker}{region.DisplayName}{electrostatic}{selected}";

                if (GUILayout.Button(label, GUILayout.Width(360f), GUILayout.Height(26f))) s_SelectedRegion = region.Id;

                if (i % RegionButtonColumns == RegionButtonColumns - 1 || i == RegionWeatherGraph.Regions.Count - 1) GUILayout.EndHorizontal();
            }
        }

        private static void DrawSelectedRegionForecast(WeatherSnapshot snapshot, GlobalWeatherState state)
        {
            WeatherRegionDefinition region = RegionWeatherGraph.Get(s_SelectedRegion);
            if (region.Id == WeatherRegionId.Unknown)
            {
                s_SelectedRegion = WeatherRegionId.Unknown;
                return;
            }

            bool isCurrent = region.Id == snapshot.RegionId;
            bool isAnchor = region.Id == state.AnchorRegion;
            GUIStyle style = isCurrent ? s_CurrentStyle : s_CellStyle;
            List<WeatherActivationPlan> plans = GlobalWeatherSimulation.BuildActivationPlanSequence(region.Id, snapshot.WorldHour, DebugForecastMaxPlans);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{BuildMarker(isCurrent, isAnchor)} {region.DisplayName}", s_SubHeaderStyle, GUILayout.Width(420f));
            GUILayout.Label($"{plans.Count} planned stage window(s) until {GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, state.HorizonEndWorldHour)}", s_MutedStyle, GUILayout.Width(470f));
            if (GUILayout.Button("Clear", GUILayout.Width(80f), GUILayout.Height(24f))) s_SelectedRegion = WeatherRegionId.Unknown;
            GUILayout.EndHorizontal();

            DrawSelectedRegionHeaderRow();

            for (int planIndex = 0; planIndex < plans.Count; planIndex++)
            {
                DrawPlanRow(plans, planIndex, snapshot, planIndex == 0, style);
            }
        }

        private static void DrawSelectedRegionHeaderRow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Entry", s_SubHeaderStyle, GUILayout.Width(70f));
            GUILayout.Label("Full 336h planned timeline", s_SubHeaderStyle, GUILayout.Width(850f));
            GUILayout.EndHorizontal();
        }

        private static void DrawPlanRow(List<WeatherActivationPlan> plans, int index, WeatherSnapshot snapshot, bool isCurrent, GUIStyle style)
        {
            string text = index < plans.Count ? FormatPlanCell(plans[index], snapshot, isCurrent) : "-";
            GUILayout.BeginHorizontal();
            GUILayout.Label("Entry " + (index + 1).ToString("00"), style, GUILayout.Width(70f));
            GUILayout.Label(text, style, GUILayout.Width(850f));
            GUILayout.EndHorizontal();
        }

        private static string FormatPlanCell(WeatherActivationPlan plan, WeatherSnapshot snapshot, bool isCurrent)
        {
            string start = isCurrent ? "Now" : GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, plan.StartWorldHour);
            string end = GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, plan.EndWorldHour);
            string stage = plan.Definition.DisplayName;
            return isCurrent ? $"{start}->{end} {stage} ({plan.RemainingHours:0.#}h)" : $"{start}->{end} {stage}";
        }

        private static string BuildMarker(bool isCurrent, bool isAnchor)
        {
            string marker = string.Empty;
            if (isCurrent) marker += ">";
            if (isAnchor) marker += "~";
            return marker;
        }

        private static void EnsureStyles()
        {
            if (s_HeaderStyle != null) return;

            s_HeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = false };
            s_SubHeaderStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };
            s_LabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = false };
            s_MutedStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            s_MutedStyle.normal.textColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            s_CurrentStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };
            s_CurrentStyle.normal.textColor = new Color(1f, 0.92f, 0.45f, 1f);
            s_CellStyle = new GUIStyle(GUI.skin.label) { wordWrap = false };
        }
    }
}