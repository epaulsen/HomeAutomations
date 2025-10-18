using System.Globalization;
using Microsoft.Extensions.Logging;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.CostSensor;

/// <summary>
/// Manages a price/tariff sensor and exposes its current value
/// </summary>
public class PriceSensor : IDisposable
{
    private readonly IHaContext _ha;
    private readonly ILogger<PriceSensor> _logger;
    private readonly string _tariffSensorId;
    private IDisposable? _subscription;
    private double _currentPrice;

    /// <summary>
    /// Gets the current price value from the tariff sensor
    /// </summary>
    public double CurrentPrice => _currentPrice;

    public PriceSensor(IHaContext ha, ILogger<PriceSensor> logger, string tariffSensorId)
    {
        _ha = ha;
        _logger = logger;
        _tariffSensorId = tariffSensorId;

        Initialize();
    }

    private void Initialize()
    {
        var tariffSensor = _ha.Entity(_tariffSensorId);
        
        // Get initial state
        var tariffState = tariffSensor.State;
        
        if (tariffState == null)
        {
            _logger.LogWarning("Tariff sensor {Tariff} not found in HomeAssistant or has no state", _tariffSensorId);
            _currentPrice = 0.0;
        }
        else if (!double.TryParse(tariffState, CultureInfo.InvariantCulture, out var tariffValue))
        {
            _logger.LogWarning("Could not parse tariff value '{TariffValue}' for {Tariff}", tariffState, _tariffSensorId);
            _currentPrice = 0.0;
        }
        else
        {
            _currentPrice = tariffValue;
            _logger.LogInformation("Retrieved tariff sensor {Tariff} from HomeAssistant with current value: {Value}", 
                _tariffSensorId, tariffValue);
        }

        // Subscribe to tariff sensor state changes
        _subscription = tariffSensor
            .StateChanges()
            .Subscribe(change =>
            {
                try
                {
                    var newTariff = change.New?.State;
                    
                    if (string.IsNullOrEmpty(newTariff))
                    {
                        _logger.LogDebug("Tariff sensor {Tariff} state change has no new value", _tariffSensorId);
                        return;
                    }
                    
                    if (!double.TryParse(newTariff, CultureInfo.InvariantCulture, out var tariffValue))
                    {
                        _logger.LogWarning("Could not parse new tariff value '{TariffValue}' for {Tariff}", 
                            newTariff, _tariffSensorId);
                        return;
                    }
                    
                    _currentPrice = tariffValue;
                    
                    _logger.LogInformation(
                        "Tariff sensor {Tariff} changed to {NewTariff}",
                        _tariffSensorId, tariffValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing tariff state change for {Sensor}", _tariffSensorId);
                }
            });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
