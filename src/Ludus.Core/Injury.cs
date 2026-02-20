namespace Ludus.Core;

/// <summary>
/// Травма гладиатора с оставшимися днями восстановления.
/// </summary>
public readonly record struct Injury
{
    public InjuryType Type { get; }
    public int RecoveryDaysLeft { get; }

    public Injury(InjuryType type, int recoveryDaysLeft)
    {
        if (recoveryDaysLeft < 1)
            throw new ValidationException(
                $"RecoveryDaysLeft должен быть >= 1, получено: {recoveryDaysLeft}");

        Type = type;
        RecoveryDaysLeft = recoveryDaysLeft;
    }

    /// <summary>
    /// Уменьшает оставшиеся дни на 1. Возвращает null если травма полностью вылечена.
    /// </summary>
    public Injury? Tick()
    {
        int remaining = RecoveryDaysLeft - 1;
        return remaining <= 0 ? null : new Injury(Type, remaining);
    }
}
