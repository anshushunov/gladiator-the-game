using System;
using System.Collections.Generic;
using System.Text;

namespace Ludus.Core;

public readonly record struct FightLog
{
    public readonly record struct Event
    {
        public int Round { get; init; }
        public string AttackerName { get; init; }
        public string DefenderName { get; init; }
        public EventType Type { get; init; }
        public float Value { get; init; }
    }

    public enum EventType
    {
        Hit,
        Miss,
        Crit,
        DamageApplied,
        Kill,
        FightEnd
    }

    public IReadOnlyList<Event> Events { get; init; } = Array.Empty<Event>();

    public FightLog() { }

    public FightLog(IReadOnlyList<Event> events)
    {
        Events = events;
    }

    public FightLog AddEvent(Event @event)
    {
        var newEvents = new List<Event>(Events) { @event };
        return new FightLog(newEvents);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Fight Log ===");

        var round = 0;
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
                    sb.AppendLine($"{@event.AttackerName} hits {@event.DefenderName} (hit chance: {@event.Value:P0})");
                    break;
                case EventType.Miss:
                    sb.AppendLine($"{@event.AttackerName} misses {@event.DefenderName} (hit chance: {@event.Value:P0})");
                    break;
                case EventType.Crit:
                    sb.AppendLine($"{@event.AttackerName} lands CRIT on {@event.DefenderName} (crit chance: {@event.Value:P0})");
                    break;
                case EventType.DamageApplied:
                    sb.AppendLine($"{@event.DefenderName} takes {@event.Value} damage");
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
