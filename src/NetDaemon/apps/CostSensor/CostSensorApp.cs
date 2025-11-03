using System.Globalization;
using System.Reactive.Concurrency;
using System.Text;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.Extensions.MqttEntityManager;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomeAutomations.Apps.CostSensor;

/// <summary>
/// App to manage electricity cost sensors
/// Listens for energy sensor changes and calculates costs based on tariff sensors
/// </summary>
[NetDaemonApp]
public class CostSensorApp : IAsyncInitializable, IDisposable
{
    private readonly IHaContext _ha;
    private readonly ILogger<CostSensorApp> _logger;
    private readonly IMqttEntityManager _entityManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IScheduler _scheduler;
    private readonly Dictionary<string, PriceSensor> _priceSensors = new();
    private readonly List<CostSensor> _costSensors = new();

    public CostSensorApp(IHaContext ha, ILogger<CostSensorApp> logger, IMqttEntityManager entityManager, ILoggerFactory loggerFactory, IScheduler scheduler)
    {
        _ha = ha;
        _logger = logger;
        _entityManager = entityManager;
        _loggerFactory = loggerFactory;
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

        // Initialize price sensors - collect unique tariff sensors and create PriceSensor instances
        InitializePriceSensors(config.CostSensors);

        // Initialize cost sensors
        foreach (var sensor in config.CostSensors)
        {
            await InitializeCostSensorAsync(sensor, cancellationToken);
        }

        _logger.LogInformation("CostSensorApp initialized successfully");
    }

    private void InitializePriceSensors(List<CostSensorEntry> costSensors)
    {
        // Get unique tariff sensors
        var uniqueTariffSensors = costSensors
            .Select(s => s.Tariff)
            .Distinct()
            .ToList();

        _logger.LogInformation("Found {Count} unique tariff sensors", uniqueTariffSensors.Count);

        foreach (var tariffSensorId in uniqueTariffSensors)
        {
            var priceSensor = new PriceSensor(
                _ha,
                _loggerFactory.CreateLogger<PriceSensor>(),
                tariffSensorId);
            
            _priceSensors[tariffSensorId] = priceSensor;
        }
    }

    private async Task InitializeCostSensorAsync(CostSensorEntry sensorConfig, CancellationToken cancellationToken)
    {
        // Get the price sensor for this cost sensor's tariff
        if (!_priceSensors.TryGetValue(sensorConfig.Tariff, out var priceSensor))
        {
            _logger.LogError("Price sensor for tariff {Tariff} not found", sensorConfig.Tariff);
            return;
        }

        var costSensor = new CostSensor(
            _ha,
            _entityManager,
            _loggerFactory.CreateLogger<CostSensor>(),
            _scheduler,
            priceSensor,
            sensorConfig);

        await costSensor.InitializeAsync(cancellationToken);
        _costSensors.Add(costSensor);
    }

    private CostSensorConfig? LoadConfiguration()
    {
        var configPath = GetConfigurationPath();
        
        if (configPath == null)
        {
            return null;
        }
        
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
            var yaml = File.ReadAllText(configPath, Encoding.UTF8);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTypeConverter(new CronScheduleTypeConverter())
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

    private string? GetConfigurationPath()
    {
        // Check if running in a container
        var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        
        if (bool.TryParse(runningInContainer, out var isContainer) && isContainer)
        {
            _logger.LogInformation("Running in container, using /config directory");
            var configDir = "/config";
            
            if (!Directory.Exists(configDir))
            {
                _logger.LogError("Configuration directory {ConfigDir} does not exist. " +
                    "Please mount a volume to /config in your docker-compose.yml or Docker run command. " +
                    "Example: volumes: - ./config:/config", configDir);
                Environment.Exit(1);
                return null;
            }
            
            return Path.Combine(configDir, "cost_sensors.yaml");
        }
        
        // Default behavior for non-container environments
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps", "config", "cost_sensors.yaml");
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
# Cron schedules:
#   - null (or omitted): No automatic reset
#   - ""daily"": Reset to 0 every day at midnight
#   - ""monthly"": Reset to 0 on the 1st of each month at midnight
#   - ""yearly"": Reset to 0 on January 1st at midnight
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
#   - name: ""Total Energy Cost""
#     unique_id: ""sensor.total_energy_cost""
#     tariff: ""sensor.electricity_tariff""
#     energy: ""sensor.total_energy""
#     cron: ""monthly""

# To activate this configuration:
# 1. Uncomment the 'cost_sensors:' line and the sensor entries below it
# 2. Replace the example sensor IDs with your actual Home Assistant entity IDs
# 3. Save the file and restart the NetDaemon app
";

            File.WriteAllText(configPath, sampleConfig, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample configuration at {Path}", configPath);
        }
    }

    public void Dispose()
    {
        // Dispose all cost sensors
        foreach (var costSensor in _costSensors)
        {
            costSensor.Dispose();
        }
        _costSensors.Clear();

        // Dispose all price sensors
        foreach (var priceSensor in _priceSensors.Values)
        {
            priceSensor.Dispose();
        }
        _priceSensors.Clear();
    }
}