namespace HomeAutomations.Services;

/// <summary>
/// Provides the current time. Can be mocked for testing.
/// </summary>
public interface ITimeProvider
{
    DateTime UtcNow { get; }
}
