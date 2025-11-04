using HomeAutomations.Apps.CostSensor;
using HomeAutomations.Extensions;
using HomeAutomations.Hosts;
using HomeAutomations.Models;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomeAutomations.apps.UnifiApp;

[NetDaemonApp]
public class DeviceTrackerApp(
    IHaContext context,
    IMqttEntityManager manager,
    UnifiData data) : IAsyncInitializable
{
    private UnifiYamlConfig? _config = null;
    private List<DeviceTracker> _trackers = new();

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _config ??= LoadConfig();
        foreach (var trackerConfig in _config.Trackers)
        {
            var tracker = new DeviceTracker(context, manager, trackerConfig);
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

    private UnifiYamlConfig? LoadConfig()
    {
        var file = Path.Combine(ConfigFolder.Path, "unifi.yaml");
        if (!File.Exists(file))
        {
            return null;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<UnifiYamlConfig>(File.ReadAllText(file));
    }
}