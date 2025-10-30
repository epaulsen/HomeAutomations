using HomeAutomations.Models;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.NordPoolApp;

[NetDaemonApp]
public class NordPoolSensorApp(NordPoolSensor nordPoolSensor, NordPoolSubsidizedSensor nordPoolSubsidizedSensor) : IAsyncInitializable
{
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await nordPoolSensor.InitializeAsync(cancellationToken);
        await nordPoolSubsidizedSensor.InitializeAsync(cancellationToken);
    }
    
    public void Dispose()
    {
        
    }
}