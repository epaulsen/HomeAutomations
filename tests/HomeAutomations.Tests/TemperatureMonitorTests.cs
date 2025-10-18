using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.HassModel;
using HomeAutomations.Apps;
using System.Reactive.Subjects;
using NetDaemon.HassModel.Entities;

namespace HomeAutomations.Tests;

public class TemperatureMonitorTests
{
    [Fact]
    public void Constructor_ShouldLogInitializationWithThresholds()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<TemperatureMonitor>>();
        
        // Setup StateAllChanges to return an observable
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var monitor = new TemperatureMonitor(mockHaContext.Object, mockLogger.Object);

        // Assert
        Assert.NotNull(monitor);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TemperatureMonitor initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
