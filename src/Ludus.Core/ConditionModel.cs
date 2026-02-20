using System;

namespace Ludus.Core;

/// <summary>
/// Балансовые параметры системы морали и усталости.
/// </summary>
public readonly record struct ConditionModel(
    int MoraleWinBonus,
    int MoraleLosePenalty,
    int MoraleInjuryPenalty,
    int MoraleDailyRestBonus,
    int MoraleDailyTrainingDrain,
    int FatigueFightGain,
    int FatigueTrainingGain,
    int FatigueDailyRestRecovery,
    int FatigueDailyTrainingRecovery,
    double HighMoraleBonus,
    double LowMoralePenalty,
    double HighFatiguePenalty,
    double LowFatigueBonus)
{
    public const int MinMorale = 0;
    public const int MaxMorale = 100;
    public const int DefaultMorale = 50;

    public const int MinFatigue = 0;
    public const int MaxFatigue = 100;
    public const int DefaultFatigue = 0;

    public static ConditionModel Default => new(
        MoraleWinBonus: 15,
        MoraleLosePenalty: -20,
        MoraleInjuryPenalty: -10,
        MoraleDailyRestBonus: 5,
        MoraleDailyTrainingDrain: -3,
        FatigueFightGain: 30,
        FatigueTrainingGain: 10,
        FatigueDailyRestRecovery: -15,
        FatigueDailyTrainingRecovery: -5,
        HighMoraleBonus: 0.15,
        LowMoralePenalty: -0.25,
        HighFatiguePenalty: -0.25,
        LowFatigueBonus: 0.05);

    public void Validate()
    {
        if (MoraleWinBonus < 0)
            throw new ArgumentOutOfRangeException(nameof(MoraleWinBonus), "Must be >= 0");
        if (MoraleLosePenalty > 0)
            throw new ArgumentOutOfRangeException(nameof(MoraleLosePenalty), "Must be <= 0");
        if (MoraleInjuryPenalty > 0)
            throw new ArgumentOutOfRangeException(nameof(MoraleInjuryPenalty), "Must be <= 0");
        if (MoraleDailyRestBonus < 0)
            throw new ArgumentOutOfRangeException(nameof(MoraleDailyRestBonus), "Must be >= 0");
        if (MoraleDailyTrainingDrain > 0)
            throw new ArgumentOutOfRangeException(nameof(MoraleDailyTrainingDrain), "Must be <= 0");
        if (FatigueFightGain < 0)
            throw new ArgumentOutOfRangeException(nameof(FatigueFightGain), "Must be >= 0");
        if (FatigueTrainingGain < 0)
            throw new ArgumentOutOfRangeException(nameof(FatigueTrainingGain), "Must be >= 0");
        if (FatigueDailyRestRecovery > 0)
            throw new ArgumentOutOfRangeException(nameof(FatigueDailyRestRecovery), "Must be <= 0");
        if (FatigueDailyTrainingRecovery > 0)
            throw new ArgumentOutOfRangeException(nameof(FatigueDailyTrainingRecovery), "Must be <= 0");
        if (HighMoraleBonus < 0)
            throw new ArgumentOutOfRangeException(nameof(HighMoraleBonus), "Must be >= 0");
        if (LowMoralePenalty > 0)
            throw new ArgumentOutOfRangeException(nameof(LowMoralePenalty), "Must be <= 0");
        if (HighFatiguePenalty > 0)
            throw new ArgumentOutOfRangeException(nameof(HighFatiguePenalty), "Must be <= 0");
        if (LowFatigueBonus < 0)
            throw new ArgumentOutOfRangeException(nameof(LowFatigueBonus), "Must be <= 0");
    }
}
