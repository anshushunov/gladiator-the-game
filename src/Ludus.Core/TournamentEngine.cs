using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludus.Core;

/// <summary>
/// Движок турнира single-elimination: формирует сетку, проводит бои,
/// начисляет призы.
/// </summary>
public static class TournamentEngine
{
    /// <summary>
    /// Проводит турнир single-elimination.
    /// </summary>
    /// <param name="allGladiators">Полный список гладиаторов (для обновления состояния).</param>
    /// <param name="participantIds">ID участников турнира.</param>
    /// <param name="prizePool">Призовой фонд.</param>
    /// <param name="rng">Генератор случайных чисел.</param>
    /// <param name="model">Балансовые параметры турнира.</param>
    public static (IReadOnlyList<Gladiator> UpdatedGladiators, TournamentResult Result)
        RunTournament(IReadOnlyList<Gladiator> allGladiators,
                      IReadOnlyList<Guid> participantIds,
                      int prizePool, IRng rng, TournamentModel model)
    {
        model.Validate();

        if (participantIds.Count < model.MinParticipants)
            throw new ValidationException(
                $"Минимум {model.MinParticipants} участника для турнира, получено {participantIds.Count}");

        // Проверяем что все участники существуют и CanFight
        var gladiatorMap = allGladiators.ToDictionary(g => g.Id);
        foreach (var id in participantIds)
        {
            if (!gladiatorMap.TryGetValue(id, out var g))
                throw new ValidationException($"Участник {id} не найден");
            if (!g.CanFight)
                throw new ValidationException($"Участник {g.Name} не может сражаться");
        }

        // Определяем размер сетки (степень 2)
        int bracketSize = NextPowerOfTwo(participantIds.Count);

        // Шаффлим участников
        var shuffled = Shuffle(participantIds, rng);

        // Распределяем byes в второй слот последних пар (гарантия: нет null vs null)
        int numByes = bracketSize - shuffled.Count;
        int numRealMatchPairs = bracketSize / 2 - numByes;
        var slots = new Guid?[bracketSize];
        int pIdx = 0;
        for (int pair = 0; pair < numRealMatchPairs; pair++)
        {
            slots[pair * 2] = shuffled[pIdx++];
            slots[pair * 2 + 1] = shuffled[pIdx++];
        }
        for (int pair = numRealMatchPairs; pair < bracketSize / 2; pair++)
        {
            slots[pair * 2] = shuffled[pIdx++];
            // slots[pair * 2 + 1] stays null (bye)
        }

        var rounds = new List<TournamentRound>();
        var currentSlots = slots;
        int roundNumber = 0;

        while (currentSlots.Length > 1)
        {
            roundNumber++;
            var matches = new List<TournamentMatch>();
            var nextSlots = new Guid?[currentSlots.Length / 2];

            for (int i = 0; i < currentSlots.Length; i += 2)
            {
                var slot1 = currentSlots[i];
                var slot2 = currentSlots[i + 1];

                var match = ResolveMatch(slot1, slot2, gladiatorMap, rng);
                matches.Add(match);
                nextSlots[i / 2] = match.WinnerId;
            }

            rounds.Add(new TournamentRound(roundNumber, matches));
            currentSlots = nextSlots;
        }

        var championId = currentSlots[0]!.Value;

        // Определяем RunnerUp: проигравший финального матча
        Guid? runnerUpId = null;
        if (rounds.Count > 0)
        {
            var finalMatch = rounds[^1].Matches[0];
            if (finalMatch.Fighter1Id.HasValue && finalMatch.Fighter2Id.HasValue)
            {
                runnerUpId = finalMatch.WinnerId == finalMatch.Fighter1Id
                    ? finalMatch.Fighter2Id
                    : finalMatch.Fighter1Id;
            }
        }

        // Призы
        int championPrize = (int)(prizePool * model.ChampionPrizeShare);
        int runnerUpPrize = runnerUpId.HasValue ? (int)(prizePool * model.RunnerUpPrizeShare) : 0;

        var result = new TournamentResult(
            rounds, participantIds, championId, runnerUpId,
            prizePool, championPrize, runnerUpPrize);

        var updatedGladiators = gladiatorMap.Values.ToArray();
        return (updatedGladiators, result);
    }

