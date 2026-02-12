using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludus.Core;

/// <summary>
/// Состояние симуляции Ludus: коллекция гладиаторов и ID текущего активного.
/// </summary>
public record LudusState
{
    /// <summary>
    /// Коллекция всех гладиаторов в симуляции.
    /// </summary>
    public IReadOnlyList<Gladiator> Gladiators { get; init; } = Array.Empty<Gladiator>();

    /// <summary>
    /// ID гладиатора, который сейчас в бою (для turn-based режима).
    /// </summary>
    public Guid? ActiveGladiatorId { get; init; }

    private const int MinGladiators = 0;
    private const int MaxGladiators = 100;

    private LudusState(IReadOnlyList<Gladiator> gladiators, Guid? activeGladiatorId)
    {
        if (gladiators.Count < MinGladiators || gladiators.Count > MaxGladiators)
            throw new ValidationException(
                $"Количество гладиаторов должно быть в диапазоне [{MinGladiators}, {MaxGladiators}]");

        if (activeGladiatorId.HasValue &&
            !gladiators.Any(g => g.Id == activeGladiatorId.Value))
            throw new ValidationException("ActiveGladiatorId должен соответствовать существующему гладиатору");

        Gladiators = gladiators;
        ActiveGladiatorId = activeGladiatorId;
    }

    /// <summary>
    /// Создаёт пустое состояние симуляции.
    /// </summary>
    public static LudusState Empty => new(Array.Empty<Gladiator>(), null);

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
