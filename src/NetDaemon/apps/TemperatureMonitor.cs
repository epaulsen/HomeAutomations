using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps;

/// <summary>
/// Example automation that monitors temperature and sends notifications
/// when temperature exceeds thresholds
/// </summary>
[NetDaemonApp]
public class TemperatureMonitor
{
    private readonly double _highThreshold = 25.0; // 25°C
    private readonly double _lowThreshold = 18.0;  // 18°C
    private bool _highAlertSent = false;
    private bool _lowAlertSent = false;

    public TemperatureMonitor(IHaContext ha, ILogger<TemperatureMonitor> logger)
    {
        // Subscribe to temperature sensor state changes
        ha.Entity("sensor.living_room_temperature")
            .StateChanges()
            .Subscribe(change =>
            {
                var newState = change.New?.State;
                
                if (double.TryParse(newState, out var temperature))
                {
                    logger.LogDebug("Temperature changed to {Temperature}°C", temperature);

                    if (temperature > _highThreshold && !_highAlertSent)
                    {
                        logger.LogWarning("Temperature too high: {Temperature}°C", temperature);
                        SendNotification(ha, $"Temperature is too high: {temperature}°C");
                        _highAlertSent = true;
                        _lowAlertSent = false;
                    }
                    else if (temperature < _lowThreshold && !_lowAlertSent)
                    {
                        logger.LogWarning("Temperature too low: {Temperature}°C", temperature);
                        SendNotification(ha, $"Temperature is too low: {temperature}°C");
                        _lowAlertSent = true;
                        _highAlertSent = false;
                    }
                    else if (temperature >= _lowThreshold && temperature <= _highThreshold)
                    {
                        // Temperature is back to normal, reset alerts
                        if (_highAlertSent || _lowAlertSent)
                        {
                            logger.LogInformation("Temperature back to normal: {Temperature}°C", temperature);
                            _highAlertSent = false;
                            _lowAlertSent = false;
                        }
                    }
                }
            });

        logger.LogInformation("TemperatureMonitor initialized with high threshold: {High}°C, low threshold: {Low}°C", 
            _highThreshold, _lowThreshold);
    }

    private static void SendNotification(IHaContext ha, string message)
    {
        ha.CallService("notify", "notify", data: new 
        { 
            message = message,
            title = "Temperature Alert"
        });
    }
}
