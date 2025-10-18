using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps;

/// <summary>
/// Example automation that turns lights on at sunset and off at sunrise
/// </summary>
[NetDaemonApp]
public class LightAutomation
{
    public LightAutomation(IHaContext ha, ILogger<LightAutomation> logger)
    {
        // Subscribe to sun state changes
        ha.Entity("sun.sun")
            .StateChanges()
            .Subscribe(change =>
            {
                if (change.New?.State == "below_horizon")
                {
                    logger.LogInformation("Sun is setting, turning on lights");
                    ha.CallService("light", "turn_on", data: new { entity_id = "light.living_room" });
                }
                else if (change.New?.State == "above_horizon")
                {
                    logger.LogInformation("Sun is rising, turning off lights");
                    ha.CallService("light", "turn_off", data: new { entity_id = "light.living_room" });
                }
            });

        logger.LogInformation("LightAutomation initialized");
    }
}
