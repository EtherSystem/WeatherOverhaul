using System.Globalization;
using WeatherOverhaul.Weather;

namespace WeatherOverhaul.UI
{
    public sealed class WeatherMapRegionClickProxy(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private int m_RegionId = -1;
        private bool m_Pressed;

        internal void Configure(int regionId)
        {
            m_RegionId = regionId;
        }

        public void OnPress(bool isDown)
        {
            m_Pressed = isDown;
            if (!WeatherMapOverlayUi.IsEditModeActive) return;
            WeatherMapOverlayUi.SelectRegionForEditing((WeatherRegionId)m_RegionId);
        }

        public void OnDrag(Vector2 delta)
        {
            if (!m_Pressed) return;
            if (!WeatherMapOverlayUi.IsEditModeActive) return;
            WeatherMapOverlayUi.DragEditedRegion((WeatherRegionId)m_RegionId, delta.x, delta.y);
        }

        public void OnClick()
        {
            if (WeatherMapOverlayUi.IsEditModeActive)
            {
                WeatherMapOverlayUi.SelectRegionForEditing((WeatherRegionId)m_RegionId);
                return;
            }

            WeatherMapOverlayUi.SelectRegion((WeatherRegionId)m_RegionId);
        }
    }

    internal static class WeatherMapOverlayUi
    {
        private const float LabelBoxHeight = 32f;
        private const float WeatherLabelHeight = 22f;
        private const float WeatherIconCombinedYOffset = -25f;
        private const float ForecastPanelWidth = 640f;
        private const float ForecastPanelHeight = 460f;
        private const float ForecastPanelMinWidth = 520f;
        private const float ForecastPanelMinHeight = 300f;
        private const float ForecastPanelScreenMargin = 20f;
        private const float ForecastResizeGripSize = 16f;
        private const float ForecastDragBarHeight = 26f;
        private const float ForecastDragRightExclusion = 42f;
        private const int ForecastPanelMaxPlans = 8192;
        private const int SummaryLabelDepthOffset = 260;
        private const string PositionFileName = "WeatherOverhaul_MapLabelPositions.txt";
        private const string ClickBoxPositionFileName = "WeatherOverhaul_MapClickBoxPositions.txt";
        private const string ClickBoxSizeFileName = "WeatherOverhaul_MapClickBoxSizes.txt";
        private const float LabelWeatherYOffset = -28f;
        private const float ClickBoxYOffset = 0f;
        private const float ClickBoxVisualYOffset = 0f;
        private const int ClickBoxVisualDepthOffset = 320;
        private const float NudgeStep = 2f;
        private const float FastNudgeStep = 10f;
        private const float EditorSafeHalfWidth = 670f;
        private const float EditorSafeHalfHeight = 375f;
        private const float MinClickBoxWidth = 20f;
        private const float MaxClickBoxWidth = 700f;
        private const float MinClickBoxHeight = 12f;
        private const float MaxClickBoxHeight = 300f;

        private static readonly MapRegionAnchor[] s_GreatBearAnchors =
        [
            new(WeatherRegionId.HRV, -298f, 258f, -300f, 244f, 158f, 41.2f),
            new(WeatherRegionId.BRM, 131f, 250f, 131f, 236f, 95f, 43.2f),
            new(WeatherRegionId.AC, 287f, 260f, 287f, 246f, 98f, 43.2f),
            new(WeatherRegionId.TWM, 236f, 174f, 234f, 160f, 162f, 41.2f),
            new(WeatherRegionId.MT, -335f, 130f, -337f, 116f, 119f, 43.2f),
            new(WeatherRegionId.PV, 96f, 47f, 98f, 33f, 128f, 41.2f),
            new(WeatherRegionId.ML, -173f, 16f, -171f, 2f, 110f, 41.2f),
            new(WeatherRegionId.Rav, -82f, -65f, -82f, -79f, 65f, 41.2f),
            new(WeatherRegionId.CH, 97f, -147f, 99f, -161f, 133f, 39.2f),
            new(WeatherRegionId.CRH, 160f, -231f, 160f, -245f, 149f, 41.2f),
            new(WeatherRegionId.DP, 361f, -202f, 361f, -216f, 132f, 39.2f),
            new(WeatherRegionId.BR, -489f, -26f, -489f, -42f, 129f, 39.2f),
            new(WeatherRegionId.FM, -315f, -97f, -315f, -111f, 125f, 39.2f),
            new(WeatherRegionId.BI, -114f, -158f, -114f, -172f, 92f, 39.2f)
        ];

        private static readonly MapRegionAnchor[] s_FarTerritoryAnchors =
        [
            new(WeatherRegionId.SP, -27f, 108f, -27f, 92f, 93f, 39.2f),
            new(WeatherRegionId.FA, -162f, -28f, -160f, -46f, 111f, 41.2f),
            new(WeatherRegionId.ZOC, 156f, 56f, 158f, 40f, 132f, 39.2f),
            new(WeatherRegionId.TP, 25f, -84f, 27f, -100f, 91f, 35.2f),
            new(WeatherRegionId.FRBL, 70f, -122f, 72f, -138f, 130f, 35.2f)
        ];

        private static readonly MapRegionAnchor[] s_AllAnchors = BuildAllAnchors();

        private static readonly Dictionary<WeatherRegionId, WeatherMapRegionWidget> s_Widgets = [];
        private static readonly Dictionary<WeatherRegionId, Vector2> s_CustomPositions = [];
        private static readonly Dictionary<WeatherRegionId, Vector2> s_CustomClickBoxPositions = [];
        private static readonly Dictionary<WeatherRegionId, Vector2> s_CustomClickBoxSizes = [];

        private static Panel_Map? s_CurrentPanel;
        private static GameObject? s_RootObject;
        private static UILabel? s_TemplateLabel;
        private static bool s_MapPanelOpen;
        private static WeatherRegionId s_SelectedRegion = WeatherRegionId.Unknown;
        private static Vector2 s_PanelScroll;
        private static GUIStyle s_HeaderStyle = null!;
        private static GUIStyle s_MutedStyle = null!;
        private static GUIStyle s_TableHeaderStyle = null!;
        private static GUIStyle s_TableCellStyle = null!;
        private static GUIStyle s_CloseButtonStyle = null!;
        private static GUIStyle s_WindowStyle = null!;
        private static GUIStyle s_ResizeGripStyle = null!;
        private static GUIStyle s_DayCardStyle = null!;
        private static GUIStyle s_DayHeaderStyle = null!;
        private static GUIStyle s_CardMetaStyle = null!;
        private static GUIStyle s_CardColumnHeaderStyle = null!;
        private static GUIStyle s_ForecastRowStyle = null!;
        private static GUIStyle s_ForecastRowAlternateStyle = null!;
        private static GUIStyle s_CurrentForecastRowStyle = null!;
        private static GUIStyle s_FooterStyle = null!;
        private static Texture2D? s_PanelBackground;
        private static Texture2D? s_TitleBarBackground;
        private static Texture2D? s_PanelBorderBackground;
        private static Texture2D? s_DayCardBackground;
        private static Texture2D? s_ForecastRowBackground;
        private static Texture2D? s_ForecastRowAlternateBackground;
        private static Texture2D? s_CurrentForecastRowBackground;
        private static bool s_StylesReady;
        private static int s_LastForecastFontSize = -1;
        private static string s_LastForecastColorSignature = string.Empty;
        private static Color s_ForecastPanelTintColor = Color.white;
        private static Color s_ForecastControlTintColor = Color.white;
        private static Rect s_ForecastPanelRect;
        private static bool s_ForecastPanelRectInitialized;
        private static ForecastResizeHandle s_ActiveForecastResizeHandle = ForecastResizeHandle.None;
        private static Vector2 s_ForecastResizeMouseStart;
        private static Rect s_ForecastResizeRectStart;
        private static bool s_ForecastPanelDragging;
        private static Vector2 s_ForecastDragMouseOffset;
        private static bool s_ForecastPanelLayoutDirty;
        private static bool s_LastWorldMapVisible;
        private static bool s_EditMode;
        private static bool s_CustomPositionsLoaded;
        private static bool s_CustomClickBoxPositionsLoaded;
        private static bool s_CustomClickBoxSizesLoaded;
        private static WeatherRegionId s_EditedRegion = WeatherRegionId.Unknown;
        private static MapOverlayEditTarget s_EditTarget = MapOverlayEditTarget.WeatherLabel;
        private static WorldMapOverlayGroup s_ActiveOverlayGroup = WorldMapOverlayGroup.GreatBear;

        internal static bool IsEditModeActive => s_EditMode;

        internal static void NotifyPanelMapEnable(Panel_Map panel, bool enable)
        {
            bool stateChanged = enable ? (!s_MapPanelOpen || s_CurrentPanel != panel) : s_MapPanelOpen;
            s_CurrentPanel = enable ? panel : null;
            s_MapPanelOpen = enable;
            if (!enable)
            {
                if (s_ForecastPanelLayoutDirty && s_ForecastPanelRectInitialized) SaveForecastPanelLayout();
                s_SelectedRegion = WeatherRegionId.Unknown;
                s_ActiveForecastResizeHandle = ForecastResizeHandle.None;
                s_ForecastPanelDragging = false;
                s_EditedRegion = WeatherRegionId.Unknown;
                s_EditMode = false;
                DestroyOverlayRoot();
            }

            if (!stateChanged || !WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) return;
            Core.Log(enable ? "[PanelMap] Map panel opened. World map weather label overlay is waiting for the world map view." : "[PanelMap] Map panel closed.");
        }

        internal static void NotifyPanelMapChanged(Panel_Map panel, string reason)
        {
            s_CurrentPanel = panel;
            bool visible = ShouldShowWorldMapOverlay();
            SetRootActive(visible);
            if (!WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) return;
            if (visible == s_LastWorldMapVisible) return;
            s_LastWorldMapVisible = visible;
            Core.Log($"[PanelMap] World map visibility changed by {reason}: {(visible ? "visible" : "hidden")}");
        }

        internal static void SelectRegion(WeatherRegionId regionId)
        {
            if (regionId == WeatherRegionId.Unknown) return;
            s_SelectedRegion = regionId;
            WeatherRegionDefinition region = RegionWeatherGraph.Get(regionId);
            if (WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation) Core.Log($"[PanelMap] Selected weather forecast region: {region.DisplayName}");
        }

        internal static void SelectRegionForEditing(WeatherRegionId regionId)
        {
            if (regionId == WeatherRegionId.Unknown) return;
            s_EditedRegion = regionId;
            s_SelectedRegion = WeatherRegionId.Unknown;
            WeatherRegionDefinition region = RegionWeatherGraph.Get(regionId);
            Core.Log($"[PanelMap][Edit] Selected {region.DisplayName} {GetEditTargetName()} | {GetEditedTargetSummary(regionId)}.");
        }

        internal static void DragEditedRegion(WeatherRegionId regionId, float deltaX, float deltaY)
        {
            if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize)
            {
                ResizeEditedRegion(regionId, deltaX, deltaY);
                return;
            }

            MoveEditedRegion(regionId, deltaX, deltaY);
        }

        internal static void MoveEditedRegion(WeatherRegionId regionId, float deltaX, float deltaY)
        {
            if (regionId == WeatherRegionId.Unknown) return;
            s_EditedRegion = regionId;
            Vector2 position = GetEditedPosition(regionId);
            position.x += deltaX;
            position.y += deltaY;
            SetEditedPosition(regionId, position);
            ApplyWidgetPosition(regionId);
        }

