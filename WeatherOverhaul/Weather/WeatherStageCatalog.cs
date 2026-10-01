namespace WeatherOverhaul.Weather
{
    internal enum WeatherStageId
    {
        Undefined = 0,

        Clear = 1,
        PartlyCloudy = 2,
        Cloudy = 3,
        LightSnow = 4,
        HeavySnow = 5,
        Blizzard = 6,
        LightFog = 7,
        DenseFog = 8,
        ClearAurora = 9,
        ToxicFog = 10,

        LowOvercast = 100,
        VeryHeavySnow = 101,
        Whiteout = 102,
        WindyLightSnow = 103,
        ViolentBlizzard = 104,
        VeryDenseFog = 105,
        FreezingFog = 106,
        Ashfall = 107,
        GlimmerFog = 108,
        CloudyAurora = 109,
        SnowyAurora = 110,
        ClearBloodMoon = 111,
        SnowBloodMoon = 112,
        HeavyOvercast = 114,
        FoggyAurora = 115
    }

    internal readonly struct WeatherStageDefinition
    {
        internal WeatherStageDefinition(WeatherStageId id, WeatherFamily family, WeatherStage engineStage, string displayName, bool isCustom)
        {
            Id = id;
            Family = family;
            EngineStage = engineStage;
            DisplayName = displayName;
            IsCustom = isCustom;
        }

        internal WeatherStageId Id { get; }
        internal WeatherFamily Family { get; }
        internal WeatherStage EngineStage { get; }
        internal string DisplayName { get; }
        internal bool IsCustom { get; }
    }

    internal static class WeatherStageCatalog
    {
        private static readonly Dictionary<WeatherStageId, WeatherStageDefinition> ById = new();
        private static readonly Dictionary<WeatherFamily, WeatherStageId> ByFamily = new();
        private static readonly Dictionary<WeatherStage, WeatherStageId> ByEngineStage = new();

        static WeatherStageCatalog()
        {
            Register(WeatherStageId.Clear, WeatherFamily.Clear, WeatherStage.Clear, "Clear", false);
            Register(WeatherStageId.PartlyCloudy, WeatherFamily.PartlyCloudy, WeatherStage.PartlyCloudy, "Partly cloudy", false);
            Register(WeatherStageId.Cloudy, WeatherFamily.Cloudy, WeatherStage.Cloudy, "Cloudy", false);
            Register(WeatherStageId.LightSnow, WeatherFamily.LightSnow, WeatherStage.LightSnow, "Light snow", false);
            Register(WeatherStageId.HeavySnow, WeatherFamily.HeavySnow, WeatherStage.HeavySnow, "Heavy snow", false);
            Register(WeatherStageId.Blizzard, WeatherFamily.Blizzard, WeatherStage.Blizzard, "Blizzard", false);
            Register(WeatherStageId.LightFog, WeatherFamily.LightFog, WeatherStage.LightFog, "Light fog", false);
            Register(WeatherStageId.DenseFog, WeatherFamily.DenseFog, WeatherStage.DenseFog, "Dense fog", false);
            Register(WeatherStageId.ClearAurora, WeatherFamily.Clear, WeatherStage.ClearAurora, "Clear aurora", false, mapFamily: false);
            Register(WeatherStageId.ToxicFog, WeatherFamily.DenseFog, WeatherStage.ToxicFog, "Toxic fog", false, mapFamily: false);

            Register(WeatherStageId.LowOvercast, WeatherFamily.LowOvercast, WeatherStage.Cloudy, "Low overcast", true);
            Register(WeatherStageId.HeavyOvercast, WeatherFamily.HeavyOvercast, WeatherStage.HeavySnow, "Heavy overcast", true);
            Register(WeatherStageId.VeryHeavySnow, WeatherFamily.VeryHeavySnow, WeatherStage.HeavySnow, "Very heavy snow", true);
            Register(WeatherStageId.Whiteout, WeatherFamily.Whiteout, WeatherStage.HeavySnow, "Whiteout", true);
            Register(WeatherStageId.WindyLightSnow, WeatherFamily.WindyLightSnow, WeatherStage.LightSnow, "Windy light snow", true);
            Register(WeatherStageId.ViolentBlizzard, WeatherFamily.ViolentBlizzard, WeatherStage.Blizzard, "Violent blizzard", true);
            Register(WeatherStageId.VeryDenseFog, WeatherFamily.VeryDenseFog, WeatherStage.DenseFog, "Very dense fog", true);
            Register(WeatherStageId.FreezingFog, WeatherFamily.FreezingFog, WeatherStage.LightFog, "Freezing fog", true);
            Register(WeatherStageId.Ashfall, WeatherFamily.Ashfall, WeatherStage.HeavySnow, "Ashfall", true);
            Register(WeatherStageId.GlimmerFog, WeatherFamily.ElectrostaticFog, WeatherStage.ElectrostaticFog, "Glimmer fog", true);
            Register(WeatherStageId.CloudyAurora, WeatherFamily.CloudyAurora, WeatherStage.ClearAurora, "Cloudy aurora", true);
            Register(WeatherStageId.SnowyAurora, WeatherFamily.SnowyAurora, WeatherStage.ClearAurora, "Snowy aurora", true);
            Register(WeatherStageId.FoggyAurora, WeatherFamily.FoggyAurora, WeatherStage.HeavySnow, "Foggy aurora", true);
            Register(WeatherStageId.ClearBloodMoon, WeatherFamily.ClearBloodMoon, WeatherStage.Clear, "Clear blood moon", true);
            Register(WeatherStageId.SnowBloodMoon, WeatherFamily.LightSnowBloodMoon, WeatherStage.PartlyCloudy, "Snow blood moon", true);
        }

        internal static WeatherStageDefinition Get(WeatherStageId id)
        {
            return ById.TryGetValue(id, out WeatherStageDefinition definition)
                ? definition
                : new WeatherStageDefinition(WeatherStageId.Undefined, WeatherFamily.Cloudy, WeatherStage.Undefined, "Unknown", false);
        }

        internal static WeatherStageId ForFamily(WeatherFamily family)
        {
            return ByFamily.TryGetValue(family, out WeatherStageId id) ? id : WeatherStageId.Cloudy;
        }

        internal static WeatherStageId FromEngineStage(WeatherStage stage)
        {
            return ByEngineStage.TryGetValue(stage, out WeatherStageId id) ? id : WeatherStageId.Cloudy;
        }

        internal static WeatherStageId Resolve(WeatherFamily family, WeatherStage fallbackStage)
        {
            if (ByFamily.TryGetValue(family, out WeatherStageId id)) return id;
            if (ByEngineStage.TryGetValue(fallbackStage, out id)) return id;
            return WeatherStageId.Cloudy;
        }

        internal static bool IsKnown(WeatherStageId id)
        {
            return ById.ContainsKey(id);
        }

        private static void Register(WeatherStageId id, WeatherFamily family, WeatherStage engineStage, string displayName, bool isCustom, bool mapFamily = true)
        {
            WeatherStageDefinition definition = new(id, family, engineStage, displayName, isCustom);
            ById[id] = definition;
            if (mapFamily) ByFamily[family] = id;
            if (!ByEngineStage.ContainsKey(engineStage)) ByEngineStage[engineStage] = id;
        }
    }
}