    private static TournamentMatch ResolveMatch(
        Guid? slot1, Guid? slot2,
        Dictionary<Guid, Gladiator> gladiatorMap, IRng rng)
    {
        // Оба null — не должно быть (гарантировано алгоритмом)
        // Bye: один null или один !CanFight
        if (!slot1.HasValue && slot2.HasValue)
            return new TournamentMatch(null, slot2, slot2.Value, null);

        if (slot1.HasValue && !slot2.HasValue)
            return new TournamentMatch(slot1, null, slot1.Value, null);

        // Оба присутствуют — проверяем CanFight
        var g1 = gladiatorMap[slot1!.Value];
        var g2 = gladiatorMap[slot2!.Value];

        if (!g1.CanFight && !g2.CanFight)
        {
            // Оба не могут — побеждает первый (edge case)
            return new TournamentMatch(slot1, slot2, slot1.Value, null);
        }

        if (!g1.CanFight)
            return new TournamentMatch(slot1, slot2, slot2.Value, null);

        if (!g2.CanFight)
            return new TournamentMatch(slot1, slot2, slot1.Value, null);

        // Реальный бой
        int g1MaxHealth = g1.MaxHealth;
        int g2MaxHealth = g2.MaxHealth;

        var fightResult = FightEngine.SimulateFight(g1, g2, rng);
        var (winner, loser) = ApplyPostFightEffects(
            fightResult, g1.Id, g1MaxHealth, g2MaxHealth, rng,
            InjuryModel.Default, ConditionModel.Default);

        gladiatorMap[winner.Id] = winner;
        gladiatorMap[loser.Id] = loser;

        return new TournamentMatch(slot1, slot2, fightResult.Winner.Id, fightResult);
    }

    /// <summary>
    /// Применяет пост-боевые эффекты (травмы, мораль/усталость).
    /// Дублирует логику из LudusState.ResolveFight:352-375.
    /// </summary>
    private static (Gladiator Winner, Gladiator Loser) ApplyPostFightEffects(
        FightResult result, Guid firstId, int firstMaxHp, int secondMaxHp,
        IRng rng, InjuryModel injuryModel, ConditionModel conditionModel)
    {
        // Травмы
        var winner = InjuryResolver.ResolveInjury(result.Winner,
            result.Winner.Id == firstId ? firstMaxHp : secondMaxHp,
            true, rng, injuryModel);
        var loser = InjuryResolver.ResolveInjury(result.Loser,
            result.Loser.Id == firstId ? firstMaxHp : secondMaxHp,
            false, rng, injuryModel);

        // Мораль/усталость
        winner = ConditionResolver.ApplyFightOutcome(winner, true, conditionModel);
        loser = ConditionResolver.ApplyFightOutcome(loser, false, conditionModel);

        // Штраф морали за травму
        if (winner.IsInjured)
            winner = ConditionResolver.ApplyInjuryMoralePenalty(winner, conditionModel);
        if (loser.IsInjured)
            loser = ConditionResolver.ApplyInjuryMoralePenalty(loser, conditionModel);

        // Снять тренировку при травме
        if (winner.IsInjured && winner.CurrentTraining.HasValue)
            winner = winner.ClearTraining();
        if (loser.IsInjured && loser.CurrentTraining.HasValue)
            loser = loser.ClearTraining();

        return (winner, loser);
    }

    private static IReadOnlyList<Guid> Shuffle(IReadOnlyList<Guid> list, IRng rng)
    {
        var arr = list.ToArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }

    private static int NextPowerOfTwo(int n)
    {
        int power = 1;
        while (power < n)
            power *= 2;
        return power;
    }
}
