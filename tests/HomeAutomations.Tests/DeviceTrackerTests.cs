using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.HassModel;
using NetDaemon.Extensions.MqttEntityManager;
using HomeAutomations.apps.UnifiApp;
using HomeAutomations.Models;

namespace HomeAutomations.Tests;

public class DeviceTrackerTests
{
    [Fact]
    public async Task SetState_WhenDeviceIsHome_ShouldSetStateToHome()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var currentTime = DateTime.UtcNow;

        // Act
        await tracker.SetState(isHome: true, currentTime);

        // Assert
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "home"),
            Times.Once);
    }

    [Fact]
    public async Task SetState_WhenDeviceNotSeenBefore_ShouldSetToNotHomeImmediately()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var currentTime = DateTime.UtcNow;

        // Act
        await tracker.SetState(isHome: false, currentTime);

        // Assert
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "not_home"),
            Times.Once);
    }

    [Fact]
    public async Task SetState_WhenDeviceDisappearsForLessThan60Seconds_ShouldNotSetToNotHome()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var baseTime = DateTime.UtcNow;

        // Act - First, device is home
        await tracker.SetState(isHome: true, baseTime);
        mockEntityManager.Invocations.Clear();

        // Device disappears for 30 seconds
        await tracker.SetState(isHome: false, baseTime.AddSeconds(30));

        // Assert - should NOT set to not_home
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "not_home"),
            Times.Never);
    }

    [Fact]
    public async Task SetState_WhenDeviceDisappearsFor60SecondsOrMore_ShouldSetToNotHome()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var baseTime = DateTime.UtcNow;

        // Act - First, device is home
        await tracker.SetState(isHome: true, baseTime);
        mockEntityManager.Invocations.Clear();

        // Device disappears for 60 seconds
        await tracker.SetState(isHome: false, baseTime.AddSeconds(60));

        // Assert - should set to not_home
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "not_home"),
            Times.Once);
    }

    [Fact]
    public async Task SetState_WhenDeviceDisappearsFor90Seconds_ShouldSetToNotHome()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var baseTime = DateTime.UtcNow;

        // Act - First, device is home
        await tracker.SetState(isHome: true, baseTime);
        mockEntityManager.Invocations.Clear();

        // Device disappears for 90 seconds
        await tracker.SetState(isHome: false, baseTime.AddSeconds(90));

        // Assert - should set to not_home
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "not_home"),
            Times.Once);
    }

    [Fact]
    public async Task SetState_WhenDeviceReappearsWithin60Seconds_ShouldSetToHome()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var baseTime = DateTime.UtcNow;

        // Act - Device is home
        await tracker.SetState(isHome: true, baseTime);
        mockEntityManager.Invocations.Clear();

        // Device disappears for 30 seconds (within debounce window)
        await tracker.SetState(isHome: false, baseTime.AddSeconds(30));

        // Device reappears
        await tracker.SetState(isHome: true, baseTime.AddSeconds(45));

        // Assert - should set to home again
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "home"),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(59)]
    public async Task SetState_DeviceAbsentForLessThan60Seconds_ShouldNotSetNotHome(int seconds)
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var baseTime = DateTime.UtcNow;

        // Act - Device is home first
        await tracker.SetState(isHome: true, baseTime);
        mockEntityManager.Invocations.Clear();

        // Device disappears for specified seconds
        await tracker.SetState(isHome: false, baseTime.AddSeconds(seconds));

        // Assert - should NOT set to not_home
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "not_home"),
            Times.Never);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(61)]
    [InlineData(120)]
    public async Task SetState_DeviceAbsentFor60SecondsOrMore_ShouldSetNotHome(int seconds)
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config);
        await tracker.InitializeAsync();

        var baseTime = DateTime.UtcNow;

        // Act - Device is home first
        await tracker.SetState(isHome: true, baseTime);
        mockEntityManager.Invocations.Clear();

        // Device disappears for specified seconds
        await tracker.SetState(isHome: false, baseTime.AddSeconds(seconds));

        // Assert - should set to not_home
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "not_home"),
            Times.Once);
    }
}
