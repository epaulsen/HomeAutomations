using System.Globalization;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.NordPoolApp;

public class NordPoolSensor(
    IHaContext context,
    IMqttEntityManager manager,
    INetDaemonScheduler scheduler,
    NordPoolDataStorage storage,
    ILogger<NordPoolSensor> logger) : IAsyncInitializable
{
    public const string SensorUniqueId = "sensor.strompris_nordpool_no2";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var costSensorEntity = context.Entity(SensorUniqueId);
        var existingState = costSensorEntity.State;
        if (string.IsNullOrWhiteSpace(existingState))
        {
            logger.LogInformation("Adding Nordpool Sensor");
            await manager.CreateAsync(
                SensorUniqueId,
                new EntityCreationOptions(DeviceClass: "monetary",
                    UniqueId: SensorUniqueId,
                    Name: "Nord Pool NO2"),
                new
                {
                    unit_of_measurement = "kr",
                    state_class = "measurement"
                });
        }

        var current = storage.CurrentHourlyPrice();
        double? state = current != null
            ? current.EntryPerArea.TryGetValue("NO2", out var no2Price) ? no2Price : null
            : null;

        string statestring = state != null ? state.Value.ToString("F2", CultureInfo.InvariantCulture) : "unavailable";
        if (state == null)
        {
            logger.LogWarning("NO2: No current price, setting to unavailable");
        }

        await manager.SetStateAsync(SensorUniqueId, statestring);


        storage.CurrentPrice.SubscribeAsync(async ma =>
        {
            if (ma == null)
            {
                await manager.SetStateAsync(SensorUniqueId, "unavailable");
                return;
            }


            var price = ma.EntryPerArea["NO2"];
            logger.LogInformation("Price changed to {price}", price);
            await manager.SetStateAsync(SensorUniqueId, price.ToString("F2", CultureInfo.InvariantCulture));
        });
    }
}