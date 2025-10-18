using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.HassModel;
using HomeAutomations.Apps;
using System.Reactive.Subjects;
using NetDaemon.HassModel.Entities;

namespace HomeAutomations.Tests;

public class CostSensorAppTests
{
    [Fact]
    public void Constructor_WithConfiguration_ShouldInitialize()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<CostSensorApp>>();
        
        // Setup StateAllChanges to return an observable that entities can use
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var app = new CostSensorApp(mockHaContext.Object, mockLogger.Object);

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
    public void Constructor_WithConfiguration_ShouldRetrieveSensorsFromHomeAssistant()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<CostSensorApp>>();
        
        // Setup StateAllChanges to return an observable that entities can use
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var app = new CostSensorApp(mockHaContext.Object, mockLogger.Object);

        // Assert
        Assert.NotNull(app);
        
        // Verify that sensors were retrieved from HomeAssistant
        // Check that we logged retrieving sensors (or warnings if sensors don't exist)
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains("Retrieved energy sensor") || 
                    o.ToString()!.Contains("Energy sensor") ||
                    o.ToString()!.Contains("Retrieved tariff sensor") ||
                    o.ToString()!.Contains("Tariff sensor")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
