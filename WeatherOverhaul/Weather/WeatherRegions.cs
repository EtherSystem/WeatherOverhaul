using System.Text;

namespace WeatherOverhaul.Weather
{
    internal enum WeatherRegionId
    {
        Unknown, SP, ZOC, FA, TP, FRBL, BR, FM, MT, HRV, ML, WR, BI, Rav, CH, CRH, DP, PV, KP_South, KP_North, BRM, BRM_Prison, TWM, AC
    }

    internal sealed class WeatherRegionDefinition
    {
        internal WeatherRegionId Id { get; }
        internal string ShortName { get; }
        internal string DisplayName { get; }
        internal string SceneName { get; }
        internal bool AllowsElectrostaticFog { get; }
        internal bool IsDynamic { get; }
        internal IReadOnlyList<WeatherRegionId> Neighbors { get; }

        internal WeatherRegionDefinition(WeatherRegionId id, string shortName, string displayName, string sceneName, bool allowsElectrostaticFog, params WeatherRegionId[] neighbors)
            : this(id, shortName, displayName, sceneName, allowsElectrostaticFog, false, neighbors)
        {
        }

        internal WeatherRegionDefinition(WeatherRegionId id, string shortName, string displayName, string sceneName, bool allowsElectrostaticFog, bool isDynamic, params WeatherRegionId[] neighbors)
        {
            Id = id;
            ShortName = shortName;
            DisplayName = displayName;
            SceneName = sceneName;
            AllowsElectrostaticFog = allowsElectrostaticFog;
            IsDynamic = isDynamic;
            Neighbors = neighbors;
        }
    }

    internal static class RegionWeatherGraph
    {
        private static readonly Dictionary<WeatherRegionId, WeatherRegionDefinition> s_ById = new Dictionary<WeatherRegionId, WeatherRegionDefinition>();
        private static readonly Dictionary<string, WeatherRegionId> s_BySceneName = new Dictionary<string, WeatherRegionId>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, WeatherRegionId> s_ByLooseName = new Dictionary<string, WeatherRegionId>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<WeatherRegionDefinition> s_Regions = new List<WeatherRegionDefinition>();

