using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.apps.UnifiApp;

public class DeviceTracker(
    IHaContext context,
    IMqttEntityManager manager,
    DeviceTrackerConfig config,
    TimeProvider timeProvider,
    ILogger logger)
{
    private string? state = null;
    private DateTime? lastSeenTime = null;

    public async Task InitializeAsync()
    {
        var trackerEntity = context.Entity(config.UniqueId);
        state = trackerEntity.State;
        if (string.IsNullOrEmpty(state))
        {
            // Create entity
            await manager.CreateAsync(
                entityId: config.UniqueId,
                options: new EntityCreationOptions()
                {
                    DeviceClass = "device_tracker",
                    Name = config.Name,
                    UniqueId = config.UniqueId
                });
        }
    }

    public string MacAddress => config.MacAddress;

    public async Task SetState(bool isHome)
    {
        var currentTime = timeProvider.GetUtcNow().UtcDateTime;
        
        if (isHome)
        {
            // Device is present, update last seen time and set to home
            lastSeenTime = currentTime;
            string newState = "home";
            await manager.SetStateAsync(config.UniqueId, newState);
            logger.LogInformation("{Person} is home", config.Name);
        }
        else
        {
            // Device is not present, only set to not_home if it hasn't been seen for more than 60 seconds
            if (lastSeenTime.HasValue)
            {
                var timeSinceLastSeen = currentTime - lastSeenTime.Value;
                if (timeSinceLastSeen.TotalSeconds >= 60)
                {
                    string newState = "not_home";
                    await manager.SetStateAsync(config.UniqueId, newState);
                    logger.LogInformation("{Person} is not_home", config.Name);
                }
                // else: still within 60 second window, don't change state
            }
            else
            {
                // Never seen before, set to not_home immediately
                string newState = "not_home";
                await manager.SetStateAsync(config.UniqueId, newState);
                logger.LogInformation("{Person} is not_home", config.Name);
            }
        }
    }
}