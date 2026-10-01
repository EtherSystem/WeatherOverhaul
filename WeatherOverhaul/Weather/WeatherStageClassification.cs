namespace WeatherOverhaul.Weather
{
    internal enum WeatherFamily
    {
        Clear = 0,
        PartlyCloudy = 1,
        Cloudy = 2,
        LowOvercast = 3,
        LightSnow = 4,
        HeavySnow = 5,
        HeavyOvercast = 6,
        VeryHeavySnow = 7,
        Whiteout = 8,
        WindyLightSnow = 9,
        Blizzard = 10,
        ViolentBlizzard = 11,
        LightFog = 12,
        DenseFog = 13,
        VeryDenseFog = 14,
        Ashfall = 15,
        ElectrostaticFog = 16,
        CloudyAurora = 17,
        SnowyAurora = 18,
        ClearBloodMoon = 19,
        LightSnowBloodMoon = 20,
        FreezingFog = 22,
        FoggyAurora = 23
    }

    internal enum WeatherDurationThreatGroup
    {
        Gentle,
        Medium,
        Severe
    }

    internal readonly struct SevereWeatherMemory
    {
        internal readonly int ActiveChainCount;
        internal readonly int CooldownDebt;

        internal SevereWeatherMemory(int activeChainCount, int cooldownDebt)
        {
            ActiveChainCount = activeChainCount;
            CooldownDebt = cooldownDebt;
        }
    }

    internal static class WeatherFamilyClassifier
    {
        internal static WeatherFamily ToLogicalFamily(WeatherFamily family)
        {
            switch (family)
            {
                case WeatherFamily.PartlyCloudy:
                case WeatherFamily.LowOvercast: return WeatherFamily.Cloudy;
                case WeatherFamily.WindyLightSnow: return WeatherFamily.LightSnow;
                case WeatherFamily.HeavyOvercast:
                case WeatherFamily.VeryHeavySnow:
                case WeatherFamily.Whiteout:
                case WeatherFamily.Ashfall: return WeatherFamily.HeavySnow;
                case WeatherFamily.ViolentBlizzard: return WeatherFamily.Blizzard;
                case WeatherFamily.VeryDenseFog: return WeatherFamily.DenseFog;
                case WeatherFamily.ElectrostaticFog:
                case WeatherFamily.FreezingFog: return WeatherFamily.LightFog;
                case WeatherFamily.CloudyAurora: return WeatherFamily.Cloudy;
                case WeatherFamily.SnowyAurora: return WeatherFamily.LightSnow;
                case WeatherFamily.FoggyAurora: return WeatherFamily.HeavySnow;
                case WeatherFamily.ClearBloodMoon:
                case WeatherFamily.LightSnowBloodMoon: return WeatherFamily.Clear;
                default: return family;
            }
        }

        internal static bool IsLogicalTransitionFamily(WeatherFamily family)
        {
            switch (family)
            {
                case WeatherFamily.Clear:
                case WeatherFamily.PartlyCloudy:
                case WeatherFamily.Cloudy:
                case WeatherFamily.LightSnow:
                case WeatherFamily.HeavySnow:
                case WeatherFamily.LightFog:
                case WeatherFamily.DenseFog:
                case WeatherFamily.Blizzard:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsSevereCooldownFamily(WeatherFamily family)
        {
            switch (family)
            {
                case WeatherFamily.Blizzard:
                case WeatherFamily.ViolentBlizzard:
                case WeatherFamily.VeryHeavySnow:
                case WeatherFamily.VeryDenseFog:
                case WeatherFamily.FreezingFog:
                    return true;
                default:
                    return false;
            }
        }

        internal static WeatherDurationThreatGroup GetDurationThreatGroup(WeatherFamily family)
        {
            switch (family)
            {
                case WeatherFamily.Blizzard:
                case WeatherFamily.ViolentBlizzard:
                case WeatherFamily.VeryHeavySnow:
                case WeatherFamily.VeryDenseFog:
                case WeatherFamily.FreezingFog:
                    return WeatherDurationThreatGroup.Severe;
                case WeatherFamily.WindyLightSnow:
                case WeatherFamily.HeavySnow:
                case WeatherFamily.HeavyOvercast:
                case WeatherFamily.Whiteout:
                case WeatherFamily.Ashfall:
                case WeatherFamily.DenseFog:
                case WeatherFamily.ElectrostaticFog:
                case WeatherFamily.CloudyAurora:
                case WeatherFamily.SnowyAurora:
                case WeatherFamily.FoggyAurora:
                    return WeatherDurationThreatGroup.Medium;
                default:
                    return WeatherDurationThreatGroup.Gentle;
            }
        }
    }

    internal static class WeatherFamilyFormatter
    {
        internal static string ToDisplayName(WeatherFamily family)
        {
            switch (family)
            {
                case WeatherFamily.Clear: return "Clear";
                case WeatherFamily.PartlyCloudy: return "Partly cloudy";
                case WeatherFamily.Cloudy: return "Cloudy";
                case WeatherFamily.LowOvercast: return "Low overcast";
                case WeatherFamily.LightSnow: return "Light snow";
                case WeatherFamily.HeavySnow: return "Heavy snow";
                case WeatherFamily.HeavyOvercast: return "Heavy overcast";
                case WeatherFamily.VeryHeavySnow: return "Very heavy snow";
                case WeatherFamily.Whiteout: return "Whiteout";
                case WeatherFamily.WindyLightSnow: return "Windy light snow";
                case WeatherFamily.Blizzard: return "Blizzard";
                case WeatherFamily.ViolentBlizzard: return "Violent blizzard";
                case WeatherFamily.LightFog: return "Light fog";
                case WeatherFamily.DenseFog: return "Dense fog";
                case WeatherFamily.VeryDenseFog: return "Very dense fog";
                case WeatherFamily.Ashfall: return "Ashfall";
                case WeatherFamily.FreezingFog: return "Freezing fog";
                case WeatherFamily.ElectrostaticFog: return "Glimmer fog";
                case WeatherFamily.CloudyAurora: return "Cloudy aurora";
                case WeatherFamily.SnowyAurora: return "Snowy aurora";
                case WeatherFamily.FoggyAurora: return "Foggy aurora";
                case WeatherFamily.ClearBloodMoon: return "Clear blood moon";
                case WeatherFamily.LightSnowBloodMoon: return "Snow blood moon";
                default: return "Unknown";
            }
        }

    }

    internal static class WeatherStageFormatter
    {
        internal static string ToDisplayName(WeatherStage stage)
        {
            switch (stage)
            {
                case WeatherStage.DenseFog: return "Dense fog";
                case WeatherStage.LightSnow: return "Light snow";
                case WeatherStage.HeavySnow: return "Heavy snow";
                case WeatherStage.PartlyCloudy: return "Partly cloudy";
                case WeatherStage.Clear: return "Clear";
                case WeatherStage.Cloudy: return "Cloudy";
                case WeatherStage.LightFog: return "Light fog";
                case WeatherStage.Blizzard: return "Blizzard";
                case WeatherStage.ClearAurora: return "Clear aurora";
                case WeatherStage.ToxicFog: return "Toxic fog";
                case WeatherStage.ElectrostaticFog: return "Glimmer fog";
                default: return "Unknown";
            }
        }
    }
}