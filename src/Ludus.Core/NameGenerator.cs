using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludus.Core;

/// <summary>
/// Генератор уникальных имён на основе списков prefix и cognomen.
/// Имена формируются как конкатенация prefix + cognomen.
/// Гарантирует уникальность до исчерпания пула комбинаций.
/// Детерминизм достигается через seed-based RNG.
/// </summary>
public sealed class NameGenerator : INameGenerator, IDisposable
{
    private readonly List<string> _prefixes;
    private readonly List<string> _cognomens;
    private readonly List<string> _combinations;
    private readonly SeededRng _rng;
    private int _currentIndex;
    private bool _isDisposed;

    /// <summary>
    /// Общее количество уникальных комбинаций в пуле.
    /// </summary>
    public int TotalCombinations => _combinations.Count;

    /// <summary>
    /// Количество оставшихся неиспользованных имён.
    /// </summary>
    public int Remaining => _combinations.Count - _currentIndex;

    /// <summary>
    /// Создаёт генератор имён с заданным seed.
    /// </summary>
    /// <param name="seed">Seed для детерминированного RNG (по умолчанию 42).</param>
    /// <param name="prefixes">Список префиксов (имён).</param>
    /// <param name="cognomens">Список когноменов (отчеств/добавочных имён).</param>
    public NameGenerator(int seed = 42, string[]? prefixes = null, string[]? cognomens = null)
    {
        _prefixes = ValidateAndNormalizeList(prefixes ?? Array.Empty<string>(), nameof(prefixes));
        _cognomens = ValidateAndNormalizeList(cognomens ?? Array.Empty<string>(), nameof(cognomens));

        // Построить все комбинации prefix × cognomen в стабильном порядке
        _combinations = BuildCombinations(_prefixes, _cognomens);

        // Перемешать пул один раз с использованием seed
        _rng = new SeededRng(seed);
        Shuffle(_combinations, _rng);

        _currentIndex = 0;
    }

    /// <summary>
    /// Выдаёт следующее имя из пула.
    /// Выбрасывает исключение, если пул исчерпан.
    /// </summary>
    public string GenerateNext()
    {
        ThrowIfDisposed();

        if (_currentIndex >= _combinations.Count)
        {
            throw new NameGenerationException(
                "Пул имён исчерпан. Все возможные комбинации уже выданы.");
        }

        string name = _combinations[_currentIndex];
        _currentIndex++;
        return name;
    }

    /// <summary>
    /// Пытается выдать следующее имя из пула.
    /// Возвращает false, если пул исчерпан.
    /// </summary>
    public bool TryGenerate(out string name)
    {
        ThrowIfDisposed();

        if (_currentIndex >= _combinations.Count)
        {
            name = string.Empty;
            return false;
        }

        name = _combinations[_currentIndex];
        _currentIndex++;
        return true;
    }

    /// <summary>
    /// Проверяет валидность и нормализует список строк.
    /// </summary>
    private static List<string> ValidateAndNormalizeList(string[] list, string paramName)
    {
        if (list == null)
            throw new ArgumentNullException(paramName);

        var normalized = new List<string>();
        var seen = new HashSet<string>();

        foreach (var item in list)
        {
            if (item == null)
                throw new ValidationException($"Элемент в списке {paramName} не может быть null.");

            string trimmed = item.Trim();

            if (string.IsNullOrEmpty(trimmed))
                throw new ValidationException($"Элемент в списке {paramName} не может быть пустой строкой или пробелами.");

            if (!seen.Add(trimmed))
                throw new ValidationException($"В списке {paramName} обнаружен дубль: '{trimmed}'.");

            normalized.Add(trimmed);
        }

        if (normalized.Count == 0)
            throw new ValidationException($"Список {paramName} не может быть пустым.");

        return normalized;
    }

    /// <summary>
    /// Строит все комбинации prefix × cognomen в стабильном порядке.
    /// Порядок: (prefix[0] × cognomen[0]), (prefix[0] × cognomen[1]), ..., (prefix[1] × cognomen[0]), ...
    /// </summary>
    private static List<string> BuildCombinations(List<string> prefixes, List<string> cognomens)
    {
        var combinations = new List<string>(prefixes.Count * cognomens.Count);

        foreach (var prefix in prefixes)
        {
            foreach (var cognomen in cognomens)
            {
                // Конкатенация с пробелом между parts
                combinations.Add($"{prefix} {cognomen}");
            }
        }

        return combinations;
    }

    /// <summary>
    /// Перемешивает список с использованием Fisher-Yates shuffle и детерминированного RNG.
    /// Выполняется один раз при инициализации.
    /// </summary>
    private static void Shuffle(IList<string> list, SeededRng rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(NameGenerator));
    }

    public void Dispose()
    {
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Возвращает общее количество уникальных имён.
    /// </summary>
    public override string ToString()
    {
        return $"NameGenerator {{ TotalCombinations={TotalCombinations}, Remaining={Remaining}, Seed={_rng.Seed} }}";
    }
}
