using System;

namespace Ludus.Core;

/// <summary>
/// Интерфейс для детерминистического генератора случайных чисел.
/// </summary>
public interface IRng
{
    /// <summary>
    /// Генерирует случайное целое число в диапазоне [minValue, maxValue].
    /// </summary>
    int Next(int minValue, int maxValue);

    /// <summary>
    /// Генерирует случайное целое число в диапазоне [0, maxValue).
    /// </summary>
    int Next(int maxValue);

    /// <summary>
    /// Генерирует случайное вещественное число в диапазоне [0.0, 1.0).
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Генерирует случайный булево значение.
    /// </summary>
    bool NextBool();
}

/// <summary>
/// Реализация детерминистического RNG на основе System.Random с задаваемым seed.
/// Сохраняет seed для возможности сериализации и воспроизведения.
/// </summary>
public sealed class SeededRng : IRng, IEquatable<SeededRng>
{
    private readonly int _seed;
    private readonly Random _random;

    /// <summary>
    /// Хранит seed, который был использован для инициализации RNG.
    /// </summary>
    public int Seed => _seed;

    /// <summary>
    /// Создаёт SeededRng с фиксированным seed для детерминизма.
    /// </summary>
    /// <param name="seed">Значение seed для инициализации RNG. По умолчанию 42.</param>
    public SeededRng(int seed = 42)
    {
        _seed = seed;
        _random = new Random(_seed);
    }

    /// <summary>
    /// Создаёт SeededRng с тем же seed, что другой экземпляр.
    /// Полезно для воспроизведения последовательности.
    /// </summary>
    /// <param name="other">Другой экземпляр SeededRng.</param>
    public SeededRng(SeededRng other)
    {
        _seed = other._seed;
        _random = new Random(_seed);
    }

    /// <summary>
    /// Генерирует случайное целое число в диапазоне [minValue, maxValue].
    /// </summary>
    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentException($"minValue ({minValue}) не может быть больше maxValue ({maxValue})");

        return _random.Next(minValue, maxValue + 1);
    }

    /// <summary>
    /// Генерирует случайное целое число в диапазоне [0, maxValue).
    /// </summary>
    public int Next(int maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), $"maxValue ({maxValue}) должен быть больше 0");

        return _random.Next(maxValue);
    }

    /// <summary>
    /// Генерирует случайное вещественное число в диапазоне [0.0, 1.0).
    /// </summary>
    public double NextDouble()
    {
        return _random.NextDouble();
    }

    /// <summary>
    /// Генерирует случайный булево значение.
    /// </summary>
    public bool NextBool()
    {
        return _random.Next(2) == 1;
    }

    /// <summary>
    /// Создаёт новый SeededRng с тем же seed.
    /// </summary>
    public SeededRng Clone()
    {
        return new SeededRng(_seed);
    }

    public override bool Equals(object? obj)
    {
        return obj is SeededRng other && Equals(other);
    }

    public bool Equals(SeededRng? other)
    {
        return other is not null && _seed == other._seed;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_seed);
    }

    /// <summary>
    /// Возвращает строковое представление RNG.
    /// </summary>
    public override string ToString()
    {
        return $"SeededRng {{ Seed={_seed} }}";
    }
}
