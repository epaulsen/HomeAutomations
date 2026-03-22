using System.Globalization;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps.NordPoolApp;

public class NordPoolSensor(
    IHaContext context,
    IMqttEntityManager manager,
    NordPoolDataStorage storage,
    ILogger<NordPoolSensor> logger) : IAsyncInitializable, IDisposable
{
    public const string SensorUniqueId = "sensor.strompris_nordpool_no2";
    private IDisposable? _subscription;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding Nordpool Sensor");
        await manager.CreateAsync(
            SensorUniqueId,
            new EntityCreationOptions(DeviceClass: "monetary",
                UniqueId: SensorUniqueId,
                Name: "Nord Pool NO2") { Persist = true },
            new
            {
                unit_of_measurement = "kr",
                state_class = "measurement"
            });

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

        _subscription = storage.CurrentPrice.SubscribeAsync(async ma =>
        {
            if (ma == null)
            {
                await manager.SetStateAsync(SensorUniqueId, "unavailable");
                return;
            }

            if (!ma.EntryPerArea.TryGetValue("NO2", out var price))
            {
                logger.LogWarning("NO2 area key not found in NordPool data");
                return;
            }

            logger.LogInformation("Price changed to {price}", price);
            await manager.SetStateAsync(SensorUniqueId, price.ToString("F2", CultureInfo.InvariantCulture));
        });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}