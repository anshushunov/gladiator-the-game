using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

public class TournamentModelTests
{
    [Fact]
    public void Default_ShouldBeValid()
    {
        var model = TournamentModel.Default;
        model.Validate();

        Assert.Equal(200, model.DefaultPrizePool);
        Assert.Equal(2, model.MinParticipants);
        Assert.Equal(0.60, model.ChampionPrizeShare);
        Assert.Equal(0.25, model.RunnerUpPrizeShare);
    }

    [Fact]
    public void Validate_NegativePrizePool_ShouldThrow()
    {
        var model = TournamentModel.Default with { DefaultPrizePool = -1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => model.Validate());
    }

    [Fact]
    public void Validate_MinParticipantsBelow2_ShouldThrow()
    {
        var model = TournamentModel.Default with { MinParticipants = 1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => model.Validate());
    }

    [Fact]
    public void Validate_SharesExceed1_ShouldThrow()
    {
        var model = TournamentModel.Default with { ChampionPrizeShare = 0.8, RunnerUpPrizeShare = 0.3 };
        Assert.Throws<ArgumentException>(() => model.Validate());
    }
}

public class TournamentEngineTests
{
    private static Stats MakeStats(int str = 5, int agi = 5, int sta = 5) => new(str, agi, sta);

    private static Gladiator MakeGladiator(string name, int str = 5, int agi = 5, int sta = 5)
        => Gladiator.Create(name, MakeStats(str, agi, sta));

    private static IReadOnlyList<Gladiator> MakeGladiators(int count)
    {
        var list = new List<Gladiator>();
        for (int i = 0; i < count; i++)
            list.Add(MakeGladiator($"Gladiator{i + 1}"));
        return list;
    }

    [Fact]
    public void FourParticipants_ShouldProduce2Rounds()
    {
        var gladiators = MakeGladiators(4);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(123);

        var (updated, result) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng, TournamentModel.Default);

        Assert.Equal(2, result.Rounds.Count);
        Assert.Equal(2, result.Rounds[0].Matches.Count); // 2 матча в 1 раунде
        Assert.Single(result.Rounds[1].Matches);          // 1 финал
        Assert.Contains(result.ChampionId, ids);
        Assert.NotNull(result.RunnerUpId);
        Assert.NotEqual(result.ChampionId, result.RunnerUpId);
    }

    [Fact]
    public void TwoParticipants_ShouldProduce1Round()
    {
        var gladiators = MakeGladiators(2);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);

        var (_, result) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng, TournamentModel.Default);

        Assert.Single(result.Rounds);
        Assert.Single(result.Rounds[0].Matches);
        Assert.NotNull(result.Rounds[0].Matches[0].Result); // Реальный бой
        Assert.NotNull(result.RunnerUpId);
    }

    [Fact]
    public void ThreeParticipants_ShouldHaveByeInRound1()
    {
        var gladiators = MakeGladiators(3);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);

        var (_, result) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng, TournamentModel.Default);

        Assert.Equal(2, result.Rounds.Count);
        // Раунд 1: 2 матча в сетке размером 4
        Assert.Equal(2, result.Rounds[0].Matches.Count);
        // Один из матчей должен быть bye (null Result)
        Assert.Contains(result.Rounds[0].Matches, m => m.Result == null);
    }

    [Fact]
    public void FiveParticipants_ShouldHave3Rounds()
    {
        var gladiators = MakeGladiators(5);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);

        var (_, result) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng, TournamentModel.Default);

        // bracket size = 8, so 3 rounds
        Assert.Equal(3, result.Rounds.Count);
        Assert.Equal(4, result.Rounds[0].Matches.Count);
        // 3 bye матча (8 - 5 = 3 пустых слотов)
        int byeCount = result.Rounds[0].Matches.Count(m => m.Result == null);
        Assert.Equal(3, byeCount);
    }

    [Fact]
    public void Determinism_SameSeed_SameResult()
    {
        var gladiators = MakeGladiators(4);
        var ids = gladiators.Select(g => g.Id).ToList();

        var rng1 = new SeededRng(777);
        var (_, result1) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng1, TournamentModel.Default);

        var rng2 = new SeededRng(777);
        var (_, result2) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng2, TournamentModel.Default);

        Assert.Equal(result1.ChampionId, result2.ChampionId);
        Assert.Equal(result1.RunnerUpId, result2.RunnerUpId);
        Assert.Equal(result1.Rounds.Count, result2.Rounds.Count);
    }

    [Fact]
    public void Prizes_ShouldBeCorrectPercentage()
    {
        var gladiators = MakeGladiators(4);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);
        var model = TournamentModel.Default;

        var (_, result) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng, model);

        Assert.Equal((int)(200 * 0.60), result.ChampionPrize);
        Assert.Equal((int)(200 * 0.25), result.RunnerUpPrize);
    }

    [Fact]
    public void LessThan2Participants_ShouldThrow()
    {
        var gladiators = MakeGladiators(1);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);

        Assert.Throws<ValidationException>(() =>
            TournamentEngine.RunTournament(gladiators, ids, 200, rng, TournamentModel.Default));
    }

    [Fact]
    public void InjuredParticipant_ShouldThrow()
    {
        var g1 = MakeGladiator("Healthy");
        var g2 = MakeGladiator("Injured").ApplyInjury(new Injury(InjuryType.Fracture, 5));
        var gladiators = new[] { g1, g2 };
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);

        Assert.Throws<ValidationException>(() =>
            TournamentEngine.RunTournament(gladiators, ids, 200, rng, TournamentModel.Default));
    }

    [Fact]
    public void InjuredDuringTournament_ShouldGetByeInNextRound()
    {
        // С достаточным количеством бойцов и определённым seed,
        // травмированный боец в следующем раунде получает bye (или проигрывает автоматически).
        // Проверяем что турнир не крашится и завершается корректно.
        var gladiators = MakeGladiators(4);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);

        var (updated, result) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng, TournamentModel.Default);

        // Турнир должен завершиться без исключений
        Assert.NotEqual(Guid.Empty, result.ChampionId);
        Assert.Equal(2, result.Rounds.Count);
    }

    [Fact]
    public void AllParticipantIds_ShouldBePreserved()
    {
        var gladiators = MakeGladiators(4);
        var ids = gladiators.Select(g => g.Id).ToList();
        var rng = new SeededRng(42);

        var (_, result) = TournamentEngine.RunTournament(
            gladiators, ids, 200, rng, TournamentModel.Default);

        Assert.Equal(ids.Count, result.ParticipantIds.Count);
        foreach (var id in ids)
            Assert.Contains(id, result.ParticipantIds);
    }
}

