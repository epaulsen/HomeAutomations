using System.Globalization;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.Extensions.Scheduler;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomeAutomations.Apps;

/// <summary>
/// App to manage electricity cost sensors
/// Listens for energy sensor changes and calculates costs based on tariff sensors
/// </summary>
[NetDaemonApp]
public class CostSensorApp : IAsyncInitializable
{
    private readonly IHaContext _ha;
    private readonly ILogger<CostSensorApp> _logger;
    private readonly IMqttEntityManager _entityManager;
    private readonly INetDaemonScheduler _scheduler;
    private readonly Dictionary<string, double> _costSensorValues = new();
    private readonly Dictionary<string, double> _tariffSensorValues = new();
    private readonly List<IDisposable> _subscriptions = new();

    public CostSensorApp(IHaContext ha, ILogger<CostSensorApp> logger, IMqttEntityManager entityManager, INetDaemonScheduler scheduler)
    {
        _ha = ha;
        _logger = logger;
        _entityManager = entityManager;
        _scheduler = scheduler;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var config = LoadConfiguration();
        
        if (config?.CostSensors == null || config.CostSensors.Count == 0)
        {
            _logger.LogInformation("No cost sensors configured, CostSensorApp will do nothing");
            return;
        }

        _logger.LogInformation("Initializing CostSensorApp with {Count} cost sensors", config.CostSensors.Count);

        // Initialize tariff sensors - collect unique tariff sensors and subscribe once per unique sensor
        InitializeTariffSensors(config.CostSensors);

        // Initialize cost sensors
        foreach (var sensor in config.CostSensors)
        {
            await InitializeCostSensorAsync(sensor, cancellationToken);
        }

        _logger.LogInformation("CostSensorApp initialized successfully");
    }

