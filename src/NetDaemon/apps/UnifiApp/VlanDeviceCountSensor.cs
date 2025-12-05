using System.Globalization;
using System.Net;
using HomeAutomations.Models;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.apps.UnifiApp;

public class VlanDeviceCountSensor(
    IHaContext context,
    IMqttEntityManager manager,
    NetworkConfig config)
{
    private int? _currentCount = null;

    public async Task InitializeAsync()
    {
        var trackerEntity = context.Entity(config.UniqueId);
        _currentCount = int.TryParse(trackerEntity.State, CultureInfo.InvariantCulture, out int count) ? count : null;

        // Create entity
        await manager.CreateAsync(
            entityId: config.UniqueId,
            options: new EntityCreationOptions()
            {
                Name = config.Name,
                UniqueId = config.UniqueId,
                Persist = true
            },
            new
            {
                state_class = "measurement",
            });
    }

    public async Task UpdateCountAsync(List<ClientDevice> devices)
    {
        var netWork = IPNetwork2.Parse(config.Vlan);
        var ipAddresses = devices.Select(d => IPNetwork2.Parse(d.IpAddress));
        var count = ipAddresses.Count(ip => netWork.Contains(ip));
        await manager.SetStateAsync(config.UniqueId, count.ToString(CultureInfo.InvariantCulture));
    }
}