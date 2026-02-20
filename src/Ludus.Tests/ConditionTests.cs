using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

public class ConditionModelTests
{
    [Fact]
    public void Default_ShouldValidate()
    {
        var model = ConditionModel.Default;
        model.Validate(); // should not throw
    }

    [Fact]
    public void InvalidMoraleWinBonus_ShouldThrow()
    {
        var model = ConditionModel.Default with { MoraleWinBonus = -1 };
        Assert.Throws<System.ArgumentOutOfRangeException>(() => model.Validate());
    }

    [Fact]
    public void InvalidMoraleLosePenalty_ShouldThrow()
    {
        var model = ConditionModel.Default with { MoraleLosePenalty = 1 };
        Assert.Throws<System.ArgumentOutOfRangeException>(() => model.Validate());
    }

    [Fact]
    public void InvalidFatigueFightGain_ShouldThrow()
    {
        var model = ConditionModel.Default with { FatigueFightGain = -1 };
        Assert.Throws<System.ArgumentOutOfRangeException>(() => model.Validate());
    }

    [Fact]
    public void InvalidHighMoraleBonus_ShouldThrow()
    {
        var model = ConditionModel.Default with { HighMoraleBonus = -0.1 };
        Assert.Throws<System.ArgumentOutOfRangeException>(() => model.Validate());
    }

    [Fact]
    public void InvalidLowMoralePenalty_ShouldThrow()
    {
        var model = ConditionModel.Default with { LowMoralePenalty = 0.1 };
        Assert.Throws<System.ArgumentOutOfRangeException>(() => model.Validate());
    }
}

public class ConditionResolverEfficiencyTests
{
    private readonly ConditionModel _model = ConditionModel.Default;

    [Fact]
    public void HighMorale_LowFatigue_ShouldGiveBonus()
    {
        var efficiency = ConditionResolver.GetEfficiency(100, 0, _model);
        // 1.0 + 0.15 + 0.05 = 1.20
        Assert.Equal(1.20, efficiency, 2);
    }

    [Fact]
    public void ZeroMorale_MaxFatigue_ShouldGivePenalty()
    {
        var efficiency = ConditionResolver.GetEfficiency(0, 100, _model);
        // 1.0 + (-0.25) + (-0.25) = 0.50
        Assert.Equal(0.50, efficiency, 2);
    }

    [Fact]
    public void DefaultValues_ShouldBeNearOne()
    {
        var efficiency = ConditionResolver.GetEfficiency(
            ConditionModel.DefaultMorale, ConditionModel.DefaultFatigue, _model);
        // morale=50: -0.25 + 0.5*(0.15-(-0.25)) = -0.25+0.20 = -0.05
        // fatigue=0: 0.05
        // total: 1.0 + (-0.05) + 0.05 = 1.0
        Assert.Equal(1.0, efficiency, 2);
    }

    [Fact]
    public void MidValues_ShouldInterpolate()
    {
        var efficiency = ConditionResolver.GetEfficiency(50, 50, _model);
        // morale=50: -0.25 + 0.5*0.40 = -0.05
        // fatigue=50: 0.05 + 0.5*(-0.30) = -0.10
        // total: 1.0 + (-0.05) + (-0.10) = 0.85
        Assert.Equal(0.85, efficiency, 2);
    }
}

public class ConditionResolverFightTests
{
    private readonly ConditionModel _model = ConditionModel.Default;

