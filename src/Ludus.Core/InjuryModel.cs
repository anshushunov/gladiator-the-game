using System;

namespace Ludus.Core;

/// <summary>
/// Балансовые параметры системы травм.
/// </summary>
public readonly record struct InjuryModel(
    double BaseInjuryChanceLoser,
    double BaseInjuryChanceWinner,
    int BruiseDays,
    int SprainDays,
    int FractureDays,
    double SprainThreshold,
    double FractureThreshold)
{
    public static InjuryModel Default => new(
        BaseInjuryChanceLoser: 0.60,
        BaseInjuryChanceWinner: 0.20,
        BruiseDays: 1,
        SprainDays: 3,
        FractureDays: 7,
        SprainThreshold: 0.40,
        FractureThreshold: 0.70);

    public void Validate()
    {
        if (BaseInjuryChanceLoser < 0 || BaseInjuryChanceLoser > 1)
            throw new ArgumentOutOfRangeException(nameof(BaseInjuryChanceLoser));
        if (BaseInjuryChanceWinner < 0 || BaseInjuryChanceWinner > 1)
            throw new ArgumentOutOfRangeException(nameof(BaseInjuryChanceWinner));
        if (BruiseDays < 1)
            throw new ArgumentOutOfRangeException(nameof(BruiseDays));
        if (SprainDays < 1)
            throw new ArgumentOutOfRangeException(nameof(SprainDays));
        if (FractureDays < 1)
            throw new ArgumentOutOfRangeException(nameof(FractureDays));
        if (SprainThreshold < 0 || SprainThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(SprainThreshold));
        if (FractureThreshold < 0 || FractureThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(FractureThreshold));
        if (SprainThreshold >= FractureThreshold)
            throw new ArgumentException("SprainThreshold must be less than FractureThreshold.");
    }
}
