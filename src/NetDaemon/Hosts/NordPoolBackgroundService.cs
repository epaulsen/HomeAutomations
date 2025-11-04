using HomeAutomations.Models;
using HomeAutomations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetDaemon.Extensions.Scheduler;

namespace HomeAutomations.Hosts;

public class NordPoolBackgroundService
    (INetDaemonScheduler scheduler, NordPoolDataStorage storage, IServiceScopeFactory scopeFactory, ILogger<NordPoolBackgroundService> logger)
    : IHostedService
{
    private static readonly TimeZoneInfo NorwegianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Fetch(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task Fetch(CancellationToken cancellationToken)
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);

        using var scope = scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<INordpoolApiClient>();

        if (!storage.HasPricesForToday)
        {
            var date = new DateOnly(now.Year, now.Month, now.Day);
            var data = await client.FetchPriceDataAsync(date, cancellationToken);
            ArgumentNullException.ThrowIfNull(data);
            storage.AddPrices(date, data);
        }

        if (!storage.HasPricesForTomorrow && now.Hour > 16)
        {
            var tomorrowTs = now.AddDays(1);
            var tomorrow = new DateOnly(tomorrowTs.Year, tomorrowTs.Month, tomorrowTs.Day);
            var data = await client.FetchPriceDataAsync(tomorrow, cancellationToken);
            ArgumentNullException.ThrowIfNull(data);
            storage.AddPrices(tomorrow, data);
        }

        DateTimeOffset next = new DateTimeOffset(now.Year, now.Month, now.Day, 18, 0, 0, NorwegianTimeZone.BaseUtcOffset);
        if (now.Hour > 16 && storage.HasPricesForTomorrow)
        {
            next = next.AddDays(1);
        }

        scheduler.RunAt(next, async void () =>
        {
            try
            {
                await Fetch(CancellationToken.None);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error fetching price data");
            }
        });
    }
}