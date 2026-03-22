using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;
using NetDaemon.Extensions.Scheduler;
using HomeAutomations.Models;

namespace HomeAutomations.Tests.NordPoolApp;

public class NordPoolDataStorageTests
{
    private static readonly TimeZoneInfo NorwegianTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    private static NordPoolDataStorage CreateStorage(out Mock<INetDaemonScheduler> schedulerMock)
    {
        schedulerMock = new Mock<INetDaemonScheduler>();
        schedulerMock
            .Setup(x => x.RunAt(It.IsAny<DateTimeOffset>(), It.IsAny<Action>()))
            .Returns(Mock.Of<IDisposable>());
        schedulerMock
            .Setup(x => x.RunEvery(It.IsAny<TimeSpan>(), It.IsAny<Action>()))
            .Returns(Mock.Of<IDisposable>());
        var mockLogger = new Mock<ILogger<NordPoolDataStorage>>();
        return new NordPoolDataStorage(schedulerMock.Object, mockLogger.Object);
    }

    private static NordpoolData CreatePricesForNorwegianHour(DateTimeOffset norwegianNow, double no2Price = 1000.0)
    {
        var start = new DateTimeOffset(norwegianNow.Year, norwegianNow.Month, norwegianNow.Day,
            norwegianNow.Hour, 0, 0, norwegianNow.Offset);
        var end = start.AddHours(1);

        return new NordpoolData
        {
            MultiAreaEntries = new List<MultiAreaEntry>
            {
                new MultiAreaEntry
                {
                    DeliveryStart = start.UtcDateTime,
                    DeliveryEnd = end.UtcDateTime,
                    EntryPerArea = new Dictionary<string, double> { { "NO2", no2Price } }
                }
            }
        };
    }

    [Fact]
    public void HasPricesForToday_WhenNoPricesStored_ReturnsFalse()
    {
        // Arrange
        var storage = CreateStorage(out _);

        // Act & Assert
        Assert.False(storage.HasPricesForToday);
    }

    [Fact]
    public void HasPricesForToday_WhenPricesForTodayAreAdded_ReturnsTrue()
    {
        // Arrange
        var storage = CreateStorage(out _);
        var now = DateTimeOffset.Now;
        var today = new DateOnly(now.Year, now.Month, now.Day);
        var norwegianNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);

        // Act
        storage.AddPrices(today, CreatePricesForNorwegianHour(norwegianNow));

        // Assert
        Assert.True(storage.HasPricesForToday);
    }

    [Fact]
    public void HasPricesForTomorrow_WhenNoPricesStored_ReturnsFalse()
    {
        // Arrange
        var storage = CreateStorage(out _);

        // Act & Assert
        Assert.False(storage.HasPricesForTomorrow);
    }

    [Fact]
    public void AddPrices_SameDateTwice_IgnoresSecondAdd()
    {
        // Arrange
        var storage = CreateStorage(out _);
        var now = DateTimeOffset.Now;
        var today = new DateOnly(now.Year, now.Month, now.Day);
        var norwegianNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);
        var firstData = CreatePricesForNorwegianHour(norwegianNow, no2Price: 1000.0);
        var secondData = CreatePricesForNorwegianHour(norwegianNow, no2Price: 9999.0);

        // Act
        storage.AddPrices(today, firstData);
        storage.AddPrices(today, secondData);
        var result = storage.CurrentHourlyPrice();

        // Assert — first data wins; 1000.0 / 1000 * 1.25 = 1.25
        Assert.NotNull(result);
        Assert.Equal(1.25, result!.EntryPerArea["NO2"], precision: 10);
    }

    [Fact]
    public void CurrentHourlyPrice_WhenPricesAvailable_ReturnsEntryForCurrentHour()
    {
        // Arrange
        var storage = CreateStorage(out _);
        var norwegianNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);
        var today = new DateOnly(norwegianNow.Year, norwegianNow.Month, norwegianNow.Day);
        storage.AddPrices(today, CreatePricesForNorwegianHour(norwegianNow, no2Price: 1000.0));

        // Act
        var result = storage.CurrentHourlyPrice();

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.EntryPerArea.ContainsKey("NO2"));
    }

    [Fact]
    public void CurrentHourlyPrice_VatCalculation_AppliesDivideBy1000AndMultiplyBy1Point25()
    {
        // Arrange
        var storage = CreateStorage(out _);
        var norwegianNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);
        var today = new DateOnly(norwegianNow.Year, norwegianNow.Month, norwegianNow.Day);
        const double rawPrice = 2000.0; // MWh price
        storage.AddPrices(today, CreatePricesForNorwegianHour(norwegianNow, no2Price: rawPrice));

        // Act
        var result = storage.CurrentHourlyPrice();

        // Assert — formula: rawPrice / 1000 * 1.25 = 2.5
        Assert.NotNull(result);
        Assert.Equal(2.5, result!.EntryPerArea["NO2"], precision: 10);
    }

    [Fact]
    public void CurrentHourlyPrice_Averaging_UsesAverageOfMultipleEntriesInSameHour()
    {
        // Arrange
        var storage = CreateStorage(out _);
        var norwegianNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);
        var today = new DateOnly(norwegianNow.Year, norwegianNow.Month, norwegianNow.Day);

        // Two entries in same hour with different prices
        var start = new DateTimeOffset(norwegianNow.Year, norwegianNow.Month, norwegianNow.Day,
            norwegianNow.Hour, 0, 0, norwegianNow.Offset);

        var data = new NordpoolData
        {
            MultiAreaEntries = new List<MultiAreaEntry>
            {
                new MultiAreaEntry
                {
                    DeliveryStart = start.UtcDateTime,
                    DeliveryEnd = start.AddMinutes(30).UtcDateTime,
                    EntryPerArea = new Dictionary<string, double> { { "NO2", 1000.0 } }
                },
                new MultiAreaEntry
                {
                    DeliveryStart = start.AddMinutes(30).UtcDateTime,
                    DeliveryEnd = start.AddHours(1).UtcDateTime,
                    EntryPerArea = new Dictionary<string, double> { { "NO2", 3000.0 } }
                }
            }
        };
        storage.AddPrices(today, data);

        // Act
        var result = storage.CurrentHourlyPrice();

        // Assert — average of [1000, 3000] = 2000; then / 1000 * 1.25 = 2.5
        Assert.NotNull(result);
        Assert.Equal(2.5, result!.EntryPerArea["NO2"], precision: 10);
    }

    [Fact]
    public void CurrentHourlyPrice_WhenNoPricesStored_ReturnsNull()
    {
        // Arrange
        var storage = CreateStorage(out _);

        // Act
        var result = storage.CurrentHourlyPrice();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AddPrices_EmitsOnCurrentPriceObservable()
    {
        // Arrange
        var storage = CreateStorage(out _);
        var norwegianNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone);
        var today = new DateOnly(norwegianNow.Year, norwegianNow.Month, norwegianNow.Day);

        MultiAreaEntry? emitted = null;
        storage.CurrentPrice.Subscribe(value => emitted = value);

        // Act
        storage.AddPrices(today, CreatePricesForNorwegianHour(norwegianNow, no2Price: 1600.0));

        // Assert — observable should have emitted the current hour price
        Assert.NotNull(emitted);
        // 1600.0 / 1000 * 1.25 = 2.0
        Assert.Equal(2.0, emitted!.EntryPerArea["NO2"], precision: 10);
    }
}
