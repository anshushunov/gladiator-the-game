using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

public class GladiatorTrainingTests
{
    [Fact]
    public void AssignTraining_ShouldSetCurrentTraining()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));

        var trained = gladiator.AssignTraining(TrainingType.Strength);

        Assert.Equal(TrainingType.Strength, trained.CurrentTraining);
    }

    [Fact]
    public void AssignTraining_DeadGladiator_ShouldThrow()
    {
        var dead = Gladiator.Create("Spartacus", new Stats(5, 5, 5)).TakeDamage(100);

        Assert.Throws<InvalidOperationException>(() => dead.AssignTraining(TrainingType.Strength));
    }

    [Fact]
    public void AssignTraining_StatAlreadyMax_ShouldThrow()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(10, 5, 5));

        Assert.Throws<InvalidOperationException>(() => gladiator.AssignTraining(TrainingType.Strength));
    }

    [Fact]
    public void ClearTraining_ShouldRemoveTraining()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .AssignTraining(TrainingType.Agility);

        var cleared = gladiator.ClearTraining();

        Assert.Null(cleared.CurrentTraining);
    }

    [Fact]
    public void Create_ShouldHaveNoTraining()
    {
        var gladiator = Gladiator.Create("Spartacus", Stats.Default);

        Assert.Null(gladiator.CurrentTraining);
    }

    [Fact]
    public void TakeDamage_ShouldPreserveTraining()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .AssignTraining(TrainingType.Strength);

        var damaged = gladiator.TakeDamage(10);

        Assert.Equal(TrainingType.Strength, damaged.CurrentTraining);
    }

    [Fact]
    public void RestoreHealth_ShouldPreserveTraining()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .AssignTraining(TrainingType.Agility)
            .TakeDamage(20);

        var restored = gladiator.RestoreHealth(10);

        Assert.Equal(TrainingType.Agility, restored.CurrentTraining);
    }

    [Fact]
    public void ApplyStatGain_Strength_ShouldIncrement()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .AssignTraining(TrainingType.Strength);

        var gained = gladiator.ApplyStatGain(TrainingType.Strength);

        Assert.Equal(6, gained.Stats.Strength);
        Assert.Equal(5, gained.Stats.Agility);
        Assert.Equal(5, gained.Stats.Stamina);
    }

    [Fact]
    public void ApplyStatGain_Stamina_ShouldUpdateMaxHealthAndHealth()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5))
            .AssignTraining(TrainingType.Stamina);

        var gained = gladiator.ApplyStatGain(TrainingType.Stamina);

        Assert.Equal(6, gained.Stats.Stamina);
        Assert.Equal(60, gained.MaxHealth);
        Assert.Equal(60, gained.Health);
    }

    [Fact]
    public void ApplyStatGain_StatReachesMax_ShouldAutoClearTraining()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(9, 5, 5))
            .AssignTraining(TrainingType.Strength);

        var gained = gladiator.ApplyStatGain(TrainingType.Strength);

        Assert.Equal(10, gained.Stats.Strength);
        Assert.Null(gained.CurrentTraining);
    }
}

