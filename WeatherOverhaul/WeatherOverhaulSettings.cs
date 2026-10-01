using WeatherOverhaul.Weather;

namespace WeatherOverhaul
{
    internal enum RegionalSeveritySetting
    {
        Forgiving,
        Normal,
        Harsh,
        Brutal,
        WhoWantsToPlayLikeThis
    }

    internal enum WeatherStageTransitionDuration
    {
        ThirtyMinutes,
        OneHour
    }

    internal enum WeatherStageDurationPreset
    {
        VeryShort,
        Short,
        Normal,
        Long,
        VeryLong,
        ExtremeLong
    }

    internal enum ForecastAccessMode
    {
        Permanent,
        TransmitterNetwork
    }

    internal enum UnmappedRegionMode
    {
        DefaultProfile,
        VanillaWeather
    }

    internal enum WorldMapWeatherStageDisplayMode
    {
        Name,
        Icons,
        NameAndIcons
    }

    internal sealed class WeatherOverhaulSettings : JsonModSettings
    {
        [Section("General")]

        [Name("Enable global simulation")]
        [Description("Enable the independent regional weather simulation, global night events, regional Glimmer Fog events, world-map forecast overlay, and optional loaded-region weather control. Basically the mod's master-switch.")]
        public bool EnableGlobalSimulation = false;

        [Name("Apply global weather to loaded region")]
        [Description("When enabled, WeatherOverhaul applies its simulated forecast to the loaded outdoor region and the World Map displays that regional simulation. When disabled, vanilla weather remains in control and the World Map switches to the loaded region's real vanilla weather and current phase timing instead of displaying WeatherOverhaul's simulated forecast.")]
        public bool ApplyGlobalWeatherToLoadedRegion = false;

        [Name("Unmapped region behavior")]
        [Description("Default: DefaultProfile - Controls regions WeatherOverhaul does not recognize, such as modded custom maps. DefaultProfile gives each unknown outdoor region its own WeatherOverhaul simulation using a generic climate profile. VanillaWeather leaves weather and auroras entirely under vanilla control in unknown regions.")]
        public UnmappedRegionMode UnmappedRegionBehavior = UnmappedRegionMode.DefaultProfile;

        [Name("Weather stage transition duration")]
        [Description("Default: 30 minutes - Duration used by both the vanilla WeatherSet transition and WeatherOverhaul custom effects. Choose either 30 or 60 in-game minutes. Scene transitions still use an immediate apply.")]
        [Choice("30 minutes", "60 minutes")]
        public WeatherStageTransitionDuration WeatherStageTransition = WeatherStageTransitionDuration.ThirtyMinutes;

        [Name("Prepare save for uninstall")]
        [Description("One-shot cleanup. On Confirm, WeatherOverhaul releases debug and custom weather overrides, restores any WeatherSet timing it changed, rebuilds the carried forecast from a clean state, and returns this option to Disabled. Save the game after using it before uninstalling WeatherOverhaul.")]
        public bool PrepareSaveForUninstall = false;

        [Name("Weather stage duration preset")]
        [Description("Default: Normal - Multiplies the average duration of regular simulated weather stages. VeryShort = x0.25, Short = x0.5, Normal = x1, Long = x2, VeryLong = x4, ExtremeLong = x10. Global night-event windows stay tied to their natural night timing. Changing this rebuilds the forecast on Confirm.")]
        public WeatherStageDurationPreset StageDurationPreset = WeatherStageDurationPreset.Normal;


        [Section("Special Events")]

