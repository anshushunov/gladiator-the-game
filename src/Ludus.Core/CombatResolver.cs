using System;
using System.Collections.Generic;

namespace Ludus.Core;

public sealed class CombatResolver
{
    public CombatModel Model { get; }
    public ConditionModel ConditionModel { get; }

    public static CombatResolver Default { get; } = new(CombatModel.Default, ConditionModel.Default);

    public CombatResolver(CombatModel model) : this(model, ConditionModel.Default)
    {
    }

    public CombatResolver(CombatModel model, ConditionModel conditionModel)
    {
        model.Validate();
        conditionModel.Validate();
        Model = model;
        ConditionModel = conditionModel;
    }

    public double GetHitChance(Gladiator attacker, Gladiator defender)
    {
        var raw = Model.BaseHitChance + (attacker.Stats.Agility - defender.Stats.Agility) * Model.HitChancePerAgilityDiff;
        return Math.Clamp(raw, Model.MinHitChance, Model.MaxHitChance);
    }

    public double GetCritChance(Gladiator attacker)
    {
        var raw = Model.BaseCritChance + attacker.Stats.Agility * Model.CritChancePerAgility;
        return Math.Clamp(raw, 0.0, Model.MaxCritChance);
    }

    public int GetDefense(Gladiator defender)
    {
        return (int)Math.Round(defender.Stats.Stamina * Model.DefensePerStamina);
    }

    public AttackResolution ResolveAttack(Gladiator attacker, Gladiator defender, IRng rng, int round)
    {
        if (!attacker.IsAlive) throw new ArgumentException("Attacker must be alive.", nameof(attacker));
        if (!defender.IsAlive) throw new ArgumentException("Defender must be alive.", nameof(defender));
        if (rng is null) throw new ArgumentNullException(nameof(rng));

        var events = new List<FightLog.Event>();
        var hitChance = GetHitChance(attacker, defender);

        if (rng.NextDouble() >= hitChance)
        {
            events.Add(new FightLog.Event
            {
                Round = round,
                AttackerName = attacker.Name,
                DefenderName = defender.Name,
                Type = FightLog.EventType.Miss,
                Value = (float)hitChance
            });

            return new AttackResolution(defender, false, false, 0, events);
        }

        events.Add(new FightLog.Event
        {
            Round = round,
            AttackerName = attacker.Name,
            DefenderName = defender.Name,
            Type = FightLog.EventType.Hit,
            Value = (float)hitChance
        });

        var damage = CalculateDamageAfterDefense(attacker, defender, rng);
        var critChance = GetCritChance(attacker);
        var isCritical = rng.NextDouble() < critChance;

        if (isCritical)
        {
            damage = Math.Max(Model.MinDamageAfterDefense, (int)Math.Round(damage * Model.CritMultiplier));
            events.Add(new FightLog.Event
            {
                Round = round,
                AttackerName = attacker.Name,
                DefenderName = defender.Name,
                Type = FightLog.EventType.Crit,
                Value = (float)critChance
            });
        }

        events.Add(new FightLog.Event
        {
            Round = round,
            AttackerName = attacker.Name,
            DefenderName = defender.Name,
            Type = FightLog.EventType.DamageApplied,
            Value = damage
        });

        var damagedDefender = defender.TakeDamage(damage);
        return new AttackResolution(damagedDefender, true, isCritical, damage, events);
    }

    private int CalculateDamageAfterDefense(Gladiator attacker, Gladiator defender, IRng rng)
    {
        var baseDamage = attacker.Stats.Strength * 2;
        var varianceRoll = rng.NextDouble();
        var varianceMultiplier = Model.DamageVarianceMin + (varianceRoll * (Model.DamageVarianceMax - Model.DamageVarianceMin));
        double efficiency = ConditionResolver.GetEfficiency(attacker.Morale, attacker.Fatigue, ConditionModel);
        var modifiedDamage = (int)Math.Round(baseDamage * varianceMultiplier * efficiency);
        var defense = GetDefense(defender);
        var reducedDamage = modifiedDamage - defense;
        return Math.Max(Model.MinDamageAfterDefense, reducedDamage);
    }

    public readonly record struct AttackResolution(
        Gladiator DefenderAfterAttack,
        bool IsHit,
        bool IsCritical,
        int Damage,
        IReadOnlyList<FightLog.Event> Events);
}
