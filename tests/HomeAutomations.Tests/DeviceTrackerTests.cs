using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.HassModel;
using NetDaemon.Extensions.MqttEntityManager;
using HomeAutomations.apps.UnifiApp;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeAutomations.Tests;

public class DeviceTrackerTests
{
    [Fact]
    public async Task SetState_WhenDeviceIsHome_ShouldSetStateToHome()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var mockTimeProvider = new Mock<TimeProvider>();
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
            NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act
        await tracker.SetState(isHome: true);

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
        var mockTimeProvider = new Mock<TimeProvider>();
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
            NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act
        await tracker.SetState(isHome: false);

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
        var mockTimeProvider = new Mock<TimeProvider>();

        var baseTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
            NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act - First, device is home
        await tracker.SetState(isHome: true);
        mockEntityManager.Invocations.Clear();

        // Device disappears after 30 seconds
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime.AddSeconds(30));
        await tracker.SetState(isHome: false);

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
        var mockTimeProvider = new Mock<TimeProvider>();

        var baseTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
            NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act - First, device is home
        await tracker.SetState(isHome: true);
        mockEntityManager.Invocations.Clear();

        // Device disappears after 60 seconds
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime.AddSeconds(60));
        await tracker.SetState(isHome: false);

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
        var mockTimeProvider = new Mock<TimeProvider>();

        var baseTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker = new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
            NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act - First, device is home
        await tracker.SetState(isHome: true);
        mockEntityManager.Invocations.Clear();

        // Device disappears after 90 seconds
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime.AddSeconds(90));
        await tracker.SetState(isHome: false);

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
        var mockTimeProvider = new Mock<TimeProvider>();

        var baseTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker =
            new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
                NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act - Device is home
        await tracker.SetState(isHome: true);
        mockEntityManager.Invocations.Clear();

        // Device disappears for 30 seconds (within debounce window)
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime.AddSeconds(30));
        await tracker.SetState(isHome: false);

        // Device reappears
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime.AddSeconds(45));
        await tracker.SetState(isHome: true);

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
        var mockTimeProvider = new Mock<TimeProvider>();

        var baseTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker =
            new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
                NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act - Device is home first
        await tracker.SetState(isHome: true);
        mockEntityManager.Invocations.Clear();

        // Device disappears for specified seconds
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime.AddSeconds(seconds));
        await tracker.SetState(isHome: false);

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
        var mockTimeProvider = new Mock<TimeProvider>();

        var baseTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime);

        var config = new DeviceTrackerConfig
        {
            Name = "Test Device",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        var tracker =
            new DeviceTracker(mockHaContext.Object, mockEntityManager.Object, config, mockTimeProvider.Object,
                NullLogger.Instance);
        await tracker.InitializeAsync();

        // Act - Device is home first
        await tracker.SetState(isHome: true);
        mockEntityManager.Invocations.Clear();

        // Device disappears for specified seconds
        mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(baseTime.AddSeconds(seconds));
        await tracker.SetState(isHome: false);

        // Assert - should set to not_home
        mockEntityManager.Verify(
            x => x.SetStateAsync(config.UniqueId, "not_home"),
            Times.Once);
    }
}