        static RegionWeatherGraph()
        {
            Register(new WeatherRegionDefinition(WeatherRegionId.SP, "SP", "Sundered Pass", "MountainPassRegion", true, WeatherRegionId.ZOC, WeatherRegionId.TP, WeatherRegionId.FA), "SunderedPass", "MountainPass");
            Register(new WeatherRegionDefinition(WeatherRegionId.ZOC, "ZOC", "Zone of Contamination", "MiningRegion", true, WeatherRegionId.TP, WeatherRegionId.SP, WeatherRegionId.FA), "ZoneOfContamination", "Mining");
            Register(new WeatherRegionDefinition(WeatherRegionId.FA, "FA", "Forsaken Airfield", "AirfieldRegion", true, WeatherRegionId.SP, WeatherRegionId.ZOC, WeatherRegionId.TP), "ForsakenAirfield", "Airfield");
            Register(new WeatherRegionDefinition(WeatherRegionId.TP, "TP", "Transfer Pass", "HubRegion", true, WeatherRegionId.SP, WeatherRegionId.ZOC, WeatherRegionId.FA, WeatherRegionId.FRBL), "TransferPass", "Hub");
            Register(new WeatherRegionDefinition(WeatherRegionId.FRBL, "FRBL", "Far Range Branch Line", "LongRailTransitionZone", true, WeatherRegionId.TP, WeatherRegionId.BR), "FarRangeBranchLine", "LongRailTransition", "LongRail");
            Register(new WeatherRegionDefinition(WeatherRegionId.BR, "BR", "Broken Railroad", "TracksRegion", false, WeatherRegionId.FRBL, WeatherRegionId.FM), "BrokenRailroad", "Tracks");
            Register(new WeatherRegionDefinition(WeatherRegionId.FM, "FM", "Forlorn Muskeg", "MarshRegion", false, WeatherRegionId.BR, WeatherRegionId.MT, WeatherRegionId.ML, WeatherRegionId.BI), "ForlornMuskeg", "Marsh");
            Register(new WeatherRegionDefinition(WeatherRegionId.MT, "MT", "Mountain Town", "MountainTownRegion", false, WeatherRegionId.HRV, WeatherRegionId.FM, WeatherRegionId.ML), "MountainTown", "Milton");
            Register(new WeatherRegionDefinition(WeatherRegionId.HRV, "HRV", "Hushed River Valley", "RiverValleyRegion", false, WeatherRegionId.MT), "HushedRiverValley", "RiverValley");
            Register(new WeatherRegionDefinition(WeatherRegionId.ML, "ML", "Mystery Lake", "LakeRegion", false, WeatherRegionId.MT, WeatherRegionId.FM, WeatherRegionId.Rav, WeatherRegionId.WR), "MysteryLake", "Lake");
            Register(new WeatherRegionDefinition(WeatherRegionId.WR, "WR", "Winding River", "DamRiverTransitionZoneB", false, WeatherRegionId.ML, WeatherRegionId.PV), "WindingRiver", "DamRiverTransitionZoneB", "DamRiver");
            Register(new WeatherRegionDefinition(WeatherRegionId.BI, "BI", "Bleak Inlet", "CanneryRegion", false, WeatherRegionId.FM, WeatherRegionId.Rav), "BleakInlet", "Cannery");
            Register(new WeatherRegionDefinition(WeatherRegionId.Rav, "Rav", "Ravine", "RavineTransitionZone", false, WeatherRegionId.BI, WeatherRegionId.ML, WeatherRegionId.CH), "Ravine");
            Register(new WeatherRegionDefinition(WeatherRegionId.CH, "CH", "Coastal Highway", "CoastalRegion", false, WeatherRegionId.Rav, WeatherRegionId.PV, WeatherRegionId.CRH), "CoastalHighway", "Coastal");
            Register(new WeatherRegionDefinition(WeatherRegionId.CRH, "CRH", "Crumbling Highway", "HighwayTransitionZone", false, WeatherRegionId.CH, WeatherRegionId.DP), "CrumblingHighway", "OldIslandConnector", "HighwayTransition");
            Register(new WeatherRegionDefinition(WeatherRegionId.DP, "DP", "Desolation Point", "WhalingStationRegion", false, WeatherRegionId.CRH), "DesolationPoint", "WhalingStation");
            Register(new WeatherRegionDefinition(WeatherRegionId.PV, "PV", "Pleasant Valley", "RuralRegion", false, WeatherRegionId.CH, WeatherRegionId.WR, WeatherRegionId.KP_South, WeatherRegionId.TWM), "PleasantValley", "Rural");
            Register(new WeatherRegionDefinition(WeatherRegionId.KP_South, "KP-S", "Keeper's Pass South", "CanyonRoadTransitionZone", false, WeatherRegionId.KP_North, WeatherRegionId.PV), "KeepersPassSouth", "KeeperPassSouth", "CanyonRoadTransition", "CanyonRoad");
            Register(new WeatherRegionDefinition(WeatherRegionId.KP_North, "KP-N", "Keeper's Pass North", "BlackrockTransitionZone", false, WeatherRegionId.KP_South, WeatherRegionId.BRM), "KeepersPassNorth", "KeeperPassNorth", "BlackrockTransition");
            Register(new WeatherRegionDefinition(WeatherRegionId.BRM, "BRM", "Blackrock Mountain", "BlackrockRegion", false, WeatherRegionId.KP_North, WeatherRegionId.TWM), "BlackrockMountain", "Blackrock", "BlackrockPrison", "Prison");
            RegisterSceneAlias(WeatherRegionId.BRM, "BlackrockPrisonSurvivalZone");
            Register(new WeatherRegionDefinition(WeatherRegionId.TWM, "TWM", "Timberwolf Mountain", "CrashMountainRegion", false, WeatherRegionId.AC, WeatherRegionId.PV, WeatherRegionId.BRM), "TimberwolfMountain", "CrashMountain");
            Register(new WeatherRegionDefinition(WeatherRegionId.AC, "AC", "Ash Canyon", "AshCanyonRegion", false, WeatherRegionId.TWM), "AshCanyon");
        }

        internal static IReadOnlyList<WeatherRegionDefinition> Regions => s_Regions;

        internal static bool IsDynamicRegion(WeatherRegionId id)
        {
            return s_ById.TryGetValue(id, out WeatherRegionDefinition definition) && definition.IsDynamic;
        }

        internal static WeatherRegionDefinition GetOrCreateDynamicRegion(string sceneName, string regionName)
        {
            if (TryGetBySceneName(sceneName, out WeatherRegionDefinition existing) && existing.IsDynamic) return existing;

            string identity = sceneName;
            if (string.IsNullOrWhiteSpace(identity)) return Get(WeatherRegionId.Unknown);

            WeatherRegionId id = BuildDynamicRegionId(identity);
            if (s_ById.TryGetValue(id, out existing))
            {
                if (existing.IsDynamic && string.Equals(existing.SceneName, sceneName, StringComparison.OrdinalIgnoreCase)) return existing;

                Core.Warn($"[RegionMap] Dynamic region id collision for '{identity}'. Falling back to vanilla weather for scene '{sceneName}'.");
                return Get(WeatherRegionId.Unknown);
            }

            string displayName = BuildDynamicDisplayName(string.IsNullOrWhiteSpace(regionName) ? sceneName : regionName);
            string shortName = "EXT-" + ((int)id & 0xFFFF).ToString("X4");
            WeatherRegionDefinition definition = new(id, shortName, displayName, sceneName, false, true);
            Register(definition, regionName);
            Core.Log($"[RegionMap] Registered unmapped outdoor region '{displayName}' as {shortName} using the Default weather profile.");
            return definition;
        }

