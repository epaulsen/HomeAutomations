using HomeAutomations.Apps.EaseeApp;

namespace HomeAutomations.Tests.EaseeApp;

public class EaseeChargingAppTests
{
    [Theory]
    [InlineData(4.83, 230, 21)]
    [InlineData(4.6, 230, 20)]
    [InlineData(0, 230, 0)]
    [InlineData(-1, 230, 0)]
    [InlineData(2.3, 0, 0)]
    public void CalculateCurrentAmpere_ReturnsExpectedAmps(double chargingPowerKw, double voltage, int expectedAmpere)
    {
        var result = EaseeChargingApp.CalculateCurrentAmpere(chargingPowerKw, voltage);
        Assert.Equal(expectedAmpere, result);
    }

    [Theory]
    [InlineData(-600, 230, 3)]
    [InlineData(-1, 230, 1)]
    [InlineData(-230, 230, 1)]
    [InlineData(-460, 230, 2)]
    [InlineData(-600, 0, 0)]
    public void CalculateReductionAmpere_UsesCeiling(double availablePowerWatts, double voltage, int expectedReductionAmpere)
    {
        var result = EaseeChargingApp.CalculateReductionAmpere(availablePowerWatts, voltage);
        Assert.Equal(expectedReductionAmpere, result);
    }

    [Fact]
    public void Evaluate_FeatureDisabled_SendsMaxOnceThenNoop()
    {
        var engine = new EaseeChargingDecisionEngine();
        var input = CreateInput(isFeatureEnabled: false, isCharging: true, availablePowerWatts: -1200, currentAmpere: 20);
        var now = DateTimeOffset.UtcNow;

        var first = engine.Evaluate(input, now);
        var second = engine.Evaluate(input, now.AddSeconds(5));

        Assert.NotNull(first);
        Assert.Equal(25, first!.TargetAmpere);
        Assert.True(first.ForceSend);
        Assert.Null(second);
    }

    [Fact]
    public void Evaluate_FeatureDisabledThenEnabled_ResumesNormalRuleProcessing()
    {
        var engine = new EaseeChargingDecisionEngine();
        var now = DateTimeOffset.UtcNow;

        _ = engine.Evaluate(CreateInput(isFeatureEnabled: false, isCharging: true, availablePowerWatts: -600, currentAmpere: 20), now);

        var firstEnabled = engine.Evaluate(
            CreateInput(isFeatureEnabled: true, isCharging: true, availablePowerWatts: -600, currentAmpere: 20),
            now.AddSeconds(1));

        var afterWindow = engine.Evaluate(
            CreateInput(isFeatureEnabled: true, isCharging: true, availablePowerWatts: -600, currentAmpere: 20),
            now.AddSeconds(62));

        Assert.Null(firstEnabled);
        Assert.NotNull(afterWindow);
        Assert.Equal(17, afterWindow!.TargetAmpere);
    }

    [Fact]
    public void Evaluate_NegativePowerAfterOneMinute_ReducesAndClampsToSixAmp()
    {
        var engine = new EaseeChargingDecisionEngine();
        var now = DateTimeOffset.UtcNow;
        var input = CreateInput(isFeatureEnabled: true, isCharging: true, availablePowerWatts: -2000, currentAmpere: 10);

        var first = engine.Evaluate(input, now);
        var second = engine.Evaluate(input, now.AddSeconds(61));

        Assert.Null(first);
        Assert.NotNull(second);
        Assert.Equal(6, second!.TargetAmpere);
    }

    [Fact]
    public void Evaluate_PositivePowerAfterTwoMinutes_IncreasesByOne()
    {
        var engine = new EaseeChargingDecisionEngine();
        var now = DateTimeOffset.UtcNow;
        var input = CreateInput(isFeatureEnabled: true, isCharging: true, availablePowerWatts: 700, currentAmpere: 20);

        var first = engine.Evaluate(input, now);
        var second = engine.Evaluate(input, now.AddSeconds(121));

        Assert.Null(first);
        Assert.NotNull(second);
        Assert.Equal(21, second!.TargetAmpere);
    }

    [Fact]
    public void Evaluate_NotCharging_DoesNothing()
    {
        var engine = new EaseeChargingDecisionEngine();
        var now = DateTimeOffset.UtcNow;
        var input = CreateInput(isFeatureEnabled: true, isCharging: false, availablePowerWatts: -1000, currentAmpere: 20);

        var result = engine.Evaluate(input, now);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_PositivePowerAtMaxAmp_DoesNothing()
    {
        var engine = new EaseeChargingDecisionEngine();
        var now = DateTimeOffset.UtcNow;
        var input = CreateInput(isFeatureEnabled: true, isCharging: true, availablePowerWatts: 900, currentAmpere: 25);

        var result = engine.Evaluate(input, now);

        Assert.Null(result);
    }

    private static EaseeEvaluationInput CreateInput(
        bool isFeatureEnabled,
        bool isCharging,
        double availablePowerWatts,
        int currentAmpere)
    {
        return new EaseeEvaluationInput(
            IsFeatureEnabled: isFeatureEnabled,
            IsCharging: isCharging,
            AvailablePowerWatts: availablePowerWatts,
            CurrentAmpere: currentAmpere,
            Voltage: 230,
            MinAmpere: 6,
            MaxAmpere: 25,
            PositiveThresholdWatts: 400,
            PositiveStepAmpere: 1,
            NegativeWindow: TimeSpan.FromSeconds(60),
            PositiveWindow: TimeSpan.FromSeconds(120));
    }
}
