namespace WeatherOverhaul.Weather
{
    internal readonly struct WeatherActivationPlan
    {
        internal WeatherActivationPlan(WeatherStageId stageId, float startWorldHour, float endWorldHour, float durationHours, float elapsedHours, float remainingHours, string reason)
        {
            WeatherStageDefinition stage = WeatherStageCatalog.Get(stageId);

            StageId = stage.Id;
            Stage = stage.EngineStage;
            Family = stage.Family;
            StartWorldHour = startWorldHour;
            EndWorldHour = endWorldHour;
            DurationHours = durationHours;
            ElapsedHours = elapsedHours;
            RemainingHours = remainingHours;
            Reason = string.IsNullOrEmpty(reason) ? "forecast segment" : reason;
        }

        internal WeatherStageId StageId { get; }
        internal WeatherStage Stage { get; }
        internal WeatherFamily Family { get; }
        internal float StartWorldHour { get; }
        internal float EndWorldHour { get; }
        internal float DurationHours { get; }
        internal float ElapsedHours { get; }
        internal float RemainingHours { get; }
        internal string Reason { get; }
        internal WeatherStageDefinition Definition => WeatherStageCatalog.Get(StageId);
    }
}