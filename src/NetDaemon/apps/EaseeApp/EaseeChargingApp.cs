using System.Globalization;
using System.Text;
using HomeAutomations.Extensions;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomeAutomations.Apps.EaseeApp;

/// <summary>
/// Controls Easee charger dynamic current limit based on available hourly power.
/// </summary>
[NetDaemonApp]
public class EaseeChargingApp(IHaContext ha, ILogger<EaseeChargingApp> logger) : IAsyncInitializable, IDisposable
{
    private readonly EaseeChargingDecisionEngine _decisionEngine = new();
    private readonly List<IDisposable> _subscriptions = [];
    private readonly SemaphoreSlim _evaluationLock = new(1, 1);
    private EaseeChargingConfig? _config;
    private int? _lastSentAmpere;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _config = LoadConfiguration();
        if (_config == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.DeviceId))
        {
            logger.LogError("Missing required configuration 'device_id' in easee_charging.yaml");
            return;
        }

        if (_config.MinAmpere < 6)
        {
            _config.MinAmpere = 6;
        }

        if (_config.MaxAmpere < _config.MinAmpere)
        {
            logger.LogError("Invalid ampere range: max {MaxAmpere} lower than min {MinAmpere}", _config.MaxAmpere, _config.MinAmpere);
            return;
        }

        logger.LogInformation(
            "EaseeChargingApp initialized with status={StatusEntity}, availablePower={AvailablePowerEntity}, featureToggle={ToggleEntity}",
            _config.ChargerStatusEntityId,
            _config.AvailablePowerEntityId,
            _config.FeatureToggleEntityId);

        SubscribeToStateChanges(_config.ChargerStatusEntityId);
        SubscribeToStateChanges(_config.DynamicChargerLimitEntityId);
        SubscribeToStateChanges(_config.VoltageEntityId);
        SubscribeToStateChanges(_config.AvailablePowerEntityId);
        SubscribeToStateChanges(_config.FeatureToggleEntityId);

        await EvaluateAndApplyAsync(DateTimeOffset.UtcNow, cancellationToken);
    }

    private void SubscribeToStateChanges(string entityId)
    {
        var subscription = ha.Entity(entityId)
            .StateChanges()
            .SubscribeAsync(async _ =>
            {
                await EvaluateAndApplyAsync(DateTimeOffset.UtcNow, CancellationToken.None);
            });

        _subscriptions.Add(subscription);
    }

    private async Task EvaluateAndApplyAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (_config == null)
        {
            return;
        }

        await _evaluationLock.WaitAsync(cancellationToken);
        try
        {
            var isFeatureEnabled = IsFeatureEnabled();
            var isCharging = IsCharging();

            var currentAmpere = 0;
            var availablePowerWatts = 0.0;
            var voltage = 230.0;

            if (isFeatureEnabled && isCharging)
            {
                if (!TryReadSensorDouble(_config.AvailablePowerEntityId, out availablePowerWatts))
                {
                    return;
                }

                if (!TryReadSensorDouble(_config.VoltageEntityId, out voltage))
                {
                    return;
                }

                if (voltage <= 0.0)
                {
                    logger.LogWarning("Voltage sensor {EntityId} returned non-positive value {Voltage}", _config.VoltageEntityId, voltage);
                    return;
                }

                if (!TryReadSensorInt(_config.DynamicChargerLimitEntityId, out currentAmpere))
                {
                    return;
                }
            }

            var decision = _decisionEngine.Evaluate(
                new EaseeEvaluationInput(
                    IsFeatureEnabled: isFeatureEnabled,
                    IsCharging: isCharging,
                    AvailablePowerWatts: availablePowerWatts,
                    CurrentAmpere: currentAmpere,
                    Voltage: voltage,
                    MinAmpere: _config.MinAmpere,
                    MaxAmpere: _config.MaxAmpere,
                    PositiveThresholdWatts: _config.PositiveThresholdWatts,
                    PositiveStepAmpere: _config.PositiveStepAmpere,
                    NegativeWindow: TimeSpan.FromSeconds(_config.NegativeDurationSeconds),
                    PositiveWindow: TimeSpan.FromSeconds(_config.PositiveDurationSeconds)),
                now);

            if (decision == null)
            {
                return;
            }

            if (!decision.ForceSend && _lastSentAmpere == decision.TargetAmpere)
            {
                logger.LogDebug("Skipping duplicate dynamic current limit command at {TargetAmpere}A", decision.TargetAmpere);
                return;
            }

            ha.CallService(_config.ServiceDomain, _config.ServiceName, data: new
            {
                device_id = _config.DeviceId,
                current = decision.TargetAmpere,
                time_to_live = _config.TimeToLive
            });

            _lastSentAmpere = decision.TargetAmpere;

            logger.LogInformation(
                "Sent Easee dynamic current limit {TargetAmpere}A via {Domain}.{Service}",
                decision.TargetAmpere,
                _config.ServiceDomain,
                _config.ServiceName);
        }
        finally
        {
            _evaluationLock.Release();
        }
    }

    private bool IsFeatureEnabled()
    {
        if (_config == null)
        {
            return false;
        }

        return string.Equals(ha.Entity(_config.FeatureToggleEntityId).State, "on", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCharging()
    {
        if (_config == null)
        {
            return false;
        }

        return string.Equals(ha.Entity(_config.ChargerStatusEntityId).State, "charging", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryReadSensorDouble(string entityId, out double value)
    {
        var raw = ha.Entity(entityId).State;
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = 0;
            logger.LogWarning("Sensor {EntityId} has no state", entityId);
            return false;
        }

        if (!double.TryParse(raw, CultureInfo.InvariantCulture, out value))
        {
            logger.LogWarning("Failed parsing sensor {EntityId} state '{State}' as number", entityId, raw);
            return false;
        }

        return true;
    }

    private bool TryReadSensorInt(string entityId, out int value)
    {
        var raw = ha.Entity(entityId).State;
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = 0;
            logger.LogWarning("Sensor {EntityId} has no state", entityId);
            return false;
        }

        if (!double.TryParse(raw, CultureInfo.InvariantCulture, out var doubleValue))
        {
            logger.LogWarning("Failed parsing sensor {EntityId} state '{State}' as number", entityId, raw);
            value = 0;
            return false;
        }

        value = (int)Math.Round(doubleValue, MidpointRounding.AwayFromZero);
        return true;
    }

    private EaseeChargingConfig? LoadConfiguration()
    {
        var configPath = Path.Combine(ConfigFolder.Path, "easee_charging.yaml");

        if (!File.Exists(configPath))
        {
            logger.LogInformation("Configuration file not found at {Path}, creating sample configuration", configPath);
            CreateSampleConfiguration(configPath);
            logger.LogInformation("Sample configuration created at {Path}. Update values before using app.", configPath);
            return null;
        }

        try
        {
            var yaml = File.ReadAllText(configPath, Encoding.UTF8);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<EaseeChargingConfig>(yaml);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Unable to read Easee configuration from {Path}", configPath);
            return null;
        }
        catch (YamlException ex)
        {
            logger.LogError(ex, "Unable to parse Easee configuration from {Path}", configPath);
            return null;
        }
    }

    private static void CreateSampleConfiguration(string configPath)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sampleConfig = """
            # Easee charging control configuration
            device_id: "7eed8e06a2305c88415b94537e836fdd"
            charger_status_entity_id: "sensor.ehzyxy7d_status"
            dynamic_charger_limit_entity_id: "sensor.ehzyxy7d_dynamic_charger_limit"
            voltage_entity_id: "sensor.0x086bd7fffeb6c404_voltage"
            available_power_entity_id: "sensor.available_power_this_hour"
            feature_toggle_entity_id: "input_boolean.overhold_effektniva"

            service_domain: "easee"
            service_name: "set_charger_dynamic_limit"
            time_to_live: 0

            min_ampere: 6
            max_ampere: 25
            positive_threshold_watts: 300
            positive_step_ampere: 1
            negative_duration_seconds: 60
            positive_duration_seconds: 120
            """;

        File.WriteAllText(configPath, sampleConfig, Encoding.UTF8);
    }

    public static int CalculateReductionAmpere(double deficitWatts, double voltage)
    {
        if (voltage <= 0.0)
        {
            return 0;
        }

        return (int)Math.Ceiling(Math.Abs(deficitWatts) / voltage);
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();

        _evaluationLock.Dispose();
    }
}
