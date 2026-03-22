using HomeAutomations.Hosts;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.UnifiApp;

[NetDaemonApp]
public class DeviceTrackerApp(
    IMqttEntityManager manager,
    UnifiData data,
    TimeProvider timeProvider,
    ILogger<DeviceTrackerApp> logger) : UnifiAppBase, IAsyncInitializable, IDisposable
{
    private List<DeviceTracker> _trackers = new();
    private IDisposable? _subscription;

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
            var tracker = new DeviceTracker(manager, trackerConfig, timeProvider, logger);
            await tracker.InitializeAsync();
            _trackers.Add(tracker);
        }

        _subscription = data.ClientDevices.SubscribeAsync(async data =>
        {
            foreach (var deviceTracker in _trackers)
            {
                bool isHome = data.Any(d =>
                    d.MacAddress?.Equals(deviceTracker.MacAddress, StringComparison.InvariantCultureIgnoreCase) == true);
                await deviceTracker.SetState(isHome);
            }
        });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}