    [Fact]
    public void Winner_ShouldGainMorale()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(50);
        var result = ConditionResolver.ApplyFightOutcome(g, true, _model);
        Assert.Equal(65, result.Morale); // 50 + 15
    }

    [Fact]
    public void Loser_ShouldLoseMorale()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(50);
        var result = ConditionResolver.ApplyFightOutcome(g, false, _model);
        Assert.Equal(30, result.Morale); // 50 - 20
    }

    [Fact]
    public void Both_ShouldGainFatigue()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithFatigue(0);
        var winner = ConditionResolver.ApplyFightOutcome(g, true, _model);
        var loser = ConditionResolver.ApplyFightOutcome(g, false, _model);
        Assert.Equal(30, winner.Fatigue); // 0 + 30
        Assert.Equal(30, loser.Fatigue);
    }

    [Fact]
    public void Morale_ShouldClampAt100()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(95);
        var result = ConditionResolver.ApplyFightOutcome(g, true, _model);
        Assert.Equal(100, result.Morale);
    }

    [Fact]
    public void Morale_ShouldClampAt0()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(5);
        var result = ConditionResolver.ApplyFightOutcome(g, false, _model);
        Assert.Equal(0, result.Morale);
    }

    [Fact]
    public void Fatigue_ShouldClampAt100()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithFatigue(90);
        var result = ConditionResolver.ApplyFightOutcome(g, true, _model);
        Assert.Equal(100, result.Fatigue);
    }
}

public class ConditionResolverDailyTests
{
    private readonly ConditionModel _model = ConditionModel.Default;

    [Fact]
    public void Idle_ShouldRecoverFatigue()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithFatigue(50);
        var result = ConditionResolver.ApplyDailyTick(g, _model);
        Assert.Equal(35, result.Fatigue); // 50 - 15
    }

    [Fact]
    public void Idle_ShouldGainMorale()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(50);
        var result = ConditionResolver.ApplyDailyTick(g, _model);
        Assert.Equal(55, result.Morale); // 50 + 5
    }

    [Fact]
    public void Training_ShouldGainFatigue()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5))
            .AssignTraining(TrainingType.Strength)
            .WithFatigue(0);
        var result = ConditionResolver.ApplyDailyTick(g, _model);
        Assert.Equal(5, result.Fatigue); // 0 + 10 - 5 = 5
    }

    [Fact]
    public void Training_ShouldDrainMorale()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5))
            .AssignTraining(TrainingType.Strength)
            .WithMorale(50);
        var result = ConditionResolver.ApplyDailyTick(g, _model);
        Assert.Equal(47, result.Morale); // 50 - 3
    }

    [Fact]
    public void Injured_ShouldBeIdleRecovery()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Sprain, 3))
            .WithFatigue(50).WithMorale(50);
        var result = ConditionResolver.ApplyDailyTick(g, _model);
        Assert.Equal(35, result.Fatigue); // idle recovery
        Assert.Equal(55, result.Morale);  // idle bonus
    }

    [Fact]
    public void Fatigue_ShouldNotGoBelowZero()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithFatigue(5);
        var result = ConditionResolver.ApplyDailyTick(g, _model);
        Assert.Equal(0, result.Fatigue);
    }

    [Fact]
    public void Morale_ShouldNotExceed100()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(98);
        var result = ConditionResolver.ApplyDailyTick(g, _model);
        Assert.Equal(100, result.Morale);
    }
}

public class ConditionResolverInjuryTests
{
    private readonly ConditionModel _model = ConditionModel.Default;

    [Fact]
    public void Injury_ShouldReduceMorale()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(50);
        var result = ConditionResolver.ApplyInjuryMoralePenalty(g, _model);
        Assert.Equal(40, result.Morale); // 50 - 10
    }

    [Fact]
    public void Injury_ShouldNotGoBelowZero()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(5);
        var result = ConditionResolver.ApplyInjuryMoralePenalty(g, _model);
        Assert.Equal(0, result.Morale);
    }
}

