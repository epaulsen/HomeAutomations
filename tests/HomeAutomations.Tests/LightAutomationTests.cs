using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.HassModel;
using HomeAutomations.Apps;
using System.Reactive.Subjects;
using NetDaemon.HassModel.Entities;

namespace HomeAutomations.Tests;

public class LightAutomationTests
{
    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<LightAutomation>>();
        
        // Setup StateAllChanges to return an observable
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var automation = new LightAutomation(mockHaContext.Object, mockLogger.Object);

        // Assert
        automation.Should().NotBeNull();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("LightAutomation initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
