using System.Reactive.Concurrency;
using HomeAutomations.Models;
using HomeAutomations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;

namespace HomeAutomations.Hosts;

public class UnifiBackgroundService(
    INetDaemonScheduler scheduler,
    IOptions<UnifiConfig> config,
    IServiceScopeFactory serviceScopeFactory,
    UnifiData data, 
    ILogger<UnifiBackgroundService> logger) : IHostedService
{
    private IDisposable? _schedule = null;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _schedule = scheduler.RunEvery(TimeSpan.FromSeconds(config.Value.PollIntervalSeconds),
            async void () =>
            {
                try
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<IUnifiClient>();
                    var devices = await client.GetDevicesAsync(cancellationToken);
                    data.SetCurrent(devices.Data);

                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to poll unifi controller");
                }
            });
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _schedule?.Dispose();
        return Task.CompletedTask;
    }
}