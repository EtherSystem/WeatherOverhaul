using WeatherOverhaul.Utilities;

namespace WeatherOverhaul.Weather
{
    internal static class RegionalWeatherStageWeights
    {
        private static readonly WeightedWeatherFamily[] s_Default = Build(10, 7, 14, 6, 12, 10, 3, 2, 4, 7, 2, 8, 7, 2, 5, 1);
        private static readonly WeightedWeatherFamily[] s_MysteryLake = Build(15, 10, 14, 8, 12, 8, 3, 2, 3, 5, 1, 9, 6, 4, 0, 1);
        private static readonly WeightedWeatherFamily[] s_CoastalHighway = Build(17, 12, 14, 8, 8, 5, 2, 1, 4, 2, 0, 12, 9, 4, 2, 1);
        private static readonly WeightedWeatherFamily[] s_PleasantValley = Build(5, 5, 8, 6, 10, 12, 6, 7, 8, 20, 5, 4, 3, 1, 0, 1);
        private static readonly WeightedWeatherFamily[] s_DesolationPoint = Build(12, 10, 14, 8, 9, 6, 3, 3, 6, 5, 1, 9, 6, 3, 5, 1);
        private static readonly WeightedWeatherFamily[] s_TimberwolfMountain = Build(4, 5, 8, 5, 12, 15, 8, 10, 8, 16, 5, 2, 1, 1, 0, 1);
        private static readonly WeightedWeatherFamily[] s_ForlornMuskeg = Build(4, 4, 7, 6, 8, 5, 3, 5, 5, 3, 1, 16, 20, 13, 0, 2);
        private static readonly WeightedWeatherFamily[] s_BrokenRailroad = Build(9, 8, 13, 9, 11, 9, 5, 5, 7, 7, 2, 6, 4, 2, 3, 1);
        private static readonly WeightedWeatherFamily[] s_MountainTown = Build(13, 10, 13, 9, 13, 9, 3, 3, 4, 5, 1, 8, 6, 3, 0, 1);
        private static readonly WeightedWeatherFamily[] s_HushedRiverValley = Build(6, 6, 10, 6, 12, 11, 6, 7, 7, 9, 2, 8, 6, 4, 0, 1);
        private static readonly WeightedWeatherFamily[] s_BleakInlet = Build(4, 5, 8, 5, 10, 12, 7, 8, 14, 14, 4, 4, 3, 2, 0, 1);
        private static readonly WeightedWeatherFamily[] s_AshCanyon = Build(6, 6, 9, 6, 11, 10, 6, 7, 7, 8, 3, 4, 3, 2, 12, 1);
        private static readonly WeightedWeatherFamily[] s_Blackrock = Build(7, 7, 12, 8, 11, 10, 5, 6, 8, 9, 3, 5, 3, 2, 4, 1);
        private static readonly WeightedWeatherFamily[] s_TransferPass = Build(12, 10, 14, 9, 12, 9, 4, 4, 5, 5, 1, 7, 5, 3, 0, 1);
        private static readonly WeightedWeatherFamily[] s_ForsakenAirfield = Build(3, 4, 7, 5, 7, 8, 5, 7, 8, 8, 2, 10, 15, 11, 0, 1);
        private static readonly WeightedWeatherFamily[] s_ZoneOfContamination = Build(2, 3, 6, 5, 5, 5, 3, 4, 5, 4, 1, 11, 16, 14, 16, 1);
        private static readonly WeightedWeatherFamily[] s_SunderedPass = Build(2, 3, 5, 4, 9, 14, 8, 10, 13, 21, 6, 2, 2, 1, 0, 1);
        private static readonly WeightedWeatherFamily[] s_Ravine = Build(12, 6, 14, 8, 14, 7, 2, 2, 4, 3, 1, 15, 8, 4, 0, 1);
        private static readonly WeightedWeatherFamily[] s_WindingRiver = Build(10, 5, 13, 10, 11, 8, 3, 3, 4, 3, 1, 15, 10, 4, 0, 1);
        private static readonly WeightedWeatherFamily[] s_CrumblingHighway = Build(13, 7, 17, 11, 11, 6, 2, 2, 4, 3, 1, 12, 6, 2, 3, 1);
        private static readonly WeightedWeatherFamily[] s_KeepersPassSouth = Build(10, 5, 10, 8, 13, 12, 5, 5, 8, 10, 2, 6, 3, 1, 2, 1);
        private static readonly WeightedWeatherFamily[] s_KeepersPassNorth = Build(8, 4, 8, 6, 13, 13, 5, 6, 10, 11, 3, 5, 3, 2, 3, 1);
        private static readonly WeightedWeatherFamily[] s_FarRangeBranchLine = Build(10, 5, 16, 14, 13, 8, 3, 3, 6, 5, 1, 9, 5, 2, 0, 1);

