using System.Globalization;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.NordPoolApp;

public class NordPoolSubsidizedSensor(
    IHaContext context,
    IMqttEntityManager manager,
    ILogger<NordPoolSubsidizedSensor> logger) : IAsyncInitializable, IDisposable
{
    private const string SensorUniqueId = "sensor.strompris_nordpool_no2_med_stromstotte";
    private const double SubsidyThreshold = 0.9375;
    private const double SubsidyRate = 0.1;
    private IDisposable? _subscription;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding Nordpool subsidized sensor");
        await manager.CreateAsync(
            SensorUniqueId,
            new EntityCreationOptions(DeviceClass: "monetary",
                UniqueId: SensorUniqueId,
                Name: "Nord Pool NO2 med strømstøtte", Persist: true),
            new
            {
                unit_of_measurement = "kr",
                state_class = "measurement"
            });

        _subscription = context.Entity(NordPoolSensor.SensorUniqueId).StateAllChanges()
            .SubscribeAsync(async state =>
            {
                if (state.New == null)
                {
                    return;
                }

                double? price = double.TryParse(state.New.State, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : null;
                if (!price.HasValue)
                {
                    logger.LogWarning("Nordpool sensor is unavailable");
                    await manager.SetStateAsync(SensorUniqueId, "Unavailable");
                    return;
                }

                price = ComputeSubsidizedPrice(price.Value);

                logger.LogInformation("New price state set {price}", price);
                await manager.SetStateAsync(SensorUniqueId, price!.Value.ToString("F2", CultureInfo.InvariantCulture));
            });
    }

    private double? ComputeSubsidizedPrice(double? price)
    {
        if (price == null)
        {
            return null;
        }

        if (price < SubsidyThreshold)
        {
            return price.Value;
        }

        var subsidy = SubsidyThreshold + (price - SubsidyThreshold) * SubsidyRate; // 10 % of amount above 93,75 øre
        return subsidy;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}