        internal static bool HandleRuntimeInput()
        {
            if (Input.GetKeyDown(KeyCode.Keypad7))
            {
                ToggleEditMode();
                return true;
            }

            if (!s_EditMode) return false;

            if (Input.GetKeyDown(KeyCode.KeypadDivide))
            {
                ToggleEditTarget();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                SaveCustomPositions();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                ResetEditedRegion();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                ResetAllCustomPositions();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                SelectNextEditableRegion(1);
                return true;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                SelectNextEditableRegion(-1);
                return true;
            }

            if (Input.GetKeyDown(KeyCode.KeypadPeriod))
            {
                CenterEditedRegion();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                ClampAllLabelsIntoEditorBounds();
                return true;
            }

            float step = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? FastNudgeStep : NudgeStep;
            if (Input.GetKeyDown(KeyCode.Keypad8)) { NudgeEditedRegion(0f, step); return true; }
            if (Input.GetKeyDown(KeyCode.Keypad2)) { NudgeEditedRegion(0f, -step); return true; }
            if (Input.GetKeyDown(KeyCode.Keypad4)) { NudgeEditedRegion(-step, 0f); return true; }
            if (Input.GetKeyDown(KeyCode.Keypad6)) { NudgeEditedRegion(step, 0f); return true; }

            return true;
        }

        internal static void Draw()
        {
            bool visible = ShouldShowWorldMapOverlay();
            SetRootActive(visible);
            if (!visible) return;

            try
            {
                UpdateActiveOverlayGroup();
                EnsureNguiOverlay();
                RefreshNguiOverlay();
                EnsureStyles();
                DrawForecastPanelIfNeeded();
                DrawEditPanelIfNeeded();
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("panel-map-overlay-update", "[PanelMap] World map weather overlay update failed.", e);
            }
        }

        private static bool ShouldShowWorldMapOverlay()
        {
            if (!WeatherOverhaulSettingsManager.EnableWeatherOverhaul) return false;
            if (!WeatherOverhaulSettingsManager.EnableGlobalSimulation) return false;
            if (!WeatherOverhaulSettingsManager.ShowMapWeatherOverlay) return false;
            if (!s_MapPanelOpen || s_CurrentPanel == null) return false;

            if (WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion)
            {
                if (!GlobalWeatherSimulation.Current.IsValid) return false;
            }
            else if (!WeatherOverhaulRuntime.CurrentSnapshot.IsValid)
            {
                return false;
            }

            return IsWorldMapVisible();
        }

        private static bool IsWorldMapVisible()
        {
            if (!s_MapPanelOpen || s_CurrentPanel == null) return false;

            try
            {
                return s_CurrentPanel.IsWorldMapActive();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("WeatherMapOverlayUi.IsWorldMapVisible.1", "WeatherMapOverlayUi.IsWorldMapVisible failed.", caughtException);
                return false;
            }
        }

        private static void UpdateActiveOverlayGroup()
        {
            WorldMapOverlayGroup group = DetectActiveOverlayGroup();
            if (group == s_ActiveOverlayGroup && s_RootObject != null) return;

            if (group != s_ActiveOverlayGroup && WeatherOverhaulSettingsManager.LogGlobalWeatherSimulation)
            {
                Core.Log($"[PanelMap] Active world map weather overlay group changed: {s_ActiveOverlayGroup} -> {group}");
            }

            s_ActiveOverlayGroup = group;
            if (s_SelectedRegion != WeatherRegionId.Unknown && !IsRegionInActiveAnchorSet(s_SelectedRegion)) s_SelectedRegion = WeatherRegionId.Unknown;
            if (s_EditedRegion != WeatherRegionId.Unknown && !IsRegionInActiveAnchorSet(s_EditedRegion)) s_EditedRegion = GetDefaultEditableRegion();
            DestroyOverlayRoot();
        }

        private static WorldMapOverlayGroup DetectActiveOverlayGroup()
        {
            string signature = BuildActiveWorldMapSignature();
            if (LooksLikeFarTerritoryMap(signature)) return WorldMapOverlayGroup.FarTerritory;
            if (LooksLikeGreatBearMap(signature)) return WorldMapOverlayGroup.GreatBear;

            WeatherSnapshot snapshot = WeatherOverhaulRuntime.CurrentSnapshot;
            if (snapshot.IsValid && IsFarTerritoryOverlayRegion(snapshot.RegionId)) return WorldMapOverlayGroup.FarTerritory;
            return WorldMapOverlayGroup.GreatBear;
        }

        private static string BuildActiveWorldMapSignature()
        {
            if (s_CurrentPanel == null || s_CurrentPanel.m_WorldMapParent == null) return string.Empty;

            try
            {
                System.Text.StringBuilder builder = new(256);
                AppendActiveTransformSignature(s_CurrentPanel.m_WorldMapParent, builder, 0);
                return builder.ToString();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("WeatherMapOverlayUi.BuildActiveWorldMapSignature.1", "WeatherMapOverlayUi.BuildActiveWorldMapSignature failed.", caughtException);
                return string.Empty;
            }
        }

        private static void AppendActiveTransformSignature(Transform transform, System.Text.StringBuilder builder, int depth)
        {
            if (transform == null || builder == null || depth > 5) return;
            GameObject gameObject = transform.gameObject;
            string name = gameObject != null ? gameObject.name : string.Empty;
            if (name.StartsWith("WO_", StringComparison.Ordinal)) return;
            if (gameObject != null && gameObject.activeInHierarchy)
            {
                builder.Append(name);
                builder.Append('|');
            }

            for (int i = 0; i < transform.childCount; i++) AppendActiveTransformSignature(transform.GetChild(i), builder, depth + 1);
        }

        private static bool LooksLikeFarTerritoryMap(string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;
            return ContainsIgnoreCase(signature, "Far") || ContainsIgnoreCase(signature, "Territory") || ContainsIgnoreCase(signature, "Airfield") || ContainsIgnoreCase(signature, "Sundered") || ContainsIgnoreCase(signature, "MountainPass") || ContainsIgnoreCase(signature, "MiningRegion") || ContainsIgnoreCase(signature, "ZoneOfContamination") || ContainsIgnoreCase(signature, "HubRegion") || ContainsIgnoreCase(signature, "LongRail");
        }

        private static bool LooksLikeGreatBearMap(string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;
            return ContainsIgnoreCase(signature, "GreatBear") || ContainsIgnoreCase(signature, "Mainland") || ContainsIgnoreCase(signature, "LowerGreatBear");
        }