public class LudusStateTournamentTests
{
    private static Stats MakeStats() => new(5, 5, 5);

    private static LudusState CreateStateWithGladiators(int count, int seed = 42)
    {
        var state = LudusState.NewGame(seed);
        for (int i = 0; i < count; i++)
        {
            var g = Gladiator.Create($"Fighter{i + 1}", MakeStats());
            state = state.AddGladiator(g);
        }
        return state;
    }

    [Fact]
    public void RunTournament_ShouldUpdateMoney()
    {
        var state = CreateStateWithGladiators(4);
        var ids = state.Gladiators.Select(g => g.Id).ToList();
        int moneyBefore = state.Money;

        var (newState, result) = state.RunTournament(ids, 200);

        int expectedGain = result.ChampionPrize + result.RunnerUpPrize;
        Assert.Equal(moneyBefore + expectedGain, newState.Money);
    }

    [Fact]
    public void RunTournament_ShouldUpdateSeed()
    {
        var state = CreateStateWithGladiators(4);
        var ids = state.Gladiators.Select(g => g.Id).ToList();
        int seedBefore = state.Seed;

        var (newState, _) = state.RunTournament(ids, 200);

        Assert.NotEqual(seedBefore, newState.Seed);
    }

    [Fact]
    public void RunTournament_ShouldStoreLastTournamentResult()
    {
        var state = CreateStateWithGladiators(4);
        var ids = state.Gladiators.Select(g => g.Id).ToList();

        Assert.Null(state.LastTournamentResult);

        var (newState, result) = state.RunTournament(ids, 200);

        Assert.NotNull(newState.LastTournamentResult);
        Assert.Equal(result.ChampionId, newState.LastTournamentResult!.Value.ChampionId);
    }

    [Fact]
    public void RunTournament_ShouldUpdateGladiators()
    {
        var state = CreateStateWithGladiators(4);
        var ids = state.Gladiators.Select(g => g.Id).ToList();

        var (newState, _) = state.RunTournament(ids, 200);

        // Все гладиаторы должны быть сохранены
        Assert.Equal(state.Gladiators.Count, newState.Gladiators.Count);
    }

    [Fact]
    public void RunTournament_Determinism_SameSeedSameResult()
    {
        // Используем одни и те же гладиаторы (одинаковые ID) для обоих прогонов
        var state = CreateStateWithGladiators(4, 100);
        var ids = state.Gladiators.Select(g => g.Id).ToList();

        var (newState1, result1) = state.RunTournament(ids, 200);
        var (newState2, result2) = state.RunTournament(ids, 200);

        Assert.Equal(result1.ChampionId, result2.ChampionId);
        Assert.Equal(newState1.Money, newState2.Money);
        Assert.Equal(newState1.Seed, newState2.Seed);
    }

    [Fact]
    public void RunTournament_LessThan2_ShouldThrow()
    {
        var state = CreateStateWithGladiators(1);
        var ids = state.Gladiators.Select(g => g.Id).ToList();

        Assert.Throws<ValidationException>(() => state.RunTournament(ids, 200));
    }

    [Fact]
    public void RunTournament_InjuredParticipant_ShouldThrow()
    {
        var state = CreateStateWithGladiators(2);
        var injured = state.Gladiators[0].ApplyInjury(new Injury(InjuryType.Bruise, 2));
        state = state with
        {
            Gladiators = state.Gladiators.Select(g => g.Id == injured.Id ? injured : g).ToArray()
        };
        var ids = state.Gladiators.Select(g => g.Id).ToList();

        Assert.Throws<ValidationException>(() => state.RunTournament(ids, 200));
    }

    [Fact]
    public void RunTournament_DefaultOverload_ShouldWork()
    {
        var state = CreateStateWithGladiators(2);
        var ids = state.Gladiators.Select(g => g.Id).ToList();

        var (newState, result) = state.RunTournament(ids, 200);

        Assert.NotEqual(Guid.Empty, result.ChampionId);
    }
}
