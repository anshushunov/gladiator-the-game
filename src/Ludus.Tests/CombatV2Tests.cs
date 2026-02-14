using System;
using System.Linq;
using Ludus.Core;
using Xunit;

namespace Ludus.Tests;

public class CombatV2Tests
{
    [Fact]
    public void CombatResolver_HitChance_ShouldBeClampedAtBoundaries()
    {
        var model = CombatModel.Default with
        {
            BaseHitChance = 0.5,
            HitChancePerAgilityDiff = 0.1,
            MinHitChance = 0.2,
            MaxHitChance = 0.8
        };
        var resolver = new CombatResolver(model);

        var highAgiAttacker = Gladiator.Create("Fast", new Stats(5, 10, 5));
        var lowAgiDefender = Gladiator.Create("Slow", new Stats(5, 1, 5));
        var lowAgiAttacker = Gladiator.Create("SlowAtk", new Stats(5, 1, 5));
        var highAgiDefender = Gladiator.Create("FastDef", new Stats(5, 10, 5));

        var maxCase = resolver.GetHitChance(highAgiAttacker, lowAgiDefender);
        var minCase = resolver.GetHitChance(lowAgiAttacker, highAgiDefender);

        Assert.Equal(model.MaxHitChance, maxCase);
        Assert.Equal(model.MinHitChance, minCase);
    }

    [Fact]
    public void CombatResolver_CritCannotHappenOnMiss()
    {
        var noHitModel = CombatModel.Default with
        {
            BaseHitChance = 0,
            MinHitChance = 0,
            MaxHitChance = 0,
            BaseCritChance = 1,
            MaxCritChance = 1
        };

        var resolver = new CombatResolver(noHitModel);
        var attacker = Gladiator.Create("A", new Stats(5, 10, 5));
        var defender = Gladiator.Create("D", new Stats(5, 1, 5));

        var result = resolver.ResolveAttack(attacker, defender, new SeededRng(42), 1);

        Assert.False(result.IsHit);
        Assert.False(result.IsCritical);
        Assert.DoesNotContain(result.Events, e => e.Type == FightLog.EventType.Crit);
    }

    [Fact]
    public void CombatResolver_DamageShouldNeverBeNegative()
    {
        var highDefenseModel = CombatModel.Default with
        {
            DefensePerStamina = 100,
            MinDamageAfterDefense = 0,
            BaseHitChance = 1,
            MinHitChance = 1,
            MaxHitChance = 1,
            BaseCritChance = 0,
            MaxCritChance = 0
        };

        var resolver = new CombatResolver(highDefenseModel);
        var attacker = Gladiator.Create("A", new Stats(1, 5, 5));
        var defender = Gladiator.Create("D", new Stats(1, 5, 10));

        var result = resolver.ResolveAttack(attacker, defender, new SeededRng(42), 1);

        Assert.True(result.Damage >= 0);
    }

    [Fact]
    public void CombatResolver_ShouldSupportZeroBaseDamageEdgeCase()
    {
        var zeroBaseDamageModel = CombatModel.Default with
        {
            BaseHitChance = 1,
            MinHitChance = 1,
            MaxHitChance = 1,
            DamageVarianceMin = 0,
            DamageVarianceMax = 0,
            DefensePerStamina = 10,
            MinDamageAfterDefense = 0,
            BaseCritChance = 0,
            MaxCritChance = 0
        };

        var resolver = new CombatResolver(zeroBaseDamageModel);
        var attacker = Gladiator.Create("A", new Stats(1, 5, 5));
        var defender = Gladiator.Create("D", new Stats(1, 5, 10));

        var result = resolver.ResolveAttack(attacker, defender, new SeededRng(123), 1);

        Assert.Equal(0, result.Damage);
        Assert.True(result.IsHit);
    }

    [Fact]
    public void SimulateFight_Determinism_SameSeed_ShouldProduceSameCombatSequence()
    {
        var g1 = Gladiator.Create("Alpha", new Stats(8, 7, 6));
        var g2 = Gladiator.Create("Beta", new Stats(7, 6, 7));

        var first = FightEngine.SimulateFight(g1, g2, 2026);
        var second = FightEngine.SimulateFight(g1, g2, 2026);

        Assert.Equal(first.Winner.Name, second.Winner.Name);
        Assert.Equal(first.Loser.Name, second.Loser.Name);
        Assert.Equal(first.Log.Events.Count, second.Log.Events.Count);

        for (var i = 0; i < first.Log.Events.Count; i++)
        {
            Assert.Equal(first.Log.Events[i].Round, second.Log.Events[i].Round);
            Assert.Equal(first.Log.Events[i].Type, second.Log.Events[i].Type);
            Assert.Equal(first.Log.Events[i].Value, second.Log.Events[i].Value);
            Assert.Equal(first.Log.Events[i].AttackerName, second.Log.Events[i].AttackerName);
            Assert.Equal(first.Log.Events[i].DefenderName, second.Log.Events[i].DefenderName);
        }
    }

    [Fact]
    public void SimulateFight_RegressionSnapshot_ShouldMatchExpectedOpeningSequence()
    {
        var g1 = Gladiator.Create("Alpha", new Stats(8, 7, 6));
        var g2 = Gladiator.Create("Beta", new Stats(7, 6, 7));

        var result = FightEngine.SimulateFight(g1, g2, 2026);
        var opening = result.Log.Events.Take(3).ToArray();

        Assert.Equal(3, opening.Length);
        Assert.Equal(1, opening[0].Round);
        Assert.Equal(FightLog.EventType.Hit, opening[0].Type);
        Assert.Equal(1, opening[1].Round);
        Assert.Equal(FightLog.EventType.DamageApplied, opening[1].Type);
        Assert.Equal(9f, opening[1].Value);
        Assert.Equal(2, opening[2].Round);
        Assert.Equal(FightLog.EventType.Miss, opening[2].Type);
    }
}
