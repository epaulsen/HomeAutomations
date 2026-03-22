using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;

namespace HomeAutomations.Apps.NordPoolApp;

[NetDaemonApp]
public class NordPoolSensorApp(
    NordPoolSensor nordPoolSensor,
    NordPoolSubsidizedSensor nordPoolSubsidizedSensor,
    ILogger<NordPoolSensorApp> logger) : IAsyncInitializable
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("NordPoolSensorApp initialized");
        await nordPoolSensor.InitializeAsync(cancellationToken);
        await nordPoolSubsidizedSensor.InitializeAsync(cancellationToken);
    }

    public void Dispose()
    {
    }
}