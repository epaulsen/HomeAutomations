using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using System.Reactive.Linq;

namespace HomeAutomations.Apps;

/// <summary>
/// Example automation that turns on lights when motion is detected
/// and turns them off after no motion for 5 minutes
/// </summary>
[NetDaemonApp]
public class MotionLightAutomation
{
    private IDisposable? _turnOffTimer;

    public MotionLightAutomation(IHaContext ha, ILogger<MotionLightAutomation> logger)
    {
        // Subscribe to motion sensor state changes
        ha.Entity("binary_sensor.motion_sensor")
            .StateChanges()
            .Subscribe(change =>
            {
                if (change.New?.State == "on")
                {
                    logger.LogInformation("Motion detected, turning on lights");
                    
                    // Cancel any pending turn off
                    _turnOffTimer?.Dispose();
                    
                    // Turn on the lights
                    ha.CallService("light", "turn_on", data: new 
                    { 
                        entity_id = "light.hallway",
                        brightness = 255
                    });
                }
                else if (change.New?.State == "off")
                {
                    logger.LogInformation("Motion cleared, scheduling light turn off in 5 minutes");
                    
                    // Schedule turning off lights after 5 minutes
                    _turnOffTimer = Observable.Timer(TimeSpan.FromMinutes(5))
                        .Subscribe(_ =>
                        {
                            logger.LogInformation("Turning off lights after motion timeout");
                            ha.CallService("light", "turn_off", data: new { entity_id = "light.hallway" });
                        });
                }
            });

        logger.LogInformation("MotionLightAutomation initialized");
    }
}
