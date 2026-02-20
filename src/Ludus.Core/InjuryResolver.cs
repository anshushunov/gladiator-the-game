namespace Ludus.Core;

/// <summary>
/// Определяет и применяет травмы после боя.
/// </summary>
public static class InjuryResolver
{
    /// <summary>
    /// Вычисляет и применяет травму гладиатору на основе полученного урона.
    /// </summary>
    /// <param name="gladiator">Гладиатор после боя.</param>
    /// <param name="originalMaxHealth">MaxHealth до боя (для расчёта % урона).</param>
    /// <param name="isWinner">Победитель или проигравший.</param>
    /// <param name="rng">Генератор случайных чисел.</param>
    /// <param name="model">Балансовые параметры травм.</param>
    public static Gladiator ResolveInjury(Gladiator gladiator, int originalMaxHealth,
        bool isWinner, IRng rng, InjuryModel model)
    {
        // Мёртвые не получают травм (они уже мертвы)
        if (!gladiator.IsAlive)
            return gladiator;

        double damageFraction = (double)(originalMaxHealth - gladiator.Health) / originalMaxHealth;

        double injuryChance = isWinner ? model.BaseInjuryChanceWinner : model.BaseInjuryChanceLoser;

        if (rng.NextDouble() >= injuryChance)
            return gladiator;

        InjuryType type;
        int days;

        if (damageFraction >= model.FractureThreshold)
        {
            type = InjuryType.Fracture;
            days = model.FractureDays;
        }
        else if (damageFraction >= model.SprainThreshold)
        {
            type = InjuryType.Sprain;
            days = model.SprainDays;
        }
        else
        {
            type = InjuryType.Bruise;
            days = model.BruiseDays;
        }

        return gladiator.ApplyInjury(new Injury(type, days));
    }
}