        internal static float GetRegionalPreferenceMultiplier(WeatherRegionId regionId, WeatherFamily logicalFamily)
        {
            float regionalWeight = GetRegionalPreferenceWeight(regionId, logicalFamily);
            float baselineWeight = GetBaselineLogicalWeight(logicalFamily);
            if (baselineWeight <= 0f) return 1f;
            return Clamp(regionalWeight / baselineWeight, 0.25f, 2.50f);
        }

        internal static float GetRegionalPreferenceWeight(WeatherRegionId regionId, WeatherFamily logicalFamily)
        {
            return GetDirectStageWeight(regionId, WeatherFamilyClassifier.ToLogicalFamily(logicalFamily));
        }

        internal static float GetDirectStageWeight(WeatherRegionId regionId, WeatherFamily family)
        {
            WeightedWeatherFamily[] weights = GetBaseWeights(regionId);
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i].Family == family) return Math.Max(0f, weights[i].Weight);
            }

            return 0f;
        }

        private static float GetBaselineLogicalWeight(WeatherFamily logicalFamily)
        {
            switch (WeatherFamilyClassifier.ToLogicalFamily(logicalFamily))
            {
                case WeatherFamily.Clear: return 10f;
                case WeatherFamily.PartlyCloudy: return 7f;
                case WeatherFamily.Cloudy: return 14f;
                case WeatherFamily.LightSnow: return 12f;
                case WeatherFamily.HeavySnow: return 10f;
                case WeatherFamily.LightFog: return 8f;
                case WeatherFamily.DenseFog: return 7f;
                case WeatherFamily.Blizzard: return 7f;
                default: return 10f;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private static WeightedWeatherFamily[] GetBaseWeights(WeatherRegionId regionId)
        {
            switch (regionId)
            {
                case WeatherRegionId.ML: return s_MysteryLake;
                case WeatherRegionId.CH: return s_CoastalHighway;
                case WeatherRegionId.PV: return s_PleasantValley;
                case WeatherRegionId.DP: return s_DesolationPoint;
                case WeatherRegionId.TWM: return s_TimberwolfMountain;
                case WeatherRegionId.FM: return s_ForlornMuskeg;
                case WeatherRegionId.BR: return s_BrokenRailroad;
                case WeatherRegionId.MT: return s_MountainTown;
                case WeatherRegionId.HRV: return s_HushedRiverValley;
                case WeatherRegionId.BI: return s_BleakInlet;
                case WeatherRegionId.AC: return s_AshCanyon;
                case WeatherRegionId.BRM:
                case WeatherRegionId.BRM_Prison: return s_Blackrock;
                case WeatherRegionId.TP: return s_TransferPass;
                case WeatherRegionId.FA: return s_ForsakenAirfield;
                case WeatherRegionId.ZOC: return s_ZoneOfContamination;
                case WeatherRegionId.SP: return s_SunderedPass;
                case WeatherRegionId.Rav: return s_Ravine;
                case WeatherRegionId.WR: return s_WindingRiver;
                case WeatherRegionId.CRH: return s_CrumblingHighway;
                case WeatherRegionId.KP_South: return s_KeepersPassSouth;
                case WeatherRegionId.KP_North: return s_KeepersPassNorth;
                case WeatherRegionId.FRBL: return s_FarRangeBranchLine;
                default: return RegionWeatherGraph.IsDynamicRegion(regionId) ? s_Default : s_MysteryLake;
            }
        }

        private static WeightedWeatherFamily[] Build(float clear, float partlyCloudy, float cloudy, float lowOvercast, float lightSnow, float heavySnow, float veryHeavySnow, float whiteout, float windyLightSnow, float blizzard, float violentBlizzard, float lightFog, float denseFog, float veryDenseFog, float ashfall, float freezingFog = 0f)
        {
            return new[]
            {
                new WeightedWeatherFamily(WeatherFamily.Clear, clear),
                new WeightedWeatherFamily(WeatherFamily.PartlyCloudy, partlyCloudy),
                new WeightedWeatherFamily(WeatherFamily.Cloudy, cloudy),
                new WeightedWeatherFamily(WeatherFamily.LowOvercast, lowOvercast),
                new WeightedWeatherFamily(WeatherFamily.LightSnow, lightSnow),
                new WeightedWeatherFamily(WeatherFamily.HeavySnow, heavySnow),
                new WeightedWeatherFamily(WeatherFamily.VeryHeavySnow, veryHeavySnow),
                new WeightedWeatherFamily(WeatherFamily.Whiteout, whiteout),
                new WeightedWeatherFamily(WeatherFamily.WindyLightSnow, windyLightSnow),
                new WeightedWeatherFamily(WeatherFamily.Blizzard, blizzard),
                new WeightedWeatherFamily(WeatherFamily.ViolentBlizzard, violentBlizzard),
                new WeightedWeatherFamily(WeatherFamily.LightFog, lightFog),
                new WeightedWeatherFamily(WeatherFamily.DenseFog, denseFog),
                new WeightedWeatherFamily(WeatherFamily.VeryDenseFog, veryDenseFog),
                new WeightedWeatherFamily(WeatherFamily.Ashfall, ashfall),
                new WeightedWeatherFamily(WeatherFamily.FreezingFog, freezingFog)
            };
        }
    }

    internal readonly struct WeightedWeatherFamily
    {
        internal readonly WeatherFamily Family;
        internal readonly float Weight;

        internal WeightedWeatherFamily(WeatherFamily family, float weight)
        {
            Family = family;
            Weight = weight;
        }
    }

    internal static class RegionalWeatherModel
    {
        private const float AuroraPreparationLeadHours = 1.0f;
        private const float AuroraPreparationTailHours = 0.75f;

        private static readonly WeatherFamily[] s_TransitionFamilies =
        {
            WeatherFamily.Clear,
            WeatherFamily.Cloudy,
            WeatherFamily.LightSnow,
            WeatherFamily.HeavySnow,
            WeatherFamily.LightFog,
            WeatherFamily.DenseFog,
            WeatherFamily.Blizzard
        };

        internal static WeatherFamily PickInitialWeightedStage(WeatherRegionId regionId, DeterministicRng rng)
        {
            RegionalSeveritySetting severity = WeatherOverhaulSettingsManager.GetRegionalSeverity(regionId);
            float total = 0f;

            for (int i = 0; i < s_TransitionFamilies.Length; i++)
            {
                WeatherFamily logicalFamily = s_TransitionFamilies[i];
                if (!CanUseLogicalFamily(logicalFamily, severity, 0)) continue;
                total += GetInitialWeight(regionId, logicalFamily, severity);
            }

            if (total <= 0f) return PickVariant(WeatherFamily.Cloudy, regionId, severity, rng, default);

            float roll = rng.Range(0f, total);
            for (int i = 0; i < s_TransitionFamilies.Length; i++)
            {
                WeatherFamily logicalFamily = s_TransitionFamilies[i];
                if (!CanUseLogicalFamily(logicalFamily, severity, 0)) continue;
                roll -= GetInitialWeight(regionId, logicalFamily, severity);
                if (roll <= 0f) return PickVariant(logicalFamily, regionId, severity, rng, default);
            }

            return PickVariant(WeatherFamily.Cloudy, regionId, severity, rng, default);
        }

        internal static WeatherFamily PickIndependentWeightedStage(WeatherFamily previousFamily, WeatherRegionId regionId, DeterministicRng rng)
        {
            return PickTransitionVariant(WeatherFamilyClassifier.ToLogicalFamily(previousFamily), regionId, rng, default);
        }

        internal static WeatherFamily PickIndependentWeightedStage(RegionWeatherTimeline timeline, WeatherRegionId regionId, DeterministicRng rng)
        {
            WeatherFamily previousFamily = timeline.Segments.Count > 0 ? timeline.Segments[timeline.Segments.Count - 1].Family : WeatherFamily.Cloudy;
            SevereWeatherMemory memory = BuildSevereWeatherMemory(timeline.Segments);
            return PickTransitionVariant(WeatherFamilyClassifier.ToLogicalFamily(previousFamily), regionId, rng, memory);
        }

        internal static float GetSeverityDurationMultiplier(WeatherRegionId regionId, WeatherFamily family)
        {
            RegionalSeveritySetting severity = WeatherOverhaulSettingsManager.GetRegionalSeverity(regionId);
            WeatherDurationThreatGroup group = WeatherFamilyClassifier.GetDurationThreatGroup(family);
            switch (severity)
            {
                case RegionalSeveritySetting.Forgiving: return GetDurationGroupMultiplier(group, 1.10f, 0.80f, 0.55f);
                case RegionalSeveritySetting.Harsh: return GetDurationGroupMultiplier(group, 0.90f, 1.15f, 1.25f);
                case RegionalSeveritySetting.Brutal: return GetDurationGroupMultiplier(group, 0.75f, 1.35f, 1.65f);
                case RegionalSeveritySetting.WhoWantsToPlayLikeThis: return GetDurationGroupMultiplier(group, 0.45f, 1.75f, 2.75f);
                default: return 1f;
            }
        }

        internal static WeatherStageId ResolveDisplayedStageId(RegionWeatherSegment segment, WeatherRegionDefinition region, GlobalAuroraSchedule auroraSchedule, GlobalGlimmerFogSchedule glimmerFogSchedule, float worldHour)
        {
            if (auroraSchedule.IsActive(worldHour)) return WeatherStageId.ClearAurora;
            if (glimmerFogSchedule.IsActive(region.Id, worldHour)) return WeatherStageId.GlimmerFog;
            if (auroraSchedule.IsPreparationWindow(worldHour, AuroraPreparationLeadHours, AuroraPreparationTailHours)) return WeatherStageId.Clear;
            return segment.StageId;
        }

        private static WeatherFamily PickTransitionVariant(WeatherFamily previousLogicalFamily, WeatherRegionId regionId, DeterministicRng rng, SevereWeatherMemory memory)
        {
            RegionalSeveritySetting severity = WeatherOverhaulSettingsManager.GetRegionalSeverity(regionId);
            float total = 0f;

            for (int i = 0; i < s_TransitionFamilies.Length; i++)
            {
                WeatherFamily candidate = s_TransitionFamilies[i];
                if (!CanUseLogicalFamily(candidate, severity, memory.CooldownDebt)) continue;
                total += GetTransitionWeight(previousLogicalFamily, candidate, regionId, severity, memory);
            }

            if (total <= 0f) return PickVariant(ToFallbackFamily(previousLogicalFamily, memory.CooldownDebt), regionId, severity, rng, memory);

            float roll = rng.Range(0f, total);
            for (int i = 0; i < s_TransitionFamilies.Length; i++)
            {
                WeatherFamily candidate = s_TransitionFamilies[i];
                if (!CanUseLogicalFamily(candidate, severity, memory.CooldownDebt)) continue;
                roll -= GetTransitionWeight(previousLogicalFamily, candidate, regionId, severity, memory);
                if (roll <= 0f) return PickVariant(candidate, regionId, severity, rng, memory);
            }

            return PickVariant(ToFallbackFamily(previousLogicalFamily, memory.CooldownDebt), regionId, severity, rng, memory);
        }

        private static float GetInitialWeight(WeatherRegionId regionId, WeatherFamily logicalFamily, RegionalSeveritySetting severity)
        {
            float regionalWeight = RegionalWeatherStageWeights.GetRegionalPreferenceWeight(regionId, logicalFamily);
            if (regionalWeight <= 0f) regionalWeight = 1f;
            return Math.Max(0f, regionalWeight * GetSeverityDestinationMultiplier(logicalFamily, severity));
        }

        private static float GetTransitionWeight(WeatherFamily previousLogicalFamily, WeatherFamily candidateLogicalFamily, WeatherRegionId regionId, RegionalSeveritySetting severity, SevereWeatherMemory memory)
        {
            float baseWeight = GetBaseTransitionWeight(previousLogicalFamily, candidateLogicalFamily, severity);
            if (baseWeight <= 0f) return 0f;

            float weight = baseWeight;
            weight *= GetSeverityDestinationMultiplier(candidateLogicalFamily, severity);
            weight *= GetEscalationMultiplier(previousLogicalFamily, candidateLogicalFamily, severity);
            weight *= RegionalWeatherStageWeights.GetRegionalPreferenceMultiplier(regionId, candidateLogicalFamily);

            if (candidateLogicalFamily == WeatherFamily.Blizzard && memory.ActiveChainCount > 0) weight *= GetSevereChainMultiplier(memory.ActiveChainCount);
            return Math.Max(0f, weight);
        }

        private static float GetBaseTransitionWeight(WeatherFamily previousLogicalFamily, WeatherFamily candidateLogicalFamily, RegionalSeveritySetting severity)
        {
            WeatherFamily previous = WeatherFamilyClassifier.ToLogicalFamily(previousLogicalFamily);
            WeatherFamily candidate = WeatherFamilyClassifier.ToLogicalFamily(candidateLogicalFamily);

            if (previous == WeatherFamily.Clear) return PickCandidate(candidate, 85f, 100f, 78f, 42f, 75f, 26f, 20f);
            if (previous == WeatherFamily.PartlyCloudy) return PickCandidate(candidate, 70f, 55f, 96f, 62f, 92f, 30f, 25f);
            if (previous == WeatherFamily.Cloudy) return PickCandidate(candidate, 82f, 40f, 60f, 56f, 58f, 24f, 31f);
            if (previous == WeatherFamily.LightSnow) return PickCandidate(candidate, 90f, 68f, 31f, 77f, 31f, 16f, 45f);
            if (previous == WeatherFamily.HeavySnow) return PickCandidate(candidate, 70f, 65f, 30f, 10f, 37f, 18f, 60f);
            if (previous == WeatherFamily.LightFog) return PickCandidate(candidate, 88f, 70f, 49f, 24f, 41f, 65f, 11f);
            if (previous == WeatherFamily.DenseFog) return PickCandidate(candidate, 96f, 59f, 35f, 15f, 30f, 3f, 5f);
            if (previous == WeatherFamily.Blizzard)
            {
                if (candidate == WeatherFamily.Blizzard) return severity == RegionalSeveritySetting.WhoWantsToPlayLikeThis ? 70f : 0f;
                return PickCandidate(candidate, 100f, 85f, 47f, 13f, 48f, 4f, 0f);
            }

            return PickCandidate(candidate, 82f, 40f, 60f, 56f, 58f, 24f, 31f);
        }

        private static float PickCandidate(WeatherFamily candidate, float clear, float cloudy, float lightSnow, float heavySnow, float lightFog, float denseFog, float blizzard)
        {
            switch (candidate)
            {
                case WeatherFamily.Clear: return clear;
                case WeatherFamily.Cloudy: return cloudy;
                case WeatherFamily.LightSnow: return lightSnow;
                case WeatherFamily.HeavySnow: return heavySnow;
                case WeatherFamily.LightFog: return lightFog;
                case WeatherFamily.DenseFog: return denseFog;
                case WeatherFamily.Blizzard: return blizzard;
                default: return 0f;
            }
        }

        private static float GetSeverityDestinationMultiplier(WeatherFamily logicalFamily, RegionalSeveritySetting severity)
        {
            switch (severity)
            {
                case RegionalSeveritySetting.Forgiving: return PickCandidate(logicalFamily, 1.45f, 1.25f, 0.75f, 0.45f, 1.20f, 0.55f, 0.25f);
                case RegionalSeveritySetting.Harsh: return PickCandidate(logicalFamily, 0.65f, 0.95f, 1.30f, 1.45f, 0.85f, 1.35f, 1.15f);
                case RegionalSeveritySetting.Brutal: return PickCandidate(logicalFamily, 0.35f, 0.80f, 1.65f, 2.10f, 0.70f, 1.75f, 1.80f);
                case RegionalSeveritySetting.WhoWantsToPlayLikeThis: return PickCandidate(logicalFamily, 0.00f, 0.45f, 1.80f, 2.60f, 0.45f, 2.20f, 3.00f);
                default: return 1f;
            }
        }

        private static float GetEscalationMultiplier(WeatherFamily previousLogicalFamily, WeatherFamily candidateLogicalFamily, RegionalSeveritySetting severity)
        {
            WeatherFamily previous = WeatherFamilyClassifier.ToLogicalFamily(previousLogicalFamily);
            WeatherFamily candidate = WeatherFamilyClassifier.ToLogicalFamily(candidateLogicalFamily);

            if (previous == WeatherFamily.Cloudy && candidate == WeatherFamily.LightSnow) return PickSeverityValue(severity, 0.90f, 1.00f, 1.25f, 1.75f, 2.50f);
            if (previous == WeatherFamily.Cloudy && candidate == WeatherFamily.HeavySnow) return PickSeverityValue(severity, 0.70f, 1.00f, 1.35f, 2.00f, 3.50f);
            if (previous == WeatherFamily.Cloudy && candidate == WeatherFamily.Blizzard) return PickSeverityValue(severity, 0.35f, 1.00f, 1.10f, 1.50f, 2.50f);
            if (previous == WeatherFamily.LightSnow && candidate == WeatherFamily.HeavySnow) return PickSeverityValue(severity, 0.75f, 1.00f, 1.50f, 2.50f, 4.00f);
            if (previous == WeatherFamily.LightSnow && candidate == WeatherFamily.Blizzard) return PickSeverityValue(severity, 0.40f, 1.00f, 1.25f, 2.00f, 3.50f);
            if (previous == WeatherFamily.HeavySnow && candidate == WeatherFamily.Blizzard) return PickSeverityValue(severity, 0.60f, 1.00f, 1.35f, 2.25f, 5.00f);
            if (previous == WeatherFamily.LightFog && candidate == WeatherFamily.DenseFog) return PickSeverityValue(severity, 0.80f, 1.00f, 1.50f, 2.50f, 4.00f);
            if (previous == WeatherFamily.DenseFog && candidate == WeatherFamily.Blizzard) return PickSeverityValue(severity, 0.50f, 1.00f, 1.20f, 1.75f, 2.50f);
            return 1f;
        }

        private static WeatherFamily PickVariant(WeatherFamily logicalFamily, WeatherRegionId regionId, RegionalSeveritySetting severity, DeterministicRng rng, SevereWeatherMemory memory)
        {
            switch (WeatherFamilyClassifier.ToLogicalFamily(logicalFamily))
            {
                case WeatherFamily.Cloudy:
                    return PickCloudyVariant(regionId, severity, rng, memory);
                case WeatherFamily.LightSnow:
                    return PickSingleVariant(regionId, WeatherFamily.LightSnow, WeatherFamily.WindyLightSnow, rng, memory);
                case WeatherFamily.HeavySnow:
                    return PickHeavySnowVariant(regionId, severity, rng, memory);
                case WeatherFamily.LightFog:
                    return PickLightFogVariant(regionId, severity, rng, memory);
                case WeatherFamily.DenseFog:
                    return PickSingleVariant(regionId, WeatherFamily.DenseFog, WeatherFamily.VeryDenseFog, rng, memory);
                case WeatherFamily.Blizzard:
                    if (memory.CooldownDebt > 0) return WeatherFamily.HeavySnow;
                    return PickSingleVariant(regionId, WeatherFamily.Blizzard, WeatherFamily.ViolentBlizzard, rng, memory);
                default:
                    return WeatherFamilyClassifier.ToLogicalFamily(logicalFamily);
            }
        }

        private static WeatherFamily PickLightFogVariant(WeatherRegionId regionId, RegionalSeveritySetting severity, DeterministicRng rng, SevereWeatherMemory memory)
        {
            return PickSingleVariant(regionId, WeatherFamily.LightFog, WeatherFamily.FreezingFog, rng, memory);
        }

        private static WeatherFamily PickCloudyVariant(WeatherRegionId regionId, RegionalSeveritySetting severity, DeterministicRng rng, SevereWeatherMemory memory)
        {
            float lowOvercastWeight = GetEnabledRegionalVariantWeight(regionId, WeatherFamily.LowOvercast, memory);
            float cloudyWeight = RegionalWeatherStageWeights.GetDirectStageWeight(regionId, WeatherFamily.Cloudy);
            float partlyCloudyWeight = RegionalWeatherStageWeights.GetDirectStageWeight(regionId, WeatherFamily.PartlyCloudy);
            float total = cloudyWeight + partlyCloudyWeight + lowOvercastWeight;
            if (total <= 0f) return WeatherFamily.Cloudy;

            float roll = rng.Range(0f, total);
            roll -= cloudyWeight;
            if (roll <= 0f) return WeatherFamily.Cloudy;
            roll -= partlyCloudyWeight;
            if (roll <= 0f) return WeatherFamily.PartlyCloudy;
            return WeatherFamily.LowOvercast;
        }

        private static WeatherFamily PickSingleVariant(WeatherRegionId regionId, WeatherFamily vanillaFamily, WeatherFamily variantFamily, DeterministicRng rng, SevereWeatherMemory memory)
        {
            float enabledVariantWeight = GetEnabledRegionalVariantWeight(regionId, variantFamily, memory);
            float vanillaWeight = RegionalWeatherStageWeights.GetDirectStageWeight(regionId, vanillaFamily);
            float total = vanillaWeight + enabledVariantWeight;
            if (total <= 0f) return vanillaFamily;

            float roll = rng.Range(0f, total);
            if (roll < vanillaWeight) return vanillaFamily;
            return variantFamily;
        }

        private static WeatherFamily PickHeavySnowVariant(WeatherRegionId regionId, RegionalSeveritySetting severity, DeterministicRng rng, SevereWeatherMemory memory)
        {
            float heavyOvercastWeight = GetEnabledVariantWeight(WeatherFamily.HeavyOvercast, GetHeavyOvercastVariantWeight(severity), memory);
            float veryHeavyWeight = GetEnabledRegionalVariantWeight(regionId, WeatherFamily.VeryHeavySnow, memory);
            float whiteoutWeight = GetEnabledRegionalVariantWeight(regionId, WeatherFamily.Whiteout, memory);
            float ashfallWeight = GetEnabledRegionalVariantWeight(regionId, WeatherFamily.Ashfall, memory);
            float vanillaWeight = RegionalWeatherStageWeights.GetDirectStageWeight(regionId, WeatherFamily.HeavySnow);
            float total = vanillaWeight + heavyOvercastWeight + veryHeavyWeight + whiteoutWeight + ashfallWeight;
            if (total <= 0f) return WeatherFamily.HeavySnow;

            float roll = rng.Range(0f, total);
            roll -= vanillaWeight;
            if (roll <= 0f) return WeatherFamily.HeavySnow;
            roll -= heavyOvercastWeight;
            if (roll <= 0f) return WeatherFamily.HeavyOvercast;
            roll -= veryHeavyWeight;
            if (roll <= 0f) return WeatherFamily.VeryHeavySnow;
            roll -= whiteoutWeight;
            if (roll <= 0f) return WeatherFamily.Whiteout;
            return WeatherFamily.Ashfall;
        }

        private static float GetEnabledRegionalVariantWeight(WeatherRegionId regionId, WeatherFamily variantFamily, SevereWeatherMemory memory)
        {
            if (!WeatherOverhaulSettingsManager.IsWeatherFamilyEnabled(variantFamily)) return 0f;
            if (IsBlockedBySevereCooldown(variantFamily, memory)) return 0f;
            return RegionalWeatherStageWeights.GetDirectStageWeight(regionId, variantFamily);
        }

        private static float GetEnabledVariantWeight(WeatherFamily variantFamily, float configuredWeight, SevereWeatherMemory memory)
        {
            if (!WeatherOverhaulSettingsManager.IsWeatherFamilyEnabled(variantFamily)) return 0f;
            if (IsBlockedBySevereCooldown(variantFamily, memory)) return 0f;
            return Math.Max(0f, configuredWeight);
        }

        private static bool CanUseLogicalFamily(WeatherFamily logicalFamily, RegionalSeveritySetting severity, int cooldownDebt)
        {
            if (!WeatherFamilyClassifier.IsLogicalTransitionFamily(logicalFamily)) return false;
            if (severity == RegionalSeveritySetting.WhoWantsToPlayLikeThis && logicalFamily == WeatherFamily.Clear) return false;
            if (cooldownDebt > 0 && logicalFamily == WeatherFamily.Blizzard) return false;
            return true;
        }

        private static bool IsBlockedBySevereCooldown(WeatherFamily family, SevereWeatherMemory memory)
        {
            return memory.CooldownDebt > 0 && WeatherFamilyClassifier.IsSevereCooldownFamily(family);
        }

        private static SevereWeatherMemory BuildSevereWeatherMemory(IReadOnlyList<RegionWeatherSegment> segments)
        {
            int activeChainCount = 0;
            int cooldownDebt = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                WeatherFamily family = segments[i].Family;
                if (WeatherFamilyClassifier.IsSevereCooldownFamily(family))
                {
                    activeChainCount = activeChainCount > 0 ? Math.Min(8, activeChainCount + 1) : 1;
                    cooldownDebt = 0;
                    continue;
                }

                if (activeChainCount > 0)
                {
                    cooldownDebt = Math.Max(0, activeChainCount - 2);
                    activeChainCount = 0;
                    continue;
                }

                if (cooldownDebt > 0) cooldownDebt = Math.Max(0, cooldownDebt - 2);
            }

            return new SevereWeatherMemory(activeChainCount, cooldownDebt);
        }

        private static float GetSevereChainMultiplier(int activeChainCount)
        {
            if (activeChainCount <= 0) return 1.00f;
            if (activeChainCount == 1) return 0.85f;
            if (activeChainCount == 2) return 0.65f;
            if (activeChainCount == 3) return 0.45f;
            if (activeChainCount == 4) return 0.30f;
            if (activeChainCount == 5) return 0.20f;
            return 0.10f;
        }

        private static float GetDurationGroupMultiplier(WeatherDurationThreatGroup group, float gentle, float medium, float severe)
        {
            switch (group)
            {
                case WeatherDurationThreatGroup.Medium: return medium;
                case WeatherDurationThreatGroup.Severe: return severe;
                default: return gentle;
            }
        }

        private static WeatherFamily ToFallbackFamily(WeatherFamily sourceFamily, int cooldownDebt)
        {
            if (cooldownDebt > 0) return WeatherFamily.HeavySnow;
            return WeatherFamilyClassifier.ToLogicalFamily(sourceFamily);
        }

        private static float PickSeverityValue(RegionalSeveritySetting severity, float forgiving, float normal, float harsh, float brutal, float absurd)
        {
            switch (severity)
            {
                case RegionalSeveritySetting.Forgiving: return forgiving;
                case RegionalSeveritySetting.Harsh: return harsh;
                case RegionalSeveritySetting.Brutal: return brutal;
                case RegionalSeveritySetting.WhoWantsToPlayLikeThis: return absurd;
                default: return normal;
            }
        }

        private static float GetHeavyOvercastVariantWeight(RegionalSeveritySetting severity)
        {
            return PickSeverityValue(severity, 20f, 18f, 15f, 12f, 10f);
        }
    }
}