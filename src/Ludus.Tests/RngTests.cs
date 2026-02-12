using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

public class SeededRngTests
{
    [Fact]
    public void SeededRng_Create_WithSameSeed_ShouldBeEqual()
    {
        // arrange
        var seed = 12345;

        // act
        var rng1 = new SeededRng(seed);
        var rng2 = new SeededRng(seed);

        // assert
        Assert.Equal(rng1.Seed, rng2.Seed);
        Assert.Equal(seed, rng1.Seed);
        Assert.Equal(seed, rng2.Seed);
    }

    [Fact]
    public void SeededRng_Create_WithDefaultSeed_ShouldUse42()
    {
        // act
        var rng = new SeededRng();

        // assert
        Assert.Equal(42, rng.Seed);
    }

    [Fact]
    public void SeededRng_NextIntWithBounds_ShouldBeInRng()
    {
        // arrange
        var rng = new SeededRng(42);
        int min = 5;
        int max = 15;

        // act & assert
        for (int i = 0; i < 100; i++)
        {
            int value = rng.Next(min, max);
            Assert.True(value >= min && value <= max, $"Value {value} out of range [{min}, {max}]");
        }
    }

    [Fact]
    public void SeededRng_NextIntMaxValue_ShouldBeLessThanMax()
    {
        // arrange
        var rng = new SeededRng(42);
        int maxValue = 10;

        // act & assert
        for (int i = 0; i < 100; i++)
        {
            int value = rng.Next(maxValue);
            Assert.True(value >= 0 && value < maxValue, $"Value {value} out of range [0, {maxValue})");
        }
    }

    [Fact]
    public void SeededRng_NextDouble_ShouldBeInRng()
    {
        // arrange
        var rng = new SeededRng(42);

        // act & assert
        for (int i = 0; i < 100; i++)
        {
            double value = rng.NextDouble();
            Assert.True(value >= 0.0 && value < 1.0, $"Value {value} out of range [0.0, 1.0)");
        }
    }

    [Fact]
    public void SeededRng_NextBool_ShouldReturnTrueOrFalse()
    {
        // arrange
        var rng = new SeededRng(42);

        // act & assert
        for (int i = 0; i < 100; i++)
        {
            bool value = rng.NextBool();
            Assert.True(value == true || value == false);
        }
    }

    [Fact]
    public void SeededRng_Determinism_WithSameSeed_ShouldGenerateSameSequence()
    {
        // arrange
        var seed = 42;
        var rng1 = new SeededRng(seed);
        var rng2 = new SeededRng(seed);

        // act
        var values1 = new int[10];
        var values2 = new int[10];
        for (int i = 0; i < 10; i++)
        {
            values1[i] = rng1.Next(0, 100);
            values2[i] = rng2.Next(0, 100);
        }

        // assert
        Assert.Equal(values1, values2);
    }

    [Fact]
    public void SeededRng_DifferentSeeds_ShouldGenerateDifferentSequences()
    {
        // arrange
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(123);

        // act
        var values1 = new int[10];
        var values2 = new int[10];
        for (int i = 0; i < 10; i++)
        {
            values1[i] = rng1.Next(0, 100);
            values2[i] = rng2.Next(0, 100);
        }

        // assert
        Assert.NotEqual(values1, values2);
    }

    [Fact]
    public void SeededRng_NextWithInvalidBounds_ShouldThrow()
    {
        // arrange
        var rng = new SeededRng(42);

        // assert
        Assert.Throws<ArgumentException>(() => rng.Next(10, 5));
    }

