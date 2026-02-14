using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

/// <summary>
/// Stability tests (snapshot tests) для NameGenerator.
/// Фиксируют точную последовательность имён для фиксированного seed,
/// чтобы ловить случайные изменения алгоритма.
/// </summary>
public class NameGeneratorStabilityTests
{
    [Fact]
    public void NameGenerator_Snapshot_Seed42_SmallLists()
    {
        // arrange
        var seed = 42;
        var prefixes = new[] { "Brutus", "Crixus", "Spartacus" };
        var cognomens = new[] { "Albus", "Major", "Primus" };
        var generator = new NameGenerator(seed, prefixes, cognomens);

        // act - сгенерировать все имена
        var names = new List<string>();
        while (generator.TryGenerate(out string name))
        {
            names.Add(name);
        }

        // assert
        // Snapshot: фиксируем точную последовательность для seed=42
        Assert.Equal(new[] {
            "Crixus Major",
            "Crixus Primus",
            "Brutus Primus",
            "Spartacus Major",
            "Spartacus Primus",
            "Crixus Albus",
            "Brutus Albus",
            "Brutus Major",
            "Spartacus Albus"
        }, names);
    }

    [Fact]
    public void NameGenerator_Snapshot_Seed123_SmallLists()
    {
        // arrange
        var seed = 123;
        var prefixes = new[] { "Brutus", "Crixus", "Spartacus" };
        var cognomens = new[] { "Albus", "Major", "Primus" };
        var generator = new NameGenerator(seed, prefixes, cognomens);

        // act
        var names = new List<string>();
        while (generator.TryGenerate(out string name))
        {
            names.Add(name);
        }

        // assert
        // Snapshot: другая последовательность для seed=123
        Assert.NotEqual(new[] {
            "Crixus Major", "Crixus Primus", "Brutus Primus", "Spartacus Major", "Spartacus Primus",
            "Crixus Albus", "Brutus Albus", "Brutus Major", "Spartacus Albus"
        }, names);
    }

    [Fact]
    public void NameGenerator_Snapshot_FirstFiveNames_Seed42()
    {
        // arrange
        var seed = 42;
        var prefixes = new[] { "Brutus", "Crixus", "Spartacus" };
        var cognomens = new[] { "Albus", "Major", "Primus" };
        var generator = new NameGenerator(seed, prefixes, cognomens);

        // act
        var firstFive = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            firstFive.Add(generator.GenerateNext());
        }

        // assert
        // Snapshot первых 5 имён для seed=42
        Assert.Equal(new[] {
            "Crixus Major",
            "Crixus Primus",
            "Brutus Primus",
            "Spartacus Major",
            "Spartacus Primus"
        }, firstFive);
    }

    [Fact]
    public void NameGenerator_Snapshot_LargeLists_Seed100_FirstTen()
    {
        // arrange
        var seed = 100;
        var prefixes = new[]
        {
            "Brutus", "Crixus", "Spartacus", "Oenomaus", "Spiculus",
            "Varro", "Digo", "Priscus", "Flamma", "Attius"
        };
        var cognomens = new[] { "Albus", "Major", "Primus", "Secundus", "Tertius" };
        var generator = new NameGenerator(seed, prefixes, cognomens);

        // act
        var firstTen = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            firstTen.Add(generator.GenerateNext());
        }

        // assert
        // Snapshot первых 10 имён для seed=100
        Assert.Equal(new[] {
            "Attius Tertius",
            "Varro Secundus",
            "Brutus Albus",
            "Spiculus Primus",
            "Varro Albus",
            "Spiculus Secundus",
            "Spartacus Albus",
            "Oenomaus Primus",
            "Brutus Tertius",
            "Oenomaus Tertius"
        }, firstTen);
    }

    [Fact]
    public void NameGenerator_Snapshot_MediumLists_Seed50()
    {
        // arrange
        var seed = 50;
        var prefixes = new[] { "A", "B", "C", "D", "E" };
        var cognomens = new[] { "1", "2", "3" };
        var generator = new NameGenerator(seed, prefixes, cognomens);

        // act
        var allNames = new List<string>();
        while (generator.TryGenerate(out string name))
        {
            allNames.Add(name);
        }

        // assert
        // 5 * 3 = 15 имён
        Assert.Equal(15, allNames.Count);
        // Проверяем формат
        Assert.All(allNames, name => Assert.Contains(" ", name));
        // Проверяем уникальность
        Assert.Equal(15, allNames.Distinct().Count());
    }

    [Fact]
    public void NameGenerator_Snapshot_EmptyPoolAfterExhaustion()
    {
        // arrange
        var generator = new NameGenerator(42, new[] { "A" }, new[] { "1" });

        // act
        Assert.True(generator.TryGenerate(out string name1));
        Assert.False(generator.TryGenerate(out string name2));

        // assert
        Assert.Equal("A 1", name1);
        Assert.Equal(string.Empty, name2);
    }
}
