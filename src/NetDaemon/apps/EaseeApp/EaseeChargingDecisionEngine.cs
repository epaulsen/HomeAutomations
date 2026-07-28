namespace HomeAutomations.Apps.EaseeApp;

public sealed class EaseeChargingDecisionEngine
{
    private DateTimeOffset? _negativeSince;
    private DateTimeOffset? _positiveSince;
    private bool _forcedMaxSentWhileDisabled;

    public EaseeChargeCommand? Evaluate(EaseeEvaluationInput input, DateTimeOffset now)
    {
        if (!input.IsFeatureEnabled)
        {
            ResetWindows();
            if (!_forcedMaxSentWhileDisabled)
            {
                _forcedMaxSentWhileDisabled = true;
                return new EaseeChargeCommand(input.MaxAmpere, ForceSend: true);
            }

            return null;
        }

        _forcedMaxSentWhileDisabled = false;

        if (!input.IsCharging)
        {
            ResetWindows();
            return null;
        }

        if (input.AvailablePowerWatts < 0)
        {
            _positiveSince = null;

            if (!_negativeSince.HasValue)
            {
                _negativeSince = now;
                return null;
            }

            if (now - _negativeSince.Value < input.NegativeWindow)
            {
                return null;
            }

            var decreaseAmpere = EaseeChargingApp.CalculateReductionAmpere(Math.Abs(input.AvailablePowerWatts), input.Voltage);
            if (decreaseAmpere <= 0)
            {
                _negativeSince = now;
                return null;
            }

            _negativeSince = now;
            var targetAmpere = Math.Clamp(input.CurrentAmpere - decreaseAmpere, input.MinAmpere, input.MaxAmpere);
            return new EaseeChargeCommand(targetAmpere, ForceSend: false);
        }

        if (input.AvailablePowerWatts > input.PositiveThresholdWatts && input.CurrentAmpere < input.MaxAmpere)
        {
            _negativeSince = null;

            if (!_positiveSince.HasValue)
            {
                _positiveSince = now;
                return null;
            }

            if (now - _positiveSince.Value < input.PositiveWindow)
            {
                return null;
            }

            _positiveSince = now;
            var targetAmpere = Math.Clamp(input.CurrentAmpere + input.PositiveStepAmpere, input.MinAmpere, input.MaxAmpere);
            return new EaseeChargeCommand(targetAmpere, ForceSend: false);
        }

        ResetWindows();
        return null;
    }

    private void ResetWindows()
    {
        _negativeSince = null;
        _positiveSince = null;
    }
}

public sealed record EaseeEvaluationInput(
    bool IsFeatureEnabled,
    bool IsCharging,
    double AvailablePowerWatts,
    int CurrentAmpere,
    double Voltage,
    int MinAmpere,
    int MaxAmpere,
    int PositiveThresholdWatts,
    int PositiveStepAmpere,
    TimeSpan NegativeWindow,
    TimeSpan PositiveWindow);

public sealed record EaseeChargeCommand(int TargetAmpere, bool ForceSend);
