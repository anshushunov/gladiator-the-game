using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

public class StatsTests
{
    [Fact]
    public void Stats_Creation_WithValidValues_ShouldSucceed()
    {
        // arrange & act
        var stats = new Stats(5, 7, 9);

        // assert
        Assert.Equal(5, stats.Strength);
        Assert.Equal(7, stats.Agility);
        Assert.Equal(9, stats.Stamina);
    }

    [Fact]
    public void Stats_Creation_WithInvalidStrength_ShouldThrow()
    {
        // arrange
        const int invalidStrength = 0;

        // assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Stats(invalidStrength, 5, 5));
        Assert.Contains("strength", ex.Message.ToLower());
    }

    [Fact]
    public void Stats_Creation_WithInvalidAgility_ShouldThrow()
    {
        // arrange
        const int invalidAgility = 11;

        // assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Stats(5, invalidAgility, 5));
        Assert.Contains("agility", ex.Message.ToLower());
    }

    [Fact]
    public void Stats_Creation_WithInvalidStamina_ShouldThrow()
    {
        // arrange
        const int invalidStamina = -1;

        // assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Stats(5, 5, invalidStamina));
        Assert.Contains("stamina", ex.Message.ToLower());
    }

    [Fact]
    public void Stats_Default_ShouldHaveMiddleValues()
    {
        // act
        var stats = Stats.Default;

        // assert
        Assert.Equal(5, stats.Strength);
        Assert.Equal(5, stats.Agility);
        Assert.Equal(5, stats.Stamina);
    }

    [Fact]
    public void Stats_Random_ShouldGenerateValidValues()
    {
        // act
        var rand = new System.Random(42);
        var stats = Stats.Random(rand);

        // assert
        Assert.InRange(stats.Strength, 1, 10);
        Assert.InRange(stats.Agility, 1, 10);
        Assert.InRange(stats.Stamina, 1, 10);
    }
}

public class GladiatorTests
{
    [Fact]
    public void Gladiator_Create_WithValidStats_ShouldSucceed()
    {
        // arrange
        var stats = Stats.Default;

        // act
        var gladiator = Gladiator.Create("Spartacus", stats);

        // assert
        Assert.NotEqual(Guid.Empty, gladiator.Id);
        Assert.Equal("Spartacus", gladiator.Name);
        Assert.Equal(stats, gladiator.Stats);
        Assert.Equal(50, gladiator.MaxHealth);
        Assert.Equal(50, gladiator.Health);
        Assert.True(gladiator.IsAlive);
    }

    [Fact]
    public void Gladiator_TakeDamage_ShouldReduceHealth()
    {
        // arrange
        var stats = Stats.Default;
        var gladiator = Gladiator.Create("Spartacus", stats);
        int damage = 10;

        // act
        var damaged = gladiator.TakeDamage(damage);

        // assert
        Assert.Equal(40, damaged.Health);
        Assert.True(damaged.IsAlive);
        Assert.Equal(50, gladiator.Health); // original unchanged
    }

    [Fact]
    public void Gladiator_TakeDamage_ExceedingHealth_ShouldSetHealthToZero()
    {
        // arrange
        var stats = Stats.Default;
        var gladiator = Gladiator.Create("Spartacus", stats);
        int damage = 100;

        // act
        var damaged = gladiator.TakeDamage(damage);

        // assert
        Assert.Equal(0, damaged.Health);
        Assert.False(damaged.IsAlive);
    }

    [Fact]
    public void Gladiator_TakeDamage_ForDeadGladiator_ShouldThrow()
    {
        // arrange
        var stats = Stats.Default;
        var dead = Gladiator.Create("Spartacus", stats).TakeDamage(100);

        // assert
        Assert.Throws<InvalidOperationException>(() => dead.TakeDamage(10));
    }

    [Fact]
    public void Gladiator_RestoreHealth_ShouldIncreaseHealth()
    {
        // arrange
        var stats = Stats.Default;
        var damaged = Gladiator.Create("Spartacus", stats).TakeDamage(20);

        // act
        var restored = damaged.RestoreHealth(10);

        // assert
        Assert.Equal(40, restored.Health);
        Assert.True(restored.IsAlive);
    }

    [Fact]
    public void Gladiator_RestoreHealth_ExceedingMax_ShouldCapAtMax()
    {
        // arrange
        var stats = Stats.Default;
        var damaged = Gladiator.Create("Spartacus", stats).TakeDamage(20);

        // act
        var restored = damaged.RestoreHealth(100);

        // assert
        Assert.Equal(50, restored.Health);
    }

    [Fact]
    public void Gladiator_NameWithWhitespace_ShouldBeTrimmed()
    {
        // act
        var gladiator = Gladiator.Create("  Test  ", Stats.Default);

        // assert
        Assert.Equal("Test", gladiator.Name);
    }

    [Fact]
    public void Gladiator_EmptyName_ShouldThrow()
    {
        // assert
        Assert.Throws<ValidationException>(() => Gladiator.Create("", Stats.Default));
    }

    [Fact]
    public void Gladiator_TakeNullDamage_ShouldThrow()
    {
        // arrange
        var gladiator = Gladiator.Create("Spartacus", Stats.Default);

        // assert
        Assert.Throws<ArgumentException>(() => gladiator.TakeDamage(-5));
    }
}

