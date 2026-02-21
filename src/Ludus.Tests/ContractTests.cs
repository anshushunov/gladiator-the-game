using System.Linq;
using Ludus.Core;
using Xunit;

namespace Ludus.Tests;

public class ContractTermsTests
{
    [Fact]
    public void Validate_InvalidDailyWage_ShouldThrow()
    {
        var terms = new ContractTerms(-1, 7, 3, true);
        Assert.Throws<ValidationException>(() => terms.Validate());
    }

    [Fact]
    public void Validate_InvalidDuration_ShouldThrow()
    {
        var terms = new ContractTerms(5, 0, 3, true);
        Assert.Throws<ValidationException>(() => terms.Validate());
    }

    [Fact]
    public void Validate_InvalidMaxOverdueDays_ShouldThrow()
    {
        var terms = new ContractTerms(5, 7, 0, true);
        Assert.Throws<ValidationException>(() => terms.Validate());
    }

    [Fact]
    public void Validate_ValidTerms_ShouldPass()
    {
        var terms = new ContractTerms(5, 7, 3, true);
        terms.Validate();
    }
}

public class ContractStateTests
{
    [Fact]
    public void TickDay_ShouldDecreaseDaysRemaining()
    {
        var state = ContractState.FromTerms(new ContractTerms(5, 7, 3, true));
        var ticked = state.TickDay();
        Assert.Equal(6, ticked.DaysRemaining);
    }

    [Fact]
    public void RenewIfNeeded_ShouldRenewWhenExpiredAndAutoRenewEnabled()
    {
        var terms = new ContractTerms(5, 7, 3, true);
        var state = new ContractState(terms, 0, 0);
        var renewed = state.RenewIfNeeded();
        Assert.Equal(7, renewed.DaysRemaining);
    }

    [Fact]
    public void MarkAndClearOverdue_ShouldUpdateOverdueDays()
    {
        var state = ContractState.Default;
        var overdue = state.MarkOverdueDay().MarkOverdueDay();
        Assert.Equal(2, overdue.OverdueDays);

        var cleared = overdue.ClearOverdueIfPaid();
        Assert.Equal(0, cleared.OverdueDays);
    }
}

public class GladiatorContractTests
{
    [Fact]
    public void Create_ShouldHaveDefaultContract()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5));
        Assert.Equal(ContractTerms.Default.DailyWage, g.Contract.Terms.DailyWage);
        Assert.Equal(ContractTerms.Default.DurationDays, g.Contract.DaysRemaining);
        Assert.Equal(0, g.Contract.OverdueDays);
    }

    [Fact]
    public void TickContractDay_ShouldDecreaseDays()
    {
        var g = Gladiator.Create("Test", new Stats(5, 5, 5));
        var updated = g.TickContractDay();
        Assert.Equal(g.Contract.DaysRemaining - 1, updated.Contract.DaysRemaining);
    }

    [Fact]
    public void RenewContractIfNeeded_ShouldRenewExpiredContract()
    {
        var terms = new ContractTerms(5, 9, 3, true);
        var g = Gladiator.Create("Test", new Stats(5, 5, 5))
            .WithContract(new ContractState(terms, 0, 0));

        var renewed = g.RenewContractIfNeeded();
        Assert.Equal(9, renewed.Contract.DaysRemaining);
    }
}

public class LudusStateContractsTests
{
    private static LudusState AdvanceAndResolve(LudusState state)
    {
        return state.AdvanceDay().ResolveDailyEvent(DailyEventOptionId.OptionB);
    }

    [Fact]
    public void AdvanceDay_ShouldDeductDailyWages()
    {
        var state = LudusState.NewGame(42).HireRandomGladiator().HireRandomGladiator();
        int wages = state.Gladiators.Where(g => g.IsAlive).Sum(g => g.Contract.Terms.DailyWage);

        var next = state.AdvanceDay();
        Assert.Equal(state.Money - wages, next.Money);
    }

    [Fact]
    public void AdvanceDay_NegativeMoney_ShouldIncreaseOverdueDays()
    {
        var terms = new ContractTerms(30, 7, 3, true);
        var g = Gladiator.Create("Debt", new Stats(5, 5, 5))
            .WithContract(ContractState.FromTerms(terms));
        var state = LudusState.NewGame(42) with { Money = 20 };
        state = state.AddGladiator(g);

        var next = state.AdvanceDay();
        var updated = next.GetGladiator(g.Id);
        Assert.Equal(1, updated.Contract.OverdueDays);
        Assert.True(next.Money < 0);
    }

    [Fact]
    public void AdvanceDay_SufficientMoney_ShouldClearOverdueDays()
    {
        var terms = new ContractTerms(5, 7, 3, true);
        var g = Gladiator.Create("Paid", new Stats(5, 5, 5))
            .WithContract(new ContractState(terms, 7, 2));
        var state = LudusState.NewGame(42) with { Money = 100 };
        state = state.AddGladiator(g);

        var next = state.AdvanceDay();
        var updated = next.GetGladiator(g.Id);
        Assert.Equal(0, updated.Contract.OverdueDays);
    }

    [Fact]
    public void AdvanceDay_AfterThreeOverdueDays_ShouldRemoveGladiator()
    {
        var terms = new ContractTerms(20, 7, 3, true);
        var g = Gladiator.Create("Leave", new Stats(5, 5, 5))
            .WithContract(ContractState.FromTerms(terms));
        var state = LudusState.NewGame(42) with { Money = 0 };
        state = state.AddGladiator(g);

        var day1 = AdvanceAndResolve(state);
        var day2 = AdvanceAndResolve(day1);
        var day3 = AdvanceAndResolve(day2);

        Assert.DoesNotContain(day3.Gladiators, x => x.Id == g.Id);
    }

    [Fact]
    public void AdvanceDay_ShouldAutoRenewExpiredContract()
    {
        var terms = new ContractTerms(5, 2, 3, true);
        var g = Gladiator.Create("Renew", new Stats(5, 5, 5))
            .WithContract(new ContractState(terms, 1, 0));
        var state = LudusState.NewGame(42).AddGladiator(g);

        var next = state.AdvanceDay();
        var updated = next.GetGladiator(g.Id);
        Assert.Equal(terms.DurationDays, updated.Contract.DaysRemaining);
    }

    [Fact]
    public void AdvanceDay_Determinism_WithSameSeed_ShouldKeepContractStateDeterministic()
    {
        var seed = 1337;
        var terms = new ContractTerms(12, 4, 3, true);

        var g1 = Gladiator.Create("A", new Stats(8, 6, 8))
            .WithContract(ContractState.FromTerms(terms));
        var g2 = new Gladiator(g1.Id, g1.Name, g1.Stats, g1.Health, g1.MaxHealth,
            g1.CurrentTraining, g1.CurrentInjury, g1.Morale, g1.Fatigue, g1.Contract);

        var s1 = LudusState.NewGame(seed).AddGladiator(g1);
        var s2 = LudusState.NewGame(seed).AddGladiator(g2);

        var n1 = s1.AdvanceDay();
        var n2 = s2.AdvanceDay();

        var u1 = n1.GetGladiator(g1.Id);
        var u2 = n2.GetGladiator(g2.Id);

        Assert.Equal(n1.Money, n2.Money);
        Assert.Equal(u1.Contract, u2.Contract);
        Assert.Equal(n1.Seed, n2.Seed);
    }
}
