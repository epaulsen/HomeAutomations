namespace HomeAutomations.Models;

/// <summary>
/// Configuration for Easee EV charging control.
/// </summary>
public class EaseeChargingConfig
{
    public string DeviceId { get; set; } = string.Empty;
    public string ChargerStatusEntityId { get; set; } = "sensor.ehzyxy7d_status";
    public string DynamicChargerLimitEntityId { get; set; } = "sensor.ehzyxy7d_dynamic_charger_limit";
    public string VoltageEntityId { get; set; } = "sensor.0x086bd7fffeb6c404_voltage";
    public string AvailablePowerEntityId { get; set; } = "sensor.available_power_this_hour";
    public string FeatureToggleEntityId { get; set; } = "input_boolean.overhold_effektniva";
    public string ServiceDomain { get; set; } = "easee";
    public string ServiceName { get; set; } = "set_charger_dynamic_limit";
    public int TimeToLive { get; set; } = 0;
    public int MinAmpere { get; set; } = 6;
    public int MaxAmpere { get; set; } = 25;
    public int PositiveThresholdWatts { get; set; } = 300;
    public int PositiveStepAmpere { get; set; } = 1;
    public int NegativeDurationSeconds { get; set; } = 60;
    public int PositiveDurationSeconds { get; set; } = 120;
}