public class LudusStateTrainingTests
{
    [Fact]
    public void AssignTraining_ShouldUpdateGladiator()
    {
        var state = LudusState.NewGame(42);
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));
        state = state.AddGladiator(gladiator);

        var newState = state.AssignTraining(gladiator.Id, TrainingType.Strength);

        Assert.Equal(TrainingType.Strength, newState.GetGladiator(gladiator.Id).CurrentTraining);
    }

    [Fact]
    public void AssignTraining_NonExistentGladiator_ShouldThrow()
    {
        var state = LudusState.NewGame(42);

        Assert.Throws<ValidationException>(() => state.AssignTraining(Guid.NewGuid(), TrainingType.Strength));
    }

    [Fact]
    public void ClearTraining_ShouldRemoveAssignment()
    {
        var state = LudusState.NewGame(42);
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));
        state = state.AddGladiator(gladiator);
        state = state.AssignTraining(gladiator.Id, TrainingType.Strength);

        var newState = state.ClearTraining(gladiator.Id);

        Assert.Null(newState.GetGladiator(gladiator.Id).CurrentTraining);
    }

    [Fact]
    public void AdvanceDay_WithTraining_Deterministic()
    {
        // Создаём два идентичных состояния с одинаковым seed
        var g1 = Gladiator.Create("Spartacus", new Stats(5, 5, 5));
        var g2 = new Gladiator(g1.Id, g1.Name, g1.Stats, g1.Health, g1.MaxHealth);

        var state1 = LudusState.NewGame(42).AddGladiator(g1).AssignTraining(g1.Id, TrainingType.Strength);
        var state2 = LudusState.NewGame(42).AddGladiator(g2).AssignTraining(g2.Id, TrainingType.Strength);

        var result1 = state1.AdvanceDay();
        var result2 = state2.AdvanceDay();

        Assert.Equal(result1.GetGladiator(g1.Id).Stats.Strength,
                     result2.GetGladiator(g2.Id).Stats.Strength);
        Assert.Equal(result1.Seed, result2.Seed);
    }

    [Fact]
    public void AdvanceDay_StatAtMax_ShouldClearTraining()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(9, 5, 5));
        var state = LudusState.NewGame(42).AddGladiator(gladiator)
            .AssignTraining(gladiator.Id, TrainingType.Strength);

        // Используем 100% шанс, чтобы гарантировать прирост
        var model = new TrainingModel(StatGainChance: 1.0);
        var newState = state.AdvanceDay(model);

        var updated = newState.GetGladiator(gladiator.Id);
        Assert.Equal(10, updated.Stats.Strength);
        Assert.Null(updated.CurrentTraining);
    }

    [Fact]
    public void AdvanceDay_WithoutTraining_ShouldNotChangeStats()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));
        var state = LudusState.NewGame(42).AddGladiator(gladiator);

        var newState = state.AdvanceDay();

        var updated = newState.GetGladiator(gladiator.Id);
        Assert.Equal(5, updated.Stats.Strength);
        Assert.Equal(5, updated.Stats.Agility);
        Assert.Equal(5, updated.Stats.Stamina);
    }

    [Fact]
    public void AdvanceDay_DeadGladiator_ShouldNotGainStats()
    {
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));
        var dead = gladiator.TakeDamage(100);
        // Мёртвый гладиатор не может получить тренировку через AssignTraining,
        // поэтому создаём его напрямую с тренировкой через конструктор
        var deadWithTraining = new Gladiator(dead.Id, dead.Name, dead.Stats, 0, dead.MaxHealth, TrainingType.Strength);
        var state = LudusState.NewGame(42).AddGladiator(deadWithTraining);

        var model = new TrainingModel(StatGainChance: 1.0);
        var newState = state.AdvanceDay(model);

        var updated = newState.GetGladiator(deadWithTraining.Id);
        Assert.Equal(5, updated.Stats.Strength);
    }

    [Fact]
    public void AdvanceDay_ShouldAdvanceSeed()
    {
        var state = LudusState.NewGame(42);
        var gladiator = Gladiator.Create("Spartacus", new Stats(5, 5, 5));
        state = state.AddGladiator(gladiator);

        var newState = state.AdvanceDay();

        Assert.NotEqual(state.Seed, newState.Seed);
    }

    [Fact]
    public void AdvanceDay_MultipleGladiators_IndependentOutcomes()
    {
        var g1 = Gladiator.Create("Alpha", new Stats(5, 5, 5));
        var g2 = Gladiator.Create("Beta", new Stats(5, 5, 5));
        var state = LudusState.NewGame(42)
            .AddGladiator(g1)
            .AddGladiator(g2)
            .AssignTraining(g1.Id, TrainingType.Strength)
            .AssignTraining(g2.Id, TrainingType.Agility);

        // 100% шанс — оба должны получить прирост
        var model = new TrainingModel(StatGainChance: 1.0);
        var newState = state.AdvanceDay(model);

        var updatedG1 = newState.GetGladiator(g1.Id);
        var updatedG2 = newState.GetGladiator(g2.Id);

        Assert.Equal(6, updatedG1.Stats.Strength);
        Assert.Equal(6, updatedG2.Stats.Agility);
    }
}

public class TrainingModelTests
{
    [Fact]
    public void Default_ShouldHaveValidValues()
    {
        var model = TrainingModel.Default;

        Assert.Equal(0.50, model.StatGainChance);
        model.Validate(); // не должен бросить исключение
    }

    [Fact]
    public void InvalidChance_ShouldThrow()
    {
        var tooHigh = new TrainingModel(StatGainChance: 1.5);
        Assert.Throws<ValidationException>(() => tooHigh.Validate());

        var tooLow = new TrainingModel(StatGainChance: -0.1);
        Assert.Throws<ValidationException>(() => tooLow.Validate());
    }
}
