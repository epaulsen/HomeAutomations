namespace HomeAutomations.Models;

/// <summary>
/// Root configuration object for cost sensors
/// </summary>
public class CostSensorConfig
{
    public List<CostSensorEntry> CostSensors { get; set; } = new();
}