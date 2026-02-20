using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

public class InjuryTests
{
    [Fact]
    public void Tick_ShouldDecreaseDays()
    {
        var injury = new Injury(InjuryType.Sprain, 3);

        var ticked = injury.Tick();

        Assert.NotNull(ticked);
        Assert.Equal(2, ticked!.Value.RecoveryDaysLeft);
    }

    [Fact]
    public void Tick_LastDay_ShouldReturnNull()
    {
        var injury = new Injury(InjuryType.Bruise, 1);

        var ticked = injury.Tick();

        Assert.Null(ticked);
    }

    [Fact]
    public void Constructor_ZeroDays_ShouldThrow()
    {
        Assert.Throws<ValidationException>(() => new Injury(InjuryType.Bruise, 0));
    }

    [Fact]
    public void Constructor_NegativeDays_ShouldThrow()
    {
        Assert.Throws<ValidationException>(() => new Injury(InjuryType.Fracture, -1));
    }
}

public class GladiatorInjuryTests
{
    [Fact]
    public void ApplyInjury_ShouldSetInjury()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));
        var injury = new Injury(InjuryType.Sprain, 3);

        var injured = gladiator.ApplyInjury(injury);

        Assert.True(injured.IsInjured);
        Assert.Equal(InjuryType.Sprain, injured.CurrentInjury!.Value.Type);
        Assert.Equal(3, injured.CurrentInjury!.Value.RecoveryDaysLeft);
    }

    [Fact]
    public void IsInjured_NoInjury_ShouldBeFalse()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));

        Assert.False(gladiator.IsInjured);
    }

    [Fact]
    public void CanFight_Healthy_ShouldBeTrue()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));

        Assert.True(gladiator.CanFight);
    }

    [Fact]
    public void CanFight_Injured_ShouldBeFalse()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Bruise, 1));

        Assert.False(gladiator.CanFight);
    }

    [Fact]
    public void CanFight_Dead_ShouldBeFalse()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(50);

        Assert.False(gladiator.CanFight);
    }

    [Fact]
    public void AssignTraining_WhenInjured_ShouldThrow()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Sprain, 3));

        Assert.Throws<InvalidOperationException>(() => gladiator.AssignTraining(TrainingType.Strength));
    }

    [Fact]
    public void TickRecovery_ShouldDecreaseDays()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Fracture, 3));

        var recovered = gladiator.TickRecovery();

        Assert.True(recovered.IsInjured);
        Assert.Equal(2, recovered.CurrentInjury!.Value.RecoveryDaysLeft);
    }

    [Fact]
    public void TickRecovery_LastDay_ShouldRemoveInjury()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Bruise, 1));

        var recovered = gladiator.TickRecovery();

        Assert.False(recovered.IsInjured);
        Assert.Null(recovered.CurrentInjury);
    }

    [Fact]
    public void TickRecovery_NoInjury_ShouldReturnSame()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));

        var result = gladiator.TickRecovery();

        Assert.Equal(gladiator, result);
    }

    [Fact]
    public void TakeDamage_ShouldPreserveInjury()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Sprain, 2));

        var damaged = gladiator.TakeDamage(10);

        Assert.True(damaged.IsInjured);
        Assert.Equal(InjuryType.Sprain, damaged.CurrentInjury!.Value.Type);
    }

    [Fact]
    public void RestoreHealth_ShouldPreserveInjury()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(20)
            .ApplyInjury(new Injury(InjuryType.Bruise, 1));

        var restored = gladiator.RestoreHealth(10);

        Assert.True(restored.IsInjured);
    }

    [Fact]
    public void ApplyInjury_DeadGladiator_ShouldThrow()
    {
        var dead = Gladiator.Create("Spartacus", new Stats(5, 5, 5)).TakeDamage(50);

        Assert.Throws<InvalidOperationException>(() =>
            dead.ApplyInjury(new Injury(InjuryType.Bruise, 1)));
    }
}

public class InjuryResolverTests
{
    [Fact]
    public void Deterministic_SameSeed_SameResult()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(25); // 50% damage
        var model = InjuryModel.Default;

        var rng1 = new SeededRng(123);
        var rng2 = new SeededRng(123);

