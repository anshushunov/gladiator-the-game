using System;
using System.Collections.Generic;
using System.Text;

namespace Ludus.Core;

/// <summary>
/// Лог боя: пошаговая запись событий.
/// </summary>
public readonly record struct FightLog
{
    /// <summary>
    /// Событие в бою (удар, промах, убийство).
    /// </summary>
    public readonly record struct Event
    {
        /// <summary>
        /// Номер раунда.
        /// </summary>
        public int Round { get; init; }

        /// <summary>
        /// Атакующий гладиатор.
        /// </summary>
        public string AttackerName { get; init; }

        /// <summary>
        /// Кто защищался (для отладки).
        /// </summary>
        public string DefenderName { get; init; }

        /// <summary>
        /// Тип события.
        /// </summary>
        public EventType Type { get; init; }

        /// <summary>
        /// Величина (урон или шанс).
        /// </summary>
        public float Value { get; init; }
    }

    /// <summary>
    /// Тип события в бою.
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Атака и попадание с уроном.
        /// </summary>
        Hit,

        /// <summary>
        /// Атака, но промах (из-за ловкости).
        /// </summary>
        Miss,

        /// <summary>
        /// Гладиатор убит.
        /// </summary>
        Kill,

        /// <summary>
        /// Завершение боя.
        /// </summary>
        FightEnd
    }

    /// <summary>
    /// Список событий.
    /// </summary>
    public IReadOnlyList<Event> Events { get; init; } = Array.Empty<Event>();

    /// <summary>
    /// Создаёт пустой лог.
    /// </summary>
    public FightLog() { }

    /// <summary>
    /// Создаёт лог с событиями.
    /// </summary>
    public FightLog(IReadOnlyList<Event> events)
    {
        Events = events;
    }

    /// <summary>
    /// Добавляет событие в лог.
    /// </summary>
    public FightLog AddEvent(Event @event)
    {
        var newEvents = new List<Event>(Events) { @event };
        return new FightLog(newEvents);
    }

    /// <summary>
    /// Возвращает строковое представление лога.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Fight Log ===");

        int round = 0;
        foreach (var @event in Events)
        {
            if (round != @event.Round)
            {
                round = @event.Round;
                sb.AppendLine($"\n[Round {round}]");
            }

            switch (@event.Type)
            {
                case EventType.Hit:
                    sb.AppendLine($"{@event.AttackerName} attacks {@event.DefenderName} for {@event.Value} damage");
                    break;

                case EventType.Miss:
                    sb.AppendLine($"{@event.AttackerName} attacks but misses (defender: {@event.DefenderName})");
                    break;

                case EventType.Kill:
                    sb.AppendLine($"*** {@event.DefenderName} is killed by {@event.AttackerName} ***");
                    break;

                case EventType.FightEnd:
                    sb.AppendLine($"Fight ended. Winner: {@event.AttackerName}");
                    break;
            }
        }

        return sb.ToString();
    }
}
