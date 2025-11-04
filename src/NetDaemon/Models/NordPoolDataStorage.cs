using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using NetDaemon.Extensions.Scheduler;

namespace HomeAutomations.Models;

public class NordPoolDataStorage
{
    private static readonly TimeZoneInfo NorwegianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
    private Dictionary<DateOnly, NordpoolData> _nordpoolData = new();
    private IDisposable? _timer = null;
    private readonly INetDaemonScheduler _scheduler;
    private readonly ILogger<NordPoolDataStorage> _logger;
    private readonly Subject<MultiAreaEntry?> _currentPrice = new();

    public Subject<MultiAreaEntry?> CurrentPrice => _currentPrice;

    public NordPoolDataStorage(INetDaemonScheduler scheduler, ILogger<NordPoolDataStorage> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
        DateTimeOffset future = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone).AddDays(1);
        future -= future.TimeOfDay;
        future = future.AddMinutes(1);
        _scheduler.RunAt(future, PurgeYesterDay);

        UpdateCurrentPrice();
    }

    public bool HasPricesForToday
    {
        get
        {
            var now = DateTimeOffset.Now;
            return _nordpoolData.ContainsKey(new DateOnly(now.Year, now.Month, now.Day));
        }
    }

    public bool HasPricesForTomorrow
    {
        get
        {
            var tomorrow = DateTimeOffset.Now.AddDays(1);
            return _nordpoolData.ContainsKey(new DateOnly(tomorrow.Year, tomorrow.Month, tomorrow.Day));
        }
    }

    public void AddPrices(DateOnly date, NordpoolData prices)
    {
        if (_nordpoolData.ContainsKey(date))
        {
            return;
        }
        _nordpoolData[date] = prices;
        _currentPrice.OnNext(CurrentHourlyPrice());
        _logger.LogInformation("Added price for {date}", date);
    }

    public void UpdateCurrentPrice()
    {
        _logger.LogInformation("Updating current price for {date}", DateTimeOffset.Now);
        var currentPrice = CurrentHourlyPrice();
        if (currentPrice == null)
        {
            _logger.LogWarning("No current price for {date}", DateTimeOffset.Now);
        }
        _currentPrice.OnNext(currentPrice);
        var next = DateTimeOffset.UtcNow.AddHours(1);
        next = next.AddMinutes(next.Minute * -1);

        _logger.LogInformation("Prices updated, next update at {next}", next);
        _scheduler.RunAt(next, UpdateCurrentPrice);
    }

    public MultiAreaEntry? CurrentHourlyPrice()
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);
        if (_nordpoolData.TryGetValue(new DateOnly(now.Year, now.Month, now.Day), out var entries))
        {
            var start = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
            var end = start.AddHours(1);
            var hour = entries.MultiAreaEntries?
                .Where(ma => ma.DeliveryEnd <= end && ma.DeliveryStart >= start);

            if (hour == null)
            {
                return null;
            }

            return ComputeAverage(hour);
        }

        return null;
    }

    private static MultiAreaEntry? ComputeAverage(IEnumerable<MultiAreaEntry> entries)
    {
        var list = entries.ToList();

        if (!list.Any())
        {
            return null;
        }

        var areas = new Dictionary<string, double>();
        var keys = list.First().EntryPerArea?.Keys;
        if (keys == null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            areas[key] = list.Select(ma => ma.EntryPerArea[key]).Average() / 1000 * 1.25;  // MWh -> kWh (and include VAT)  
        }

        return new MultiAreaEntry()
        {
            DeliveryStart = list.MinBy(ma => ma.DeliveryStart)!.DeliveryStart,
            DeliveryEnd = list.MaxBy(ma => ma.DeliveryEnd)!.DeliveryEnd,
            EntryPerArea = areas
        };
    }

    private void PurgeYesterDay()
    {
        var yesterday = DateTimeOffset.Now.AddDays(-1);
        var date = new DateOnly(yesterday.Year, yesterday.Month, yesterday.Day);
        if (_nordpoolData.ContainsKey(date))
        {
            _nordpoolData.Remove(date);
        }

        if (_timer == null)
        {
            _timer = _scheduler.RunEvery(TimeSpan.FromDays(1), PurgeYesterDay);
        }
    }
}