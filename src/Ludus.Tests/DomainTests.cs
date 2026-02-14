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

public class FightEngineTests
{
    [Fact]
    public void SimulateFight_Determinism_SameSeed_ShouldProduceSameResult()
    {
        // arrange
        var seed = 42;
        var g1 = Gladiator.Create("Strong", new Stats(8, 5, 6));
        var g2 = Gladiator.Create("Weak", new Stats(5, 5, 5));

        // act
        var result1 = FightEngine.SimulateFight(g1, g2, seed);
        var result2 = FightEngine.SimulateFight(g1, g2, seed);

        // assert
        Assert.Equal(result1.Winner.Name, result2.Winner.Name);
        Assert.Equal(result1.Loser.Name, result2.Loser.Name);
        Assert.Equal(result1.Log.Events.Count, result2.Log.Events.Count);
        for (int i = 0; i < result1.Log.Events.Count; i++)
        {
            var e1 = result1.Log.Events[i];
            var e2 = result2.Log.Events[i];
            Assert.Equal(e1.Round, e2.Round);
            Assert.Equal(e1.Type, e2.Type);
            Assert.Equal(e1.Value, e2.Value);
            Assert.Equal(e1.AttackerName, e2.AttackerName);
        }
    }

    [Fact]
    public void SimulateFight_StrongerShouldWin()
    {
        // arrange
        var seed = 123;
        var strong = Gladiator.Create("Strong", new Stats(9, 5, 9));
        var weak = Gladiator.Create("Weak", new Stats(3, 5, 3));

        // act
        var result = FightEngine.SimulateFight(strong, weak, seed);

        // assert
        Assert.Equal("Strong", result.Winner.Name);
        Assert.Equal("Weak", result.Loser.Name);
        Assert.False(result.Loser.IsAlive);
        Assert.True(result.Winner.IsAlive);
    }

    [Fact]
    public void SimulateFight_BothEqualStrength_TakesRandomWinner()
    {
        // arrange
        var strong = Gladiator.Create("G1", new Stats(7, 7, 7));
        var weak = Gladiator.Create("G2", new Stats(7, 7, 7));

        // act
        var result1 = FightEngine.SimulateFight(strong, weak, 42);
        var result2 = FightEngine.SimulateFight(strong, weak, 42);

        // assert — с одним seed всегда один результат (детерминизм)
        Assert.Equal(result1.Winner.Name, result2.Winner.Name);
    }

    [Fact]
    public void SimulateFight_LogContainsEvents()
    {
        // arrange
        var g1 = Gladiator.Create("G1", new Stats(8, 5, 6));
        var g2 = Gladiator.Create("G2", new Stats(5, 5, 5));

        // act
        var result = FightEngine.SimulateFight(g1, g2, 42);

        // assert
        Assert.True(result.Log.Events.Count > 0);
        Assert.Contains(result.Log.Events, e => e.Type == FightLog.EventType.Hit);
        Assert.Contains(result.Log.Events, e => e.Type == FightLog.EventType.FightEnd);
    }

    [Fact]
    public void SimulateFight_LogToString_ShouldIncludeFightDetails()
    {
        // arrange
        var g1 = Gladiator.Create("G1", new Stats(8, 5, 6));
        var g2 = Gladiator.Create("G2", new Stats(5, 5, 5));

        // act
        var result = FightEngine.SimulateFight(g1, g2, 42);

        // assert
        string logText = result.Log.ToString();
        Assert.Contains("Fight Log", logText);
        Assert.Contains("G1", logText);
        Assert.Contains("G2", logText);
    }

    [Fact]
    public void SimulateFight_FirstAttackerAdvantage()
    {
        // arrange — разница в силе значительная
        var strong = Gladiator.Create("Strong", new Stats(10, 5, 10));
        var weak = Gladiator.Create("Weak", new Stats(3, 3, 3));

        // act
        var result = FightEngine.SimulateFight(strong, weak, 42);

        // assert
        Assert.Equal("Strong", result.Winner.Name);
        Assert.Equal("Weak", result.Loser.Name);
    }

    [Fact]
    public void SimulateFight_WithAgility_DefenderMightMiss()
    {
        // arrange — защитник ловкий, атакующий слабый
        var weakAttacker = Gladiator.Create("WeakAtk", new Stats(3, 8, 6));
        var strongDefender = Gladiator.Create("StrongDef", new Stats(5, 10, 10));

        // act
        var result = FightEngine.SimulateFight(weakAttacker, strongDefender, 123);

        // assert — с высокой ловкостью защитника, есть шанс на промах
        Assert.True(result.Winner.IsAlive);
        Assert.False(result.Loser.IsAlive);
    }

    [Fact]
    public void SimulateFight_ThrowsOnDeadGladiator()
    {
        // arrange
        var alive = Gladiator.Create("Alive", new Stats(5, 5, 5));
        var dead = Gladiator.Create("Dead", new Stats(5, 5, 5)).TakeDamage(100);

        // assert
        Assert.Throws<ArgumentException>(() => FightEngine.SimulateFight(alive, dead, 42));
    }
}