        internal static bool RestoreDynamicRegion(WeatherRegionId id, string sceneName, string displayName, string shortName)
        {
            if (id == WeatherRegionId.Unknown || string.IsNullOrWhiteSpace(sceneName)) return false;
            if (s_ById.TryGetValue(id, out WeatherRegionDefinition existing))
            {
                return existing.IsDynamic && string.Equals(existing.SceneName, sceneName, StringComparison.OrdinalIgnoreCase);
            }

            if (s_BySceneName.TryGetValue(sceneName, out WeatherRegionId sceneRegionId) && sceneRegionId != id) return false;
            string safeDisplayName = string.IsNullOrWhiteSpace(displayName) ? BuildDynamicDisplayName(sceneName) : displayName;
            string safeShortName = string.IsNullOrWhiteSpace(shortName) ? "EXT-" + ((int)id & 0xFFFF).ToString("X4") : shortName;
            Register(new WeatherRegionDefinition(id, safeShortName, safeDisplayName, sceneName, false, true));
            Core.Log($"[RegionMap] Restored dynamic region definition '{safeDisplayName}' as {safeShortName} from persisted forecast metadata.");
            return true;
        }

        private static WeatherRegionId BuildDynamicRegionId(string identity)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string normalized = NormalizeAlias(identity);
                for (int i = 0; i < normalized.Length; i++)
                {
                    hash ^= normalized[i];
                    hash *= 16777619u;
                }

