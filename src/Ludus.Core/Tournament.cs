using System;
using System.Collections.Generic;

namespace Ludus.Core;

/// <summary>
/// Матч турнира. Если один из слотов null — это bye (автопроход).
/// </summary>
public readonly record struct TournamentMatch(
    Guid? Fighter1Id,
    Guid? Fighter2Id,
    Guid WinnerId,
    FightResult? Result);

/// <summary>
/// Раунд турнира: набор матчей.
/// </summary>
public readonly record struct TournamentRound(
    int RoundNumber,
    IReadOnlyList<TournamentMatch> Matches);

/// <summary>
/// Итоговый результат турнира.
/// </summary>
public readonly record struct TournamentResult(
    IReadOnlyList<TournamentRound> Rounds,
    IReadOnlyList<Guid> ParticipantIds,
    Guid ChampionId,
    Guid? RunnerUpId,
    int PrizePool,
    int ChampionPrize,
    int RunnerUpPrize);
