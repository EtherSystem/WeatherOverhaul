using WeatherOverhaul.Weather;

namespace WeatherOverhaul
{
    internal static class DevConsoleCommands
    {
        internal static void Register()
        {
            RegisterNightEventCommand("wo_clearaurora", GlobalNightEventType.ClearAurora);
            RegisterNightEventCommand("wo_cloudyaurora", GlobalNightEventType.CloudyAurora);
            RegisterNightEventCommand("wo_snowyaurora", GlobalNightEventType.SnowyAurora);
            RegisterNightEventCommand("wo_foggyaurora", GlobalNightEventType.FoggyAurora);
            RegisterNightEventCommand("wo_clearbloodmoon", GlobalNightEventType.ClearBloodMoon);
            RegisterNightEventCommand("wo_snowbloodmoon", GlobalNightEventType.LightSnowBloodMoon);
            RegisterNightEventCommand("wo_lightsnowbloodmoon", GlobalNightEventType.LightSnowBloodMoon);
        }

        private static void RegisterNightEventCommand(string command, GlobalNightEventType eventType)
        {
            uConsole.RegisterCommand(command, new Action(() => ForceNextNightEvent(eventType)));
        }

        private static void ForceNextNightEvent(GlobalNightEventType eventType)
        {
            WeatherSnapshot snapshot = WeatherSnapshot.Capture();
            if (GlobalWeatherSimulation.TryForceNextNightEventVariant(eventType, snapshot, out string message))
            {
                uConsole.Log(message);
                return;
            }

            uConsole.Log(string.IsNullOrWhiteSpace(message) ? "WeatherOverhaul could not force the requested night event." : message);
        }
    }
}