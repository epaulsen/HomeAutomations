using HomeAutomations.Hosts;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.apps.UnifiApp;

[NetDaemonApp]
public class NetworkDeviceTrackerApp(
    IHaContext context,
    IMqttEntityManager manager,
    UnifiData data,
    ILogger<NetworkDeviceTrackerApp> logger) : UnifiAppBase, IAsyncInitializable
{
    private readonly List<VlanDeviceCountSensor> _sensors = new();

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _config ??= LoadConfig();
        if (_config == null)
        {
            logger.LogCritical("No config loaded");
            return;
        }

        foreach (var sensor in _config.Networks)
        {
            var vlan = new VlanDeviceCountSensor(context, manager, sensor);
            await vlan.InitializeAsync();
            _sensors.Add(vlan);
        }

        data.ClientDevices.SubscribeAsync(async devices =>
        {
            foreach (var sensor in _sensors)
            {
                await sensor.UpdateCountAsync(devices);
            }
        });
    }
}