        var result1 = InjuryResolver.ResolveInjury(gladiator, 50, true, rng1, model);
        var result2 = InjuryResolver.ResolveInjury(gladiator, 50, true, rng2, model);

        Assert.Equal(result1.IsInjured, result2.IsInjured);
        Assert.Equal(result1.CurrentInjury, result2.CurrentInjury);
    }

    [Fact]
    public void Loser_HighChance_ShouldGetInjury()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(25);
        var model = InjuryModel.Default with { BaseInjuryChanceLoser = 1.0 };

        var rng = new SeededRng(42);
        var result = InjuryResolver.ResolveInjury(gladiator, 50, false, rng, model);

        Assert.True(result.IsInjured);
    }

    [Fact]
    public void Winner_HighChance_ShouldGetInjury()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(10);
        var model = InjuryModel.Default with { BaseInjuryChanceWinner = 1.0 };

        var rng = new SeededRng(42);
        var result = InjuryResolver.ResolveInjury(gladiator, 50, true, rng, model);

        Assert.True(result.IsInjured);
    }

    [Fact]
    public void ZeroChance_ShouldNotGetInjury()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(25);
        var model = InjuryModel.Default with { BaseInjuryChanceLoser = 0.0, BaseInjuryChanceWinner = 0.0 };

        var rng = new SeededRng(42);
        var result = InjuryResolver.ResolveInjury(gladiator, 50, false, rng, model);

        Assert.False(result.IsInjured);
    }

    [Fact]
    public void HighDamage_ShouldGetFracture()
    {
        // 80% damage -> above FractureThreshold (0.70)
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(40); // 40/50 = 80% damage
        var model = InjuryModel.Default with { BaseInjuryChanceLoser = 1.0 };

        var rng = new SeededRng(42);
        var result = InjuryResolver.ResolveInjury(gladiator, 50, false, rng, model);

        Assert.True(result.IsInjured);
        Assert.Equal(InjuryType.Fracture, result.CurrentInjury!.Value.Type);
        Assert.Equal(model.FractureDays, result.CurrentInjury!.Value.RecoveryDaysLeft);
    }

    [Fact]
    public void MediumDamage_ShouldGetSprain()
    {
        // 50% damage -> above SprainThreshold (0.40), below FractureThreshold (0.70)
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(25); // 25/50 = 50% damage
        var model = InjuryModel.Default with { BaseInjuryChanceLoser = 1.0 };

        var rng = new SeededRng(42);
        var result = InjuryResolver.ResolveInjury(gladiator, 50, false, rng, model);

        Assert.True(result.IsInjured);
        Assert.Equal(InjuryType.Sprain, result.CurrentInjury!.Value.Type);
        Assert.Equal(model.SprainDays, result.CurrentInjury!.Value.RecoveryDaysLeft);
    }

    [Fact]
    public void LowDamage_ShouldGetBruise()
    {
        // 10% damage -> below SprainThreshold (0.40)
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(5); // 5/50 = 10% damage
        var model = InjuryModel.Default with { BaseInjuryChanceWinner = 1.0 };

        var rng = new SeededRng(42);
        var result = InjuryResolver.ResolveInjury(gladiator, 50, true, rng, model);

        Assert.True(result.IsInjured);
        Assert.Equal(InjuryType.Bruise, result.CurrentInjury!.Value.Type);
        Assert.Equal(model.BruiseDays, result.CurrentInjury!.Value.RecoveryDaysLeft);
    }

    [Fact]
    public void DeadGladiator_ShouldNotGetInjury()
    {
        var dead = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(50);
        var model = InjuryModel.Default with { BaseInjuryChanceLoser = 1.0 };

        var rng = new SeededRng(42);
        var result = InjuryResolver.ResolveInjury(dead, 50, false, rng, model);

        Assert.False(result.IsInjured);
    }
}

public class LudusStateInjuryTests
{
    [Fact]
    public void ResolveFight_BlocksInjuredFighter()
    {
        var g1 = Gladiator.Create("Alpha", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Sprain, 3));
        var g2 = Gladiator.Create("Beta", new Stats(5, 5, 5));
        var state = LudusState.NewGame(42).AddGladiator(g1).AddGladiator(g2);

