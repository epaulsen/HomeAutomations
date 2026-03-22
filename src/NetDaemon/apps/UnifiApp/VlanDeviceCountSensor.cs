using System.Globalization;
using System.Net;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.Extensions.MqttEntityManager;

namespace HomeAutomations.Apps.UnifiApp;

public class VlanDeviceCountSensor(
    IMqttEntityManager manager,
    NetworkConfig config,
    ILogger logger)
{
    public async Task InitializeAsync()
    {
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
        var ipAddresses = devices
            .Where(d =>
            {
                if (string.IsNullOrWhiteSpace(d.IpAddress))
                {
                    logger.LogWarning("Skipping device {Id} with null/empty IpAddress", d.Id);
                    return false;
                }
                return true;
            })
            .Select(d => IPNetwork2.Parse(d.IpAddress));
        var count = ipAddresses.Count(ip => netWork.Contains(ip));
        await manager.SetStateAsync(config.UniqueId, count.ToString(CultureInfo.InvariantCulture));
    }
}