    private void InitializeTariffSensors(List<CostSensorEntry> costSensors)
    {
        // Get unique tariff sensors
        var uniqueTariffSensors = costSensors
            .Select(s => s.Tariff)
            .Distinct()
            .ToList();

        _logger.LogInformation("Found {Count} unique tariff sensors", uniqueTariffSensors.Count);

        foreach (var tariffSensorId in uniqueTariffSensors)
        {
            var tariffSensor = _ha.Entity(tariffSensorId);
            
            // Get initial state
            var tariffState = tariffSensor.State;
            
            if (tariffState == null)
            {
                _logger.LogWarning("Tariff sensor {Tariff} not found in HomeAssistant or has no state", tariffSensorId);
                _tariffSensorValues[tariffSensorId] = 0.0;
            }
            else if (!double.TryParse(tariffState, CultureInfo.InvariantCulture, out var tariffValue))
            {
                _logger.LogWarning("Could not parse tariff value '{TariffValue}' for {Tariff}", tariffState, tariffSensorId);
                _tariffSensorValues[tariffSensorId] = 0.0;
            }
            else
            {
                _tariffSensorValues[tariffSensorId] = tariffValue;
                _logger.LogInformation("Retrieved tariff sensor {Tariff} from HomeAssistant with current value: {Value}", 
                    tariffSensorId, tariffValue);
            }

            // Subscribe to tariff sensor state changes
            var tariffSubscription = tariffSensor
                .StateChanges()
                .Subscribe(change =>
                {
                    try
                    {
                        var newTariff = change.New?.State;
                        
                        if (string.IsNullOrEmpty(newTariff))
                        {
                            _logger.LogDebug("Tariff sensor {Tariff} state change has no new value", tariffSensorId);
                            return;
                        }
                        
                        if (!double.TryParse(newTariff, CultureInfo.InvariantCulture, out var tariffValue))
                        {
                            _logger.LogWarning("Could not parse new tariff value '{TariffValue}' for {Tariff}", 
                                newTariff, tariffSensorId);
                            return;
                        }
                        
                        _tariffSensorValues[tariffSensorId] = tariffValue;
                        
                        _logger.LogInformation(
                            "Tariff sensor {Tariff} changed to {NewTariff}",
                            tariffSensorId, tariffValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing tariff state change for {Sensor}", tariffSensorId);
                    }
                });

            _subscriptions.Add(tariffSubscription);
        }
    }

    private async Task InitializeCostSensorAsync(CostSensorEntry sensor, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting up cost sensor: {Name} (ID: {Id})", sensor.Name, sensor.UniqueId);

        // Fetch and verify energy sensor from HomeAssistant
        var energySensor = _ha.Entity(sensor.Energy);
        
        // Verify energy sensor exists by checking its state
        var energyState = energySensor.State;
        
        if (energyState == null)
        {
            _logger.LogWarning("Energy sensor {Energy} not found in HomeAssistant or has no state", sensor.Energy);
        }
        else
        {
            _logger.LogInformation("Retrieved energy sensor {Energy} from HomeAssistant with current state: {State}", 
                sensor.Energy, energyState);
        }

        // Check if the cost sensor entity exists in Home Assistant
        var costSensorEntity = _ha.Entity(sensor.UniqueId);
        var existingState = costSensorEntity.State;
        
        if (!string.IsNullOrEmpty(existingState) && double.TryParse(existingState, CultureInfo.InvariantCulture, out var existingValue))
        {
            // Load the existing value from Home Assistant
            _costSensorValues[sensor.UniqueId] = existingValue;
            _logger.LogInformation("Cost sensor {UniqueId} exists in HomeAssistant with value: {Value}", 
                sensor.UniqueId, existingValue);
        }
        else
        {
            // Initialize the cost sensor value to 0 and create the entity
            _costSensorValues[sensor.UniqueId] = 0.0;
            _logger.LogInformation("Cost sensor {UniqueId} does not exist in HomeAssistant, creating it", 
                sensor.UniqueId);
            
            // Create the cost sensor entity using MQTT Entity Manager
            try
            {
                await _entityManager.CreateAsync(
                    sensor.UniqueId,
                    new EntityCreationOptions(
                        DeviceClass: "monetary",
                        UniqueId: sensor.UniqueId.Replace("sensor.", ""),
                        Name: sensor.Name,
                        Persist: true
                    ),
                    new
                    {
                        unit_of_measurement = "kr",
                        state_class = "measurement"
                    }
                );
                
                // Set initial state to 0
                await _entityManager.SetStateAsync(sensor.UniqueId, "0.00");
                await _entityManager.SetAvailabilityAsync(sensor.UniqueId, "online");
                
                _logger.LogInformation("Successfully created cost sensor {UniqueId}", sensor.UniqueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cost sensor {UniqueId}", sensor.UniqueId);
            }
        }

        // Subscribe to energy sensor state changes
        var energySubscription = energySensor
            .StateChanges()
            .SubscribeAsync(async change =>
            {
                try
                {
                    // Get the old and new values
                    var oldValue = change.Old?.State;
                    var newValue = change.New?.State;

                    // If OldValue is null, skip this event (as per requirements)
                    if (string.IsNullOrEmpty(oldValue))
                    {
                        _logger.LogDebug("Skipping first state change for {Sensor} - no old value", sensor.Energy);
                        return;
                    }

                    // Parse the old and new energy values
                    if (!double.TryParse(oldValue, CultureInfo.InvariantCulture, out var oldEnergy))
                    {
                        _logger.LogWarning("Could not parse old value '{OldValue}' for {Sensor}", oldValue, sensor.Energy);
                        return;
                    }

                    if (!double.TryParse(newValue, CultureInfo.InvariantCulture, out var newEnergy))
                    {
                        _logger.LogWarning("Could not parse new value '{NewValue}' for {Sensor}", newValue, sensor.Energy);
                        return;
                    }

                    // Get the current tariff value from the dictionary
                    if (!_tariffSensorValues.TryGetValue(sensor.Tariff, out var tariff))
                    {
                        _logger.LogWarning("Tariff sensor {Tariff} value not available in dictionary", sensor.Tariff);
                        return;
                    }

                    // Calculate the cost increment
                    var energyDelta = newEnergy - oldEnergy;
                    var costIncrement = energyDelta * tariff;

                    // Update the cost sensor value
                    _costSensorValues[sensor.UniqueId] += costIncrement;

                    _logger.LogDebug(
                        "Cost update for {Name}: energy delta = {Delta} kWh, tariff = {Tariff}, cost increment = {CostIncrement}, total cost = {TotalCost}",
                        sensor.Name, energyDelta, tariff, costIncrement, _costSensorValues[sensor.UniqueId]);

                    // Update the cost sensor entity in Home Assistant
                    try
                    {
                        await _entityManager.SetStateAsync(sensor.UniqueId, _costSensorValues[sensor.UniqueId].ToString("F2"));
                        _logger.LogDebug("Successfully updated cost sensor {UniqueId} to {Value} kr", 
                            sensor.UniqueId, _costSensorValues[sensor.UniqueId]);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating cost sensor state for {UniqueId}", sensor.UniqueId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing state change for {Sensor}", sensor.Energy);
                }
            });

        _subscriptions.Add(energySubscription);
        
        // Setup cron-based reset schedules
        SetupCronSchedule(sensor);
    }

    private void SetupCronSchedule(CostSensorEntry sensor)
    {
        var schedule = sensor.CronSchedule;
        
        if (schedule == CronSchedule.None)
        {
            _logger.LogDebug("No cron schedule configured for {Name}", sensor.Name);
            return;
        }
        
        _logger.LogInformation("Setting up {Schedule} reset schedule for {Name}", schedule, sensor.Name);
        
        switch (schedule)
        {
            case CronSchedule.Daily:
                // Reset daily at midnight
                ScheduleDailyReset(sensor);
                break;
                
            case CronSchedule.Monthly:
                // Reset monthly at midnight on the first day of the month
                ScheduleMonthlyReset(sensor);
                break;
                
            case CronSchedule.Yearly:
                // Reset yearly at midnight on January 1st
                ScheduleYearlyReset(sensor);
                break;
        }
    }
    
    private void ScheduleDailyReset(CostSensorEntry sensor)
    {
        // Calculate the next midnight
        var now = _scheduler.Now;
        var nextMidnight = now.Date.AddDays(1);
        
        // Schedule the first reset at next midnight
        _scheduler.RunAt(nextMidnight, () =>
        {
            ResetCostSensor(sensor);
            // Schedule recurring daily resets
            _scheduler.RunEvery(TimeSpan.FromDays(1), () => ResetCostSensor(sensor));
        });
    }
    
    private void ScheduleMonthlyReset(CostSensorEntry sensor)
    {
        // Calculate the next first day of month at midnight
        var now = _scheduler.Now;
        var nextFirstOfMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        
        // Schedule the first reset
        _scheduler.RunAt(nextFirstOfMonth, () =>
        {
            ResetCostSensor(sensor);
            // Schedule next month's reset (this will be called each time)
            ScheduleNextMonthlyReset(sensor);
        });
    }
    
    private void ScheduleNextMonthlyReset(CostSensorEntry sensor)
    {
        // Calculate the next first day of month at midnight
        var now = _scheduler.Now;
        var nextFirstOfMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        
        _scheduler.RunAt(nextFirstOfMonth, () =>
        {
            ResetCostSensor(sensor);
            ScheduleNextMonthlyReset(sensor);
        });
    }
    
    private void ScheduleYearlyReset(CostSensorEntry sensor)
    {
        // Calculate the next January 1st at midnight
        var now = _scheduler.Now;
        var nextNewYear = new DateTime(now.Year + 1, 1, 1);
        
        // Schedule the first reset
        _scheduler.RunAt(nextNewYear, () =>
        {
            ResetCostSensor(sensor);
            // Schedule next year's reset
            ScheduleNextYearlyReset(sensor);
        });
    }
    
    private void ScheduleNextYearlyReset(CostSensorEntry sensor)
    {
        // Calculate the next January 1st at midnight
        var now = _scheduler.Now;
        var nextNewYear = new DateTime(now.Year + 1, 1, 1);
        
        _scheduler.RunAt(nextNewYear, () =>
        {
            ResetCostSensor(sensor);
            ScheduleNextYearlyReset(sensor);
        });
    }
    
    private async void ResetCostSensor(CostSensorEntry sensor)
    {
        try
        {
            _logger.LogInformation("Resetting cost sensor {Name} (ID: {Id}) to 0.00 due to {Schedule} schedule", 
                sensor.Name, sensor.UniqueId, sensor.CronSchedule);
            
            // Reset the in-memory value
            _costSensorValues[sensor.UniqueId] = 0.0;
            
            // Update the sensor state in Home Assistant
            await _entityManager.SetStateAsync(sensor.UniqueId, "0.00");
            
            _logger.LogInformation("Successfully reset cost sensor {Name} to 0.00", sensor.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting cost sensor {Name} (ID: {Id})", sensor.Name, sensor.UniqueId);
        }
    }

    private CostSensorConfig? LoadConfiguration()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps", "config", "cost_sensors.yaml");
        
        _logger.LogDebug("Looking for configuration file at: {Path}", configPath);

        if (!File.Exists(configPath))
        {
            _logger.LogInformation("Configuration file not found at {Path}, creating sample configuration", configPath);
            CreateSampleConfiguration(configPath);
            _logger.LogInformation("Sample configuration file created at {Path}. Please edit it with your sensor IDs.", configPath);
            return null;
        }

        try
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<CostSensorConfig>(yaml);
            _logger.LogInformation("Successfully loaded configuration with {Count} cost sensors", 
                config?.CostSensors?.Count ?? 0);
            
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {Path}", configPath);
            return null;
        }
    }

    private void CreateSampleConfiguration(string configPath)
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create sample configuration with commented examples
            var sampleConfig = @"# Cost sensors configuration
# This file defines cost sensors that calculate energy costs based on tariff and energy sensors
#
# Configuration structure:
# cost_sensors:
#   - name: <name of cost sensor>           # Human-readable name for the sensor
#     unique_id: <unique id of sensor>      # Unique identifier (e.g., sensor.my_cost)
#     tariff: <unique_id of tariff sensor>  # Sensor containing the price per unit (e.g., sensor.electricity_tariff)
#     energy: <unique_id of energy sensor>  # Sensor containing energy consumption (e.g., sensor.my_energy)
#     cron: <reset schedule>                # Optional: null, ""daily"", ""monthly"", or ""yearly""
#
# Cron reset schedules:
#   - null: No automatic reset
#   - ""daily"": Resets to 0.0 at midnight every day
#   - ""monthly"": Resets to 0.0 at midnight on the first day of each month
#   - ""yearly"": Resets to 0.0 at midnight on January 1st
#
# Example configuration (remove the # to enable):

# cost_sensors:
#   - name: ""Living Room Cost""
#     unique_id: ""sensor.living_room_energy_cost""
#     tariff: ""sensor.electricity_tariff""
#     energy: ""sensor.living_room_energy""
#     cron: null
#
#   - name: ""Kitchen Cost""
#     unique_id: ""sensor.kitchen_energy_cost""
#     tariff: ""sensor.electricity_tariff""
#     energy: ""sensor.kitchen_energy""
#     cron: ""daily""
#
#   - name: ""Monthly Energy Cost""
#     unique_id: ""sensor.monthly_energy_cost""
#     tariff: ""sensor.electricity_tariff""
#     energy: ""sensor.total_energy""
#     cron: ""monthly""
#
#   - name: ""Yearly Energy Cost""
#     unique_id: ""sensor.yearly_energy_cost""
#     tariff: ""sensor.electricity_tariff""
#     energy: ""sensor.total_energy""
#     cron: ""yearly""

# To activate this configuration:
# 1. Uncomment the 'cost_sensors:' line and the sensor entries below it
# 2. Replace the example sensor IDs with your actual Home Assistant entity IDs
# 3. Save the file and restart the NetDaemon app
";

            File.WriteAllText(configPath, sampleConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample configuration at {Path}", configPath);
        }
    }
}