        Assert.Throws<ValidationException>(() => state.ResolveFight(g1.Id, g2.Id));
    }

    [Fact]
    public void ResolveFight_ClearsTrainingOnInjury()
    {
        // Use a seed and model that guarantees injury for loser
        var g1 = Gladiator.Create("Alpha", new Stats(8, 8, 8));
        var g2 = Gladiator.Create("Beta", new Stats(3, 3, 3));
        var state = LudusState.NewGame(42)
            .AddGladiator(g1)
            .AddGladiator(g2)
            .AssignTraining(g1.Id, TrainingType.Strength)
            .AssignTraining(g2.Id, TrainingType.Agility);

        var (newState, _) = state.ResolveFight(g1.Id, g2.Id);

        // Check that injured gladiators have no training
        foreach (var g in newState.Gladiators)
        {
            if (g.IsInjured)
            {
                Assert.Null(g.CurrentTraining);
            }
        }
    }

    [Fact]
    public void AdvanceDay_DecreasesRecoveryDays()
    {
        var g = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Sprain, 3));
        var state = LudusState.NewGame(42).AddGladiator(g);

        var newState = state.AdvanceDay();

        var updated = newState.GetGladiator(g.Id);
        Assert.True(updated.IsInjured);
        Assert.Equal(2, updated.CurrentInjury!.Value.RecoveryDaysLeft);
    }

    [Fact]
    public void AdvanceDay_RemovesInjuryWhenExpired()
    {
        var g = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Bruise, 1));
        var state = LudusState.NewGame(42).AddGladiator(g);

        var newState = state.AdvanceDay();

        var updated = newState.GetGladiator(g.Id);
        Assert.False(updated.IsInjured);
    }

    [Fact]
    public void AdvanceDay_RestoresHP()
    {
        var g = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(30); // HP: 20/50
        var state = LudusState.NewGame(42).AddGladiator(g);

        var newState = state.AdvanceDay();

        var updated = newState.GetGladiator(g.Id);
        // MaxHealth * 0.1 = 5, so HP should be 25
        Assert.Equal(25, updated.Health);
    }

    [Fact]
    public void AdvanceDay_HPDoesNotExceedMax()
    {
        var g = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .TakeDamage(2); // HP: 48/50
        var state = LudusState.NewGame(42).AddGladiator(g);

        var newState = state.AdvanceDay();

        var updated = newState.GetGladiator(g.Id);
        Assert.Equal(50, updated.Health);
    }

    [Fact]
    public void AdvanceDay_Determinism_SameSeed_SameInjuries()
    {
        var g1 = Gladiator.Create("Alpha", new Stats(5, 5, 5))
            .ApplyInjury(new Injury(InjuryType.Fracture, 5));
        var g2 = new Gladiator(g1.Id, g1.Name, g1.Stats, g1.Health, g1.MaxHealth,
            g1.CurrentTraining, g1.CurrentInjury);

        var state1 = LudusState.NewGame(42).AddGladiator(g1);
        var state2 = LudusState.NewGame(42).AddGladiator(g2);

        var result1 = state1.AdvanceDay();
        var result2 = state2.AdvanceDay();

        var updated1 = result1.GetGladiator(g1.Id);
        var updated2 = result2.GetGladiator(g2.Id);

        Assert.Equal(updated1.CurrentInjury, updated2.CurrentInjury);
        Assert.Equal(updated1.Health, updated2.Health);
        Assert.Equal(result1.Seed, result2.Seed);
    }
}

public class InjuryModelTests
{
    [Fact]
    public void Default_ShouldHaveValidValues()
    {
        var model = InjuryModel.Default;
        model.Validate();
    }

    [Fact]
    public void Invalid_NegativeChance_ShouldThrow()
    {
        var model = InjuryModel.Default with { BaseInjuryChanceLoser = -0.1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => model.Validate());
    }

    [Fact]
    public void Invalid_ThresholdOrder_ShouldThrow()
    {
        var model = InjuryModel.Default with { SprainThreshold = 0.80, FractureThreshold = 0.70 };
        Assert.Throws<ArgumentException>(() => model.Validate());
    }
}
