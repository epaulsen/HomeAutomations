using HomeAutomations.Hosts;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.UnifiApp;

[NetDaemonApp]
public class NetworkDeviceTrackerApp(
    IMqttEntityManager manager,
    UnifiData data,
    ILogger<NetworkDeviceTrackerApp> logger) : UnifiAppBase, IAsyncInitializable, IDisposable
{
    private readonly List<VlanDeviceCountSensor> _sensors = new();
    private IDisposable? _subscription;

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
            var vlan = new VlanDeviceCountSensor(manager, sensor, logger);
            await vlan.InitializeAsync();
            _sensors.Add(vlan);
        }

        _subscription = data.ClientDevices.SubscribeAsync(async devices =>
        {
            foreach (var sensor in _sensors)
            {
                await sensor.UpdateCountAsync(devices);
            }
        });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}