using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludus.Core;

/// <summary>
/// Состояние симуляции Ludus: коллекция гладиаторов и ID текущего активного.
/// </summary>
public partial record LudusState
{
    /// <summary>
    /// Коллекция всех гладиаторов в симуляции.
    /// </summary>
    public IReadOnlyList<Gladiator> Gladiators { get; init; } = Array.Empty<Gladiator>();

    /// <summary>
    /// ID гладиатора, который сейчас в бою (для turn-based режима).
    /// </summary>
    public Guid? ActiveGladiatorId { get; init; }

    /// <summary>
    /// Текущий день симуляции.
    /// </summary>
    public int Day { get; init; }

    /// <summary>
    /// Текущее количество денег.
    /// </summary>
    public int Money { get; init; }

    /// <summary>
    /// Seed для детерignantического генератора случайных чисел.
    /// Хранится как int для сериализуемости и воспроизводимости.
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// Константа для значения seed по умолчанию.
    /// </summary>
    public const int DefaultSeed = 42;

    /// <summary>
    /// Стартовое количество денег.
    /// </summary>
    public const int StartingMoney = 500;

    /// <summary>
    /// Стоимость найма гладиатора.
    /// </summary>
    public const int HireCost = 100;

    /// <summary>
    /// Ежедневное содержание одного гладиатора.
    /// </summary>
    public const int DailyUpkeepPerGladiator = 5;

    private const int MinGladiators = 0;
    private const int MaxGladiators = 100;

    private LudusState(IReadOnlyList<Gladiator> gladiators, Guid? activeGladiatorId, int day, int money, int seed)
    {
        if (gladiators.Count < MinGladiators || gladiators.Count > MaxGladiators)
            throw new ValidationException(
                $"Количество гладиаторов должно быть в диапазоне [{MinGladiators}, {MaxGladiators}]");

        if (activeGladiatorId.HasValue &&
            !gladiators.Any(g => g.Id == activeGladiatorId.Value))
            throw new ValidationException("ActiveGladiatorId должен соответствовать существующему гладиатору");

        if (day < 1)
            throw new ValidationException("Day должен быть >= 1");

        if (money < 0)
            throw new ValidationException("Money не может быть отрицательным");

        Gladiators = gladiators;
        ActiveGladiatorId = activeGladiatorId;
        Day = day;
        Money = money;
        Seed = seed;
    }

    /// <summary>
    /// Создаёт пустое состояние симуляции.
    /// </summary>
    public static LudusState Empty => new(Array.Empty<Gladiator>(), null, 1, StartingMoney, DefaultSeed);

    /// <summary>
    /// Создаёт новую игру с заданным seed.
    /// </summary>
    public static LudusState NewGame(int seed)
    {
        return new LudusState(Array.Empty<Gladiator>(), null, 1, StartingMoney, seed);
    }

    /// <summary>
    /// Добавляет гладиатора в симуляцию.
    /// </summary>
    public LudusState AddGladiator(Gladiator gladiator)
    {
        if (gladiator.Id == Guid.Empty)
            throw new ValidationException("ID гладиатора не может быть пустым");

        if (Gladiators.Any(g => g.Id == gladiator.Id))
            throw new ValidationException($"Гладиатор с ID {gladiator.Id} уже существует в симуляции");

        if (Gladiators.Count >= MaxGladiators)
            throw new ValidationException($"Достигнут максимальный лимит гладиаторов: {MaxGladiators}");

        var updatedGladiators = Gladiators.Append(gladiator).ToArray();
        Guid? newActiveId = ActiveGladiatorId ?? gladiator.Id;

        return this with { Gladiators = updatedGladiators, ActiveGladiatorId = newActiveId };
    }

    /// <summary>
    /// Удаляет гладиатора из симуляции.
    /// </summary>
    public LudusState RemoveGladiator(Guid gladiatorId)
    {
        if (!Gladiators.Any(g => g.Id == gladiatorId))
            throw new ValidationException($"Гладиатор с ID {gladiatorId} не найден в симуляции");

        var updatedGladiators = Gladiators.Where(g => g.Id != gladiatorId).ToArray();
        Guid? newActiveId = ActiveGladiatorId == gladiatorId ? null : ActiveGladiatorId;
        return this with { Gladiators = updatedGladiators, ActiveGladiatorId = newActiveId };
    }