        [Name("Glimmer Fog")]
        [Description("Allow Glimmer Fog event windows to appear in Far Territory regions. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableGlimmerFog = true;

        [Name("Aurora chance")]
        [Description("Default: 10% - Chance that a natural night becomes an Aurora night event. Variant toggles below can turn some successful Aurora nights into Cloudy, Snowy or Foggy Aurora while keeping aurora gameplay active. Changing this rebuilds the forecast on Confirm.")]
        [Slider(0f, 100f, 101, NumberFormat = "{0:0}%")]
        public float AuroraChancePercent = 10f;

        [Name("Cloudy Aurora variant")]
        [Description("Allow some successful Aurora night events to use ClearAurora gameplay with a light cloudy visual overlay. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableCloudyAurora = true;


        [Name("Snowy Aurora variant")]
        [Description("Allow some successful Aurora night events to use the exact Cloudy Aurora profile with light, normally coloured snowfall added. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableSnowyAurora = true;

        [Name("Foggy Aurora variant")]
        [Description("Allow some successful Aurora night events to use the exact Heavy Overcast atmosphere, wind and blowing snow, with falling snow suppressed and full aurora effects active. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableFoggyAurora = true;

        [Name("Blood Moon chance")]
        [Description("Default: 10% - Chance that an eligible natural night becomes a Blood Moon night event. The Clear/Snow variant is chosen after the night succeeds, so 100% means every eligible night is a Blood Moon. Changing this rebuilds the forecast on Confirm.")]
        [Slider(0f, 100f, 101, NumberFormat = "{0:0}%")]
        public float BloodMoonChancePercent = 10f;

        [Name("Blood Moon requires full moon")]
        [Description("Default: Enabled - If enabled, Blood Moon and Snow Blood Moon can only be generated on full-moon nights. If disabled, they can be generated on any natural night. Changing this rebuilds the forecast on Confirm.")]
        public bool BloodMoonRequiresFullMoon = true;

        [Section("Regional Events")]

        [Name("Glimmer Fog chance")]
        [Description("Default: 10% - Daily chance for regional Glimmer Fog in Far Territory regions. Changing this rebuilds the forecast on Confirm.")]
        [Slider(0f, 100f, 101, NumberFormat = "{0:0}%")]
        public float GlimmerFogChancePercent = 10f;


        [Section("Custom Weather Stages")]

        [Name("Custom weather stages")]
        [Description("Show or hide custom weather stage toggles. Clear Aurora is intentionally controlled only by its chance setting above.")]
        [Choice("+", "-")]
        public bool ShowCustomWeatherStages = false;

        [Name("Low Overcast")]
        [Description("Allow Low Overcast to appear in generated regional forecasts. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableLowOvercast = true;

        [Name("Heavy Overcast")]
        [Description("Allow Heavy Overcast to appear in generated regional forecasts. Uses the Heavy Snow atmosphere and wind without falling snow. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableHeavyOvercast = true;

        [Name("Very Heavy Snow")]
        [Description("Allow Very Heavy Snow to appear in generated regional forecasts. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableVeryHeavySnow = true;

        [Name("Whiteout")]
        [Description("Allow Whiteout to appear in generated regional forecasts. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableWhiteout = true;

        [Name("Windy Light Snow")]
        [Description("Allow Windy Light Snow to appear in generated regional forecasts. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableWindyLightSnow = true;

        [Name("Violent Blizzard")]
        [Description("Allow Violent Blizzard to appear in generated regional forecasts. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableViolentBlizzard = true;

        [Name("Very Dense Fog")]
        [Description("Allow Very Dense Fog to appear in generated regional forecasts. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableVeryDenseFog = true;

        [Name("Freezing Fog")]
        [Description("Allow Freezing Fog to appear in generated regional forecasts. Calm Light Fog variant with extreme ambient cold and almost no wind. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableFreezingFog = true;

        [Name("Ashfall")]
        [Description("Allow Ashfall to appear in generated regional forecasts. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableAshfall = true;

        [Name("Blood Moon")]
        [Description("Allow Blood Moon global night events to be generated. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableClearBloodMoon = true;

        [Name("Snow Blood Moon")]
        [Description("Allow Snow Blood Moon global night events to be generated. Changing this rebuilds the forecast on Confirm.")]
        public bool EnableSnowBloodMoon = true;

        [Section("Regional Severity")]

        [Name("Regional severity")]
        [Description("Show or hide regional severity settings. Higher presets push the local forecast toward harsher snow, fog, blizzard and special severe families while reducing clear weather.")]
        [Choice("+", "-")]
        public bool ShowRegionalSeverity = false;

        [Name("Mystery Lake")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting MysteryLakeSeverity = RegionalSeveritySetting.Normal;

        [Name("Coastal Highway")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting CoastalHighwaySeverity = RegionalSeveritySetting.Normal;

        [Name("Pleasant Valley")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting PleasantValleySeverity = RegionalSeveritySetting.Normal;

        [Name("Desolation Point")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting DesolationPointSeverity = RegionalSeveritySetting.Normal;

        [Name("Timberwolf Mountain")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting TimberwolfMountainSeverity = RegionalSeveritySetting.Normal;

        [Name("Forlorn Muskeg")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting ForlornMuskegSeverity = RegionalSeveritySetting.Normal;

        [Name("Broken Railroad")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting BrokenRailroadSeverity = RegionalSeveritySetting.Normal;

        [Name("Mountain Town")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting MountainTownSeverity = RegionalSeveritySetting.Normal;

        [Name("Hushed River Valley")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting HushedRiverValleySeverity = RegionalSeveritySetting.Normal;

        [Name("Bleak Inlet")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting BleakInletSeverity = RegionalSeveritySetting.Normal;

        [Name("Ash Canyon")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting AshCanyonSeverity = RegionalSeveritySetting.Normal;

        [Name("Blackrock Mountain")]
        [Description("Regional severity profile. Also applies to Blackrock Prison. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting BlackrockSeverity = RegionalSeveritySetting.Normal;

        [Name("Transfer Pass")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting TransferPassSeverity = RegionalSeveritySetting.Normal;

        [Name("Forsaken Airfield")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting ForsakenAirfieldSeverity = RegionalSeveritySetting.Normal;

        [Name("Zone of Contamination")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting ZoneOfContaminationSeverity = RegionalSeveritySetting.Normal;

        [Name("Sundered Pass")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting SunderedPassSeverity = RegionalSeveritySetting.Normal;

        [Name("Ravine")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting RavineSeverity = RegionalSeveritySetting.Normal;

        [Name("Winding River")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting WindingRiverSeverity = RegionalSeveritySetting.Normal;

        [Name("Crumbling Highway")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting CrumblingHighwaySeverity = RegionalSeveritySetting.Normal;

        [Name("Keeper's Pass South")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting KeepersPassSouthSeverity = RegionalSeveritySetting.Normal;

        [Name("Keeper's Pass North")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting KeepersPassNorthSeverity = RegionalSeveritySetting.Normal;

        [Name("Far Range Branch Line")]
        [Description("Regional severity profile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting FarRangeBranchLineSeverity = RegionalSeveritySetting.Normal;

        [Name("Unmapped Regions")]
        [Description("Regional severity profile used by unmapped custom regions when Unmapped region behavior is set to DefaultProfile. Changing this rebuilds the forecast on Confirm.")]
        public RegionalSeveritySetting UnmappedRegionsSeverity = RegionalSeveritySetting.Normal;


        [Section("World Map Weather")]

        [Name("Show weather on world map")]
        [Description("Show clickable weather labels on visible region names on the vanilla world map. The overlay switches between Great Bear and Far Territory map anchor sets when the world map changes.")]
        public bool ShowMapWeatherOverlay = true;

        [Name("Forecast access mode")]
        [Description("Permanent = all regional weather and the full forecast horizon are always available. Transmitter Network = only your current local weather is directly observable; repaired vanilla transmitters define regional coverage and unlock 48 hours of forecast depth each, with all 6 unlocking the full horizon. Aurora-powered refresh behavior can be enabled below.")]
        public ForecastAccessMode ForecastAccess = ForecastAccessMode.Permanent;

        [Name("Aurora-powered forecast updates")]
        [Description("Default: On - In Transmitter Network mode, repaired transmitters only refresh player-visible forecast data while an aurora is powering the network. During an aurora the full unlocked depth stays available; when it ends, that same forecast window counts down until it expires. A transmitter repaired after the last aurora must wait for the next aurora to synchronize.")]
        public bool RequireAuroraForTransmitterForecast = true;

        [Name("Forecast font size")]
        [Description("Default: 15 - Base font size used by the regional forecast panel. The panel title and secondary text scale automatically around this value.")]
        [Slider(10f, 24f, 15, NumberFormat = "{0:0}")]
        public float ForecastFontSize = 15f;

        [Name("Forecast panel X (internal)")]
        [Slider(-1f, 10000f, 10002, NumberFormat = "{0:0}")]
        public int ForecastPanelX = 1926;

        [Name("Forecast panel Y (internal)")]
        [Slider(-1f, 10000f, 10002, NumberFormat = "{0:0}")]
        public int ForecastPanelY = 285;

        [Name("Forecast panel width (internal)")]
        [Slider(-1f, 10000f, 10002, NumberFormat = "{0:0}")]
        public int ForecastPanelWidth = 614;

        [Name("Forecast panel height (internal)")]
        [Slider(-1f, 10000f, 10002, NumberFormat = "{0:0}")]
        public int ForecastPanelHeight = 811;



        [Name("Forecast colors")]
        [Description("Show or hide advanced RGB color settings for the regional forecast panel.")]
        [Choice("+", "-")]
        public bool ShowForecastColors = true;

        [Name("Panel tint - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelTintR = 255;
        [Name("Panel tint - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelTintG = 255;
        [Name("Panel tint - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelTintB = 255;

        [Name("Panel background - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBackgroundR = 12;
        [Name("Panel background - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBackgroundG = 12;
        [Name("Panel background - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBackgroundB = 12;
        [Name("Panel background - Opacity")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBackgroundA = 180;

        [Name("Title bar background - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastTitleBarR = 35;
        [Name("Title bar background - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastTitleBarG = 35;
        [Name("Title bar background - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastTitleBarB = 35;
        [Name("Title bar background - Opacity")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastTitleBarA = 242;

        [Name("Panel border - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBorderR = 96;
        [Name("Panel border - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBorderG = 94;
        [Name("Panel border - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBorderB = 84;
        [Name("Panel border - Opacity")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastPanelBorderA = 220;

        [Name("Window title - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastWindowTitleR = 255;
        [Name("Window title - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastWindowTitleG = 255;
        [Name("Window title - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastWindowTitleB = 255;

        [Name("Region title - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRegionTitleR = 235;
        [Name("Region title - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRegionTitleG = 230;
        [Name("Region title - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRegionTitleB = 209;

        [Name("Info text - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastInfoTextR = 215;
        [Name("Info text - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastInfoTextG = 215;
        [Name("Info text - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastInfoTextB = 215;

        [Name("Day card background - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayCardR = 14;
        [Name("Day card background - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayCardG = 14;
        [Name("Day card background - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayCardB = 14;
        [Name("Day card background - Opacity")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayCardA = 199;

        [Name("Day title - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayHeaderR = 235;
        [Name("Day title - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayHeaderG = 230;
        [Name("Day title - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayHeaderB = 209;

        [Name("Day meta - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayMetaR = 148;
        [Name("Day meta - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayMetaG = 145;
        [Name("Day meta - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastDayMetaB = 133;

        [Name("Column headers - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastColumnHeaderR = 168;
        [Name("Column headers - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastColumnHeaderG = 163;
        [Name("Column headers - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastColumnHeaderB = 152;

        [Name("Row background - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRowR = 26;
        [Name("Row background - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRowG = 26;
        [Name("Row background - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRowB = 24;
        [Name("Row background - Opacity")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRowA = 148;

        [Name("Alternate row background - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastAlternateRowR = 37;
        [Name("Alternate row background - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastAlternateRowG = 36;
        [Name("Alternate row background - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastAlternateRowB = 33;
        [Name("Alternate row background - Opacity")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastAlternateRowA = 148;

        [Name("Current row background - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastCurrentRowR = 59;
        [Name("Current row background - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastCurrentRowG = 56;
        [Name("Current row background - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastCurrentRowB = 46;
        [Name("Current row background - Opacity")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastCurrentRowA = 163;

        [Name("Row text - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRowTextR = 204;
        [Name("Row text - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRowTextG = 199;
        [Name("Row text - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastRowTextB = 179;

        [Name("Footer text - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastFooterR = 173;
        [Name("Footer text - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastFooterG = 168;
        [Name("Footer text - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastFooterB = 150;

        [Name("Controls - Red")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastControlR = 255;
        [Name("Controls - Green")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastControlG = 255;
        [Name("Controls - Blue")]
        [Slider(0f, 255f, 256, NumberFormat = "{0:0}")]
        public int ForecastControlB = 255;

        [Name("Show night event in forecast")]
        [Description("Show the next global night event summary near the top of the regional forecast panel.")]
        public bool ShowForecastNightEvent = true;

        [Name("Show Glimmer Fog in forecast")]
        [Description("Show the next regional Glimmer Fog summary near the top of the regional forecast panel.")]
        public bool ShowForecastGlimmerFog = true;

        [Name("Show weather stage")]
        [Description("Choose how the simulated current WeatherStage is shown below each visible world map region label. Name keeps the current text-only display, Icons replaces the name with the stage icon, and Name + Icons shows the icon below the stage name.")]
        public WorldMapWeatherStageDisplayMode MapWeatherStageDisplay = WorldMapWeatherStageDisplayMode.Name;

        [Name("Weather icon height")]
        [Description("Default: 0 - Adjusts the vertical position of weather icons on the world map relative to their default position. Positive values move icons up; negative values move them down.")]
        [Slider(-80f, 80f, 161, NumberFormat = "{0:0}")]
        public float MapWeatherIconYOffset = 0f;

        [Name("Weather icon size")]
        [Description("Default: 28 - Sets the width and height of weather icons displayed on the world map.")]
        [Slider(10f, 80f, 71, NumberFormat = "{0:0}")]
        public float MapWeatherIconSize = 28f;

        [Name("Enable clickable regions")]
        [Description("Add click targets around supported world map region names. Clicking a region opens that region's forecast panel.")]
        public bool ShowMapWeatherClickBoxes = true;

        [Name("Show clickable box outlines")]
        [Description("Show rectangular outlines around the world map forecast click targets. This is only a visual aid; click targets can remain enabled while outlines are hidden.")]
        public bool ShowMapWeatherClickBoxOutlines = false;


        [Section("Advanced")]

        [Name("Debug")]
        [Description("Enable debug hotkeys and the global weather debug UI.")]
        public bool Debug = false;

        [Name("ML Logging")]
        [Description("Add logs for debugging in the ML console.")]
        public bool MLLogging = true;

        [Name("Log current WeatherStage")]
        [Description("Log the current vanilla WeatherStage when ML Logging is enabled.")]
        public bool LogCurrentWeatherStage = true;

        [Name("Log global simulation")]
        [Description("Log scene-region mapping, global simulation rebuilds, current region forecasts, neighboring region forecasts, and vanilla weather transitions when ML Logging is enabled.")]
        public bool LogGlobalWeatherSimulation = true;

        protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
        {
            WeatherOverhaulSettingsManager.RefreshVisibility();
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();
            WeatherOverhaulSettingsManager.ApplyConfirmedSettings();
            WeatherOverhaulSettingsManager.RefreshVisibility();
            WeatherOverhaulRuntime.NotifySettingsConfirmed();
        }
    }

    internal static class WeatherOverhaulSettingsManager
    {
        private sealed class AppliedSettingsSnapshot
        {
            internal bool EnableGlobalSimulation;
            internal bool ShowMapWeatherOverlay;
            internal ForecastAccessMode ForecastAccess;
            internal bool RequireAuroraForTransmitterForecast;
            internal float ForecastFontSize;
            internal Color ForecastPanelTint = Color.white;
            internal Color ForecastPanelBackgroundColor = new Color32(12, 12, 12, 230);
            internal Color ForecastTitleBarColor = new Color32(22, 22, 22, 242);
            internal Color ForecastPanelBorderColor = new Color32(96, 94, 84, 220);
            internal Color ForecastWindowTitleColor = Color.white;
            internal Color ForecastRegionTitleColor = new Color32(235, 230, 209, 255);
            internal Color ForecastInfoTextColor = new Color32(158, 153, 138, 255);
            internal Color ForecastDayCardColor = new Color32(14, 14, 14, 199);
            internal Color ForecastDayHeaderColor = new Color32(235, 230, 209, 255);
            internal Color ForecastDayMetaColor = new Color32(148, 145, 133, 255);
            internal Color ForecastColumnHeaderColor = new Color32(168, 163, 152, 255);
            internal Color ForecastRowColor = new Color32(26, 26, 24, 148);
            internal Color ForecastAlternateRowColor = new Color32(37, 36, 33, 148);
            internal Color ForecastCurrentRowColor = new Color32(59, 56, 46, 163);
            internal Color ForecastRowTextColor = new Color32(204, 199, 179, 255);
            internal Color ForecastFooterColor = new Color32(173, 168, 150, 255);
            internal Color ForecastControlTint = Color.white;
            internal bool ShowForecastNightEvent;
            internal bool ShowForecastGlimmerFog;
            internal WorldMapWeatherStageDisplayMode MapWeatherStageDisplay;
            internal float MapWeatherIconYOffset;
            internal int MapWeatherIconSize;
            internal bool ShowMapWeatherClickBoxes;
            internal bool ShowMapWeatherClickBoxOutlines;
            internal bool ApplyGlobalWeatherToLoadedRegion;
            internal UnmappedRegionMode UnmappedRegionBehavior;
            internal float WeatherStageTransitionHours;
            internal WeatherStageDurationPreset StageDurationPreset;
            internal float StageDurationMultiplier;
            internal float AuroraChancePercent;
            internal bool EnableCloudyAurora;
            internal bool EnableSnowyAurora;
            internal bool EnableFoggyAurora;
            internal float BloodMoonChancePercent;
            internal bool BloodMoonRequiresFullMoon;
            internal float GlimmerFogChancePercent;
            internal bool EnableLowOvercast;
            internal bool EnableHeavyOvercast;
            internal bool EnableVeryHeavySnow;
            internal bool EnableWhiteout;
            internal bool EnableWindyLightSnow;
            internal bool EnableViolentBlizzard;
            internal bool EnableVeryDenseFog;
            internal bool EnableFreezingFog;
            internal bool EnableAshfall;
            internal bool EnableGlimmerFog;
            internal bool EnableClearBloodMoon;
            internal bool EnableSnowBloodMoon;
            internal bool Debug;
            internal bool MLLogging;
            internal bool LogCurrentWeatherStage;
            internal bool LogGlobalWeatherSimulation;
            internal RegionalSeveritySetting MysteryLakeSeverity;
            internal RegionalSeveritySetting CoastalHighwaySeverity;
            internal RegionalSeveritySetting PleasantValleySeverity;
            internal RegionalSeveritySetting DesolationPointSeverity;
            internal RegionalSeveritySetting TimberwolfMountainSeverity;
            internal RegionalSeveritySetting ForlornMuskegSeverity;
            internal RegionalSeveritySetting BrokenRailroadSeverity;
            internal RegionalSeveritySetting MountainTownSeverity;
            internal RegionalSeveritySetting HushedRiverValleySeverity;
            internal RegionalSeveritySetting BleakInletSeverity;
            internal RegionalSeveritySetting AshCanyonSeverity;
            internal RegionalSeveritySetting BlackrockSeverity;
            internal RegionalSeveritySetting TransferPassSeverity;
            internal RegionalSeveritySetting ForsakenAirfieldSeverity;
            internal RegionalSeveritySetting ZoneOfContaminationSeverity;
            internal RegionalSeveritySetting SunderedPassSeverity;
            internal RegionalSeveritySetting RavineSeverity;
            internal RegionalSeveritySetting WindingRiverSeverity;
            internal RegionalSeveritySetting CrumblingHighwaySeverity;
            internal RegionalSeveritySetting KeepersPassSouthSeverity;
            internal RegionalSeveritySetting KeepersPassNorthSeverity;
            internal RegionalSeveritySetting FarRangeBranchLineSeverity;
            internal RegionalSeveritySetting UnmappedRegionsSeverity;
        }

        private static AppliedSettingsSnapshot s_Applied = new AppliedSettingsSnapshot();

        internal static WeatherOverhaulSettings Options { get; private set; } = new WeatherOverhaulSettings();

        internal static bool EnableWeatherOverhaul => true;
        internal static bool EnableGlobalSimulation => s_Applied.EnableGlobalSimulation;
        internal static bool ShowMapWeatherOverlay => s_Applied.ShowMapWeatherOverlay;
        internal static ForecastAccessMode ForecastAccess => s_Applied.ForecastAccess;
        internal static bool RequireAuroraForTransmitterForecast => s_Applied.RequireAuroraForTransmitterForecast;
        internal static int ForecastFontSize => (int)Math.Round(s_Applied.ForecastFontSize);
        internal static Color ForecastPanelTint => s_Applied.ForecastPanelTint;
        internal static Color ForecastPanelBackgroundColor => s_Applied.ForecastPanelBackgroundColor;
        internal static Color ForecastTitleBarColor => s_Applied.ForecastTitleBarColor;
        internal static Color ForecastPanelBorderColor => s_Applied.ForecastPanelBorderColor;
        internal static Color ForecastWindowTitleColor => s_Applied.ForecastWindowTitleColor;
        internal static Color ForecastRegionTitleColor => s_Applied.ForecastRegionTitleColor;
        internal static Color ForecastInfoTextColor => s_Applied.ForecastInfoTextColor;
        internal static Color ForecastDayCardColor => s_Applied.ForecastDayCardColor;
        internal static Color ForecastDayHeaderColor => s_Applied.ForecastDayHeaderColor;
        internal static Color ForecastDayMetaColor => s_Applied.ForecastDayMetaColor;
        internal static Color ForecastColumnHeaderColor => s_Applied.ForecastColumnHeaderColor;
        internal static Color ForecastRowColor => s_Applied.ForecastRowColor;
        internal static Color ForecastAlternateRowColor => s_Applied.ForecastAlternateRowColor;
        internal static Color ForecastCurrentRowColor => s_Applied.ForecastCurrentRowColor;
        internal static Color ForecastRowTextColor => s_Applied.ForecastRowTextColor;
        internal static Color ForecastFooterColor => s_Applied.ForecastFooterColor;
        internal static Color ForecastControlTint => s_Applied.ForecastControlTint;
        internal static bool ShowForecastNightEvent => s_Applied.ShowForecastNightEvent;
        internal static bool ShowForecastGlimmerFog => s_Applied.ShowForecastGlimmerFog;
        internal static WorldMapWeatherStageDisplayMode MapWeatherStageDisplay => s_Applied.MapWeatherStageDisplay;
        internal static float MapWeatherIconYOffset => s_Applied.MapWeatherIconYOffset;
        internal static int MapWeatherIconSize => s_Applied.MapWeatherIconSize;
        internal static bool ShowMapWeatherClickBoxes => s_Applied.ShowMapWeatherClickBoxes;
        internal static bool ShowMapWeatherClickBoxOutlines => s_Applied.ShowMapWeatherClickBoxOutlines;
        internal static bool ApplyGlobalWeatherToLoadedRegion => s_Applied.ApplyGlobalWeatherToLoadedRegion;
        internal static UnmappedRegionMode UnmappedRegionBehavior => s_Applied.UnmappedRegionBehavior;
        internal static bool UseDefaultProfileForUnmappedRegions => UnmappedRegionBehavior == UnmappedRegionMode.DefaultProfile;
        internal static float WeatherStageTransitionHours => s_Applied.WeatherStageTransitionHours;
        internal static WeatherStageDurationPreset StageDurationPreset => s_Applied.StageDurationPreset;
        internal static float StageDurationMultiplier => s_Applied.StageDurationMultiplier;
        internal static float AuroraChancePercent => s_Applied.AuroraChancePercent;
        internal static bool EnableCloudyAurora => s_Applied.EnableCloudyAurora;
        internal static bool EnableSnowyAurora => s_Applied.EnableSnowyAurora;
        internal static bool EnableFoggyAurora => s_Applied.EnableFoggyAurora;
        internal static float BloodMoonChancePercent => s_Applied.BloodMoonChancePercent;
        internal static bool BloodMoonRequiresFullMoon => s_Applied.BloodMoonRequiresFullMoon;
        internal static float GlimmerFogChancePercent => s_Applied.GlimmerFogChancePercent;
        internal static bool EnableLowOvercast => s_Applied.EnableLowOvercast;
        internal static bool EnableHeavyOvercast => s_Applied.EnableHeavyOvercast;
        internal static bool EnableVeryHeavySnow => s_Applied.EnableVeryHeavySnow;
        internal static bool EnableWhiteout => s_Applied.EnableWhiteout;
        internal static bool EnableWindyLightSnow => s_Applied.EnableWindyLightSnow;
        internal static bool EnableViolentBlizzard => s_Applied.EnableViolentBlizzard;
        internal static bool EnableVeryDenseFog => s_Applied.EnableVeryDenseFog;
        internal static bool EnableFreezingFog => s_Applied.EnableFreezingFog;
        internal static bool EnableAshfall => s_Applied.EnableAshfall;
        internal static bool EnableGlimmerFog => s_Applied.EnableGlimmerFog;
        internal static bool EnableClearBloodMoon => s_Applied.EnableClearBloodMoon;
        internal static bool EnableSnowBloodMoon => s_Applied.EnableSnowBloodMoon;
        internal static float AuroraChanceProbability => PercentToProbability(AuroraChancePercent);
        internal static float BloodMoonChanceProbability => PercentToProbability(BloodMoonChancePercent);
        internal static float GlimmerFogChanceProbability => PercentToProbability(GlimmerFogChancePercent);
        internal static string EventSettingsSignature => AuroraChancePercent.ToString("0.#") + "|" + BoolShort(EnableCloudyAurora) + BoolShort(EnableSnowyAurora) + BoolShort(EnableFoggyAurora) + "|" + BloodMoonChancePercent.ToString("0.#") + "|" + BloodMoonRequiresFullMoon + "|" + GlimmerFogChancePercent.ToString("0.#") + "|" + StageDurationPreset + "|" + CustomWeatherStageSignature + "|" + RegionalSeveritySignature;
        internal static string CustomWeatherStageSignature => BoolShort(EnableLowOvercast) + BoolShort(EnableHeavyOvercast) + BoolShort(EnableVeryHeavySnow) + BoolShort(EnableWhiteout) + BoolShort(EnableWindyLightSnow) + BoolShort(EnableViolentBlizzard) + BoolShort(EnableVeryDenseFog) + BoolShort(EnableFreezingFog) + BoolShort(EnableAshfall) + BoolShort(EnableGlimmerFog) + BoolShort(EnableClearBloodMoon) + BoolShort(EnableSnowBloodMoon);
        internal static bool Debug => s_Applied.Debug;
        internal static bool MLLogging => s_Applied.MLLogging;
        internal static bool LogCurrentWeatherStage => MLLogging && s_Applied.LogCurrentWeatherStage;
        internal static bool LogGlobalWeatherSimulation => MLLogging && s_Applied.LogGlobalWeatherSimulation;

        internal static string RegionalSeveritySignature =>
            ToShort(s_Applied.MysteryLakeSeverity) + ToShort(s_Applied.CoastalHighwaySeverity) + ToShort(s_Applied.PleasantValleySeverity) + ToShort(s_Applied.DesolationPointSeverity) +
            ToShort(s_Applied.TimberwolfMountainSeverity) + ToShort(s_Applied.ForlornMuskegSeverity) + ToShort(s_Applied.BrokenRailroadSeverity) + ToShort(s_Applied.MountainTownSeverity) +
            ToShort(s_Applied.HushedRiverValleySeverity) + ToShort(s_Applied.BleakInletSeverity) + ToShort(s_Applied.AshCanyonSeverity) + ToShort(s_Applied.BlackrockSeverity) +
            ToShort(s_Applied.TransferPassSeverity) + ToShort(s_Applied.ForsakenAirfieldSeverity) + ToShort(s_Applied.ZoneOfContaminationSeverity) + ToShort(s_Applied.SunderedPassSeverity) +
            ToShort(s_Applied.RavineSeverity) + ToShort(s_Applied.WindingRiverSeverity) + ToShort(s_Applied.CrumblingHighwaySeverity) + ToShort(s_Applied.KeepersPassSouthSeverity) +
            ToShort(s_Applied.KeepersPassNorthSeverity) + ToShort(s_Applied.FarRangeBranchLineSeverity) + ToShort(s_Applied.UnmappedRegionsSeverity);

        private static Color MakeForecastColor(int r, int g, int b, int a = 255)
        {
            return new Color32(
                (byte)Math.Clamp(r, 0, 255),
                (byte)Math.Clamp(g, 0, 255),
                (byte)Math.Clamp(b, 0, 255),
                (byte)Math.Clamp(a, 0, 255));
        }

        internal static void Initialize()
        {
            Options.AddToModSettings("WeatherOverhaul");
            ApplyConfirmedSettings();
            RefreshVisibility();
        }

        internal static bool TryGetForecastPanelLayout(out Rect rect)
        {
            if (Options.ForecastPanelX < 0 || Options.ForecastPanelY < 0 || Options.ForecastPanelWidth <= 0 || Options.ForecastPanelHeight <= 0)
            {
                rect = default;
                return false;
            }

            rect = new Rect(Options.ForecastPanelX, Options.ForecastPanelY, Options.ForecastPanelWidth, Options.ForecastPanelHeight);
            return true;
        }

        internal static void SaveForecastPanelLayout(Rect rect)
        {
            int x = Mathf.RoundToInt(rect.x);
            int y = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            if (Options.ForecastPanelX == x && Options.ForecastPanelY == y && Options.ForecastPanelWidth == width && Options.ForecastPanelHeight == height) return;

            Options.ForecastPanelX = x;
            Options.ForecastPanelY = y;
            Options.ForecastPanelWidth = width;
            Options.ForecastPanelHeight = height;
            Options.Save();
        }

        internal static void ApplyConfirmedSettings()
        {
            s_Applied = new AppliedSettingsSnapshot
            {
                EnableGlobalSimulation = Options.EnableGlobalSimulation,
                ShowMapWeatherOverlay = Options.ShowMapWeatherOverlay,
                ForecastAccess = Options.ForecastAccess,
                RequireAuroraForTransmitterForecast = Options.RequireAuroraForTransmitterForecast,
                ForecastFontSize = ClampAndRoundWholeHours(Options.ForecastFontSize, 10f, 24f),
                ForecastPanelTint = MakeForecastColor(Options.ForecastPanelTintR, Options.ForecastPanelTintG, Options.ForecastPanelTintB),
                ForecastPanelBackgroundColor = MakeForecastColor(Options.ForecastPanelBackgroundR, Options.ForecastPanelBackgroundG, Options.ForecastPanelBackgroundB, Options.ForecastPanelBackgroundA),
                ForecastTitleBarColor = MakeForecastColor(Options.ForecastTitleBarR, Options.ForecastTitleBarG, Options.ForecastTitleBarB, Options.ForecastTitleBarA),
                ForecastPanelBorderColor = MakeForecastColor(Options.ForecastPanelBorderR, Options.ForecastPanelBorderG, Options.ForecastPanelBorderB, Options.ForecastPanelBorderA),
                ForecastWindowTitleColor = MakeForecastColor(Options.ForecastWindowTitleR, Options.ForecastWindowTitleG, Options.ForecastWindowTitleB),
                ForecastRegionTitleColor = MakeForecastColor(Options.ForecastRegionTitleR, Options.ForecastRegionTitleG, Options.ForecastRegionTitleB),
                ForecastInfoTextColor = MakeForecastColor(Options.ForecastInfoTextR, Options.ForecastInfoTextG, Options.ForecastInfoTextB),
                ForecastDayCardColor = MakeForecastColor(Options.ForecastDayCardR, Options.ForecastDayCardG, Options.ForecastDayCardB, Options.ForecastDayCardA),
                ForecastDayHeaderColor = MakeForecastColor(Options.ForecastDayHeaderR, Options.ForecastDayHeaderG, Options.ForecastDayHeaderB),
                ForecastDayMetaColor = MakeForecastColor(Options.ForecastDayMetaR, Options.ForecastDayMetaG, Options.ForecastDayMetaB),
                ForecastColumnHeaderColor = MakeForecastColor(Options.ForecastColumnHeaderR, Options.ForecastColumnHeaderG, Options.ForecastColumnHeaderB),
                ForecastRowColor = MakeForecastColor(Options.ForecastRowR, Options.ForecastRowG, Options.ForecastRowB, Options.ForecastRowA),
                ForecastAlternateRowColor = MakeForecastColor(Options.ForecastAlternateRowR, Options.ForecastAlternateRowG, Options.ForecastAlternateRowB, Options.ForecastAlternateRowA),
                ForecastCurrentRowColor = MakeForecastColor(Options.ForecastCurrentRowR, Options.ForecastCurrentRowG, Options.ForecastCurrentRowB, Options.ForecastCurrentRowA),
                ForecastRowTextColor = MakeForecastColor(Options.ForecastRowTextR, Options.ForecastRowTextG, Options.ForecastRowTextB),
                ForecastFooterColor = MakeForecastColor(Options.ForecastFooterR, Options.ForecastFooterG, Options.ForecastFooterB),
                ForecastControlTint = MakeForecastColor(Options.ForecastControlR, Options.ForecastControlG, Options.ForecastControlB),
                ShowForecastNightEvent = Options.ShowForecastNightEvent,
                ShowForecastGlimmerFog = Options.ShowForecastGlimmerFog,
                MapWeatherStageDisplay = Options.MapWeatherStageDisplay,
                MapWeatherIconYOffset = ClampAndRoundWholeHours(Options.MapWeatherIconYOffset, -80f, 80f),
                MapWeatherIconSize = (int)ClampAndRoundWholeHours(Options.MapWeatherIconSize, 10f, 80f),
                ShowMapWeatherClickBoxes = Options.ShowMapWeatherClickBoxes,
                ShowMapWeatherClickBoxOutlines = Options.ShowMapWeatherClickBoxOutlines,
                ApplyGlobalWeatherToLoadedRegion = Options.ApplyGlobalWeatherToLoadedRegion,
                UnmappedRegionBehavior = Options.UnmappedRegionBehavior,
                WeatherStageTransitionHours = GetTransitionHours(Options.WeatherStageTransition),
                StageDurationPreset = Options.StageDurationPreset,
                StageDurationMultiplier = GetStageDurationMultiplier(Options.StageDurationPreset),
                AuroraChancePercent = ClampAndRoundPercent(Options.AuroraChancePercent, 0f, 100f),
                EnableCloudyAurora = Options.EnableCloudyAurora,
                EnableSnowyAurora = Options.EnableSnowyAurora,
                EnableFoggyAurora = Options.EnableFoggyAurora,
                BloodMoonChancePercent = ClampAndRoundPercent(Options.BloodMoonChancePercent, 0f, 100f),
                BloodMoonRequiresFullMoon = Options.BloodMoonRequiresFullMoon,
                GlimmerFogChancePercent = ClampAndRoundPercent(Options.GlimmerFogChancePercent, 0f, 100f),
                EnableLowOvercast = Options.EnableLowOvercast,
                EnableHeavyOvercast = Options.EnableHeavyOvercast,
                EnableVeryHeavySnow = Options.EnableVeryHeavySnow,
                EnableWhiteout = Options.EnableWhiteout,
                EnableWindyLightSnow = Options.EnableWindyLightSnow,
                EnableViolentBlizzard = Options.EnableViolentBlizzard,
                EnableVeryDenseFog = Options.EnableVeryDenseFog,
                EnableFreezingFog = Options.EnableFreezingFog,
                EnableAshfall = Options.EnableAshfall,
                EnableGlimmerFog = Options.EnableGlimmerFog,
                EnableClearBloodMoon = Options.EnableClearBloodMoon,
                EnableSnowBloodMoon = Options.EnableSnowBloodMoon,
                Debug = Options.Debug,
                MLLogging = Options.MLLogging,
                LogCurrentWeatherStage = Options.LogCurrentWeatherStage,
                LogGlobalWeatherSimulation = Options.LogGlobalWeatherSimulation,
                MysteryLakeSeverity = Options.MysteryLakeSeverity,
                CoastalHighwaySeverity = Options.CoastalHighwaySeverity,
                PleasantValleySeverity = Options.PleasantValleySeverity,
                DesolationPointSeverity = Options.DesolationPointSeverity,
                TimberwolfMountainSeverity = Options.TimberwolfMountainSeverity,
                ForlornMuskegSeverity = Options.ForlornMuskegSeverity,
                BrokenRailroadSeverity = Options.BrokenRailroadSeverity,
                MountainTownSeverity = Options.MountainTownSeverity,
                HushedRiverValleySeverity = Options.HushedRiverValleySeverity,
                BleakInletSeverity = Options.BleakInletSeverity,
                AshCanyonSeverity = Options.AshCanyonSeverity,
                BlackrockSeverity = Options.BlackrockSeverity,
                TransferPassSeverity = Options.TransferPassSeverity,
                ForsakenAirfieldSeverity = Options.ForsakenAirfieldSeverity,
                ZoneOfContaminationSeverity = Options.ZoneOfContaminationSeverity,
                SunderedPassSeverity = Options.SunderedPassSeverity,
                RavineSeverity = Options.RavineSeverity,
                WindingRiverSeverity = Options.WindingRiverSeverity,
                CrumblingHighwaySeverity = Options.CrumblingHighwaySeverity,
                KeepersPassSouthSeverity = Options.KeepersPassSouthSeverity,
                KeepersPassNorthSeverity = Options.KeepersPassNorthSeverity,
                FarRangeBranchLineSeverity = Options.FarRangeBranchLineSeverity,
                UnmappedRegionsSeverity = Options.UnmappedRegionsSeverity
            };
        }

        internal static RegionalSeveritySetting GetRegionalSeverity(WeatherRegionId regionId)
        {
            if (RegionWeatherGraph.IsDynamicRegion(regionId)) return s_Applied.UnmappedRegionsSeverity;

            switch (regionId)
            {
                case WeatherRegionId.ML: return s_Applied.MysteryLakeSeverity;
                case WeatherRegionId.CH: return s_Applied.CoastalHighwaySeverity;
                case WeatherRegionId.PV: return s_Applied.PleasantValleySeverity;
                case WeatherRegionId.DP: return s_Applied.DesolationPointSeverity;
                case WeatherRegionId.TWM: return s_Applied.TimberwolfMountainSeverity;
                case WeatherRegionId.FM: return s_Applied.ForlornMuskegSeverity;
                case WeatherRegionId.BR: return s_Applied.BrokenRailroadSeverity;
                case WeatherRegionId.MT: return s_Applied.MountainTownSeverity;
                case WeatherRegionId.HRV: return s_Applied.HushedRiverValleySeverity;
                case WeatherRegionId.BI: return s_Applied.BleakInletSeverity;
                case WeatherRegionId.AC: return s_Applied.AshCanyonSeverity;
                case WeatherRegionId.BRM:
                case WeatherRegionId.BRM_Prison: return s_Applied.BlackrockSeverity;
                case WeatherRegionId.TP: return s_Applied.TransferPassSeverity;
                case WeatherRegionId.FA: return s_Applied.ForsakenAirfieldSeverity;
                case WeatherRegionId.ZOC: return s_Applied.ZoneOfContaminationSeverity;
                case WeatherRegionId.SP: return s_Applied.SunderedPassSeverity;
                case WeatherRegionId.Rav: return s_Applied.RavineSeverity;
                case WeatherRegionId.WR: return s_Applied.WindingRiverSeverity;
                case WeatherRegionId.CRH: return s_Applied.CrumblingHighwaySeverity;
                case WeatherRegionId.KP_South: return s_Applied.KeepersPassSouthSeverity;
                case WeatherRegionId.KP_North: return s_Applied.KeepersPassNorthSeverity;
                case WeatherRegionId.FRBL: return s_Applied.FarRangeBranchLineSeverity;
                default: return RegionalSeveritySetting.Normal;
            }
        }

        internal static void RefreshVisibility()
        {
            UpdateGlobalSimulationVisibility();
            UpdateWorldMapVisibility();
            UpdateLoggingVisibility();
        }

        private static void UpdateGlobalSimulationVisibility()
        {
            bool showGlobalSimulationSettings = Options.EnableGlobalSimulation;
            bool showLoadedRegionApplySettings = showGlobalSimulationSettings && Options.ApplyGlobalWeatherToLoadedRegion;

            Options.SetFieldVisible(nameof(Options.ApplyGlobalWeatherToLoadedRegion), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.UnmappedRegionBehavior), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.WeatherStageTransition), showLoadedRegionApplySettings);
            Options.SetFieldVisible(nameof(Options.PrepareSaveForUninstall), true);
            Options.SetFieldVisible(nameof(Options.StageDurationPreset), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.AuroraChancePercent), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.EnableCloudyAurora), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.EnableSnowyAurora), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.EnableFoggyAurora), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.BloodMoonChancePercent), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.BloodMoonRequiresFullMoon), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.GlimmerFogChancePercent), showGlobalSimulationSettings);
            bool showCustomWeatherStages = showGlobalSimulationSettings && Options.ShowCustomWeatherStages;
            Options.SetFieldVisible(nameof(Options.ShowCustomWeatherStages), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.EnableLowOvercast), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableHeavyOvercast), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableVeryHeavySnow), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableWhiteout), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableWindyLightSnow), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableViolentBlizzard), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableVeryDenseFog), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableFreezingFog), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableAshfall), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableGlimmerFog), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableClearBloodMoon), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.EnableSnowBloodMoon), showCustomWeatherStages);
            Options.SetFieldVisible(nameof(Options.ShowMapWeatherOverlay), showGlobalSimulationSettings);
            bool showRegionalSeverity = showGlobalSimulationSettings && Options.ShowRegionalSeverity;
            Options.SetFieldVisible(nameof(Options.ShowRegionalSeverity), showGlobalSimulationSettings);
            Options.SetFieldVisible(nameof(Options.MysteryLakeSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.CoastalHighwaySeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.PleasantValleySeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.DesolationPointSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.TimberwolfMountainSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.ForlornMuskegSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.BrokenRailroadSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.MountainTownSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.HushedRiverValleySeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.BleakInletSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.AshCanyonSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.BlackrockSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.TransferPassSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.ForsakenAirfieldSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.ZoneOfContaminationSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.SunderedPassSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.RavineSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.WindingRiverSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.CrumblingHighwaySeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.KeepersPassSouthSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.KeepersPassNorthSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.FarRangeBranchLineSeverity), showRegionalSeverity);
            Options.SetFieldVisible(nameof(Options.UnmappedRegionsSeverity), showRegionalSeverity);
        }

        private static void UpdateWorldMapVisibility()
        {
            bool showMapSettings = Options.EnableGlobalSimulation && Options.ShowMapWeatherOverlay;
            bool showSimulatedForecastSettings = showMapSettings && Options.ApplyGlobalWeatherToLoadedRegion;
            bool showClickBoxOutlineSetting = showMapSettings && Options.ShowMapWeatherClickBoxes;
            bool showWeatherIconSettings = showMapSettings && Options.MapWeatherStageDisplay != WorldMapWeatherStageDisplayMode.Name;

            Options.SetFieldVisible(nameof(Options.ForecastAccess), showSimulatedForecastSettings);
            bool showAuroraForecastSetting = showSimulatedForecastSettings && Options.ForecastAccess == ForecastAccessMode.TransmitterNetwork;
            Options.SetFieldVisible(nameof(Options.RequireAuroraForTransmitterForecast), showAuroraForecastSetting);
            Options.SetFieldVisible(nameof(Options.ForecastFontSize), showMapSettings);
            Options.SetFieldVisible(nameof(Options.ForecastPanelX), false);
            Options.SetFieldVisible(nameof(Options.ForecastPanelY), false);
            Options.SetFieldVisible(nameof(Options.ForecastPanelWidth), false);
            Options.SetFieldVisible(nameof(Options.ForecastPanelHeight), false);
            bool showForecastColors = showMapSettings && Options.ShowForecastColors;
            Options.SetFieldVisible(nameof(Options.ShowForecastColors), showMapSettings);
            Options.SetFieldVisible(nameof(Options.ForecastPanelTintR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelTintG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelTintB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBackgroundR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBackgroundG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBackgroundB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBackgroundA), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastTitleBarR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastTitleBarG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastTitleBarB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastTitleBarA), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBorderR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBorderG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBorderB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastPanelBorderA), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastWindowTitleR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastWindowTitleG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastWindowTitleB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRegionTitleR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRegionTitleG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRegionTitleB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastInfoTextR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastInfoTextG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastInfoTextB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayCardR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayCardG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayCardB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayCardA), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayHeaderR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayHeaderG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayHeaderB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayMetaR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayMetaG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastDayMetaB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastColumnHeaderR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastColumnHeaderG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastColumnHeaderB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRowR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRowG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRowB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRowA), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastAlternateRowR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastAlternateRowG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastAlternateRowB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastAlternateRowA), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastCurrentRowR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastCurrentRowG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastCurrentRowB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastCurrentRowA), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRowTextR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRowTextG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastRowTextB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastFooterR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastFooterG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastFooterB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastControlR), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastControlG), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ForecastControlB), showForecastColors);
            Options.SetFieldVisible(nameof(Options.ShowForecastNightEvent), showSimulatedForecastSettings);
            Options.SetFieldVisible(nameof(Options.ShowForecastGlimmerFog), showSimulatedForecastSettings);
            Options.SetFieldVisible(nameof(Options.MapWeatherStageDisplay), showMapSettings);
            Options.SetFieldVisible(nameof(Options.MapWeatherIconYOffset), showWeatherIconSettings);
            Options.SetFieldVisible(nameof(Options.MapWeatherIconSize), showWeatherIconSettings);
            Options.SetFieldVisible(nameof(Options.ShowMapWeatherClickBoxes), showMapSettings);
            Options.SetFieldVisible(nameof(Options.ShowMapWeatherClickBoxOutlines), showClickBoxOutlineSetting);
        }

        private static void UpdateLoggingVisibility()
        {
            bool showLoggingSettings = Options.MLLogging;

            Options.SetFieldVisible(nameof(Options.LogCurrentWeatherStage), showLoggingSettings);
            Options.SetFieldVisible(nameof(Options.LogGlobalWeatherSimulation), showLoggingSettings);
        }

        internal static bool IsWeatherFamilyEnabled(WeatherFamily family)
        {
            switch (family)
            {
                case WeatherFamily.LowOvercast: return EnableLowOvercast;
                case WeatherFamily.HeavyOvercast: return EnableHeavyOvercast;
                case WeatherFamily.VeryHeavySnow: return EnableVeryHeavySnow;
                case WeatherFamily.Whiteout: return EnableWhiteout;
                case WeatherFamily.WindyLightSnow: return EnableWindyLightSnow;
                case WeatherFamily.ViolentBlizzard: return EnableViolentBlizzard;
                case WeatherFamily.VeryDenseFog: return EnableVeryDenseFog;
                case WeatherFamily.FreezingFog: return EnableFreezingFog;
                case WeatherFamily.Ashfall: return EnableAshfall;
                case WeatherFamily.ElectrostaticFog: return EnableGlimmerFog;
                case WeatherFamily.CloudyAurora: return EnableCloudyAurora;
                case WeatherFamily.SnowyAurora: return EnableSnowyAurora;
                case WeatherFamily.FoggyAurora: return EnableFoggyAurora;
                case WeatherFamily.ClearBloodMoon: return EnableClearBloodMoon;
                case WeatherFamily.LightSnowBloodMoon: return EnableSnowBloodMoon;
                default: return true;
            }
        }

        private static float GetStageDurationMultiplier(WeatherStageDurationPreset preset)
        {
            switch (preset)
            {
                case WeatherStageDurationPreset.VeryShort: return 0.25f;
                case WeatherStageDurationPreset.Short: return 0.5f;
                case WeatherStageDurationPreset.Long: return 2f;
                case WeatherStageDurationPreset.VeryLong: return 4f;
                case WeatherStageDurationPreset.ExtremeLong: return 10f;
                default: return 1f;
            }
        }

        private static float GetTransitionHours(WeatherStageTransitionDuration duration)
        {
            return duration == WeatherStageTransitionDuration.OneHour ? 1f : 0.5f;
        }

        internal static bool ConsumePrepareForUninstallRequest()
        {
            if (!Options.PrepareSaveForUninstall) return false;
            Options.PrepareSaveForUninstall = false;
            Options.Save();
            return true;
        }

        private static float ClampAndRoundPercent(float value, float min, float max)
        {
            float clamped = Math.Max(min, Math.Min(max, value));
            return (float)Math.Round(clamped);
        }

        private static float ClampAndRoundWholeHours(float value, float min, float max)
        {
            float clamped = Math.Max(min, Math.Min(max, value));
            return (float)Math.Round(clamped);
        }

        private static float PercentToProbability(float percent)
        {
            return Math.Max(0f, Math.Min(1f, percent / 100f));
        }

        private static string ToShort(RegionalSeveritySetting severity)
        {
            return ((int)severity).ToString();
        }

        private static string BoolShort(bool value)
        {
            return value ? "1" : "0";
        }
    }
}