    [Fact]
    public void SeededRng_NextWithZeroMaxValue_ShouldThrow()
    {
        // arrange
        var rng = new SeededRng(42);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0));
    }

    [Fact]
    public void SeededRng_Clone_ShouldHaveSameSeed()
    {
        // arrange
        var rng = new SeededRng(42);

        // act
        var cloned = rng.Clone();

        // assert
        Assert.Equal(rng.Seed, cloned.Seed);
    }

    [Fact]
    public void SeededRng_Equals_WithSameSeed_ShouldBeEqual()
    {
        // arrange
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(42);

        // assert
        Assert.Equal(rng1.GetHashCode(), rng2.GetHashCode());
    }

    [Fact]
    public void SeededRng_Equals_WithDifferentSeed_ShouldNotBeEqual()
    {
        // arrange
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(123);

        // assert
        Assert.NotEqual(rng1.GetHashCode(), rng2.GetHashCode());
    }

    [Fact]
    public void SeededRng_CreateCloneFromOther_ShouldHaveSameSeed()
    {
        // arrange
        var original = new SeededRng(123);

        // act
        var clone = new SeededRng(original);

        // assert
        Assert.Equal(original.Seed, clone.Seed);
    }
}

public class LudusStateRngTests
{
    [Fact]
    public void LudusState_DefaultSeed_ShouldBe42()
    {
        // act
        var state = LudusState.Empty;

        // assert
        Assert.Equal(LudusState.DefaultSeed, state.Seed);
        Assert.Equal(42, state.Seed);
    }

    [Fact]
    public void LudusState_CreateRng_WithDefaultSeed_ShouldGenerateDeterministicSequence()
    {
        // arrange
        var state1 = LudusState.Empty;
        var state2 = LudusState.Empty;

        // act
        var rng1 = state1.CreateRng();
        var rng2 = state2.CreateRng();

        // assert
        Assert.Equal(state1.Seed, state2.Seed);
        Assert.Equal(rng1.Seed, rng2.Seed);

        // генерируем последовательность и проверяем детерминизм
        var values1 = new int[5];
        var values2 = new int[5];
        for (int i = 0; i < 5; i++)
        {
            values1[i] = rng1.Next(0, 100);
            values2[i] = rng2.Next(0, 100);
        }

        Assert.Equal(values1, values2);
    }

    [Fact]
    public void LudusState_CreateRng_DifferentStatesSameSeed_ShouldGeneratesSame()
    {
        // arrange
        var state1 = LudusState.Empty;
        var state2 = LudusState.Empty with { Seed = 42 };

        // act
        var rng1 = state1.CreateRng();
        var rng2 = state2.CreateRng();

        // assert
        Assert.Equal(rng1.Next(0, 100), rng2.Next(0, 100));
    }

    [Fact]
    public void LudusState_CreateRng_DifferentSeeds_ShouldGenerateDifferent()
    {
        // arrange
        var state1 = LudusState.Empty with { Seed = 100 };
        var state2 = LudusState.Empty with { Seed = 200 };

        // act
        var rng1 = state1.CreateRng();
        var rng2 = state2.CreateRng();

        // assert
        Assert.NotEqual(rng1.Next(0, 100), rng2.Next(0, 100));
    }

    [Fact]
    public void LudusState_AddGladiator_PreservesSeed()
    {
        // arrange
        var state = LudusState.Empty with { Seed = 42 };
        var gladiator = Gladiator.Create("Test", Stats.Default);

        // act
        var newState = state.AddGladiator(gladiator);

        // assert
        Assert.Equal(state.Seed, newState.Seed);
    }

    [Fact]
    public void LudusState_RemoveGladiator_PreservesSeed()
    {
        // arrange
        var state = LudusState.Empty with { Seed = 42 };
        var gladiator = Gladiator.Create("Test", Stats.Default);
        state = state.AddGladiator(gladiator);

        // act
        var newState = state.RemoveGladiator(gladiator.Id);

        // assert
        Assert.Equal(state.Seed, newState.Seed);
    }

    [Fact]
    public void LudusState_SetActiveGladiator_PreservesSeed()
    {
        // arrange
        var state = LudusState.Empty with { Seed = 42 };
        var gladiator = Gladiator.Create("Test", Stats.Default);
        state = state.AddGladiator(gladiator);

        // act
        var newState = state.SetActiveGladiator(gladiator.Id);

        // assert
        Assert.Equal(state.Seed, newState.Seed);
    }
}
