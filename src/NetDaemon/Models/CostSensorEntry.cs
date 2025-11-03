namespace HomeAutomations.Models;

/// <summary>
/// Configuration entry for a single cost sensor
/// </summary>

public class CostSensorEntry
{
    /// <summary>
    /// Human-readable name of the cost sensor
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID of the cost sensor
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID of the tariff sensor (pricing)
    /// </summary>
    public string Tariff { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID of the energy sensor
    /// </summary>
    public string Energy { get; set; } = string.Empty;

    /// <summary>
    /// Reset schedule for the cost sensor
    /// </summary>
    public CronSchedule Cron { get; set; } = CronSchedule.None;
}