        private static bool ContainsIgnoreCase(string value, string pattern)
        {
            return value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private static MapRegionAnchor[] GetActiveAnchors()
        {
            return s_ActiveOverlayGroup == WorldMapOverlayGroup.FarTerritory ? s_FarTerritoryAnchors : s_GreatBearAnchors;
        }

        private static bool IsRegionInActiveAnchorSet(WeatherRegionId regionId)
        {
            MapRegionAnchor[] anchors = GetActiveAnchors();
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i].RegionId == regionId) return true;
            }

            return false;
        }

        private static bool IsFarTerritoryOverlayRegion(WeatherRegionId regionId)
        {
            return regionId == WeatherRegionId.SP || regionId == WeatherRegionId.ZOC || regionId == WeatherRegionId.FA || regionId == WeatherRegionId.TP || regionId == WeatherRegionId.FRBL;
        }

        private static WeatherRegionId GetDefaultEditableRegion()
        {
            MapRegionAnchor[] anchors = GetActiveAnchors();
            return anchors.Length > 0 ? anchors[0].RegionId : WeatherRegionId.Unknown;
        }

        private static MapRegionAnchor[] BuildAllAnchors()
        {
            return [.. s_GreatBearAnchors, .. s_FarTerritoryAnchors];
        }

        private static void EnsureNguiOverlay()
        {
            if (s_CurrentPanel == null || s_CurrentPanel.m_WorldMapParent == null) return;
            if (s_RootObject != null) return;

            EnsureCustomPositionsLoaded();
            EnsureCustomClickBoxPositionsLoaded();
            EnsureCustomClickBoxSizesLoaded();

            s_TemplateLabel = GetTemplateLabel();
            if (s_TemplateLabel == null)
            {
                Core.Warn("[PanelMap] Cannot create weather map labels: no UILabel template found on Panel_Map.");
                return;
            }

            s_RootObject = new("WO_WorldMapWeatherOverlay")
            {
                layer = s_CurrentPanel.m_WorldMapParent.gameObject.layer
            };
            s_RootObject.transform.SetParent(s_CurrentPanel.m_WorldMapParent, false);
            s_RootObject.transform.localPosition = Vector3.zero;
            s_RootObject.transform.localRotation = Quaternion.identity;
            s_RootObject.transform.localScale = Vector3.one;

            MapRegionAnchor[] anchors = GetActiveAnchors();
            for (int i = 0; i < anchors.Length; i++)
            {
                CreateRegionWidget(anchors[i]);
            }
        }

        private static UILabel? GetTemplateLabel()
        {
            if (s_CurrentPanel == null) return null;
            if (s_CurrentPanel.m_LastUpdatedLabel != null) return s_CurrentPanel.m_LastUpdatedLabel;
            if (s_CurrentPanel.m_HeaderLabel != null) return s_CurrentPanel.m_HeaderLabel;
            if (s_CurrentPanel.m_ObjectiveLabel != null) return s_CurrentPanel.m_ObjectiveLabel;
            return null;
        }
        private static void CreateRegionWidget(MapRegionAnchor anchor)
        {
            if (s_RootObject == null || s_TemplateLabel == null) return;

            WeatherRegionDefinition region = RegionWeatherGraph.Get(anchor.RegionId);
            if (region.Id == WeatherRegionId.Unknown) return;

            GameObject labelObject = UnityEngine.Object.Instantiate(s_TemplateLabel.gameObject);
            labelObject.name = "WO_WeatherSummary_" + region.ShortName;
            labelObject.layer = s_RootObject.layer;
            labelObject.SetActive(true);
            labelObject.transform.SetParent(s_RootObject.transform, false);
            Vector2 position = GetAnchorPosition(anchor.RegionId);
            labelObject.transform.localPosition = new(position.x, position.y + LabelWeatherYOffset, -2f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one;

            UILabel? label = labelObject.GetComponent<UILabel>();
            ConfigureSummaryLabel(label);

            GameObject iconObject = new("WO_WeatherIcon_" + region.ShortName)
            {
                layer = s_RootObject.layer
            };
            iconObject.SetActive(false);
            iconObject.transform.SetParent(s_RootObject.transform, false);
            iconObject.transform.localPosition = new(position.x, position.y + LabelWeatherYOffset, -2f);
            iconObject.transform.localRotation = Quaternion.identity;
            iconObject.transform.localScale = Vector3.one;

            UITexture icon = iconObject.AddComponent<UITexture>();
            ConfigureWeatherIcon(icon);

            GameObject clickObject = new("WO_WeatherClick_" + region.ShortName)
            {
                layer = s_RootObject.layer
            };
            clickObject.SetActive(true);
            clickObject.transform.SetParent(s_RootObject.transform, false);
            clickObject.transform.localPosition = new(position.x, position.y + ClickBoxYOffset, -4f);
            clickObject.transform.localRotation = Quaternion.identity;
            clickObject.transform.localScale = Vector3.one;

            BoxCollider collider = clickObject.AddComponent<BoxCollider>();
            collider.enabled = WeatherOverhaulSettingsManager.ShowMapWeatherClickBoxes;
            collider.isTrigger = true;
            collider.center = Vector3.zero;
            ConfigureClickBoxCollider(collider, anchor.RegionId, anchor);

            WeatherMapRegionClickProxy proxy = clickObject.AddComponent<WeatherMapRegionClickProxy>();
            proxy.Configure((int)anchor.RegionId);

            GameObject boxVisualObject = new("WO_WeatherClickBoxVisual_" + region.ShortName)
            {
                layer = s_RootObject.layer
            };
            boxVisualObject.SetActive(false);
            boxVisualObject.transform.SetParent(s_RootObject.transform, false);
            boxVisualObject.transform.localPosition = new(position.x, position.y + ClickBoxVisualYOffset, -3f);
            boxVisualObject.transform.localRotation = Quaternion.identity;
            boxVisualObject.transform.localScale = Vector3.one;

            Vector2 clickBoxSize = GetClickBoxSize(anchor.RegionId, anchor);
            UIWidget[] boxVisualLines = CreateClickBoxVisualLines(boxVisualObject.transform, clickBoxSize.x, clickBoxSize.y);

            s_Widgets[anchor.RegionId] = new(anchor.RegionId, labelObject, iconObject, clickObject, boxVisualObject, label, icon, boxVisualLines, collider);
        }

        private static void ConfigureSummaryLabel(UILabel? label)
        {
            if (label == null) return;

            label.enabled = true;
            label.text = string.Empty;
            label.alpha = 1f;
            label.width = 170;
            label.height = (int)WeatherLabelHeight;
            label.depth = GetMapOverlayDepth();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.color = new(0.88f, 0.86f, 0.78f, 1f);
            label.fontSize = 15;
            label.effectStyle = UILabel.Effect.Outline;
            label.effectColor = new(0.02f, 0.02f, 0.018f, 0.95f);
            label.effectDistance = new(1f, 1f);
        }


        private static void ConfigureWeatherIcon(UITexture? icon)
        {
            if (icon == null) return;

            icon.enabled = true;
            icon.alpha = 1f;
            icon.width = WeatherOverhaulSettingsManager.MapWeatherIconSize;
            icon.height = WeatherOverhaulSettingsManager.MapWeatherIconSize;
            icon.depth = GetMapOverlayDepth();
            icon.pivot = UIWidget.Pivot.Center;
            icon.color = Color.white;
            icon.shader = Shader.Find("Unlit/Transparent Colored");
        }

        private static Vector2 GetDefaultClickBoxSize(MapRegionAnchor? anchor)
        {
            float width = anchor != null ? anchor.ClickWidth : 160f;
            float height = anchor != null ? anchor.ClickHeight : LabelBoxHeight;
            return ClampClickBoxSize(new(width, height));
        }

        private static Vector2 GetClickBoxSize(WeatherRegionId regionId, MapRegionAnchor? anchor)
        {
            EnsureCustomClickBoxSizesLoaded();
            if (s_CustomClickBoxSizes.TryGetValue(regionId, out Vector2 customSize)) return ClampClickBoxSize(customSize);
            return GetDefaultClickBoxSize(anchor);
        }

        private static Vector2 ClampClickBoxSize(Vector2 size)
        {
            return new(Mathf.Clamp(size.x, MinClickBoxWidth, MaxClickBoxWidth), Mathf.Clamp(size.y, MinClickBoxHeight, MaxClickBoxHeight));
        }

        private static void ConfigureClickBoxCollider(BoxCollider collider, WeatherRegionId regionId, MapRegionAnchor? anchor)
        {
            if (collider == null) return;
            Vector2 size = GetClickBoxSize(regionId, anchor);
            collider.size = new(size.x, size.y, 1f);
        }

        private static UIWidget[] CreateClickBoxVisualLines(Transform parent, float clickWidth, float clickHeight)
        {
            if (parent == null) return [];

            float halfWidth = clickWidth * 0.5f;
            float halfHeight = clickHeight * 0.5f;
            const float lineThickness = 3f;
            Color color = new(0.18f, 0.95f, 0.30f, 0.90f);

            UIWidget top = CreateClickBoxVisualLine(parent, "Top", 0f, halfHeight, clickWidth, lineThickness, color);
            UIWidget bottom = CreateClickBoxVisualLine(parent, "Bottom", 0f, -halfHeight, clickWidth, lineThickness, color);
            UIWidget left = CreateClickBoxVisualLine(parent, "Left", -halfWidth, 0f, lineThickness, clickHeight, color);
            UIWidget right = CreateClickBoxVisualLine(parent, "Right", halfWidth, 0f, lineThickness, clickHeight, color);

            return [top, bottom, left, right];
        }

        private static UIWidget CreateClickBoxVisualLine(Transform parent, string name, float localX, float localY, float width, float height, Color color)
        {
            GameObject lineObject = new("WO_ClickBoxLine_" + name);
            lineObject.layer = s_RootObject != null ? s_RootObject.layer : lineObject.layer;
            lineObject.SetActive(true);
            lineObject.transform.SetParent(parent, false);
            lineObject.transform.localPosition = new(localX, localY, -0.01f);
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale = Vector3.one;

            UITexture texture = lineObject.AddComponent<UITexture>();
            texture.mainTexture = Texture2D.whiteTexture;
            texture.shader = Shader.Find("Unlit/Transparent Colored");
            ConfigureClickBoxVisualWidget(texture, width, height, color);
            return texture;
        }

        private static void ConfigureClickBoxVisualWidget(UIWidget widget, float width, float height, Color color)
        {
            if (widget == null) return;
            widget.enabled = true;
            widget.alpha = color.a;
            widget.color = color;
            widget.width = Mathf.Max(1, Mathf.RoundToInt(width));
            widget.height = Mathf.Max(1, Mathf.RoundToInt(height));
            widget.depth = GetClickBoxVisualDepth();
        }

        private static void SetClickBoxVisualColor(WeatherMapRegionWidget widget, Color color)
        {
            if (widget == null || widget.BoxVisualLines == null) return;
            for (int i = 0; i < widget.BoxVisualLines.Length; i++)
            {
                UIWidget line = widget.BoxVisualLines[i];
                if (line == null) continue;
                line.color = color;
                line.alpha = color.a;
            }
        }

        private static void ConfigureClickBoxVisualLines(WeatherMapRegionWidget widget, MapRegionAnchor? anchor)
        {
            if (widget == null || widget.BoxVisualLines == null || widget.BoxVisualLines.Length < 4) return;

            Vector2 size = GetClickBoxSize(widget.RegionId, anchor);
            float clickWidth = size.x;
            float clickHeight = size.y;
            float halfWidth = clickWidth * 0.5f;
            float halfHeight = clickHeight * 0.5f;
            const float lineThickness = 3f;

            ConfigureClickBoxVisualLine(widget.BoxVisualLines[0], 0f, halfHeight, clickWidth, lineThickness);
            ConfigureClickBoxVisualLine(widget.BoxVisualLines[1], 0f, -halfHeight, clickWidth, lineThickness);
            ConfigureClickBoxVisualLine(widget.BoxVisualLines[2], -halfWidth, 0f, lineThickness, clickHeight);
            ConfigureClickBoxVisualLine(widget.BoxVisualLines[3], halfWidth, 0f, lineThickness, clickHeight);
        }

        private static void ConfigureClickBoxVisualLine(UIWidget line, float localX, float localY, float width, float height)
        {
            if (line == null) return;
            line.transform.localPosition = new(localX, localY, -0.01f);
            line.width = Mathf.Max(1, Mathf.RoundToInt(width));
            line.height = Mathf.Max(1, Mathf.RoundToInt(height));
            line.depth = GetClickBoxVisualDepth();
        }

        private static int GetClickBoxVisualDepth()
        {
            if (s_CurrentPanel != null && s_CurrentPanel.m_HeaderLabel != null) return s_CurrentPanel.m_HeaderLabel.depth + ClickBoxVisualDepthOffset;
            if (s_CurrentPanel != null && s_CurrentPanel.m_LastUpdatedLabel != null) return s_CurrentPanel.m_LastUpdatedLabel.depth + ClickBoxVisualDepthOffset;
            return 495;
        }

        private static int GetMapOverlayDepth()
        {
            if (s_CurrentPanel != null && s_CurrentPanel.m_HeaderLabel != null) return s_CurrentPanel.m_HeaderLabel.depth + SummaryLabelDepthOffset;
            if (s_CurrentPanel != null && s_CurrentPanel.m_LastUpdatedLabel != null) return s_CurrentPanel.m_LastUpdatedLabel.depth + SummaryLabelDepthOffset;
            return 500;
        }

        private static void RefreshNguiOverlay()
        {
            if (s_RootObject == null) return;

            WeatherSnapshot snapshot = WeatherOverhaulRuntime.CurrentSnapshot;
            float now = GetDisplayWorldHour(snapshot);
            bool useVanillaWeather = !WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion;
            WeatherActivationPlan vanillaPlan = default;
            bool hasVanillaPlan = useVanillaWeather && VanillaWeatherForecast.TryGetCurrentPlan(snapshot, out vanillaPlan);
            WorldMapWeatherStageDisplayMode displayMode = WeatherOverhaulSettingsManager.MapWeatherStageDisplay;
            bool showName = displayMode == WorldMapWeatherStageDisplayMode.Name || displayMode == WorldMapWeatherStageDisplayMode.NameAndIcons;
            bool showIcon = displayMode == WorldMapWeatherStageDisplayMode.Icons || displayMode == WorldMapWeatherStageDisplayMode.NameAndIcons;

            foreach (KeyValuePair<WeatherRegionId, WeatherMapRegionWidget> pair in s_Widgets)
            {
                WeatherMapRegionWidget widget = pair.Value;
                bool isLoadedRegion = snapshot.IsValid && widget.RegionId == snapshot.RegionId;
                bool hasPlan;
                WeatherActivationPlan plan;
                bool canSeeCurrentStage;

                if (useVanillaWeather)
                {
                    hasPlan = isLoadedRegion && hasVanillaPlan;
                    plan = hasPlan ? vanillaPlan : default;
                    canSeeCurrentStage = hasPlan;
                }
                else
                {
                    hasPlan = GlobalWeatherSimulation.TryGetActivationPlan(widget.RegionId, now, out plan);
                    canSeeCurrentStage = ForecastAccessManager.CanSeeCurrentStage(widget.RegionId, snapshot);
                }

                ApplyWidgetPosition(widget.RegionId);

                string text = showName && (!useVanillaWeather || hasPlan) ? BuildCompactConditionText(canSeeCurrentStage, hasPlan, plan) : string.Empty;
                WeatherStageId iconStage = canSeeCurrentStage && hasPlan ? plan.Definition.Id : WeatherStageId.Undefined;
                bool showSummary = showName && !string.IsNullOrEmpty(text);
                bool selectedRegion = s_EditMode && s_EditedRegion == widget.RegionId;
                bool selectedWeatherLabel = selectedRegion && s_EditTarget == MapOverlayEditTarget.WeatherLabel;
                bool selectedClickBox = selectedRegion && (s_EditTarget == MapOverlayEditTarget.ClickBox || s_EditTarget == MapOverlayEditTarget.ClickBoxSize);

                if (s_EditMode)
                {
                    WeatherRegionDefinition region = RegionWeatherGraph.Get(widget.RegionId);
                    string editLine;
                    if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize)
                    {
                        MapRegionAnchor? anchor = GetAnchor(widget.RegionId);
                        Vector2 size = GetClickBoxSize(widget.RegionId, anchor);
                        editLine = $"{region.ShortName} SIZE {size.x:0},{size.y:0}";
                    }
                    else
                    {
                        Vector2 position = GetEditedPosition(widget.RegionId);
                        string target = s_EditTarget == MapOverlayEditTarget.ClickBox ? "BOX" : "TXT";
                        editLine = $"{region.ShortName} {target} {position.x:0},{position.y:0}";
                    }

                    text = string.IsNullOrEmpty(text) ? editLine : $"{text}\n{editLine}";
                    showSummary = true;
                }

                widget.LabelObject?.SetActive(showSummary);
                if (widget.Label != null)
                {
                    widget.Label.text = text;
                    widget.Label.height = s_EditMode ? 44 : (int)WeatherLabelHeight;
                    widget.Label.color = selectedWeatherLabel ? new(1f, 0.92f, 0.45f, 1f) : s_EditMode ? new(0.70f, 0.95f, 0.70f, 1f) : new(0.88f, 0.86f, 0.78f, 1f);
                }

                bool iconVisible = showIcon && !s_EditMode && (!useVanillaWeather || hasPlan);
                widget.IconObject?.SetActive(iconVisible);
                if (iconVisible && widget.Icon != null)
                {
                    widget.Icon.mainTexture = WeatherMapStageIconLibrary.GetIcon(iconStage);
                    widget.Icon.color = Color.white;
                    widget.Icon.alpha = 1f;
                }

                bool clickEnabled = WeatherOverhaulSettingsManager.ShowMapWeatherClickBoxes || s_EditMode;
                bool showBoxVisual = WeatherOverhaulSettingsManager.ShowMapWeatherClickBoxOutlines || s_EditMode;
                if (widget.Collider != null) widget.Collider.enabled = clickEnabled;
                widget.ClickObject?.SetActive(clickEnabled);
                widget.BoxVisualObject?.SetActive(showBoxVisual);
                SetClickBoxVisualColor(widget, selectedClickBox ? new(1f, 0.92f, 0.18f, 0.90f) : new(0.18f, 0.95f, 0.30f, 0.82f));
            }
        }

        private static void SetRootActive(bool active)
        {
            if (s_RootObject == null) return;
            if (s_RootObject.activeSelf == active) return;
            s_RootObject.SetActive(active);
        }

        private static void DestroyOverlayRoot()
        {
            s_Widgets.Clear();
            if (s_RootObject == null) return;
            UnityEngine.Object.Destroy(s_RootObject);
            s_RootObject = null;
            s_TemplateLabel = null;
        }

        private static void DrawForecastPanelIfNeeded()
        {
            if (s_SelectedRegion == WeatherRegionId.Unknown) return;

            WeatherSnapshot snapshot = WeatherOverhaulRuntime.CurrentSnapshot;
            WeatherRegionDefinition selected = RegionWeatherGraph.Get(s_SelectedRegion);
            if (selected.Id == WeatherRegionId.Unknown) return;

            if (!WeatherOverhaulSettingsManager.ApplyGlobalWeatherToLoadedRegion)
            {
                DrawVanillaForecastPanel(selected, snapshot);
                return;
            }

            GlobalWeatherState state = GlobalWeatherSimulation.Current;
            if (!state.IsValid) return;

            Rect rect = BuildForecastPanelRect();
            HandleForecastPanelResize(ref rect);
            HandleForecastPanelDrag(ref rect);

            Color previousGuiColor = GUI.color;
            DrawForecastPanelBackground(rect);

            GUILayout.BeginArea(rect, "Weather Forecast", s_WindowStyle);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(selected.DisplayName, s_HeaderStyle);
            previousGuiColor = GUI.color;
            GUI.color = s_ForecastControlTintColor;
            bool closeForecast = GUILayout.Button("X", s_CloseButtonStyle, GUILayout.Width(26f), GUILayout.Height(24f));
            GUI.color = previousGuiColor;
            if (closeForecast)
            {
                s_SelectedRegion = WeatherRegionId.Unknown;
                s_ActiveForecastResizeHandle = ForecastResizeHandle.None;
                s_ForecastPanelDragging = false;
            }
            GUILayout.EndHorizontal();

            float now = GetDisplayWorldHour(snapshot);
            GUILayout.Label($"Current time: {BuildClock(snapshot)}", s_MutedStyle);
            bool canSeeFutureForecast = ForecastAccessManager.CanSeeFutureForecast(selected.Id);
            float visibleUntil = canSeeFutureForecast ? ForecastAccessManager.GetVisibleUntilWorldHour(selected.Id, snapshot, state.HorizonEndWorldHour) : now;
            if (canSeeFutureForecast && WeatherOverhaulSettingsManager.ShowForecastNightEvent)
            {
                GUILayout.Label($"Night event: {state.NightEventSchedule.BuildSummary(now, visibleUntil)}", s_MutedStyle);
            }
            if (canSeeFutureForecast &&
                WeatherOverhaulSettingsManager.ShowForecastGlimmerFog &&
                GlobalGlimmerFogSchedule.IsGlimmerFogRegion(selected.Id))
            {
                GUILayout.Label($"Glimmer Fog: {state.GlimmerFogSchedule.BuildSummary(now, visibleUntil, selected.Id)}", s_MutedStyle);
            }
            GUILayout.Space(8f);

            previousGuiColor = GUI.color;
            GUI.color = s_ForecastControlTintColor;
            s_PanelScroll = GUILayout.BeginScrollView(s_PanelScroll, false, true);
            GUI.color = previousGuiColor;

            if (canSeeFutureForecast)
            {
                bool restrictToVisibleHorizon = visibleUntil < state.HorizonEndWorldHour - 0.01f;
                DrawForecastCards(selected.Id, snapshot, Math.Max(430f, rect.width - 42f), visibleUntil, restrictToVisibleHorizon);
            }
            else
            {
                DrawUnavailableForecastCard(selected.Id, snapshot, Math.Max(430f, rect.width - 42f));
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            GUILayout.Label(ForecastAccessManager.BuildAccessLabel(selected.Id), s_FooterStyle);
            GUILayout.Label(ForecastAccessManager.BuildHorizonLabel(selected.Id, snapshot, state.HorizonEndWorldHour), s_FooterStyle);
            GUILayout.EndArea();

            DrawForecastResizeGrips(rect);
            s_ForecastPanelRect = rect;
        }

        private static void DrawVanillaForecastPanel(WeatherRegionDefinition selected, WeatherSnapshot snapshot)
        {
            Rect rect = BuildForecastPanelRect();
            HandleForecastPanelResize(ref rect);
            HandleForecastPanelDrag(ref rect);

            Color previousGuiColor = GUI.color;
            DrawForecastPanelBackground(rect);

            GUILayout.BeginArea(rect, "Weather Forecast", s_WindowStyle);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(selected.DisplayName, s_HeaderStyle);
            previousGuiColor = GUI.color;
            GUI.color = s_ForecastControlTintColor;
            bool closeForecast = GUILayout.Button("X", s_CloseButtonStyle, GUILayout.Width(26f), GUILayout.Height(24f));
            GUI.color = previousGuiColor;
            if (closeForecast)
            {
                s_SelectedRegion = WeatherRegionId.Unknown;
                s_ActiveForecastResizeHandle = ForecastResizeHandle.None;
                s_ForecastPanelDragging = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Current time: {BuildClock(snapshot)}", s_MutedStyle);
            GUILayout.Label("Source: vanilla weather", s_MutedStyle);
            GUILayout.Space(8f);

            previousGuiColor = GUI.color;
            GUI.color = s_ForecastControlTintColor;
            s_PanelScroll = GUILayout.BeginScrollView(s_PanelScroll, false, true);
            GUI.color = previousGuiColor;

            bool isLoadedRegion = snapshot.IsValid && selected.Id == snapshot.RegionId;
            if (!isLoadedRegion)
            {
                DrawVanillaUnavailableCard(selected, snapshot);
            }
            else if (VanillaWeatherForecast.TryGetCurrent(snapshot, out VanillaWeatherForecastSnapshot vanillaForecast))
            {
                DrawVanillaCurrentPhaseCard(vanillaForecast, snapshot, Math.Max(430f, rect.width - 42f));
            }
            else
            {
                DrawVanillaUnavailableCard(selected, snapshot);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            GUILayout.Label("Forecast source: vanilla WeatherTransition (loaded region only)", s_FooterStyle);
            if (isLoadedRegion && VanillaWeatherForecast.TryGetCurrent(snapshot, out VanillaWeatherForecastSnapshot currentForecast) && currentForecast.HasTiming)
            {
                float endWorldHour = snapshot.WorldHour + currentForecast.RemainingHours;
                GUILayout.Label($"Current phase ends: {GlobalWeatherSimulation.FormatWorldHourForSnapshot(snapshot, endWorldHour)}", s_FooterStyle);
            }
            else
            {
                GUILayout.Label("Forecast horizon: current loaded-region phase only", s_FooterStyle);
            }
            GUILayout.EndArea();

            DrawForecastResizeGrips(rect);
            s_ForecastPanelRect = rect;
        }

        private static void DrawVanillaCurrentPhaseCard(VanillaWeatherForecastSnapshot forecast, WeatherSnapshot snapshot, float contentWidth)
        {
            GUILayout.BeginVertical(s_DayCardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(BuildForecastDayHeader(Math.Max(1, snapshot.DayNumber), Math.Max(1, snapshot.DayNumber)), s_DayHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Vanilla current phase", s_CardMetaStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(3f);

            float remainingWidth = Mathf.Clamp(contentWidth * 0.20f, 100f, 140f);
            float weatherWidth = Mathf.Clamp(contentWidth * 0.31f, 145f, 230f);
            const float remainingWeatherGap = 36f;
            float timeWidth = Math.Max(170f, contentWidth - remainingWidth - weatherWidth - remainingWeatherGap - 30f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("TIME", s_CardColumnHeaderStyle, GUILayout.Width(timeWidth));
            GUILayout.Label("REMAINING", s_CardColumnHeaderStyle, GUILayout.Width(remainingWidth));
            GUILayout.Space(remainingWeatherGap);
            GUILayout.Label("WEATHER", s_CardColumnHeaderStyle, GUILayout.Width(weatherWidth));
            GUILayout.EndHorizontal();

            string timeRange = "Now";
            string remaining = "Unknown";
            if (forecast.HasTiming)
            {
                float endWorldHour = snapshot.WorldHour + forecast.RemainingHours;
                timeRange = $"Now -> {FormatForecastClock(snapshot, endWorldHour, false, snapshot.DayNumber)}";
                remaining = FormatVanillaRemainingTime(forecast.RemainingHours);
            }

            GUILayout.BeginHorizontal(s_CurrentForecastRowStyle);
            GUILayout.Label(timeRange, s_TableCellStyle, GUILayout.Width(timeWidth));
            GUILayout.Label(remaining, s_TableCellStyle, GUILayout.Width(remainingWidth));
            GUILayout.Space(remainingWeatherGap);
            GUILayout.Label(forecast.Definition.DisplayName, s_TableCellStyle, GUILayout.Width(weatherWidth));
            GUILayout.EndHorizontal();

            if (forecast.HasTiming)
            {
                GUILayout.Space(7f);
                GUILayout.Label($"Vanilla phase progress: {forecast.ElapsedHours:0.##}h / {forecast.TotalDurationHours:0.##}h", s_MutedStyle);
            }

            GUILayout.EndVertical();
        }

        private static void DrawVanillaUnavailableCard(WeatherRegionDefinition selected, WeatherSnapshot snapshot)
        {
            GUILayout.BeginVertical(s_DayCardStyle);
            GUILayout.Label("Vanilla weather unavailable", s_DayHeaderStyle);
            GUILayout.Space(3f);

            if (!snapshot.IsValid)
            {
                GUILayout.Label("The current vanilla weather state is not available in this scene.", s_MutedStyle);
            }
            else
            {
                GUILayout.Label($"Vanilla only maintains a live WeatherTransition for the currently loaded region ({snapshot.RegionDisplayName}). WeatherOverhaul does not substitute its regional simulation while loaded-region weather control is disabled, so {selected.DisplayName} has no remote forecast until you travel there.", s_MutedStyle);
            }

            GUILayout.EndVertical();
        }

        private static string FormatVanillaRemainingTime(float remainingHours)
        {
            int totalMinutes = Math.Max(0, (int)Math.Ceiling(remainingHours * 60f));
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            if (hours <= 0) return $"{minutes}m";
            if (minutes <= 0) return $"{hours}h";
            return $"{hours}h {minutes:00}m";
        }

        private static void DrawForecastPanelBackground(Rect rect)
        {
            Color previousGuiColor = GUI.color;

            if (s_PanelBackground != null)
            {
                GUI.color = s_ForecastPanelTintColor;
                GUI.DrawTexture(rect, s_PanelBackground, ScaleMode.StretchToFill, true);
            }

            if (s_TitleBarBackground != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, ForecastDragBarHeight), s_TitleBarBackground, ScaleMode.StretchToFill, true);
            }

            if (s_PanelBorderBackground != null)
            {
                const float borderThickness = 1f;
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderThickness), s_PanelBorderBackground, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - borderThickness, rect.width, borderThickness), s_PanelBorderBackground, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(new Rect(rect.x, rect.y, borderThickness, rect.height), s_PanelBorderBackground, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(new Rect(rect.xMax - borderThickness, rect.y, borderThickness, rect.height), s_PanelBorderBackground, ScaleMode.StretchToFill, true);
            }

            GUI.color = previousGuiColor;
        }

        private static Rect BuildForecastPanelRect()
        {
            if (!s_ForecastPanelRectInitialized)
            {
                if (WeatherOverhaulSettingsManager.TryGetForecastPanelLayout(out Rect savedRect))
                {
                    s_ForecastPanelRect = savedRect;
                }
                else
                {
                    float width = Math.Min(ForecastPanelWidth, Screen.width - 80f);
                    float height = Math.Min(ForecastPanelHeight, Screen.height - 170f);
                    float x = Screen.width - width - 44f;
                    float y = Math.Max(40f, (Screen.height - height) * 0.5f);
                    s_ForecastPanelRect = new(x, y, width, height);
                }

                s_ForecastPanelRectInitialized = true;
            }

            s_ForecastPanelRect = ClampForecastPanelRect(s_ForecastPanelRect);
            return s_ForecastPanelRect;
        }

        private static void SaveForecastPanelLayout()
        {
            s_ForecastPanelRect = ClampForecastPanelRect(s_ForecastPanelRect);
            WeatherOverhaulSettingsManager.SaveForecastPanelLayout(s_ForecastPanelRect);
            s_ForecastPanelLayoutDirty = false;
        }

        private static void DrawForecastResizeGrips(Rect rect)
        {
            float size = ForecastResizeGripSize;
            Color previousGuiColor = GUI.color;
            GUI.color = s_ForecastControlTintColor;
            GUI.Box(new(rect.x - size * 0.5f, rect.y - size * 0.5f, size, size), string.Empty, s_ResizeGripStyle);
            GUI.Box(new(rect.xMax - size * 0.5f, rect.y - size * 0.5f, size, size), string.Empty, s_ResizeGripStyle);
            GUI.Box(new(rect.x - size * 0.5f, rect.yMax - size * 0.5f, size, size), string.Empty, s_ResizeGripStyle);
            GUI.Box(new(rect.xMax - size * 0.5f, rect.yMax - size * 0.5f, size, size), string.Empty, s_ResizeGripStyle);
            GUI.color = previousGuiColor;
        }

        private static void HandleForecastPanelResize(ref Rect rect)
        {
            Event current = Event.current;
            if (current == null) return;

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                ForecastResizeHandle handle = GetForecastResizeHandle(rect, current.mousePosition);
                if (handle == ForecastResizeHandle.None) return;

                s_ActiveForecastResizeHandle = handle;
                s_ForecastResizeMouseStart = current.mousePosition;
                s_ForecastResizeRectStart = rect;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && s_ActiveForecastResizeHandle != ForecastResizeHandle.None)
            {
                Vector2 delta = current.mousePosition - s_ForecastResizeMouseStart;
                rect = ResizeForecastPanel(s_ForecastResizeRectStart, s_ActiveForecastResizeHandle, delta);
                s_ForecastPanelLayoutDirty = true;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0 && s_ActiveForecastResizeHandle != ForecastResizeHandle.None)
            {
                s_ActiveForecastResizeHandle = ForecastResizeHandle.None;
                s_ForecastPanelRect = rect;
                if (s_ForecastPanelLayoutDirty) SaveForecastPanelLayout();
                current.Use();
            }
        }

        private static void HandleForecastPanelDrag(ref Rect rect)
        {
            Event current = Event.current;
            if (current == null || s_ActiveForecastResizeHandle != ForecastResizeHandle.None) return;

            Rect dragRect = new(rect.x + ForecastResizeGripSize * 0.5f, rect.y, Math.Max(0f, rect.width - ForecastDragRightExclusion - ForecastResizeGripSize), ForecastDragBarHeight);

            if (current.type == EventType.MouseDown && current.button == 0 && dragRect.Contains(current.mousePosition))
            {
                s_ForecastPanelDragging = true;
                s_ForecastDragMouseOffset = current.mousePosition - new Vector2(rect.x, rect.y);
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && s_ForecastPanelDragging)
            {
                rect.x = current.mousePosition.x - s_ForecastDragMouseOffset.x;
                rect.y = current.mousePosition.y - s_ForecastDragMouseOffset.y;
                rect = ClampForecastPanelRect(rect);
                s_ForecastPanelLayoutDirty = true;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0 && s_ForecastPanelDragging)
            {
                s_ForecastPanelDragging = false;
                s_ForecastPanelRect = rect;
                if (s_ForecastPanelLayoutDirty) SaveForecastPanelLayout();
                current.Use();
            }
        }

        private static ForecastResizeHandle GetForecastResizeHandle(Rect rect, Vector2 mousePosition)
        {
            float size = ForecastResizeGripSize;
            if (new Rect(rect.x - size * 0.5f, rect.y - size * 0.5f, size, size).Contains(mousePosition)) return ForecastResizeHandle.TopLeft;
            if (new Rect(rect.xMax - size * 0.5f, rect.y - size * 0.5f, size, size).Contains(mousePosition)) return ForecastResizeHandle.TopRight;
            if (new Rect(rect.x - size * 0.5f, rect.yMax - size * 0.5f, size, size).Contains(mousePosition)) return ForecastResizeHandle.BottomLeft;
            if (new Rect(rect.xMax - size * 0.5f, rect.yMax - size * 0.5f, size, size).Contains(mousePosition)) return ForecastResizeHandle.BottomRight;
            return ForecastResizeHandle.None;
        }

        private static Rect ResizeForecastPanel(Rect start, ForecastResizeHandle handle, Vector2 delta)
        {
            float maxWidth = Math.Max(ForecastPanelMinWidth, Screen.width - ForecastPanelScreenMargin * 2f);
            float maxHeight = Math.Max(ForecastPanelMinHeight, Screen.height - ForecastPanelScreenMargin * 2f);
            float minWidth = Math.Min(ForecastPanelMinWidth, maxWidth);
            float minHeight = Math.Min(ForecastPanelMinHeight, maxHeight);

            bool resizeLeft = handle == ForecastResizeHandle.TopLeft || handle == ForecastResizeHandle.BottomLeft;
            bool resizeTop = handle == ForecastResizeHandle.TopLeft || handle == ForecastResizeHandle.TopRight;

            float width = Mathf.Clamp(start.width + (resizeLeft ? -delta.x : delta.x), minWidth, maxWidth);
            float height = Mathf.Clamp(start.height + (resizeTop ? -delta.y : delta.y), minHeight, maxHeight);
            float x = resizeLeft ? start.xMax - width : start.x;
            float y = resizeTop ? start.yMax - height : start.y;

            return ClampForecastPanelRect(new Rect(x, y, width, height));
        }

        private static Rect ClampForecastPanelRect(Rect rect)
        {
            float maxWidth = Math.Max(100f, Screen.width - ForecastPanelScreenMargin * 2f);
            float maxHeight = Math.Max(100f, Screen.height - ForecastPanelScreenMargin * 2f);
            rect.width = Mathf.Clamp(rect.width, Math.Min(ForecastPanelMinWidth, maxWidth), maxWidth);
            rect.height = Mathf.Clamp(rect.height, Math.Min(ForecastPanelMinHeight, maxHeight), maxHeight);
            rect.x = Mathf.Clamp(rect.x, ForecastPanelScreenMargin, Math.Max(ForecastPanelScreenMargin, Screen.width - ForecastPanelScreenMargin - rect.width));
            rect.y = Mathf.Clamp(rect.y, ForecastPanelScreenMargin, Math.Max(ForecastPanelScreenMargin, Screen.height - ForecastPanelScreenMargin - rect.height));
            return rect;
        }

        private static void DrawForecastCards(WeatherRegionId regionId, WeatherSnapshot snapshot, float contentWidth, float visibleUntil, bool restrictToVisibleHorizon)
        {
            float now = GetDisplayWorldHour(snapshot);
            List<WeatherActivationPlan> plans = GlobalWeatherSimulation.BuildActivationPlanSequence(regionId, now, ForecastPanelMaxPlans);
            if (restrictToVisibleHorizon) plans = FilterPlansByForecastVisibility(plans, visibleUntil);

            if (plans.Count <= 0)
            {
                GUILayout.BeginVertical(s_DayCardStyle);
                GUILayout.Label("Forecast unavailable", s_DayHeaderStyle);
                GUILayout.Label("No planned weather periods are currently available for this region.", s_MutedStyle);
                GUILayout.EndVertical();
                return;
            }

            int index = 0;
            while (index < plans.Count)
            {
                int day = GetForecastDay(snapshot, Math.Max(now, plans[index].StartWorldHour));
                int groupEnd = index + 1;
                while (groupEnd < plans.Count && GetForecastDay(snapshot, Math.Max(now, plans[groupEnd].StartWorldHour)) == day) groupEnd++;

                DrawForecastDayCard(plans, index, groupEnd, day, snapshot, now, restrictToVisibleHorizon, visibleUntil, contentWidth);
                index = groupEnd;
                if (index < plans.Count) GUILayout.Space(7f);
            }
        }

        private static void DrawForecastDayCard(List<WeatherActivationPlan> plans, int startIndex, int endIndex, int day, WeatherSnapshot snapshot, float now, bool restrictToVisibleHorizon, float visibleUntil, float contentWidth)
        {
            GUILayout.BeginVertical(s_DayCardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(BuildForecastDayHeader(day, snapshot.DayNumber), s_DayHeaderStyle);
            GUILayout.FlexibleSpace();
            int count = endIndex - startIndex;
            GUILayout.Label(count == 1 ? "1 period" : $"{count} periods", s_CardMetaStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(3f);

            float durationWidth = Mathf.Clamp(contentWidth * 0.17f, 82f, 118f);
            float weatherWidth = Mathf.Clamp(contentWidth * 0.31f, 145f, 230f);
            const float durationWeatherGap = 36f;
            float timeWidth = Math.Max(180f, contentWidth - durationWidth - weatherWidth - durationWeatherGap - 30f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("TIME", s_CardColumnHeaderStyle, GUILayout.Width(timeWidth));
            GUILayout.Label("DURATION", s_CardColumnHeaderStyle, GUILayout.Width(durationWidth));
            GUILayout.Space(durationWeatherGap);
            GUILayout.Label("WEATHER", s_CardColumnHeaderStyle, GUILayout.Width(weatherWidth));
            GUILayout.EndHorizontal();

            for (int i = startIndex; i < endIndex; i++)
            {
                WeatherActivationPlan plan = plans[i];
                bool isCurrent = i == 0 && plan.StartWorldHour <= now + 0.05f;
                float displayedEnd = restrictToVisibleHorizon ? Math.Min(plan.EndWorldHour, visibleUntil) : plan.EndWorldHour;
                GUIStyle rowStyle = isCurrent ? s_CurrentForecastRowStyle : ((i - startIndex) % 2 == 0 ? s_ForecastRowStyle : s_ForecastRowAlternateStyle);

                GUILayout.BeginHorizontal(rowStyle);
                GUILayout.Label(BuildForecastTimeRange(plan, snapshot, isCurrent, displayedEnd, day), s_TableCellStyle, GUILayout.Width(timeWidth));
                GUILayout.Label(BuildForecastDurationText(plan, now, displayedEnd), s_TableCellStyle, GUILayout.Width(durationWidth));
                GUILayout.Space(durationWeatherGap);
                GUILayout.Label(plan.Definition.DisplayName, s_TableCellStyle, GUILayout.Width(weatherWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private static void DrawUnavailableForecastCard(WeatherRegionId regionId, WeatherSnapshot snapshot, float contentWidth)
        {
            float now = GetDisplayWorldHour(snapshot);
            int day = snapshot.IsValid ? Math.Max(1, snapshot.DayNumber) : GetForecastDay(snapshot, now);
            bool isCurrentRegion = snapshot.IsValid && regionId == snapshot.RegionId;
            string weather = "Unknown";

            if (isCurrentRegion && GlobalWeatherSimulation.TryGetActivationPlan(regionId, now, out WeatherActivationPlan currentPlan))
            {
                weather = currentPlan.Definition.DisplayName;
            }

            GUILayout.BeginVertical(s_DayCardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(BuildForecastDayHeader(day, day), s_DayHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Observation only", s_CardMetaStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(3f);

            float durationWidth = Mathf.Clamp(contentWidth * 0.17f, 82f, 118f);
            float weatherWidth = Mathf.Clamp(contentWidth * 0.31f, 145f, 230f);
            const float durationWeatherGap = 36f;
            float timeWidth = Math.Max(180f, contentWidth - durationWidth - weatherWidth - durationWeatherGap - 30f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("TIME", s_CardColumnHeaderStyle, GUILayout.Width(timeWidth));
            GUILayout.Label("DURATION", s_CardColumnHeaderStyle, GUILayout.Width(durationWidth));
            GUILayout.Space(durationWeatherGap);
            GUILayout.Label("WEATHER", s_CardColumnHeaderStyle, GUILayout.Width(weatherWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(s_CurrentForecastRowStyle);
            GUILayout.Label("Unknown", s_TableCellStyle, GUILayout.Width(timeWidth));
            GUILayout.Label("Unknown", s_TableCellStyle, GUILayout.Width(durationWidth));
            GUILayout.Space(durationWeatherGap);
            GUILayout.Label(weather, s_TableCellStyle, GUILayout.Width(weatherWidth));
            GUILayout.EndHorizontal();

            GUILayout.Space(7f);
            GUILayout.Label(ForecastAccessManager.BuildUnavailableMessage(regionId, snapshot), s_MutedStyle);
            GUILayout.EndVertical();
        }

        private static string BuildForecastDayHeader(int day, int currentDay)
        {
            if (day == currentDay) return $"Day {day} - Today";
            if (day == currentDay + 1) return $"Day {day} - Tomorrow";
            return $"Day {day}";
        }

        private static string BuildForecastTimeRange(WeatherActivationPlan plan, WeatherSnapshot snapshot, bool isCurrent, float displayedEndWorldHour, int groupDay)
        {
            string start = isCurrent ? "Now" : FormatForecastClock(snapshot, plan.StartWorldHour, false, groupDay);
            string end = FormatForecastClock(snapshot, displayedEndWorldHour, false, groupDay);
            return $"{start} -> {end}";
        }

        private static string BuildForecastDurationText(WeatherActivationPlan plan, float now, float displayedEndWorldHour)
        {
            float displayedDuration = Math.Max(0f, displayedEndWorldHour - Math.Max(now, plan.StartWorldHour));
            if (displayedDuration < plan.DurationHours - 0.05f) return $"{displayedDuration:0.#}h visible";
            return $"{plan.DurationHours:0.#}h";
        }

        private static string FormatForecastClock(WeatherSnapshot snapshot, float worldHour, bool showDayWhenDifferent, int referenceDay)
        {
            GetForecastClockParts(snapshot, worldHour, out int day, out int hour, out int minute);
            if (showDayWhenDifferent && day != referenceDay) return $"D{day} {hour:00}:{minute:00}";
            return $"{hour:00}:{minute:00}";
        }

        private static int GetForecastDay(WeatherSnapshot snapshot, float worldHour)
        {
            GetForecastClockParts(snapshot, worldHour, out int day, out _, out _);
            return day;
        }

        private static void GetForecastClockParts(WeatherSnapshot snapshot, float worldHour, out int day, out int hour, out int minute)
        {
            float deltaHours = worldHour - snapshot.WorldHour;
            int currentAbsoluteMinutes = ((Math.Max(1, snapshot.DayNumber) - 1) * 24 * 60) + (snapshot.Hour * 60) + snapshot.Minute;
            int targetAbsoluteMinutes = currentAbsoluteMinutes + (int)Math.Round(deltaHours * 60f);
            if (targetAbsoluteMinutes < 0) targetAbsoluteMinutes = 0;

            day = (targetAbsoluteMinutes / (24 * 60)) + 1;
            int minuteOfDay = targetAbsoluteMinutes % (24 * 60);
            hour = minuteOfDay / 60;
            minute = minuteOfDay % 60;
        }

        private static List<WeatherActivationPlan> FilterPlansByForecastVisibility(List<WeatherActivationPlan> plans, float visibleUntil)
        {
            if (plans == null || plans.Count <= 0) return plans ?? [];

            List<WeatherActivationPlan> visible = [];
            for (int i = 0; i < plans.Count; i++)
            {
                WeatherActivationPlan plan = plans[i];
                if (plan.StartWorldHour >= visibleUntil - 0.01f) break;
                visible.Add(plan);
                if (plan.EndWorldHour >= visibleUntil - 0.01f) break;
            }

            return visible;
        }

        private static string BuildCompactConditionText(bool canSeeCurrentStage, bool hasPlan, WeatherActivationPlan plan)
        {
            if (!canSeeCurrentStage) return "Unknown";
            return hasPlan ? plan.Definition.DisplayName : "Unknown";
        }

        private static float GetDisplayWorldHour(WeatherSnapshot snapshot)
        {
            return snapshot.IsValid ? snapshot.WorldHour : GlobalWeatherSimulation.Current.GeneratedAtWorldHour;
        }

        private static void ToggleEditMode()
        {
            if (!ShouldShowWorldMapOverlay())
            {
                s_EditMode = false;
                Core.Warn("[PanelMap][Edit] Open the world map with WeatherOverhaul global simulation enabled before editing map weather overlay positions.");
                return;
            }

            s_EditMode = !s_EditMode;
            if (s_EditMode && !IsRegionInActiveAnchorSet(s_EditedRegion)) s_EditedRegion = GetDefaultEditableRegion();
            Core.Log(s_EditMode ? $"[PanelMap][Edit] Enabled. Target={GetEditTargetName()}. Numpad / cycles weather text, clickbox position, and clickbox size. Drag or Numpad 8/2/4/6 edits the selected target, Numpad +/- select previous/next, Numpad . center/reset selected target, Numpad * clamp selected target type, Numpad 5 save all files, Numpad 9 reset selected target, Numpad 0 reset all selected target type, Numpad 7 exit." : "[PanelMap][Edit] Disabled.");
        }

        private static void ToggleEditTarget()
        {
            if (s_EditTarget == MapOverlayEditTarget.WeatherLabel) s_EditTarget = MapOverlayEditTarget.ClickBox;
            else if (s_EditTarget == MapOverlayEditTarget.ClickBox) s_EditTarget = MapOverlayEditTarget.ClickBoxSize;
            else s_EditTarget = MapOverlayEditTarget.WeatherLabel;

            if (!IsRegionInActiveAnchorSet(s_EditedRegion)) s_EditedRegion = GetDefaultEditableRegion();
            WeatherRegionDefinition region = RegionWeatherGraph.Get(s_EditedRegion);
            Core.Log($"[PanelMap][Edit] Target switched to {GetEditTargetName()} | Selected={region.DisplayName} | {GetEditedTargetSummary(s_EditedRegion)}.");
        }

        private static string GetEditTargetName()
        {
            return s_EditTarget switch
            {
                MapOverlayEditTarget.ClickBox => "clickbox position",
                MapOverlayEditTarget.ClickBoxSize => "clickbox size",
                _ => "weather label"
            };
        }

        private static string GetEditedTargetSummary(WeatherRegionId regionId)
        {
            if (regionId == WeatherRegionId.Unknown) return "none";
            if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize)
            {
                Vector2 size = GetClickBoxSize(regionId, GetAnchor(regionId));
                return $"W={size.x:0.#}, H={size.y:0.#}";
            }

            Vector2 position = GetEditedPosition(regionId);
            return $"X={position.x:0.#}, Y={position.y:0.#}";
        }

        private static void SelectNextEditableRegion(int direction)
        {
            MapRegionAnchor[] anchors = GetActiveAnchors();
            if (anchors.Length == 0) return;

            int currentIndex = -1;
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i].RegionId == s_EditedRegion)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + direction + anchors.Length) % anchors.Length;
            SelectRegionForEditing(anchors[nextIndex].RegionId);
        }

        private static void CenterEditedRegion()
        {
            if (s_EditedRegion == WeatherRegionId.Unknown)
            {
                Core.Warn("[PanelMap][Edit] No region selected. Use Numpad +/- to select one first.");
                return;
            }

            if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize)
            {
                RemoveEditedClickBoxSize(s_EditedRegion);
                ApplyWidgetPosition(s_EditedRegion);
                WeatherRegionDefinition sizeRegion = RegionWeatherGraph.Get(s_EditedRegion);
                Vector2 size = GetClickBoxSize(s_EditedRegion, GetAnchor(s_EditedRegion));
                Core.Log($"[PanelMap][Edit] Reset {sizeRegion.DisplayName} clickbox size to default W={size.x:0.#}, H={size.y:0.#}. Use Numpad 5 to save.");
                return;
            }

            SetEditedPosition(s_EditedRegion, Vector2.zero);
            ApplyWidgetPosition(s_EditedRegion);
            WeatherRegionDefinition region = RegionWeatherGraph.Get(s_EditedRegion);
            Core.Log($"[PanelMap][Edit] Centered {region.DisplayName} {GetEditTargetName()} at X=0, Y=0. Move it with drag or Numpad 8/2/4/6, then save with Numpad 5.");
        }

        private static void ClampAllLabelsIntoEditorBounds()
        {
            EnsureCustomPositionsLoaded();
            EnsureCustomClickBoxPositionsLoaded();
            EnsureCustomClickBoxSizesLoaded();

            if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize)
            {
                int sizeChanged = 0;
                MapRegionAnchor[] sizeAnchors = GetActiveAnchors();
                for (int i = 0; i < sizeAnchors.Length; i++)
                {
                    WeatherRegionId regionId = sizeAnchors[i].RegionId;
                    Vector2 before = GetClickBoxSize(regionId, sizeAnchors[i]);
                    Vector2 after = ClampClickBoxSize(before);
                    if (Math.Abs(after.x - before.x) < 0.01f && Math.Abs(after.y - before.y) < 0.01f) continue;

                    s_CustomClickBoxSizes[regionId] = after;
                    ApplyWidgetPosition(regionId);
                    sizeChanged++;
                }

                Core.Log($"[PanelMap][Edit] Clamped {sizeChanged} clickbox size(s). Use Numpad 5 to save.");
                return;
            }

            int changed = 0;
            MapRegionAnchor[] anchors = GetActiveAnchors();
            for (int i = 0; i < anchors.Length; i++)
            {
                WeatherRegionId regionId = anchors[i].RegionId;
                Vector2 before = GetEditedPosition(regionId);
                Vector2 after = ClampToEditorBounds(before);
                if (Math.Abs(after.x - before.x) < 0.01f && Math.Abs(after.y - before.y) < 0.01f) continue;

                SetEditedPosition(regionId, after);
                ApplyWidgetPosition(regionId);
                changed++;
            }

            Core.Log($"[PanelMap][Edit] Clamped {changed} {GetEditTargetName()} position(s) into the editable world map bounds. Use Numpad 5 to save.");
        }

        private static Vector2 ClampToEditorBounds(Vector2 position)
        {
            float halfWidth = Math.Max(380f, Math.Min(EditorSafeHalfWidth, Screen.width * 0.35f));
            float halfHeight = Math.Max(220f, Math.Min(EditorSafeHalfHeight, Screen.height * 0.35f));
            return new(Mathf.Clamp(position.x, -halfWidth, halfWidth), Mathf.Clamp(position.y, -halfHeight, halfHeight));
        }

        private static void NudgeEditedRegion(float deltaX, float deltaY)
        {
            if (s_EditedRegion == WeatherRegionId.Unknown)
            {
                Core.Warn("[PanelMap][Edit] No region selected. Click a region hitbox first or use Numpad +/- to select one.");
                return;
            }

            if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize) ResizeEditedRegion(s_EditedRegion, deltaX, deltaY);
            else MoveEditedRegion(s_EditedRegion, deltaX, deltaY);
        }

        private static void ResizeEditedRegion(WeatherRegionId regionId, float deltaWidth, float deltaHeight)
        {
            if (regionId == WeatherRegionId.Unknown) return;
            s_EditedRegion = regionId;
            Vector2 size = GetClickBoxSize(regionId, GetAnchor(regionId));
            size.x += deltaWidth;
            size.y += deltaHeight;
            s_CustomClickBoxSizes[regionId] = ClampClickBoxSize(size);
            ApplyWidgetPosition(regionId);
        }

        private static void ResetEditedRegion()
        {
            if (s_EditedRegion == WeatherRegionId.Unknown)
            {
                Core.Warn("[PanelMap][Edit] No region selected to reset.");
                return;
            }

            if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize) RemoveEditedClickBoxSize(s_EditedRegion);
            else RemoveEditedPosition(s_EditedRegion);

            ApplyWidgetPosition(s_EditedRegion);
            WeatherRegionDefinition region = RegionWeatherGraph.Get(s_EditedRegion);
            Core.Log($"[PanelMap][Edit] Reset {region.DisplayName} {GetEditTargetName()} to default.");
        }

        private static void ResetAllCustomPositions()
        {
            if (s_EditTarget == MapOverlayEditTarget.ClickBoxSize) s_CustomClickBoxSizes.Clear();
            else if (s_EditTarget == MapOverlayEditTarget.ClickBox) s_CustomClickBoxPositions.Clear();
            else s_CustomPositions.Clear();

            foreach (WeatherRegionId regionId in s_Widgets.Keys) ApplyWidgetPosition(regionId);
            Core.Log($"[PanelMap][Edit] Reset all map {GetEditTargetName()} values to defaults. Use Numpad 5 to save this reset.");
        }

        private static Vector2 GetAnchorPosition(WeatherRegionId regionId)
        {
            EnsureCustomPositionsLoaded();
            if (s_CustomPositions.TryGetValue(regionId, out Vector2 customPosition)) return customPosition;
            MapRegionAnchor? anchor = GetAnchor(regionId);
            return anchor != null ? anchor.DefaultLabelPosition : Vector2.zero;
        }

        private static Vector2 GetClickBoxPosition(WeatherRegionId regionId)
        {
            EnsureCustomClickBoxPositionsLoaded();
            if (s_CustomClickBoxPositions.TryGetValue(regionId, out Vector2 customPosition)) return customPosition;
            MapRegionAnchor? anchor = GetAnchor(regionId);
            return anchor != null ? anchor.DefaultClickBoxPosition : Vector2.zero;
        }

        private static Vector2 GetEditedPosition(WeatherRegionId regionId)
        {
            return s_EditTarget == MapOverlayEditTarget.ClickBox ? GetClickBoxPosition(regionId) : GetAnchorPosition(regionId);
        }

        private static void SetEditedPosition(WeatherRegionId regionId, Vector2 position)
        {
            if (s_EditTarget == MapOverlayEditTarget.ClickBox) s_CustomClickBoxPositions[regionId] = position;
            else s_CustomPositions[regionId] = position;
        }

        private static void RemoveEditedPosition(WeatherRegionId regionId)
        {
            if (s_EditTarget == MapOverlayEditTarget.ClickBox) s_CustomClickBoxPositions.Remove(regionId);
            else s_CustomPositions.Remove(regionId);
        }

        private static void RemoveEditedClickBoxSize(WeatherRegionId regionId)
        {
            s_CustomClickBoxSizes.Remove(regionId);
        }

        private static MapRegionAnchor? GetAnchor(WeatherRegionId regionId)
        {
            for (int i = 0; i < s_AllAnchors.Length; i++)
            {
                if (s_AllAnchors[i].RegionId == regionId) return s_AllAnchors[i];
            }

            return null;
        }

        private static void ApplyWidgetPosition(WeatherRegionId regionId)
        {
            if (!s_Widgets.TryGetValue(regionId, out WeatherMapRegionWidget widget)) return;
            Vector2 labelPosition = GetAnchorPosition(regionId);
            Vector2 clickBoxPosition = GetClickBoxPosition(regionId);
            if (widget.LabelObject != null) widget.LabelObject.transform.localPosition = new(labelPosition.x, labelPosition.y + LabelWeatherYOffset, -2f);
            if (widget.IconObject != null)
            {
                float iconYOffset = WeatherOverhaulSettingsManager.MapWeatherStageDisplay == WorldMapWeatherStageDisplayMode.NameAndIcons ? WeatherIconCombinedYOffset : 0f;
                iconYOffset += WeatherOverhaulSettingsManager.MapWeatherIconYOffset;
                widget.IconObject.transform.localPosition = new(labelPosition.x, labelPosition.y + LabelWeatherYOffset + iconYOffset, -2f);
            }
            if (widget.Icon != null)
            {
                int iconSize = WeatherOverhaulSettingsManager.MapWeatherIconSize;
                widget.Icon.width = iconSize;
                widget.Icon.height = iconSize;
            }
            if (widget.ClickObject != null) widget.ClickObject.transform.localPosition = new(clickBoxPosition.x, clickBoxPosition.y + ClickBoxYOffset, -4f);
            if (widget.BoxVisualObject != null) widget.BoxVisualObject.transform.localPosition = new(clickBoxPosition.x, clickBoxPosition.y + ClickBoxVisualYOffset, -3f);

            MapRegionAnchor? anchor = GetAnchor(regionId);
            ConfigureClickBoxCollider(widget.Collider, regionId, anchor);
            ConfigureClickBoxVisualLines(widget, anchor);
        }

        private static void EnsureCustomPositionsLoaded()
        {
            if (s_CustomPositionsLoaded) return;
            s_CustomPositionsLoaded = true;
            s_CustomPositions.Clear();

            string path = GetPositionsFilePath();
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length <= 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    if (!Enum.TryParse(parts[0].Trim(), out WeatherRegionId regionId)) continue;
                    string[] values = parts[1].Split(',');
                    if (values.Length != 2) continue;
                    if (!float.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                    if (!float.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                    if (GetAnchor(regionId) == null) continue;
                    s_CustomPositions[regionId] = new(x, y);
                }

                Core.Log($"[PanelMap][Edit] Loaded {s_CustomPositions.Count} custom world map weather label position(s).");
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("panel-map-label-position-load", "[PanelMap][Edit] Failed to load custom map weather label positions.", e);
            }
        }

        private static void EnsureCustomClickBoxPositionsLoaded()
        {
            if (s_CustomClickBoxPositionsLoaded) return;
            s_CustomClickBoxPositionsLoaded = true;
            s_CustomClickBoxPositions.Clear();

            string path = GetClickBoxPositionsFilePath();
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length <= 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    if (!Enum.TryParse(parts[0].Trim(), out WeatherRegionId regionId)) continue;
                    string[] values = parts[1].Split(',');
                    if (values.Length != 2) continue;
                    if (!float.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                    if (!float.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                    if (GetAnchor(regionId) == null) continue;
                    s_CustomClickBoxPositions[regionId] = new(x, y);
                }

                Core.Log($"[PanelMap][Edit] Loaded {s_CustomClickBoxPositions.Count} custom world map forecast clickbox position(s).");
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("panel-map-clickbox-position-load", "[PanelMap][Edit] Failed to load custom map forecast clickbox positions.", e);
            }
        }

        private static void EnsureCustomClickBoxSizesLoaded()
        {
            if (s_CustomClickBoxSizesLoaded) return;
            s_CustomClickBoxSizesLoaded = true;
            s_CustomClickBoxSizes.Clear();

            string path = GetClickBoxSizesFilePath();
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length <= 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    if (!Enum.TryParse(parts[0].Trim(), out WeatherRegionId regionId)) continue;
                    string[] values = parts[1].Split(',');
                    if (values.Length != 2) continue;
                    if (!float.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float width)) continue;
                    if (!float.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float height)) continue;
                    if (GetAnchor(regionId) == null) continue;
                    s_CustomClickBoxSizes[regionId] = ClampClickBoxSize(new(width, height));
                }

                Core.Log($"[PanelMap][Edit] Loaded {s_CustomClickBoxSizes.Count} custom world map forecast clickbox size(s).");
            }
            catch (Exception e)
            {
                Core.LogExceptionOnce("panel-map-clickbox-size-load", "[PanelMap][Edit] Failed to load custom map forecast clickbox sizes.", e);
            }
        }

        private static void SaveCustomPositions()
        {
            EnsureCustomPositionsLoaded();
            EnsureCustomClickBoxPositionsLoaded();
            EnsureCustomClickBoxSizesLoaded();
            SavePositionFile(GetPositionsFilePath(), "# WeatherOverhaul world map weather label positions", GetAnchorPosition, "weather label");
            SavePositionFile(GetClickBoxPositionsFilePath(), "# WeatherOverhaul world map forecast clickbox positions", GetClickBoxPosition, "forecast clickbox");
            SaveClickBoxSizeFile(GetClickBoxSizesFilePath());
        }

        private static void SavePositionFile(string path, string header, Func<WeatherRegionId, Vector2> positionGetter, string label)
        {
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                List<string> lines =
                [
                    header,
                    "# Format: WeatherRegionId=X,Y"
                ];
                for (int i = 0; i < s_AllAnchors.Length; i++)
                {
                    MapRegionAnchor anchor = s_AllAnchors[i];
                    Vector2 position = positionGetter(anchor.RegionId);
                    lines.Add(string.Format(CultureInfo.InvariantCulture, "{0}={1:0.###},{2:0.###}", anchor.RegionId, position.x, position.y));
                }

                File.WriteAllLines(path, [.. lines]);
                Core.Log($"[PanelMap][Edit] Saved world map {label} positions to {path}");
            }
            catch (Exception e)
            {
                Core.Warn($"[PanelMap][Edit] Failed to save map {label} positions: {e.Message}");
            }
        }


        private static void SaveClickBoxSizeFile(string path)
        {
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                List<string> lines =
                [
                    "# WeatherOverhaul world map forecast clickbox sizes",
                    "# Format: WeatherRegionId=Width,Height",
                    "# Values are absolute NGUI units, edited with the in-game world map overlay editor."
                ];
                for (int i = 0; i < s_AllAnchors.Length; i++)
                {
                    MapRegionAnchor anchor = s_AllAnchors[i];
                    Vector2 size = GetClickBoxSize(anchor.RegionId, anchor);
                    lines.Add(string.Format(CultureInfo.InvariantCulture, "{0}={1:0.###},{2:0.###}", anchor.RegionId, size.x, size.y));
                }

                File.WriteAllLines(path, [.. lines]);
                Core.Log($"[PanelMap][Edit] Saved world map forecast clickbox sizes to {path}");
            }
            catch (Exception e)
            {
                Core.Warn($"[PanelMap][Edit] Failed to save map forecast clickbox sizes: {e.Message}");
            }
        }

        private static string GetPositionsFilePath()
        {
            return Path.Combine(Environment.CurrentDirectory, "UserData", PositionFileName);
        }

        private static string GetClickBoxPositionsFilePath()
        {
            return Path.Combine(Environment.CurrentDirectory, "UserData", ClickBoxPositionFileName);
        }

        private static string GetClickBoxSizesFilePath()
        {
            return Path.Combine(Environment.CurrentDirectory, "UserData", ClickBoxSizeFileName);
        }

        private static void DrawEditPanelIfNeeded()
        {
            if (!s_EditMode) return;

            Rect rect = new(30f, Screen.height - 180f, 760f, 145f);
            GUILayout.BeginArea(rect, "World Map Weather Overlay Editor", GUI.skin.window);
            GUILayout.Label($"Target: {GetEditTargetName()} | Numpad / cycles label position, clickbox position, clickbox size. Yellow text/rectangle = selected target. Unsaved edits are kept until map close/reload.", s_MutedStyle);
            GUILayout.Label("Position target: Numpad 8/2/4/6 moves. Size target: 4/6 changes width, 2/8 changes height. Shift = fast | Numpad +/- select region | Numpad . reset selected target | Numpad * clamp | Numpad 5 save all files | Numpad 9 reset selected target | Numpad 0 reset all selected target type | Numpad 7 exit", s_MutedStyle);
            if (s_EditedRegion != WeatherRegionId.Unknown)
            {
                WeatherRegionDefinition region = RegionWeatherGraph.Get(s_EditedRegion);
                Vector2 labelPosition = GetAnchorPosition(s_EditedRegion);
                Vector2 clickPosition = GetClickBoxPosition(s_EditedRegion);
                MapRegionAnchor? anchor = GetAnchor(s_EditedRegion);
                Vector2 clickSize = GetClickBoxSize(s_EditedRegion, anchor);
                GUILayout.Label($"Selected: {region.DisplayName} | Weather label X={labelPosition.x:0.#}, Y={labelPosition.y:0.#} | Clickbox X={clickPosition.x:0.#}, Y={clickPosition.y:0.#} | Size W={clickSize.x:0.#}, H={clickSize.y:0.#}", s_TableCellStyle);
            }
            else
            {
                GUILayout.Label("Selected: none", s_TableCellStyle);
            }
            GUILayout.EndArea();
        }

        private static string BuildClock(WeatherSnapshot snapshot)
        {
            return snapshot.IsValid ? snapshot.GetClockText() : "unknown time";
        }

        private static void EnsureStyles()
        {
            int fontSize = Mathf.Clamp(WeatherOverhaulSettingsManager.ForecastFontSize, 10, 24);
            string colorSignature = BuildForecastColorSignature();
            if (s_StylesReady && s_LastForecastFontSize == fontSize && string.Equals(s_LastForecastColorSignature, colorSignature, StringComparison.Ordinal)) return;
            s_StylesReady = true;
            s_LastForecastFontSize = fontSize;
            s_LastForecastColorSignature = colorSignature;
            s_ForecastPanelTintColor = WeatherOverhaulSettingsManager.ForecastPanelTint;
            s_ForecastControlTintColor = WeatherOverhaulSettingsManager.ForecastControlTint;

            s_HeaderStyle = new(GUI.skin.label)
            {
                fontSize = fontSize + 7,
                fontStyle = FontStyle.Bold
            };
            s_HeaderStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastRegionTitleColor;

            s_MutedStyle = new(GUI.skin.label)
            {
                fontSize = Math.Max(10, fontSize - 1)
            };
            s_MutedStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastInfoTextColor;
            s_MutedStyle.wordWrap = true;

            s_TableHeaderStyle = new(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            s_TableHeaderStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastColumnHeaderColor;

            s_TableCellStyle = new(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true
            };
            s_TableCellStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastRowTextColor;

            s_CloseButtonStyle = new(GUI.skin.button)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };

            s_WindowStyle = new(GUI.skin.window)
            {
                fontSize = Math.Max(12, fontSize + 1),
                fontStyle = FontStyle.Bold
            };
            s_WindowStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastWindowTitleColor;
            ClearGuiStyleBackgrounds(s_WindowStyle);

            s_ResizeGripStyle = new(GUI.skin.box);

            RebuildForecastStyleTextures();

            s_DayCardStyle = new(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 9),
                margin = new RectOffset(0, 7, 0, 7)
            };
            s_DayCardStyle.normal.background = s_DayCardBackground;

            s_DayHeaderStyle = new(GUI.skin.label)
            {
                fontSize = fontSize + 2,
                fontStyle = FontStyle.Bold
            };
            s_DayHeaderStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastDayHeaderColor;

            s_CardMetaStyle = new(GUI.skin.label)
            {
                fontSize = Math.Max(10, fontSize - 1),
                alignment = TextAnchor.MiddleRight
            };
            s_CardMetaStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastDayMetaColor;

            s_CardColumnHeaderStyle = new(GUI.skin.label)
            {
                fontSize = Math.Max(10, fontSize - 1),
                fontStyle = FontStyle.Bold
            };
            s_CardColumnHeaderStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastColumnHeaderColor;

            s_ForecastRowStyle = new(GUI.skin.box)
            {
                padding = new RectOffset(7, 7, 4, 4),
                margin = new RectOffset(0, 0, 1, 1)
            };
            s_ForecastRowStyle.normal.background = s_ForecastRowBackground;

            s_ForecastRowAlternateStyle = new(s_ForecastRowStyle);
            s_ForecastRowAlternateStyle.normal.background = s_ForecastRowAlternateBackground;

            s_CurrentForecastRowStyle = new(s_ForecastRowStyle);
            s_CurrentForecastRowStyle.normal.background = s_CurrentForecastRowBackground;

            s_FooterStyle = new(GUI.skin.label)
            {
                fontSize = Math.Max(10, fontSize - 1)
            };
            s_FooterStyle.normal.textColor = WeatherOverhaulSettingsManager.ForecastFooterColor;
        }

        private static void RebuildForecastStyleTextures()
        {
            DestroyForecastStyleTexture(s_PanelBackground);
            DestroyForecastStyleTexture(s_TitleBarBackground);
            DestroyForecastStyleTexture(s_PanelBorderBackground);
            DestroyForecastStyleTexture(s_DayCardBackground);
            DestroyForecastStyleTexture(s_ForecastRowBackground);
            DestroyForecastStyleTexture(s_ForecastRowAlternateBackground);
            DestroyForecastStyleTexture(s_CurrentForecastRowBackground);

            s_PanelBackground = CreateForecastStyleTexture("WeatherOverhaul.Forecast.PanelBackground", WeatherOverhaulSettingsManager.ForecastPanelBackgroundColor);
            s_TitleBarBackground = CreateForecastStyleTexture("WeatherOverhaul.Forecast.TitleBar", WeatherOverhaulSettingsManager.ForecastTitleBarColor);
            s_PanelBorderBackground = CreateForecastStyleTexture("WeatherOverhaul.Forecast.PanelBorder", WeatherOverhaulSettingsManager.ForecastPanelBorderColor);
            s_DayCardBackground = CreateForecastStyleTexture("WeatherOverhaul.Forecast.DayCard", WeatherOverhaulSettingsManager.ForecastDayCardColor);
            s_ForecastRowBackground = CreateForecastStyleTexture("WeatherOverhaul.Forecast.Row", WeatherOverhaulSettingsManager.ForecastRowColor);
            s_ForecastRowAlternateBackground = CreateForecastStyleTexture("WeatherOverhaul.Forecast.RowAlternate", WeatherOverhaulSettingsManager.ForecastAlternateRowColor);
            s_CurrentForecastRowBackground = CreateForecastStyleTexture("WeatherOverhaul.Forecast.CurrentRow", WeatherOverhaulSettingsManager.ForecastCurrentRowColor);
        }

        private static void DestroyForecastStyleTexture(Texture2D? texture)
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }

        private static string BuildForecastColorSignature()
        {
            return string.Join("|",
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastPanelTint),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastPanelBackgroundColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastTitleBarColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastPanelBorderColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastWindowTitleColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastRegionTitleColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastInfoTextColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastDayCardColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastDayHeaderColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastDayMetaColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastColumnHeaderColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastRowColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastAlternateRowColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastCurrentRowColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastRowTextColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastFooterColor),
                ForecastColorSignature(WeatherOverhaulSettingsManager.ForecastControlTint));
        }

        private static string ForecastColorSignature(Color color)
        {
            Color32 c = color;
            return $"{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
        }

        private static void ClearGuiStyleBackgrounds(GUIStyle style)
        {
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.onNormal.background = null;
            style.onHover.background = null;
            style.onActive.background = null;
            style.onFocused.background = null;
        }

        private static Texture2D CreateForecastStyleTexture(string name, Color color)
        {
            Texture2D texture = new(1, 1);
            texture.name = name;
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private enum ForecastResizeHandle
        {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private enum WorldMapOverlayGroup
        {
            GreatBear,
            FarTerritory
        }

        private enum MapOverlayEditTarget
        {
            WeatherLabel,
            ClickBox,
            ClickBoxSize
        }

        private sealed class WeatherMapRegionWidget
        {
            internal readonly WeatherRegionId RegionId;
            internal readonly GameObject LabelObject;
            internal readonly GameObject IconObject;
            internal readonly GameObject ClickObject;
            internal readonly GameObject BoxVisualObject;
            internal readonly UILabel? Label;
            internal readonly UITexture? Icon;
            internal readonly UIWidget[] BoxVisualLines;
            internal readonly BoxCollider Collider;

            internal WeatherMapRegionWidget(WeatherRegionId regionId, GameObject labelObject, GameObject iconObject, GameObject clickObject, GameObject boxVisualObject, UILabel? label, UITexture? icon, UIWidget[] boxVisualLines, BoxCollider collider)
            {
                RegionId = regionId;
                LabelObject = labelObject;
                IconObject = iconObject;
                ClickObject = clickObject;
                BoxVisualObject = boxVisualObject;
                Label = label;
                Icon = icon;
                BoxVisualLines = boxVisualLines ?? [];
                Collider = collider;
            }
        }

        private sealed class MapRegionAnchor
        {
            internal readonly WeatherRegionId RegionId;
            internal readonly float LabelX;
            internal readonly float LabelY;
            internal readonly float ClickBoxX;
            internal readonly float ClickBoxY;
            internal readonly float ClickWidth;
            internal readonly float ClickHeight;
            internal Vector2 DefaultLabelPosition => new(LabelX, LabelY);
            internal Vector2 DefaultClickBoxPosition => new(ClickBoxX, ClickBoxY);

            internal MapRegionAnchor(WeatherRegionId regionId, float labelX, float labelY, float clickBoxX, float clickBoxY, float clickWidth, float clickHeight)
            {
                RegionId = regionId;
                LabelX = labelX;
                LabelY = labelY;
                ClickBoxX = clickBoxX;
                ClickBoxY = clickBoxY;
                ClickWidth = clickWidth;
                ClickHeight = clickHeight;
            }
        }
    }
}