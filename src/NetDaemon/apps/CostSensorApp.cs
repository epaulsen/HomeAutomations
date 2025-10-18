using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomeAutomations.Apps;

/// <summary>
/// App to manage electricity cost sensors
/// Listens for energy sensor changes and calculates costs based on tariff sensors
/// </summary>
[NetDaemonApp]
public class CostSensorApp
{
    private readonly IHaContext _ha;
    private readonly ILogger<CostSensorApp> _logger;
    private readonly Dictionary<string, double> _costSensorValues = new();
    private readonly List<IDisposable> _subscriptions = new();

    public CostSensorApp(IHaContext ha, ILogger<CostSensorApp> logger)
    {
        _ha = ha;
        _logger = logger;

        var config = LoadConfiguration();
        
        if (config?.CostSensors == null || config.CostSensors.Count == 0)
        {
            logger.LogInformation("No cost sensors configured, CostSensorApp will do nothing");
            return;
        }

        logger.LogInformation("Initializing CostSensorApp with {Count} cost sensors", config.CostSensors.Count);

        foreach (var sensor in config.CostSensors)
        {
            InitializeCostSensor(sensor);
        }

        logger.LogInformation("CostSensorApp initialized successfully");
    }

    private void InitializeCostSensor(CostSensorEntry sensor)
    {
        _logger.LogInformation("Setting up cost sensor: {Name} (ID: {Id})", sensor.Name, sensor.UniqueId);

        // Initialize the cost sensor value to 0
        _costSensorValues[sensor.UniqueId] = 0.0;

        // Subscribe to energy sensor state changes
        var subscription = _ha.Entity(sensor.Energy)
            .StateChanges()
            .Subscribe(change =>
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
                    if (!double.TryParse(oldValue, out var oldEnergy))
                    {
                        _logger.LogWarning("Could not parse old value '{OldValue}' for {Sensor}", oldValue, sensor.Energy);
                        return;
                    }

                    if (!double.TryParse(newValue, out var newEnergy))
                    {
                        _logger.LogWarning("Could not parse new value '{NewValue}' for {Sensor}", newValue, sensor.Energy);
                        return;
                    }

                    // Get the tariff sensor value
                    var tariffState = _ha.Entity(sensor.Tariff).State;
                    if (tariffState == null)
                    {
                        _logger.LogWarning("Tariff sensor {Tariff} has no state", sensor.Tariff);
                        return;
                    }

                    if (!double.TryParse(tariffState, out var tariff))
                    {
                        _logger.LogWarning("Could not parse tariff value '{TariffValue}' for {Tariff}", 
                            tariffState, sensor.Tariff);
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

                    // TODO: In a full implementation, we would publish this value back to Home Assistant
                    // This would require additional NetDaemon APIs for creating/updating sensors
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing state change for {Sensor}", sensor.Energy);
                }
            });

        _subscriptions.Add(subscription);
    }

    private CostSensorConfig? LoadConfiguration()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps", "cost_sensors.yaml");
        
        _logger.LogDebug("Looking for configuration file at: {Path}", configPath);

        if (!File.Exists(configPath))
        {
            _logger.LogInformation("Configuration file not found at {Path}, app will do nothing", configPath);
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
}