public class LudusStateTests
{
    [Fact]
    public void LudusState_Empty_ShouldHaveNoGladiators()
    {
        // act
        var state = LudusState.Empty;

        // assert
        Assert.Empty(state.Gladiators);
        Assert.Null(state.ActiveGladiatorId);
    }

    [Fact]
    public void LudusState_AddGladiator_ShouldAddSuccessfully()
    {
        // arrange
        var state = LudusState.Empty;
        var gladiator = Gladiator.Create("Spartacus", Stats.Default);

        // act
        var newState = state.AddGladiator(gladiator);

        // assert
        Assert.Equal(1, newState.Count);
        Assert.Equal(gladiator.Id, newState.Gladiators[0].Id);
        Assert.Equal(gladiator.Id, newState.ActiveGladiatorId);
    }

    [Fact]
    public void LudusState_AddGladiator_WithSameId_ShouldThrow()
    {
        // arrange
        var state = LudusState.Empty;
        var gladiator = Gladiator.Create("Spartacus", Stats.Default);
        state = state.AddGladiator(gladiator);

        // assert
        var ex = Assert.Throws<ValidationException>(() => state.AddGladiator(gladiator));
        Assert.Contains(gladiator.Id.ToString(), ex.Message);
    }

    [Fact]
    public void LudusState_AddGladiator_EmptyId_ShouldThrow()
    {
        // arrange
        var state = LudusState.Empty;
        var gladiator = new Gladiator(Guid.Empty, "Test", new Stats(5, 5, 5), 50, 50);

        // assert
        var ex = Assert.Throws<ValidationException>(() => state.AddGladiator(gladiator));
        Assert.Equal("ID гладиатора не может быть пустым", ex.Message);
    }

    [Fact]
    public void LudusState_RemoveGladiator_ShouldRemoveSuccessfully()
    {
        // arrange
        var state = LudusState.Empty;
        var gladiator = Gladiator.Create("Spartacus", Stats.Default);
        state = state.AddGladiator(gladiator);

        // act
        var newState = state.RemoveGladiator(gladiator.Id);

        // assert
        Assert.Empty(newState.Gladiators);
        Assert.Null(newState.ActiveGladiatorId);
    }

    [Fact]
    public void LudusState_RemoveNonExistentGladiator_ShouldThrow()
    {
        // arrange
        var state = LudusState.Empty;
        var nonExistentId = Guid.NewGuid();

        // assert
        Assert.Throws<ValidationException>(() => state.RemoveGladiator(nonExistentId));
    }

    [Fact]
    public void LudusState_SetActiveGladiator_ShouldSetSuccessfully()
    {
        // arrange
        var state = LudusState.Empty;
        var gladiator = Gladiator.Create("Spartacus", Stats.Default);
        state = state.AddGladiator(gladiator);
        var another = Gladiator.Create("Crixus", Stats.Default);
        state = state.AddGladiator(another);

        // act
        var newState = state.SetActiveGladiator(another.Id);

        // assert
        Assert.Equal(another.Id, newState.ActiveGladiatorId);
    }

    [Fact]
    public void LudusState_GetAliveGladiators_ShouldFilterDead()
    {
        // arrange
        var state = LudusState.Empty;
        var alive = Gladiator.Create("Alive", Stats.Default);
        var dead = Gladiator.Create("Dead", Stats.Default).TakeDamage(100);
        state = state.AddGladiator(alive);
        state = state.AddGladiator(dead);

        // act
        var aliveList = state.AliveGladiators;

        // assert
        Assert_EQ(1, aliveList.Count);
        Assert.Equal(alive.Id, aliveList[0].Id);
    }

    [Fact]
    public void HasAliveGladiators_ShouldDetectAlive()
    {
        // arrange
        var state = LudusState.Empty;
        var dead = Gladiator.Create("Dead", Stats.Default).TakeDamage(100);
        state = state.AddGladiator(dead);

        // act & assert
        Assert.False(state.HasAliveGladiators);
    }

    [Fact]
    public void HasAliveGladiators_WithAliveOne_ShouldReturnTrue()
    {
        // arrange
        var state = LudusState.Empty;
        var alive = Gladiator.Create("Alive", Stats.Default);
        state = state.AddGladiator(alive);

        // act & assert
        Assert.True(state.HasAliveGladiators);
    }

    [Fact]
    public void GetGladiator_Existing_ShouldReturnGladiator()
    {
        // arrange
        var state = LudusState.Empty;
        var gladiator = Gladiator.Create("Spartacus", Stats.Default);
        state = state.AddGladiator(gladiator);

        // act
        var found = state.GetGladiator(gladiator.Id);

        // assert
        Assert.Equal(gladiator.Id, found.Id);
        Assert.Equal(gladiator.Name, found.Name);
    }

    [Fact]
    public void GetGladiator_NonExisting_ShouldThrow()
    {
        // arrange
        var state = LudusState.Empty;
        var nonExistentId = Guid.NewGuid();

        // assert
        var ex = Assert.Throws<ValidationException>(() => state.GetGladiator(nonExistentId));
        Assert.Contains(nonExistentId.ToString(), ex.Message);
    }

    // Mock helper
    private static void Assert_EQ(int expected, int actual)
    {
        Assert.Equal(expected, actual);
    }
}
