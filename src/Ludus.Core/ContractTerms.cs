namespace Ludus.Core;

/// <summary>
/// Contract configuration for a gladiator.
/// </summary>
public readonly record struct ContractTerms(
    int DailyWage,
    int DurationDays,
    int MaxOverdueDays,
    bool AutoRenew)
{
    public static ContractTerms Default => new(
        DailyWage: 5,
        DurationDays: 7,
        MaxOverdueDays: 3,
        AutoRenew: true);

    public void Validate()
    {
        if (DailyWage < 0)
            throw new ValidationException("DailyWage must be >= 0");
        if (DurationDays < 1)
            throw new ValidationException("DurationDays must be >= 1");
        if (MaxOverdueDays < 1)
            throw new ValidationException("MaxOverdueDays must be >= 1");
    }
}
