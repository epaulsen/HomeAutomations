using HomeAutomations.Apps.CostSensor;
using HomeAutomations.Hosts;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.apps.UnifiApp;

[NetDaemonApp]
public class DeviceTrackerApp(
    IHaContext context,
    IMqttEntityManager manager,
    UnifiData data,
    TimeProvider timeProvider,
    ILogger<DeviceTrackerApp> logger) : UnifiAppBase, IAsyncInitializable
{
    private List<DeviceTracker> _trackers = new();

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _config ??= LoadConfig();
        if (_config == null)
        {
            logger.LogCritical("No config loaded");
            return;
        }
        foreach (var trackerConfig in _config.Trackers)
        {
            var tracker = new DeviceTracker(context, manager, trackerConfig, timeProvider);
            await tracker.InitializeAsync();
            _trackers.Add(tracker);
        }

        data.ClientDevices.SubscribeAsync(async data =>
        {
            foreach (var deviceTracker in _trackers)
            {
                bool isHome = data.Any(d =>
                    d.MacAddress.Equals(deviceTracker.MacAddress, StringComparison.InvariantCultureIgnoreCase));
                await deviceTracker.SetState(isHome);
            }
        });
    }
}