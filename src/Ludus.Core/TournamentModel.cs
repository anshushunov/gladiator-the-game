using System;

namespace Ludus.Core;

/// <summary>
/// Балансовые параметры турнирной системы.
/// </summary>
public readonly record struct TournamentModel(
    int DefaultPrizePool,
    int MinParticipants,
    double ChampionPrizeShare,
    double RunnerUpPrizeShare)
{
    public static TournamentModel Default => new(
        DefaultPrizePool: 200,
        MinParticipants: 2,
        ChampionPrizeShare: 0.60,
        RunnerUpPrizeShare: 0.25);

    public void Validate()
    {
        if (DefaultPrizePool < 0)
            throw new ArgumentOutOfRangeException(nameof(DefaultPrizePool), "Must be >= 0");
        if (MinParticipants < 2)
            throw new ArgumentOutOfRangeException(nameof(MinParticipants), "Must be >= 2");
        if (ChampionPrizeShare < 0 || ChampionPrizeShare > 1)
            throw new ArgumentOutOfRangeException(nameof(ChampionPrizeShare), "Must be in [0, 1]");
        if (RunnerUpPrizeShare < 0 || RunnerUpPrizeShare > 1)
            throw new ArgumentOutOfRangeException(nameof(RunnerUpPrizeShare), "Must be in [0, 1]");
        if (ChampionPrizeShare + RunnerUpPrizeShare > 1)
            throw new ArgumentException("ChampionPrizeShare + RunnerUpPrizeShare must be <= 1");
    }
}
