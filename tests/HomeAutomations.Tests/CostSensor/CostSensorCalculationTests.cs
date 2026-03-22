using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using HomeAutomations.Apps.CostSensor;
using HomeAutomations.Models;

namespace HomeAutomations.Tests.CostSensor;

public class CostSensorCalculationTests
{
    private const string EnergySensorId = "sensor.energy_test";
    private const string TariffSensorId = "sensor.tariff_test";
    private const string CostSensorUniqueId = "sensor.cost_test";

    private record SutComponents(
        Apps.CostSensor.CostSensor Sensor,
        Mock<IHaContext> HaContext,
        Mock<IMqttEntityManager> EntityManager,
        Subject<StateChange> StateSubject);

    private SutComponents CreateSut(
        CronSchedule cron = CronSchedule.None,
        string? initialCostState = null,
        double tariffPrice = 1.5)
    {
        var mockHaContext = new Mock<IHaContext>();
        var mockEntityManager = new Mock<IMqttEntityManager>();
        var mockLogger = new Mock<ILogger<Apps.CostSensor.CostSensor>>();
        var mockScheduler = new Mock<IScheduler>(MockBehavior.Loose);
        var mockPriceSensorLogger = new Mock<ILogger<PriceSensor>>();
        var stateSubject = new Subject<StateChange>();

        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        mockHaContext
            .Setup(x => x.GetState(EnergySensorId))
            .Returns(new EntityState { EntityId = EnergySensorId, State = "100.0" });

        mockHaContext
            .Setup(x => x.GetState(TariffSensorId))
            .Returns(new EntityState
            {
                EntityId = TariffSensorId,
                State = tariffPrice.ToString(CultureInfo.InvariantCulture)
            });

        mockHaContext
            .Setup(x => x.GetState(CostSensorUniqueId))
            .Returns(initialCostState != null
                ? new EntityState { EntityId = CostSensorUniqueId, State = initialCostState }
                : null);

        mockEntityManager
            .Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<EntityCreationOptions>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        mockEntityManager
            .Setup(x => x.SetStateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mockEntityManager
            .Setup(x => x.SetAvailabilityAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var priceSensor = new PriceSensor(mockHaContext.Object, mockPriceSensorLogger.Object, TariffSensorId);

        var config = new CostSensorEntry
        {
            Name = "Test Cost Sensor",
            UniqueId = CostSensorUniqueId,
            Energy = EnergySensorId,
            Tariff = TariffSensorId,
            Cron = cron
        };

        var sensor = new Apps.CostSensor.CostSensor(
            mockHaContext.Object,
            mockEntityManager.Object,
            mockLogger.Object,
            mockScheduler.Object,
            priceSensor,
            config);

        return new SutComponents(sensor, mockHaContext, mockEntityManager, stateSubject);
    }

    private StateChange CreateStateChange(IHaContext haContext, string? oldState, string? newState)
    {
        var entity = new Entity(haContext, EnergySensorId);
        return new StateChange(
            entity,
            oldState != null ? new EntityState { EntityId = EnergySensorId, State = oldState } : null,
            newState != null ? new EntityState { EntityId = EnergySensorId, State = newState } : null);
    }

    [Fact]
    public async Task StateChange_WhenOldValueIsNull_SkipsCalculation()
    {
        // Arrange
        var sut = CreateSut();
        await sut.Sensor.InitializeAsync(CancellationToken.None);
        sut.EntityManager.Invocations.Clear();

        // Act — first startup state change: no old value
        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, null, "150.0"));
        await Task.Delay(300);

        // Assert — no cost update should be written
        sut.EntityManager.Verify(
            x => x.SetStateAsync(CostSensorUniqueId, It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task StateChange_ValidDelta_CalculatesCostCorrectly()
    {
        // Arrange — tariff is 2.0 kr/kWh
        var sut = CreateSut(tariffPrice: 2.0);
        await sut.Sensor.InitializeAsync(CancellationToken.None);
        sut.EntityManager.Invocations.Clear();

        // Act — energy delta = 100.5 - 100.0 = 0.5 kWh
        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, "100.0", "100.5"));
        await Task.Delay(300);

        // Assert — cost = 0.5 * 2.0 = 1.0 kr
        sut.EntityManager.Verify(
            x => x.SetStateAsync(CostSensorUniqueId, "1.0000"),
            Times.Once);
    }

    [Fact]
    public async Task StateChange_SpikeDetectedWithin60Seconds_IgnoresChange()
    {
        // Arrange
        var sut = CreateSut(tariffPrice: 1.0);
        await sut.Sensor.InitializeAsync(CancellationToken.None);
        sut.EntityManager.Invocations.Clear();

        // First change — small delta, establishes _lastStateChangeTime
        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, "100.0", "100.5"));
        await Task.Delay(300);
        sut.EntityManager.Invocations.Clear();

