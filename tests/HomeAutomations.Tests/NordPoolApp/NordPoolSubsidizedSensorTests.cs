using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;
using HomeAutomations.Apps.NordPoolApp;

namespace HomeAutomations.Tests.NordPoolApp;

public class NordPoolSubsidizedSensorTests
{
    private static NordPoolSubsidizedSensor CreateSensor()
    {
        var mockContext = new Mock<IHaContext>();
        var mockManager = new Mock<IMqttEntityManager>();
        var mockLogger = new Mock<ILogger<NordPoolSubsidizedSensor>>();
        return new NordPoolSubsidizedSensor(mockContext.Object, mockManager.Object, mockLogger.Object);
    }

    private static double? InvokeComputeSubsidizedPrice(NordPoolSubsidizedSensor sensor, double? price)
    {
        var method = typeof(NordPoolSubsidizedSensor)
            .GetMethod("ComputeSubsidizedPrice", BindingFlags.NonPublic | BindingFlags.Instance);
        return (double?)method!.Invoke(sensor, new object?[] { price });
    }

    [Fact]
    public void ComputeSubsidizedPrice_NullPrice_ReturnsNull()
    {
        // Arrange
        var sensor = CreateSensor();

        // Act
        var result = InvokeComputeSubsidizedPrice(sensor, null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ComputeSubsidizedPrice_ZeroPrice_ReturnsZeroUnchanged()
    {
        // Arrange
        var sensor = CreateSensor();

        // Act
        var result = InvokeComputeSubsidizedPrice(sensor, 0.0);

        // Assert — 0.0 < 0.9375, so no subsidy is applied
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void ComputeSubsidizedPrice_NegativePrice_ReturnsNegativeUnchanged()
    {
        // Arrange
        var sensor = CreateSensor();

        // Act
        var result = InvokeComputeSubsidizedPrice(sensor, -1.0);

        // Assert — -1.0 < 0.9375, so no subsidy calculation, returned as-is
        Assert.Equal(-1.0, result);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.9374)]
    [InlineData(0.1)]
    public void ComputeSubsidizedPrice_PriceBelowThreshold_ReturnsOriginalPrice(double price)
    {
        // Arrange
        var sensor = CreateSensor();

        // Act
        var result = InvokeComputeSubsidizedPrice(sensor, price);

        // Assert — below 0.9375 NOK/kWh: no subsidy, price returned as-is
        Assert.Equal(price, result);
    }

    [Fact]
    public void ComputeSubsidizedPrice_PriceAtThreshold_ReturnsThresholdValue()
    {
        // Arrange
        var sensor = CreateSensor();
        const double threshold = 0.9375;

        // Act
        var result = InvokeComputeSubsidizedPrice(sensor, threshold);

        // Assert — exactly at threshold: subsidy = 0.9375 + (0.9375 - 0.9375) * 0.1 = 0.9375
        Assert.Equal(threshold, result);
    }

    [Theory]
    [InlineData(2.0, 1.04375)]   // 0.9375 + (2.0 - 0.9375) * 0.1 = 0.9375 + 0.10625 = 1.04375
    [InlineData(1.9375, 1.0375)] // 0.9375 + (1.9375 - 0.9375) * 0.1 = 0.9375 + 0.1 = 1.0375
    [InlineData(3.9375, 1.2375)] // 0.9375 + (3.9375 - 0.9375) * 0.1 = 0.9375 + 0.3 = 1.2375
    public void ComputeSubsidizedPrice_PriceAboveThreshold_AppliesSubsidyFormula(double price, double expectedSubsidy)
    {
        // Arrange
        var sensor = CreateSensor();

        // Act
        var result = InvokeComputeSubsidizedPrice(sensor, price);

        // Assert — formula: 0.9375 + (price - 0.9375) * 0.1 (consumer pays 10% of amount above threshold)
        Assert.NotNull(result);
        Assert.Equal(expectedSubsidy, result!.Value, precision: 10);
    }

    [Fact]
    public void ComputeSubsidizedPrice_HighPrice_SubsidyIsSignificantlyLowerThanRawPrice()
    {
        // Arrange
        var sensor = CreateSensor();
        const double highPrice = 5.0;

        // Act
        var result = InvokeComputeSubsidizedPrice(sensor, highPrice);

        // Assert — subsidy result should be well below raw price
        Assert.NotNull(result);
        Assert.True(result!.Value < highPrice, $"Subsidized price {result} should be less than raw price {highPrice}");

        // Expected: 0.9375 + (5.0 - 0.9375) * 0.1 = 0.9375 + 0.40625 = 1.34375
        Assert.Equal(1.34375, result!.Value, precision: 10);
    }
}
