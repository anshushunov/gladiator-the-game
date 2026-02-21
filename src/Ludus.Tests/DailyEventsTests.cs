using System;
using System.Linq;
using Ludus.Core;
using Xunit;

namespace Ludus.Tests;

public class DailyEventsTests
{
    [Fact]
    public void AdvanceDay_ShouldCreatePendingDailyEvent()
    {
        var state = LudusState.NewGame(42).HireRandomGladiator();

        var next = state.AdvanceDay();

        Assert.True(next.PendingDailyEvent.HasValue);
        Assert.False(next.LastDailyEventResolution.HasValue);
    }

    [Fact]
    public void AdvanceDay_WithPendingDailyEvent_ShouldThrow()
    {
        var state = LudusState.NewGame(42).HireRandomGladiator();
        var next = state.AdvanceDay();

        Assert.Throws<ValidationException>(() => next.AdvanceDay());
    }

    [Fact]
    public void ResolveDailyEvent_ShouldApplySelectedOptionAndClearPending()
    {
        var state = LudusState.NewGame(42).HireRandomGladiator();
        var next = state.AdvanceDay();

        var resolved = next.ResolveDailyEvent(DailyEventOptionId.OptionA);

        Assert.False(resolved.PendingDailyEvent.HasValue);
        Assert.True(resolved.LastDailyEventResolution.HasValue);
        Assert.Equal(DailyEventOptionId.OptionA, resolved.LastDailyEventResolution!.Value.SelectedOption);
    }

    [Fact]
    public void ResolveDailyEvent_OptionB_ShouldPreserveMoneyForCurrentV1Pool()
    {
        var state = LudusState.NewGame(42).HireRandomGladiator();
        var next = state.AdvanceDay();
        int moneyBefore = next.Money;

        var resolved = next.ResolveDailyEvent(DailyEventOptionId.OptionB);

        Assert.Equal(moneyBefore, resolved.Money);
        Assert.Equal(0, resolved.LastDailyEventResolution!.Value.MoneyDelta);
    }

    [Fact]
    public void DailyEvents_Determinism_SameSeedAndChoices_ShouldMatch()
    {
        var s1 = LudusState.NewGame(2026).HireRandomGladiator();
        var s2 = LudusState.NewGame(2026).HireRandomGladiator();

        var d1 = s1.AdvanceDay();
        var d2 = s2.AdvanceDay();

        Assert.Equal(d1.PendingDailyEvent!.Value.Type, d2.PendingDailyEvent!.Value.Type);
        Assert.Equal(d1.PendingDailyEvent!.Value.TargetGladiatorId.HasValue, d2.PendingDailyEvent!.Value.TargetGladiatorId.HasValue);

        var r1 = d1.ResolveDailyEvent(DailyEventOptionId.OptionA);
        var r2 = d2.ResolveDailyEvent(DailyEventOptionId.OptionA);

        Assert.Equal(r1.Money, r2.Money);
        Assert.Equal(r1.Seed, r2.Seed);
        Assert.Equal(r1.LastDailyEventResolution!.Value.Type, r2.LastDailyEventResolution!.Value.Type);
        Assert.Equal(r1.LastDailyEventResolution!.Value.MoneyDelta, r2.LastDailyEventResolution!.Value.MoneyDelta);
    }

    [Fact]
    public void DailyEvents_WithNoAliveGladiators_ShouldNotRequireTarget()
    {
        var dead = Gladiator.Create("Dead", new Stats(5, 5, 5)).TakeDamage(1000);
        var state = LudusState.NewGame(42).AddGladiator(dead);

        var next = state.AdvanceDay();
        var evt = next.PendingDailyEvent!.Value;

        Assert.False(evt.TargetGladiatorId.HasValue);
        var resolved = next.ResolveDailyEvent(DailyEventOptionId.OptionB);
        Assert.True(resolved.LastDailyEventResolution.HasValue);
    }

    [Fact]
    public void ResolveDailyEvent_ThenAdvanceDay_ShouldCreateNewPendingEvent()
    {
        var state = LudusState.NewGame(42).HireRandomGladiator();
        var day1 = state.AdvanceDay().ResolveDailyEvent(DailyEventOptionId.OptionB);

        var day2 = day1.AdvanceDay();

        Assert.True(day2.PendingDailyEvent.HasValue);
        Assert.False(day2.LastDailyEventResolution.HasValue);
        Assert.Equal(day1.Day + 1, day2.Day);
    }
}
