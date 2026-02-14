using System;

namespace Ludus.Core;

public readonly record struct CombatModel(
    double BaseHitChance,
    double HitChancePerAgilityDiff,
    double MinHitChance,
    double MaxHitChance,
    double BaseCritChance,
    double CritChancePerAgility,
    double MaxCritChance,
    double DamageVarianceMin,
    double DamageVarianceMax,
    double CritMultiplier,
    double DefensePerStamina,
    int MinDamageAfterDefense)
{
    public static CombatModel Default => new(
        BaseHitChance: 0.65,
        HitChancePerAgilityDiff: 0.05,
        MinHitChance: 0.10,
        MaxHitChance: 0.95,
        BaseCritChance: 0.05,
        CritChancePerAgility: 0.02,
        MaxCritChance: 0.40,
        DamageVarianceMin: 0.85,
        DamageVarianceMax: 1.15,
        CritMultiplier: 1.50,
        DefensePerStamina: 0.75,
        MinDamageAfterDefense: 1);

    public void Validate()
    {
        if (MinHitChance < 0 || MinHitChance > 1) throw new ArgumentOutOfRangeException(nameof(MinHitChance));
        if (MaxHitChance < 0 || MaxHitChance > 1) throw new ArgumentOutOfRangeException(nameof(MaxHitChance));
        if (MinHitChance > MaxHitChance) throw new ArgumentException("MinHitChance cannot exceed MaxHitChance.");
        if (BaseHitChance < 0 || BaseHitChance > 1) throw new ArgumentOutOfRangeException(nameof(BaseHitChance));
        if (BaseCritChance < 0 || BaseCritChance > 1) throw new ArgumentOutOfRangeException(nameof(BaseCritChance));
        if (MaxCritChance < 0 || MaxCritChance > 1) throw new ArgumentOutOfRangeException(nameof(MaxCritChance));
        if (DamageVarianceMin < 0) throw new ArgumentOutOfRangeException(nameof(DamageVarianceMin));
        if (DamageVarianceMax < DamageVarianceMin) throw new ArgumentException("DamageVarianceMax cannot be smaller than DamageVarianceMin.");
        if (CritMultiplier < 1.0) throw new ArgumentOutOfRangeException(nameof(CritMultiplier));
        if (DefensePerStamina < 0) throw new ArgumentOutOfRangeException(nameof(DefensePerStamina));
        if (MinDamageAfterDefense < 0) throw new ArgumentOutOfRangeException(nameof(MinDamageAfterDefense));
    }
}
