namespace HomeAutomations.Apps.CostSensor;

/// <summary>
/// Defines the reset schedule for cost sensors
/// </summary>
public enum CronSchedule
{
    /// <summary>
    /// No automatic reset
    /// </summary>
    None,

    /// <summary>
    /// Reset daily at midnight
    /// </summary>
    Daily,

    /// <summary>
    /// Reset monthly at midnight on the 1st of each month
    /// </summary>
    Monthly,

    /// <summary>
    /// Reset yearly at midnight on January 1st
    /// </summary>
    Yearly
}
