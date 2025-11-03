using System.Globalization;
using System.Reactive.Concurrency;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.HassModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.Extensions.Scheduler;

namespace HomeAutomations.Apps.CostSensor;

/// <summary>
/// Manages a cost sensor that tracks energy costs based on a price sensor and energy sensor
/// </summary>
public class CostSensor : IDisposable
{
    private readonly IHaContext _ha;
    private readonly IMqttEntityManager _entityManager;
    private readonly ILogger<CostSensor> _logger;
    private readonly IScheduler _scheduler;
    private readonly PriceSensor _priceSensor;
    private readonly CostSensorEntry _config;
    private IDisposable? _subscription;
    private IDisposable? _cronSubscription;
    private double _currentCost;
    private DateTime? _lastStateChangeTime;

    public CostSensor(
        IHaContext ha, 
        IMqttEntityManager entityManager,
        ILogger<CostSensor> logger,
        IScheduler scheduler,
        PriceSensor priceSensor,
        CostSensorEntry config)
    {
        _ha = ha;
        _entityManager = entityManager;
        _logger = logger;
        _scheduler = scheduler;
        _priceSensor = priceSensor;
        _config = config;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting up cost sensor: {Name} (ID: {Id})", _config.Name, _config.UniqueId);

        // Fetch and verify energy sensor from HomeAssistant
        var energySensor = _ha.Entity(_config.Energy);
        
        // Verify energy sensor exists by checking its state
        var energyState = energySensor.State;
        
        if (energyState == null)
        {
            _logger.LogWarning("Energy sensor {Energy} not found in HomeAssistant or has no state", _config.Energy);
        }
        else
        {
            _logger.LogInformation("Retrieved energy sensor {Energy} from HomeAssistant with current state: {State}", 
                _config.Energy, energyState);
        }

        // Check if the cost sensor entity exists in Home Assistant
        var costSensorEntity = _ha.Entity(_config.UniqueId);
        var existingState = costSensorEntity.State;
        
        if (!string.IsNullOrEmpty(existingState) && double.TryParse(existingState, CultureInfo.InvariantCulture, out var existingValue))
        {
            // Load the existing value from Home Assistant
            _currentCost = existingValue;
            _logger.LogInformation("Cost sensor {UniqueId} exists in HomeAssistant with value: {Value}", 
                _config.UniqueId, existingValue);
        }
        else
        {
            // Initialize the cost sensor value to 0 and create the entity
            _currentCost = 0.0;
            _logger.LogInformation("Cost sensor {UniqueId} does not exist in HomeAssistant, creating it", 
                _config.UniqueId);
            
            // Create the cost sensor entity using MQTT Entity Manager
            try
            {
                await _entityManager.CreateAsync(
                    _config.UniqueId,
                    new EntityCreationOptions(
                        DeviceClass: "monetary",
                        UniqueId: _config.UniqueId.Replace("sensor.", ""),
                        Name: _config.Name,
                        Persist: true
                    ),
                    new
                    {
                        unit_of_measurement = "kr",
                        state_class = "measurement"
                    }
                );
                
                // Set initial state to 0
                await _entityManager.SetStateAsync(_config.UniqueId, "0.00");
                await _entityManager.SetAvailabilityAsync(_config.UniqueId, "online");
                
                _logger.LogInformation("Successfully created cost sensor {UniqueId}", _config.UniqueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cost sensor {UniqueId}", _config.UniqueId);
            }
        }

        // Set up cron schedule for resetting the cost sensor
        SetupCronSchedule();

        // Subscribe to energy sensor state changes
        _subscription = energySensor
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
                        _logger.LogDebug("Skipping first state change for {Sensor} - no old value", _config.Energy);
                        return;
                    }

                    // Parse the old and new energy values
                    if (!double.TryParse(oldValue, CultureInfo.InvariantCulture, out var oldEnergy))
                    {
                        _logger.LogWarning("Could not parse old value '{OldValue}' for {Sensor}", oldValue, _config.Energy);
                        return;
                    }

                    if (!double.TryParse(newValue, CultureInfo.InvariantCulture, out var newEnergy))
                    {
                        _logger.LogWarning("Could not parse new value '{NewValue}' for {Sensor}", newValue, _config.Energy);
                        return;
                    }

                    // Calculate the energy delta
                    var energyDelta = newEnergy - oldEnergy;

                    // Spike detection: if the previous value was recorded less than 60 seconds ago
                    // and the computed delta absolute value is 10 or above, ignore this state change
                    var currentTime = DateTime.Now;
                    if (_lastStateChangeTime.HasValue)
                    {
                        var timeDelta = currentTime - _lastStateChangeTime.Value;
                        if (timeDelta.TotalSeconds < 60 && Math.Abs(energyDelta) >= 10)
                        {
                            _logger.LogWarning(
                                "Spike detected for {Sensor}: energy delta = {Delta} kWh in {TimeDelta} seconds. Ignoring this state change.",
                                _config.Energy, energyDelta, timeDelta.TotalSeconds);
                            return;
                        }
                    }

                    // Update the timestamp of the last state change
                    _lastStateChangeTime = currentTime;

                    // Get the current tariff value from the price sensor
                    var tariff = _priceSensor.CurrentPrice;

                    // Calculate the cost increment
                    var costIncrement = energyDelta * tariff;

                    // Update the cost sensor value
                    _currentCost += costIncrement;

                    _logger.LogDebug(
                        "Cost update for {Name}: energy delta = {Delta} kWh, tariff = {Tariff}, cost increment = {CostIncrement}, total cost = {TotalCost}",
                        _config.Name, energyDelta, tariff, costIncrement, _currentCost);

                    // Update the cost sensor entity in Home Assistant
                    try
                    {
                        await _entityManager.SetStateAsync(_config.UniqueId, _currentCost.ToString("F4", CultureInfo.InvariantCulture));
                        _logger.LogDebug("Successfully updated cost sensor {UniqueId} to {Value} kr", 
                            _config.UniqueId, _currentCost);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating cost sensor state for {UniqueId}", _config.UniqueId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing state change for {Sensor}", _config.Energy);
                }
            });
    }

    private void SetupCronSchedule()
    {
        if (_config.Cron == CronSchedule.None)
        {
            _logger.LogInformation("Cost sensor {Name} has no cron schedule, will not reset automatically", _config.Name);
            return;
        }

        string cronExpression = _config.Cron switch
        {
            CronSchedule.Daily => "0 0 * * *",      // Every day at midnight
            CronSchedule.Monthly => "0 0 1 * *",    // 1st of every month at midnight
            CronSchedule.Yearly => "0 0 1 1 *",     // January 1st at midnight
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(cronExpression))
        {
            _logger.LogInformation("Setting up {Schedule} reset schedule for cost sensor {Name} with cron: {Cron}", 
                _config.Cron, _config.Name, cronExpression);

            _cronSubscription = _scheduler.ScheduleCron(cronExpression, async () =>
            {
                _logger.LogInformation("Cron schedule triggered for {Name}, resetting cost to 0", _config.Name);
                await ResetCostAsync();
            });
        }
    }

    private async Task ResetCostAsync()
    {
        _currentCost = 0.0;
        
        try
        {
            await _entityManager.SetStateAsync(_config.UniqueId, "0.00");
            _logger.LogInformation("Successfully reset cost sensor {UniqueId} to 0.00", _config.UniqueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting cost sensor {UniqueId}", _config.UniqueId);
        }
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _cronSubscription?.Dispose();
    }
}
