using System;
using System.Collections.Generic;

namespace Ludus.Core;

public static class FightEngine
{
    public static FightResult SimulateFight(Gladiator g1, Gladiator g2, IRng rng)
    {
        return SimulateFight(g1, g2, rng, CombatResolver.Default);
    }

    public static FightResult SimulateFight(Gladiator g1, Gladiator g2, IRng rng, CombatResolver resolver)
    {
        if (!g1.IsAlive) throw new ArgumentException("First gladiator must be alive.", nameof(g1));
        if (!g2.IsAlive) throw new ArgumentException("Second gladiator must be alive.", nameof(g2));
        if (rng is null) throw new ArgumentNullException(nameof(rng));
        if (resolver is null) throw new ArgumentNullException(nameof(resolver));

        var fighter1 = g1;
        var fighter2 = g2;
        var events = new List<FightLog.Event>();
        var round = 0;

        while (fighter1.IsAlive && fighter2.IsAlive)
        {
            round++;

            var firstAttacks = round % 2 == 0;
            var attacker = firstAttacks ? fighter1 : fighter2;
            var defender = firstAttacks ? fighter2 : fighter1;

            var resolution = resolver.ResolveAttack(attacker, defender, rng, round);
            events.AddRange(resolution.Events);

            if (firstAttacks)
            {
                fighter2 = resolution.DefenderAfterAttack;
            }
            else
            {
                fighter1 = resolution.DefenderAfterAttack;
            }

            if (!resolution.DefenderAfterAttack.IsAlive)
            {
                events.Add(new FightLog.Event
                {
                    Round = round,
                    AttackerName = attacker.Name,
                    DefenderName = defender.Name,
                    Type = FightLog.EventType.Kill,
                    Value = 0
                });

                events.Add(new FightLog.Event
                {
                    Round = round,
                    AttackerName = attacker.Name,
                    DefenderName = defender.Name,
                    Type = FightLog.EventType.FightEnd,
                    Value = 0
                });
            }
        }

        var winner = fighter1.IsAlive ? fighter1 : fighter2;
        var loser = fighter1.IsAlive ? fighter2 : fighter1;
        return new FightResult(winner, loser, new FightLog(events));
    }

    public static FightResult SimulateFight(Gladiator g1, Gladiator g2, int seed = 42)
    {
        return SimulateFight(g1, g2, new SeededRng(seed), CombatResolver.Default);
    }
}
