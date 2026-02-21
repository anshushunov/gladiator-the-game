namespace Ludus.Core;

/// <summary>
/// Mutable-by-copy runtime contract state.
/// </summary>
public readonly record struct ContractState(
    ContractTerms Terms,
    int DaysRemaining,
    int OverdueDays)
{
    public static ContractState Default => FromTerms(ContractTerms.Default);

    public bool IsExpired => DaysRemaining <= 0;

    public bool IsOverdueLimitReached => OverdueDays >= Terms.MaxOverdueDays;

    public static ContractState FromTerms(ContractTerms terms)
    {
        terms.Validate();
        return new ContractState(terms, terms.DurationDays, 0);
    }

    public void Validate()
    {
        Terms.Validate();

        if (DaysRemaining < 0)
            throw new ValidationException("DaysRemaining must be >= 0");
        if (OverdueDays < 0)
            throw new ValidationException("OverdueDays must be >= 0");
    }

    public ContractState TickDay()
    {
        int nextDaysRemaining = DaysRemaining > 0 ? DaysRemaining - 1 : 0;
        return this with { DaysRemaining = nextDaysRemaining };
    }

    public ContractState RenewIfNeeded()
    {
        if (!IsExpired || !Terms.AutoRenew)
            return this;

        return this with { DaysRemaining = Terms.DurationDays };
    }

    public ContractState MarkOverdueDay()
    {
        return this with { OverdueDays = OverdueDays + 1 };
    }

    public ContractState ClearOverdueIfPaid()
    {
        if (OverdueDays == 0)
            return this;

        return this with { OverdueDays = 0 };
    }
}
