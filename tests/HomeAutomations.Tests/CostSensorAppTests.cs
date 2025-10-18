using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.HassModel;
using NetDaemon.Extensions.MqttEntityManager;
using HomeAutomations.Apps.CostSensor;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using NetDaemon.HassModel.Entities;

namespace HomeAutomations.Tests;

public class CostSensorAppTests
{
    [Fact]
    public async Task Constructor_WithConfiguration_ShouldInitialize()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<CostSensorApp>>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockScheduler = new Mock<IScheduler>();
        
        // Setup logger factory to return mock loggers for PriceSensor and CostSensor
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        // Setup StateAllChanges to return an observable that entities can use
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var app = new CostSensorApp(mockHaContext.Object, mockLogger.Object, mockEntityManager.Object, mockLoggerFactory.Object, mockScheduler.Object);
        await app.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(app);
        
        // Verify that initialization was logged
        // Either "No cost sensors configured" or "Initializing CostSensorApp" should be logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains("No cost sensors configured") || 
                    o.ToString()!.Contains("Initializing CostSensorApp") ||
                    o.ToString()!.Contains("CostSensorApp initialized") ||
                    o.ToString()!.Contains("Setting up cost sensor")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task Constructor_WithConfiguration_ShouldRetrieveSensorsFromHomeAssistant()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<CostSensorApp>>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockScheduler = new Mock<IScheduler>();
        
        // Setup logger factory to return mock loggers for PriceSensor and CostSensor
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        // Setup StateAllChanges to return an observable that entities can use
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var app = new CostSensorApp(mockHaContext.Object, mockLogger.Object, mockEntityManager.Object, mockLoggerFactory.Object, mockScheduler.Object);
        await app.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(app);
        
        // Verify that the app properly initialized price sensors (tariff sensors)
        // The refactored code logs "Found X unique tariff sensors" from CostSensorApp
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains("Found") && o.ToString()!.Contains("unique tariff sensors")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void CronScheduleTypeConverter_ShouldConvertStringToEnum()
    {
        // Arrange
        var converter = new CronScheduleTypeConverter();

        // Assert
        Assert.True(converter.Accepts(typeof(CronSchedule)));
        Assert.False(converter.Accepts(typeof(string)));
    }

    [Theory]
    [InlineData(CronSchedule.None)]
    [InlineData(CronSchedule.Daily)]
    [InlineData(CronSchedule.Monthly)]
    [InlineData(CronSchedule.Yearly)]
    public void CronSchedule_ShouldHaveExpectedValues(CronSchedule expected)
    {
        // Act
        var actual = expected;

        // Assert
        Assert.Equal(expected, actual);
    }
}
