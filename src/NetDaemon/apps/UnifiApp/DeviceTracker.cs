using HomeAutomations.Models;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.apps.UnifiApp;

public class DeviceTracker(
    IHaContext context,
    IMqttEntityManager manager,
    DeviceTrackerConfig config)
{
    private string? state = null;
    
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
        string newState = isHome ? "home" : "not_home";
        await manager.SetStateAsync(config.UniqueId, newState);
    }
}