public class GladiatorConditionTests
{
    [Fact]
    public void Create_ShouldHaveDefaultMoraleAndFatigue()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5));
        Assert.Equal(ConditionModel.DefaultMorale, g.Morale);
        Assert.Equal(ConditionModel.DefaultFatigue, g.Fatigue);
    }

    [Fact]
    public void WithMorale_ShouldClampHigh()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5));
        var result = g.WithMorale(150);
        Assert.Equal(100, result.Morale);
    }

    [Fact]
    public void WithMorale_ShouldClampLow()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5));
        var result = g.WithMorale(-10);
        Assert.Equal(0, result.Morale);
    }

    [Fact]
    public void WithFatigue_ShouldClampHigh()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5));
        var result = g.WithFatigue(150);
        Assert.Equal(100, result.Fatigue);
    }

    [Fact]
    public void WithFatigue_ShouldClampLow()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5));
        var result = g.WithFatigue(-10);
        Assert.Equal(0, result.Fatigue);
    }

    [Fact]
    public void TakeDamage_ShouldPreserveMoraleAndFatigue()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(80).WithFatigue(30);
        var damaged = g.TakeDamage(10);
        Assert.Equal(80, damaged.Morale);
        Assert.Equal(30, damaged.Fatigue);
    }

    [Fact]
    public void RestoreHealth_ShouldPreserveMoraleAndFatigue()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithMorale(80).WithFatigue(30);
        var damaged = g.TakeDamage(10);
        var restored = damaged.RestoreHealth(5);
        Assert.Equal(80, restored.Morale);
        Assert.Equal(30, restored.Fatigue);
    }

    [Fact]
    public void InvalidMorale_ShouldThrow()
    {
        Assert.Throws<ValidationException>(() =>
            new Gladiator(System.Guid.NewGuid(), "Test", new Stats(5, 5, 5), 50, 50,
                morale: 101));
    }

    [Fact]
    public void InvalidFatigue_ShouldThrow()
    {
        Assert.Throws<ValidationException>(() =>
            new Gladiator(System.Guid.NewGuid(), "Test", new Stats(5, 5, 5), 50, 50,
                fatigue: -1));
    }
}

public class LudusStateConditionTests
{
    [Fact]
    public void AdvanceDay_IdleGladiator_ShouldChangeMoraleAndFatigue()
    {
        var state = LudusState.NewGame(42);
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithFatigue(50).WithMorale(50);
        state = state.AddGladiator(g);

        var newState = state.AdvanceDay();
        var updated = newState.Gladiators[0];

        Assert.Equal(55, updated.Morale);  // idle: +5
        Assert.Equal(35, updated.Fatigue); // idle: -15
    }

    [Fact]
    public void AdvanceDay_TrainingGladiator_ShouldDrainMoraleAndGainFatigue()
    {
        var state = LudusState.NewGame(42);
        var g = Gladiator.Create("Test", new Stats(5, 5, 5)).WithFatigue(20).WithMorale(50);
        state = state.AddGladiator(g);
        state = state.AssignTraining(g.Id, TrainingType.Strength);

        var newState = state.AdvanceDay();
        var updated = newState.Gladiators[0];

        Assert.Equal(47, updated.Morale);  // training: -3
        Assert.Equal(25, updated.Fatigue); // training: +10-5 = +5
    }

    [Fact]
    public void ResolveFight_ShouldApplyConditionEffects()
    {
        var state = LudusState.NewGame(42);
        var g1 = Gladiator.Create("Fighter1", new Stats(8, 5, 8)).WithMorale(50).WithFatigue(0);
        var g2 = Gladiator.Create("Fighter2", new Stats(5, 5, 8)).WithMorale(50).WithFatigue(0);
        state = state.AddGladiator(g1).AddGladiator(g2);

        var (newState, result) = state.ResolveFight(g1.Id, g2.Id);

        var winner = newState.Gladiators.First(g => g.Id == result.Winner.Id);
        var loser = newState.Gladiators.First(g => g.Id == result.Loser.Id);

        // Winner should have increased morale and gained fatigue
        Assert.True(winner.Morale >= 50); // at least original + win bonus (may have injury penalty)
        Assert.True(winner.Fatigue > 0);  // gained fight fatigue

        // Loser (if alive) should have decreased morale
        if (loser.IsAlive)
        {
            Assert.True(loser.Morale < 50); // lost morale from loss (possibly more from injury)
        }
        Assert.True(loser.Fatigue > 0); // gained fight fatigue
    }
}
