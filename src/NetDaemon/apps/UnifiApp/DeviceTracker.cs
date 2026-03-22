using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.UnifiApp;

public class DeviceTracker(
    IMqttEntityManager manager,
    DeviceTrackerConfig config,
    TimeProvider timeProvider,
    ILogger logger)
{
    private const string StateHome = "home";
    private const string StateNotHome = "not_home";

    private DateTime? _lastSeenTime = null;

    public async Task InitializeAsync()
    {
        try
        {
            await manager.CreateAsync(
                entityId: config.UniqueId,
                options: new EntityCreationOptions()
                {
                    DeviceClass = "device_tracker",
                    Name = config.Name,
                    UniqueId = config.UniqueId,
                    Persist = true
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create entity for {EntityId}", config.UniqueId);
        }
    }

    public string MacAddress => config.MacAddress;

    public async Task SetState(bool isHome)
    {
        var currentTime = timeProvider.GetUtcNow().UtcDateTime;

        if (isHome)
        {
            _lastSeenTime = currentTime;
            try
            {
                await manager.SetStateAsync(config.UniqueId, StateHome);
                logger.LogInformation("{Person} is home", config.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set state for {EntityId}", config.UniqueId);
            }
        }
        else
        {
            if (_lastSeenTime.HasValue)
            {
                var timeSinceLastSeen = currentTime - _lastSeenTime.Value;
                if (timeSinceLastSeen.TotalSeconds >= 60)
                {
                    try
                    {
                        await manager.SetStateAsync(config.UniqueId, StateNotHome);
                        logger.LogInformation("{Person} is not_home", config.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to set state for {EntityId}", config.UniqueId);
                    }
                }
                // else: still within 60 second window, don't change state
            }
            else
            {
                // Never seen before, set to not_home immediately
                try
                {
                    await manager.SetStateAsync(config.UniqueId, StateNotHome);
                    logger.LogInformation("{Person} is not_home", config.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to set state for {EntityId}", config.UniqueId);
                }
            }
        }
    }
}