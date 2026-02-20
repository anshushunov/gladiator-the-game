using System;

namespace Ludus.Core;

/// <summary>
/// Чистые функции расчёта эффективности и обновления морали/усталости.
/// </summary>
public static class ConditionResolver
{
    /// <summary>
    /// Рассчитывает множитель эффективности на основе морали и усталости.
    /// Линейная интерполяция: 1.0 + moraleModifier + fatigueModifier.
    /// </summary>
    public static double GetEfficiency(int morale, int fatigue, ConditionModel model)
    {
        // morale: 0 → LowMoralePenalty, 100 → HighMoraleBonus
        double moraleFraction = morale / (double)ConditionModel.MaxMorale;
        double moraleModifier = model.LowMoralePenalty + moraleFraction * (model.HighMoraleBonus - model.LowMoralePenalty);

        // fatigue: 0 → LowFatigueBonus, 100 → HighFatiguePenalty
        double fatigueFraction = fatigue / (double)ConditionModel.MaxFatigue;
        double fatigueModifier = model.LowFatigueBonus + fatigueFraction * (model.HighFatiguePenalty - model.LowFatigueBonus);

        return 1.0 + moraleModifier + fatigueModifier;
    }

    /// <summary>
    /// Применяет результат боя к морали/усталости гладиатора.
    /// </summary>
    public static Gladiator ApplyFightOutcome(Gladiator gladiator, bool isWinner, ConditionModel model)
    {
        int moraleChange = isWinner ? model.MoraleWinBonus : model.MoraleLosePenalty;
        int newMorale = Math.Clamp(gladiator.Morale + moraleChange, ConditionModel.MinMorale, ConditionModel.MaxMorale);
        int newFatigue = Math.Clamp(gladiator.Fatigue + model.FatigueFightGain, ConditionModel.MinFatigue, ConditionModel.MaxFatigue);

        return gladiator.WithMorale(newMorale).WithFatigue(newFatigue);
    }

    /// <summary>
    /// Применяет дневной тик морали/усталости.
    /// Тренирующийся (не травмированный): fatigue += FatigueTrainingGain + FatigueDailyTrainingRecovery, morale += MoraleDailyTrainingDrain.
    /// Отдыхающий/травмированный: fatigue += FatigueDailyRestRecovery, morale += MoraleDailyRestBonus.
    /// </summary>
    public static Gladiator ApplyDailyTick(Gladiator gladiator, ConditionModel model)
    {
        int moraleChange;
        int fatigueChange;

        if (gladiator.CurrentTraining.HasValue && !gladiator.IsInjured)
        {
            fatigueChange = model.FatigueTrainingGain + model.FatigueDailyTrainingRecovery;
            moraleChange = model.MoraleDailyTrainingDrain;
        }
        else
        {
            fatigueChange = model.FatigueDailyRestRecovery;
            moraleChange = model.MoraleDailyRestBonus;
        }

        int newMorale = Math.Clamp(gladiator.Morale + moraleChange, ConditionModel.MinMorale, ConditionModel.MaxMorale);
        int newFatigue = Math.Clamp(gladiator.Fatigue + fatigueChange, ConditionModel.MinFatigue, ConditionModel.MaxFatigue);

        return gladiator.WithMorale(newMorale).WithFatigue(newFatigue);
    }

    /// <summary>
    /// Применяет штраф морали за получение травмы.
    /// </summary>
    public static Gladiator ApplyInjuryMoralePenalty(Gladiator gladiator, ConditionModel model)
    {
        int newMorale = Math.Clamp(gladiator.Morale + model.MoraleInjuryPenalty, ConditionModel.MinMorale, ConditionModel.MaxMorale);
        return gladiator.WithMorale(newMorale);
    }
}
