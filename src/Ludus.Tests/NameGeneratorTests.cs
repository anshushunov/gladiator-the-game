using Xunit;
using Ludus.Core;

namespace Ludus.Tests;

public class NameGeneratorTests
{
    private static readonly string[] DefaultPrefixes = ["Brutus", "Crixus", "Spartacus"];
    private static readonly string[] DefaultCognomens = ["Albus", "Major", "Primus"];

    [Fact]
    public void NameGenerator_Create_WithValidData_ShouldSucceed()
    {
        // arrange & act
        var generator = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);

        // assert
        Assert.Equal(9, generator.TotalCombinations); // 3 * 3
        Assert.Equal(9, generator.Remaining);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(12345)]
    public void NameGenerator_Create_WithDifferentSeeds_ShouldSucceed(int seed)
    {
        // arrange & act
        var generator = new NameGenerator(seed, DefaultPrefixes, DefaultCognomens);

        // assert
        Assert.Equal(9, generator.TotalCombinations);
        Assert.True(generator.TryGenerate(out _)); // Just verify it doesn't throw
    }

    [Fact]
    public void NameGenerator_GenerateNext_Determinism_SameSeed_ShouldProduceSameSequence()
    {
        // arrange
        var seed = 123;
        var generator1 = new NameGenerator(seed, DefaultPrefixes, DefaultCognomens);
        var generator2 = new NameGenerator(seed, DefaultPrefixes, DefaultCognomens);

        // act
        var names1 = new List<string>();
        var names2 = new List<string>();
        for (int i = 0; i < 9; i++)
        {
            names1.Add(generator1.GenerateNext());
            names2.Add(generator2.GenerateNext());
        }

        // assert
        Assert.Equal(names1, names2);
    }

    [Fact]
    public void NameGenerator_GenerateNext_DifferentSeeds_ShouldProduceDifferentSequences()
    {
        // arrange
        var generator1 = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);
        var generator2 = new NameGenerator(123, DefaultPrefixes, DefaultCognomens);

        // act
        var names1 = new List<string>();
        var names2 = new List<string>();
        for (int i = 0; i < 9; i++)
        {
            names1.Add(generator1.GenerateNext());
            names2.Add(generator2.GenerateNext());
        }

        // assert
        Assert.NotEqual(names1, names2);
    }

    [Fact]
    public void NameGenerator_TryGenerate_ShouldReturnTrueForValidNames()
    {
        // arrange
        var generator = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);

        // act
        var result1 = generator.TryGenerate(out string name1);
        var result2 = generator.TryGenerate(out string name2);

        // assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.NotNull(name1);
        Assert.NotNull(name2);
    }

    [Fact]
    public void NameGenerator_TryGenerate_AtEndOfPool_ShouldReturnFalse()
    {
        // arrange
        var generator = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);

        // act - exhaust the pool
        for (int i = 0; i < 9; i++)
        {
            generator.TryGenerate(out _);
        }

        // assert
        Assert.False(generator.TryGenerate(out string name));
        Assert.Equal(string.Empty, name);
    }

    [Fact]
    public void NameGenerator_GenerateNext_AtEndOfPool_ShouldThrow()
    {
        // arrange
        var generator = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);

        // act - exhaust the pool
        for (int i = 0; i < 9; i++)
        {
            generator.GenerateNext();
        }

        // assert
        var ex = Assert.Throws<NameGenerationException>(() => generator.GenerateNext());
        Assert.Contains("исчерпан", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_NoDuplicates_UntilExhaustion()
    {
        // arrange
        var generator = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);
        var names = new HashSet<string>();

        // act
        for (int i = 0; i < 9; i++)
        {
            string name = generator.GenerateNext();
            Assert.True(names.Add(name), $"Дубль обнаружен: {name}");
        }

        // assert
        Assert.Equal(9, names.Count);
    }

    [Fact]
    public void NameGenerator_Validation_NullPrefixes_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, null, DefaultCognomens));
        Assert.Contains("пустым", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_NullCognomens_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, DefaultPrefixes, null));
        Assert.Contains("пустым", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_EmptyPrefixesList_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, Array.Empty<string>(), DefaultCognomens));
        Assert.Contains("пустым", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_EmptyCognomensList_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, DefaultPrefixes, Array.Empty<string>()));
        Assert.Contains("пустым", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_PrefixWithOnlyWhitespace_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, new[] { "  " }, DefaultCognomens));
        Assert.Contains("пустой", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_CognomenWithOnlyWhitespace_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, DefaultPrefixes, new[] { "   " }));
        Assert.Contains("пустой", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_EmptyStringItem_ShouldThrow()
    {
        // arrange & assert - пустые строки запрещены
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, new[] { "Prefix", "" }, DefaultCognomens));
        Assert.Contains("пустой", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_DuplicatePrefix_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, new[] { "Brutus", "Brutus" }, DefaultCognomens));
        Assert.Contains("дубль", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_Validation_DuplicateCognomen_ShouldThrow()
    {
        // arrange & assert
        var ex = Assert.Throws<ValidationException>(() => new NameGenerator(42, DefaultPrefixes, new[] { "Albus", "Albus" }));
        Assert.Contains("дубль", ex.Message.ToLower());
    }

    [Fact]
    public void NameGenerator_TrimsWhitespace_FromInputStrings()
    {
        // arrange & act
        var generator = new NameGenerator(42, new[] { "  Brutus  " }, new[] { "  Albus  " });

        // assert
        string name = generator.GenerateNext();
        Assert.Equal("Brutus Albus", name);
    }

    [Fact]
    public void NameGenerator_NameFormat_IsPrefixSpaceCognomen()
    {
        // arrange
        var prefixes = new[] { "Brutus", "Crixus" };
        var cognomens = new[] { "Albus", "Major" };
        var generator = new NameGenerator(42, prefixes, cognomens);

        // act & assert
        Assert.StartsWith("Brutus ", generator.GenerateNext()); // First in determinist order
    }

    [Fact]
    public void NameGenerator_RemoveAfterExhaustion_ShouldThrow()
    {
        // arrange
        var generator = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);

        // act - exhaust
        for (int i = 0; i < 9; i++)
        {
            generator.GenerateNext();
        }

        // assert
        Assert.Throws<NameGenerationException>(() => generator.GenerateNext());
        Assert.False(generator.TryGenerate(out _));
    }

    [Fact]
    public void NameGenerator_ToString_ShouldIncludeInfo()
    {
        // arrange
        var generator = new NameGenerator(42, DefaultPrefixes, DefaultCognomens);

        // act
        string toString = generator.ToString();

        // assert
        Assert.Contains("TotalCombinations", toString);
        Assert.Contains("Remaining", toString);
        Assert.Contains("42", toString);
    }

    [Fact]
    public void NameGenerator_GenerateAll_ShouldUseAllCombinations()
    {
        // arrange
        var prefixes = new[] { "A", "B" };
        var cognomens = new[] { "1", "2", "3" };
        var generator = new NameGenerator(42, prefixes, cognomens);

        // act
        var names = new List<string>();
        while (generator.TryGenerate(out string name))
        {
            names.Add(name);
        }

        // assert
        Assert.Equal(6, names.Count);
        Assert.All(names, name => Assert.Contains(" ", name));
    }

    [Fact]
    public void NameGenerator_Determinism_MultipleInstances_SameSeed_ShouldMatch()
    {
        // arrange
        var seed = 999;
        var names1 = new List<string>();
        var names2 = new List<string>();

        // act
        using (var gen1 = new NameGenerator(seed, DefaultPrefixes, DefaultCognomens))
        {
            for (int i = 0; i < 5; i++) names1.Add(gen1.GenerateNext());
        }

        using (var gen2 = new NameGenerator(seed, DefaultPrefixes, DefaultCognomens))
        {
            for (int i = 0; i < 5; i++) names2.Add(gen2.GenerateNext());
        }

        // assert
        Assert.Equal(names1, names2);
    }
}
