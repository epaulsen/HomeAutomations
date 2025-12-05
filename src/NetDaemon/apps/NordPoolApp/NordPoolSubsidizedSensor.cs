using System.Globalization;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.NordPoolApp;

public class NordPoolSubsidizedSensor(
    IHaContext context,
    IMqttEntityManager manager,
    ILogger<NordPoolSubsidizedSensor> logger) : IAsyncInitializable
{
    private const string SensorUniqueId = "sensor.strompris_nordpool_no2_med_stromstotte";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var sensorEntity = context.Entity(SensorUniqueId);
        var existingState = sensorEntity.State;

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


        context.Entity(NordPoolSensor.SensorUniqueId).StateAllChanges()
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

        if (price < 0.9375)
        {
            return price.Value;
        }

        var subsidy = 0.9375 + (price - 0.9375) * 0.1; // 10 % of amount above 93,75 øre
        return subsidy;
    }
}