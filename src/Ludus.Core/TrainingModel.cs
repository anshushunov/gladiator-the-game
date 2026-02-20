namespace Ludus.Core;

/// <summary>
/// Модель баланса тренировок: вероятность прироста стата за день.
/// </summary>
public readonly record struct TrainingModel(double StatGainChance)
{
    public static TrainingModel Default => new(StatGainChance: 0.50);

    public void Validate()
    {
        if (StatGainChance < 0.0 || StatGainChance > 1.0)
            throw new ValidationException(
                $"StatGainChance должен быть в диапазоне [0.0, 1.0], получено: {StatGainChance}");
    }
}