public class EconomyTests
{
    [Fact]
    public void LudusState_NewGame_ShouldInitializeWithCorrectValues()
    {
        // act
        var state = LudusState.NewGame(123);

        // assert
        Assert.Equal(1, state.Day);
        Assert.Equal(LudusState.StartingMoney, state.Money);
        Assert.Equal(123, state.Seed);
        Assert.Empty(state.Gladiators);
    }

    [Fact]
    public void LudusState_NewGame_WithDefaultSeed_ShouldUseDefaultSeed()
    {
        // act
        var state = LudusState.NewGame(LudusState.DefaultSeed);

        // assert
        Assert.Equal(LudusState.DefaultSeed, state.Seed);
        Assert.Equal(LudusState.StartingMoney, state.Money);
    }

    [Fact]
    public void HireRandomGladiator_ShouldGenerateRandomStats()
    {
        // arrange
        var state = LudusState.NewGame(42);

        // act
        var newState = state.HireRandomGladiator();

        // assert
        Assert.Equal(1, newState.Count);
        Assert.Equal(LudusState.StartingMoney - LudusState.HireCost, newState.Money);
        // Seed should be updated (different from original 42)
        Assert.NotEqual(42, newState.Seed);
    }

    [Fact]
    public void HireRandomGladiator_Determinism_WithSameSeed_ShouldGenerateSameGladiator()
    {
        // arrange
        var seed = 12345;
        var state1 = LudusState.NewGame(seed);
        var state2 = LudusState.NewGame(seed);

        // act
        var newState1 = state1.HireRandomGladiator();
        var newState2 = state2.HireRandomGladiator();

        // assert
        Assert.Equal(newState1.Gladiators[0].Stats.Strength, newState2.Gladiators[0].Stats.Strength);
        Assert.Equal(newState1.Gladiators[0].Stats.Agility, newState2.Gladiators[0].Stats.Agility);
        Assert.Equal(newState1.Gladiators[0].Stats.Stamina, newState2.Gladiators[0].Stats.Stamina);
        Assert.Equal(newState1.Gladiators[0].Name, newState2.Gladiators[0].Name);
        Assert.Equal(newState1.Money, newState2.Money);
        // Seed should be deterministic based on RNG sequence
        Assert.Equal(newState1.Seed, newState2.Seed);
    }

    [Fact]
    public void HireRandomGladiator_ShouldDeductHireCost()
    {
        // arrange
        var state = LudusState.NewGame(42);
        int initialMoney = state.Money;

        // act
        var newState = state.HireRandomGladiator();

        // assert
        Assert.Equal(initialMoney - LudusState.HireCost, newState.Money);
    }

    [Fact]
    public void HireRandomGladiator_Multiple_ShouldDeductMoneyEachTime()
    {
        // arrange
        var state = LudusState.NewGame(42);

        // act
        var newState1 = state.HireRandomGladiator();
        var newState2 = newState1.HireRandomGladiator();
        var newState3 = newState2.HireRandomGladiator();

        // assert
        Assert.Equal(3, newState3.Count);
        Assert.Equal(LudusState.StartingMoney - 3 * LudusState.HireCost, newState3.Money);
    }

    [Fact]
    public void AdvanceDay_ShouldIncrementDayAndDeductUpkeep()
    {
        // arrange
        var state = LudusState.NewGame(42);
        state = state.HireRandomGladiator();
        int initialDay = state.Day;
        int initialMoney = state.Money;
        int upkeep = LudusState.DailyUpkeepPerGladiator * state.Count;

        // act
        var newState = state.AdvanceDay();

        // assert
        Assert.Equal(initialDay + 1, newState.Day);
        Assert.Equal(initialMoney - upkeep, newState.Money);
    }

    [Fact]
    public void AdvanceDay_NoGladiators_ShouldDeductZero()
    {
        // arrange
        var state = LudusState.NewGame(42);
        int initialDay = state.Day;
        int initialMoney = state.Money;

        // act
        var newState = state.AdvanceDay();

        // assert
        Assert.Equal(initialDay + 1, newState.Day);
        Assert.Equal(initialMoney, newState.Money); // No upkeep for 0 gladiators
    }

    [Fact]
    public void AdvanceDay_MultipleGladiators_ShouldDeductCorrectly()
    {
        // arrange
        var state = LudusState.NewGame(42);
        state = state.HireRandomGladiator();
        state = state.HireRandomGladiator();
        int upkeep = LudusState.DailyUpkeepPerGladiator * state.Count;

        // act
        var newState = state.AdvanceDay();

        // assert
        Assert.Equal(state.Day + 1, newState.Day);
        Assert.Equal(state.Money - upkeep, newState.Money);
    }
}