                int value = 1000000 + (int)(hash & 0x3FFFFFFF);
                return (WeatherRegionId)value;
            }
        }

        private static string BuildDynamicDisplayName(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity)) return "Unmapped Region";
            string value = identity.Trim();
            if (value.EndsWith("Region", StringComparison.OrdinalIgnoreCase) && value.Length > 6) value = value.Substring(0, value.Length - 6);
            return value.Replace('_', ' ').Trim();
        }

        internal static WeatherRegionDefinition Get(WeatherRegionId id)
        {
            if (s_ById.TryGetValue(id, out WeatherRegionDefinition definition)) return definition;
            return new WeatherRegionDefinition(WeatherRegionId.Unknown, "???", "Unknown", "Unknown", false);
        }

        internal static bool TryGetBySceneName(string sceneName, out WeatherRegionDefinition definition)
        {
            definition = Get(WeatherRegionId.Unknown);
            if (string.IsNullOrEmpty(sceneName)) return false;
            if (!s_BySceneName.TryGetValue(sceneName, out WeatherRegionId id)) return false;
            definition = Get(id);
            return true;
        }

        internal static bool TryGetByRegionAlias(string rawName, out WeatherRegionDefinition definition)
        {
            definition = Get(WeatherRegionId.Unknown);
            if (string.IsNullOrWhiteSpace(rawName)) return false;

            if (TryGetBySceneName(rawName, out definition)) return true;

            string normalized = NormalizeAlias(rawName);
            if (string.IsNullOrEmpty(normalized)) return false;
            if (!s_ByLooseName.TryGetValue(normalized, out WeatherRegionId id)) return false;

            definition = Get(id);
            return true;
        }

        private static void Register(WeatherRegionDefinition definition, params string[] aliases)
        {
            s_ById[definition.Id] = definition;
            s_BySceneName[definition.SceneName] = definition.Id;
            s_Regions.Add(definition);
            RegisterAlias(definition.Id, definition.SceneName);
            RegisterAlias(definition.Id, definition.ShortName);
            RegisterAlias(definition.Id, definition.DisplayName);

            for (int i = 0; i < aliases.Length; i++)
            {
                RegisterAlias(definition.Id, aliases[i]);
            }
        }

        private static void RegisterSceneAlias(WeatherRegionId id, string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return;
            s_BySceneName[sceneName] = id;
            RegisterAlias(id, sceneName);
        }

        private static void RegisterAlias(WeatherRegionId id, string alias)
        {
            string normalized = NormalizeAlias(alias);
            if (string.IsNullOrEmpty(normalized)) return;
            s_ByLooseName[normalized] = id;
        }

        private static string NormalizeAlias(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c)) builder.Append(char.ToUpperInvariant(c));
            }

            return builder.ToString();
        }
    }

    internal static class SceneRegionMapper
    {
        private static WeatherRegionId s_LastKnownRegionId = WeatherRegionId.Unknown;

        internal static void ResetSession()
        {
            s_LastKnownRegionId = WeatherRegionId.Unknown;
        }

        internal static WeatherRegionDefinition Resolve(string sceneName)
        {
            if (RegionWeatherGraph.TryGetBySceneName(sceneName, out WeatherRegionDefinition definition))
            {
                if (!definition.IsDynamic || WeatherOverhaulSettingsManager.UseDefaultProfileForUnmappedRegions) return Remember(definition, sceneName);
            }

            if (TryResolveVanillaSceneRegion(sceneName, out definition, out string regionName)) return Remember(definition, sceneName);

            if (WeatherOverhaulSettingsManager.UseDefaultProfileForUnmappedRegions && !string.IsNullOrWhiteSpace(regionName))
            {
                definition = RegionWeatherGraph.GetOrCreateDynamicRegion(sceneName, regionName);
                if (definition.Id != WeatherRegionId.Unknown) return Remember(definition, sceneName);
            }

            if (CanUseLastKnownRegion(sceneName) && s_LastKnownRegionId != WeatherRegionId.Unknown)
            {
                WeatherRegionDefinition lastKnown = RegionWeatherGraph.Get(s_LastKnownRegionId);
                if (!lastKnown.IsDynamic || WeatherOverhaulSettingsManager.UseDefaultProfileForUnmappedRegions) return lastKnown;
            }

            if (WeatherOverhaulSettingsManager.UseDefaultProfileForUnmappedRegions && IsCurrentOutdoorScene(sceneName))
            {
                definition = RegionWeatherGraph.GetOrCreateDynamicRegion(sceneName, sceneName);
                if (definition.Id != WeatherRegionId.Unknown) return Remember(definition, sceneName);
            }

            return RegionWeatherGraph.Get(WeatherRegionId.Unknown);
        }


        private static bool TryResolveVanillaSceneRegion(string sceneName, out WeatherRegionDefinition definition, out string regionName)
        {
            definition = RegionWeatherGraph.Get(WeatherRegionId.Unknown);
            regionName = string.Empty;
            if (string.IsNullOrEmpty(sceneName)) return false;
            if (IsSaveBoundaryOrMenuScene(sceneName)) return false;

            regionName = TryGetVanillaRegionForScene(sceneName);
            if (string.IsNullOrWhiteSpace(regionName)) regionName = TryGetVanillaRegionForScene(StripKnownChildSuffix(sceneName));
            if (string.IsNullOrWhiteSpace(regionName)) return false;

            return RegionWeatherGraph.TryGetByRegionAlias(regionName, out definition) && definition.Id != WeatherRegionId.Unknown && !definition.IsDynamic;
        }

        private static string TryGetVanillaRegionForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return string.Empty;

            try
            {
                return InterfaceManager.GetRegionForScene(sceneName) ?? string.Empty;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("WeatherRegions.TryGetVanillaRegionForScene.1", "WeatherRegions.TryGetVanillaRegionForScene failed.", caughtException);
                return string.Empty;
            }
        }

        private static WeatherRegionDefinition Remember(WeatherRegionDefinition definition, string sceneName)
        {
            if (definition.Id == WeatherRegionId.Unknown) return definition;
            if (IsSaveBoundaryOrMenuScene(sceneName)) return definition;

            s_LastKnownRegionId = definition.Id;
            return definition;
        }

        private static bool CanUseLastKnownRegion(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            if (IsSaveBoundaryOrMenuScene(sceneName)) return false;

            try
            {
                Il2Cpp.Weather weather = GameManager.GetWeatherComponent();
                if (weather != null && weather.IsIndoorEnvironment()) return true;
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("WeatherRegions.CanUseLastKnownRegion.1", "WeatherRegions.CanUseLastKnownRegion failed.", caughtException);
            }

            return false;
        }

        private static bool IsCurrentOutdoorScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || IsSaveBoundaryOrMenuScene(sceneName)) return false;

            try
            {
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (!string.Equals(activeSceneName, sceneName, StringComparison.OrdinalIgnoreCase)) return false;
                Il2Cpp.Weather weather = GameManager.GetWeatherComponent();
                return weather != null && !weather.IsIndoorEnvironment();
            }
            catch (Exception caughtException)
            {
                Core.LogExceptionOnce("WeatherRegions.IsCurrentOutdoorScene.1", "WeatherRegions.IsCurrentOutdoorScene failed.", caughtException);
                return false;
            }
        }

        private static bool IsSaveBoundaryOrMenuScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return true;
            if (string.Equals(sceneName, "Empty", StringComparison.OrdinalIgnoreCase)) return true;
            if (sceneName.StartsWith("MainMenu", StringComparison.OrdinalIgnoreCase)) return true;
            if (sceneName.StartsWith("Boot", StringComparison.OrdinalIgnoreCase)) return true;
            if (sceneName.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0) return true;
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
                if (sceneName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return sceneName.Substring(0, sceneName.Length - suffix.Length);
            }

            return sceneName;
        }
    }
}