        // Act — immediate second change with large delta (>= 10 kWh within < 60 seconds)
        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, "100.5", "115.0")); // delta = 14.5
        await Task.Delay(300);

        // Assert — spike detected, no cost update
        sut.EntityManager.Verify(
            x => x.SetStateAsync(CostSensorUniqueId, It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task StateChange_LargeDeltaOnFirstEverChange_ProcessesNormally()
    {
        // Arrange — no prior state changes so _lastStateChangeTime is null
        var sut = CreateSut(tariffPrice: 1.0);
        await sut.Sensor.InitializeAsync(CancellationToken.None);
        sut.EntityManager.Invocations.Clear();

        // Act — large delta (>= 10 kWh), but no previous time recorded → no spike detection
        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, "100.0", "115.0")); // delta = 15.0
        await Task.Delay(500);

        // Assert — processed normally (15.0 * 1.0 = 15.0)
        sut.EntityManager.Verify(
            x => x.SetStateAsync(CostSensorUniqueId, "15.0000"),
            Times.Once);
    }

    [Fact]
    public async Task StateChange_SmallDeltaWithin60Seconds_NotTreatedAsSpike()
    {
        // Arrange
        var sut = CreateSut(tariffPrice: 1.0);
        await sut.Sensor.InitializeAsync(CancellationToken.None);
        sut.EntityManager.Invocations.Clear();

        // First change — establishes _lastStateChangeTime
        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, "100.0", "100.5"));
        await Task.Delay(300);
        sut.EntityManager.Invocations.Clear();

        // Act — small delta immediately after (delta = 0.3, well below threshold of 10)
        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, "100.5", "100.8")); // delta = 0.3
        await Task.Delay(300);

        // Assert — no spike, cost should be updated
        sut.EntityManager.Verify(
            x => x.SetStateAsync(CostSensorUniqueId, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetCost_ResetsAccumulatedCostToZero()
    {
        // Arrange — accumulate some cost first
        var sut = CreateSut(tariffPrice: 1.0);
        await sut.Sensor.InitializeAsync(CancellationToken.None);

        sut.StateSubject.OnNext(CreateStateChange(sut.HaContext.Object, "100.0", "101.0")); // delta = 1.0, cost = 1.0
        await Task.Delay(300);
        sut.EntityManager.Invocations.Clear();

        // Act — reset via private method
        var method = typeof(Apps.CostSensor.CostSensor)
            .GetMethod("ResetCostAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(sut.Sensor, null)!;

        // Assert — state set to 0.00
        sut.EntityManager.Verify(
            x => x.SetStateAsync(CostSensorUniqueId, "0.00"),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenExistingStateFound_LoadsValueFromHomeAssistant()
    {
        // Arrange — cost sensor already exists in HA with value 42.5
        var sut = CreateSut(initialCostState: "42.5", tariffPrice: 1.0);

        // Act
        await sut.Sensor.InitializeAsync(CancellationToken.None);

        // Assert — CreateAsync should NOT be called (entity already exists)
        sut.EntityManager.Verify(
            x => x.CreateAsync(It.IsAny<string>(), It.IsAny<EntityCreationOptions>(), It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_WhenNoExistingState_CreatesCostSensorEntity()
    {
        // Arrange — cost sensor does not exist yet (GetState returns null)
        var sut = CreateSut(initialCostState: null, tariffPrice: 1.0);

        // Act
        await sut.Sensor.InitializeAsync(CancellationToken.None);

        // Assert — CreateAsync should be called once
        sut.EntityManager.Verify(
            x => x.CreateAsync(CostSensorUniqueId, It.IsAny<EntityCreationOptions>(), It.IsAny<object>()),
            Times.Once);
    }
}