    /// <summary>
    /// Устанавливает активного гладиатора.
    /// </summary>
    public LudusState SetActiveGladiator(Guid gladiatorId)
    {
        if (!Gladiators.Any(g => g.Id == gladiatorId))
            throw new ValidationException($"Гладиатор с ID {gladiatorId} не найден в симуляции");

        return this with { ActiveGladiatorId = gladiatorId };
    }

    /// <summary>
    /// Нанимает случайного гладиатора с использованием RNG.
    /// Генерирует случайные статы и имя, списывает HireCost и обновляет Seed.
    /// </summary>
    public LudusState HireRandomGladiator()
    {
        // Создаём RNG на основе текущего Seed
        var rng = CreateRng();

        // Генерируем статы в диапазоне [1, 10]
        int strength = rng.Next(1, 10);
        int agility = rng.Next(1, 10);
        int stamina = rng.Next(1, 10);
        var stats = new Stats(strength, agility, stamina);

        // Генерируем имя из префикса + номер
        string[] prefixes = ["Brutus", "Crixus", "Spartacus", "Oenomaus", "Roma", "Spiculus", "Varro"];
        string prefix = prefixes[rng.Next(prefixes.Length)];
        string name = $"{prefix} #{Count + 1}";

        // Создаём гладиатора
        var gladiator = Gladiator.Create(name, stats);

        // Обновляем состояние: добавляем гладиатора и списываем деньги
        var newState = AddGladiator(gladiator) with { Money = Money - HireCost };

        // Обновляем Seed детерминированно: используем следующее значение из RNG
        int newSeed = rng.Next(int.MaxValue);
        return newState with { Seed = newSeed };
    }

    /// <summary>
    /// Переходит к следующему дню: увеличивает Day и списывает ежедневное содержание.
    /// </summary>
    public LudusState AdvanceDay()
    {
        // Списываем содержание: DailyUpkeepPerGladiator * Gladiators.Count
        int upkeep = DailyUpkeepPerGladiator * Gladiators.Count;
        int newMoney = Money - upkeep;

        return this with { Day = Day + 1, Money = newMoney };
    }

    /// <summary>
    /// Проводит бой между двумя живыми гладиаторами, применяет результат к состоянию
    /// и детерминированно продвигает seed.
    /// </summary>
    public (LudusState State, FightResult Result) ResolveFight(Guid firstGladiatorId, Guid secondGladiatorId)
    {
        if (firstGladiatorId == secondGladiatorId)
            throw new ValidationException("Гладиаторы для боя должны быть разными");

        var first = GetGladiator(firstGladiatorId);
        var second = GetGladiator(secondGladiatorId);

        if (!first.IsAlive || !second.IsAlive)
            throw new ValidationException("В бою могут участвовать только живые гладиаторы");

        var rng = CreateRng();
        var result = FightEngine.SimulateFight(first, second, rng);

        var updatedGladiators = Gladiators
            .Select(g =>
            {
                if (g.Id == result.Winner.Id) return result.Winner;
                if (g.Id == result.Loser.Id) return result.Loser;
                return g;
            })
            .ToArray();

        int newSeed = rng.Next(int.MaxValue);
        var updatedState = this with
        {
            Gladiators = updatedGladiators,
            Seed = newSeed
        };

        return (updatedState, result);
    }

    /// <summary>
    /// Создаёт новый RNG на основе stored seed.
    /// Каждый вызов возвращает новый экземпляр RNG с тем же seed.
    /// </summary>
    public SeededRng CreateRng()
    {
        return new SeededRng(Seed);
    }

    /// <summary>
    /// Получает гладиатора по ID.
    /// </summary>
    public Gladiator GetGladiator(Guid gladiatorId)
    {
        var gladiator = Gladiators.FirstOrDefault(g => g.Id == gladiatorId);
        if (gladiator.Id == Guid.Empty)
            throw new ValidationException($"Гладиатор с ID {gladiatorId} не найден в симуляции");

        return gladiator;
    }

    /// <summary>
    /// Получает количество гладиаторов в симуляции.
    /// </summary>
    public int Count => Gladiators.Count;

    /// <summary>
    /// Проверяет, есть ли хотя бы один живой гладиатор.
    /// </summary>
    public bool HasAliveGladiators => Gladiators.Any(g => g.IsAlive);

    /// <summary>
    /// Получает список живых гладиаторов.
    /// </summary>
    public IReadOnlyList<Gladiator> AliveGladiators => Gladiators.Where(g => g.IsAlive).ToArray();
}