/// <summary>
/// Cron schedule types for resetting cost sensors
/// </summary>
public enum CronSchedule
{
    /// <summary>
    /// No scheduled reset
    /// </summary>
    None,
    
    /// <summary>
    /// Reset daily at midnight
    /// </summary>
    Daily,
    
    /// <summary>
    /// Reset monthly at midnight on the first day of the month
    /// </summary>
    Monthly,
    
    /// <summary>
    /// Reset yearly at midnight on January 1st
    /// </summary>
    Yearly
}

/// <summary>
/// Root configuration object for cost sensors
/// </summary>
public class CostSensorConfig
{
    public List<CostSensorEntry> CostSensors { get; set; } = new();
}

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
    /// Reset schedule: null, "daily", "monthly", or "yearly"
    /// </summary>
    public string? Cron { get; set; }
    
    /// <summary>
    /// Gets the parsed cron schedule from the Cron string property
    /// </summary>
    public CronSchedule CronSchedule => ParseCronSchedule(Cron);
    
    private static CronSchedule ParseCronSchedule(string? cronValue)
    {
        if (string.IsNullOrWhiteSpace(cronValue))
            return CronSchedule.None;
            
        return cronValue.ToLowerInvariant() switch
        {
            "daily" => CronSchedule.Daily,
            "monthly" => CronSchedule.Monthly,
            "yearly" => CronSchedule.Yearly,
            _ => CronSchedule.None
        };
    }
}
