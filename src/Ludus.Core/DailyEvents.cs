using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludus.Core;

public enum DailyEventType
{
    SponsorDeal = 0,
    HarshDrill = 1,
    TavernRumor = 2
}

public enum DailyEventOptionId
{
    OptionA = 0,
    OptionB = 1
}

public readonly record struct DailyEventOption(
    DailyEventOptionId Id,
    string Label,
    string Description)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Label))
            throw new ValidationException("DailyEventOption.Label must be non-empty");

        if (string.IsNullOrWhiteSpace(Description))
            throw new ValidationException("DailyEventOption.Description must be non-empty");
    }
}

public readonly record struct DailyEventInstance(
    DailyEventType Type,
    string Title,
    string Description,
    DailyEventOption OptionA,
    DailyEventOption OptionB,
    Guid? TargetGladiatorId = null)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new ValidationException("DailyEventInstance.Title must be non-empty");

        if (string.IsNullOrWhiteSpace(Description))
            throw new ValidationException("DailyEventInstance.Description must be non-empty");

        OptionA.Validate();
        OptionB.Validate();
    }

    public DailyEventOption GetOption(DailyEventOptionId optionId) => optionId switch
    {
        DailyEventOptionId.OptionA => OptionA,
        DailyEventOptionId.OptionB => OptionB,
        _ => throw new ValidationException($"Unsupported daily event option: {optionId}")
    };
}

public readonly record struct DailyEventResolution(
    DailyEventType Type,
    DailyEventOptionId SelectedOption,
    int MoneyDelta,
    string Summary);

public static class DailyEventResolver
{
    public static DailyEventInstance RollEvent(LudusState state, IRng rng)
    {
        var alive = state.Gladiators.Where(g => g.IsAlive).ToArray();
        int roll = rng.Next(3);

        if (roll == 0)
            return CreateSponsorDeal();

        if (roll == 1 && alive.Length > 0)
        {
            var target = alive[rng.Next(alive.Length)];
            return CreateHarshDrill(target.Id, target.Name);
        }

        return CreateTavernRumor();
    }

    public static (LudusState State, DailyEventResolution Resolution) ApplyChoice(
        LudusState state,
        DailyEventInstance dailyEvent,
        DailyEventOptionId optionId)
    {
        dailyEvent.Validate();
        _ = dailyEvent.GetOption(optionId);

        return dailyEvent.Type switch
        {
            DailyEventType.SponsorDeal => ResolveSponsorDeal(state, dailyEvent.Type, optionId),
            DailyEventType.HarshDrill => ResolveHarshDrill(state, dailyEvent, optionId),
            DailyEventType.TavernRumor => ResolveTavernRumor(state, dailyEvent.Type, optionId),
            _ => throw new ValidationException($"Unsupported daily event type: {dailyEvent.Type}")
        };
    }

    private static DailyEventInstance CreateSponsorDeal()
    {
        return new DailyEventInstance(
            Type: DailyEventType.SponsorDeal,
            Title: "Merchant's Sponsor Deal",
            Description: "A wealthy merchant offers coin for putting your ludus banner on his stalls.",
            OptionA: new DailyEventOption(
                DailyEventOptionId.OptionA,
                "Take the coin",
                "+40 money, -3 morale for all living gladiators"),
            OptionB: new DailyEventOption(
                DailyEventOptionId.OptionB,
                "Keep your pride",
                "+2 morale for all living gladiators"));
    }

    private static DailyEventInstance CreateHarshDrill(Guid targetId, string targetName)
    {
        return new DailyEventInstance(
            Type: DailyEventType.HarshDrill,
            Title: "Harsh Drill Proposal",
            Description: $"Your trainer asks to push {targetName} through a punishing drill.",
            OptionA: new DailyEventOption(
                DailyEventOptionId.OptionA,
                "Push harder",
                "Target gets +20 fatigue and -8 morale"),
            OptionB: new DailyEventOption(
                DailyEventOptionId.OptionB,
                "Allow recovery",
                "Target gets -15 fatigue and +4 morale"),
            TargetGladiatorId: targetId);
    }

    private static DailyEventInstance CreateTavernRumor()
    {
        return new DailyEventInstance(
            Type: DailyEventType.TavernRumor,
            Title: "Tavern Rumor",
            Description: "Rumors spread that your ludus is entering a dangerous rivalry.",
            OptionA: new DailyEventOption(
                DailyEventOptionId.OptionA,
                "Pay for information",
                "-20 money, +5 morale for all living gladiators"),
            OptionB: new DailyEventOption(
                DailyEventOptionId.OptionB,
                "Ignore the gossip",
                "+5 fatigue for all living gladiators"));
    }

    private static (LudusState State, DailyEventResolution Resolution) ResolveSponsorDeal(
        LudusState state,
        DailyEventType type,
        DailyEventOptionId optionId)
    {
        return optionId switch
        {
            DailyEventOptionId.OptionA => ApplyAllAlive(
                state,
                type,
                moneyDelta: 40,
                moraleDelta: -3,
                fatigueDelta: 0,
                optionId,
                "Accepted sponsor money. The roster grumbles about pride."),
            DailyEventOptionId.OptionB => ApplyAllAlive(
                state,
                type,
                moneyDelta: 0,
                moraleDelta: 2,
                fatigueDelta: 0,
                optionId,
                "Refused sponsorship. Gladiators respect the decision."),
            _ => throw new ValidationException($"Unsupported daily event option: {optionId}")
        };
    }

    private static (LudusState State, DailyEventResolution Resolution) ResolveHarshDrill(
        LudusState state,
        DailyEventInstance dailyEvent,
        DailyEventOptionId optionId)
    {
        if (!dailyEvent.TargetGladiatorId.HasValue)
        {
            throw new ValidationException("HarshDrill event requires TargetGladiatorId");
        }

        Guid targetId = dailyEvent.TargetGladiatorId.Value;
        var target = state.Gladiators.FirstOrDefault(g => g.Id == targetId);
        if (target.Id == Guid.Empty)
            throw new ValidationException($"Daily event target not found: {targetId}");

        if (!target.IsAlive)
            throw new ValidationException("Daily event target must be alive");

        return optionId switch
        {
            DailyEventOptionId.OptionA => ApplySingleTarget(
                state,
                dailyEvent.Type,
                targetId,
                moneyDelta: 0,
                moraleDelta: -8,
                fatigueDelta: 20,
                optionId,
                $"{target.Name} was pushed hard in training."),
            DailyEventOptionId.OptionB => ApplySingleTarget(
                state,
                dailyEvent.Type,
                targetId,
                moneyDelta: 0,
                moraleDelta: 4,
                fatigueDelta: -15,
                optionId,
                $"{target.Name} received a recovery day."),
            _ => throw new ValidationException($"Unsupported daily event option: {optionId}")
        };
    }

    private static (LudusState State, DailyEventResolution Resolution) ResolveTavernRumor(
        LudusState state,
        DailyEventType type,
        DailyEventOptionId optionId)
    {
        return optionId switch
        {
            DailyEventOptionId.OptionA => ApplyAllAlive(
                state,
                type,
                moneyDelta: -20,
                moraleDelta: 5,
                fatigueDelta: 0,
                optionId,
                "You bought information. Confidence rises at a small cost."),
            DailyEventOptionId.OptionB => ApplyAllAlive(
                state,
                type,
                moneyDelta: 0,
                moraleDelta: 0,
                fatigueDelta: 5,
                optionId,
                "You ignored the rumor. Tension lingers in the barracks."),
            _ => throw new ValidationException($"Unsupported daily event option: {optionId}")
        };
    }

    private static (LudusState State, DailyEventResolution Resolution) ApplyAllAlive(
        LudusState state,
        DailyEventType type,
        int moneyDelta,
        int moraleDelta,
        int fatigueDelta,
        DailyEventOptionId optionId,
        string summary)
    {
        var updatedGladiators = state.Gladiators
            .Select(g => !g.IsAlive
                ? g
                : g.WithMorale(g.Morale + moraleDelta).WithFatigue(g.Fatigue + fatigueDelta))
            .ToArray();

        var updatedState = state with
        {
            Money = state.Money + moneyDelta,
            Gladiators = updatedGladiators
        };

        var resolution = new DailyEventResolution(
            Type: type,
            SelectedOption: optionId,
            MoneyDelta: moneyDelta,
            Summary: summary);

        return (updatedState, resolution);
    }

    private static (LudusState State, DailyEventResolution Resolution) ApplySingleTarget(
        LudusState state,
        DailyEventType type,
        Guid targetId,
        int moneyDelta,
        int moraleDelta,
        int fatigueDelta,
        DailyEventOptionId optionId,
        string summary)
    {
        var updatedGladiators = state.Gladiators
            .Select(g =>
            {
                if (g.Id != targetId || !g.IsAlive)
                    return g;

                return g.WithMorale(g.Morale + moraleDelta).WithFatigue(g.Fatigue + fatigueDelta);
            })
            .ToArray();

        var updatedState = state with
        {
            Money = state.Money + moneyDelta,
            Gladiators = updatedGladiators
        };

        var resolution = new DailyEventResolution(
            Type: type,
            SelectedOption: optionId,
            MoneyDelta: moneyDelta,
            Summary: summary);

        return (updatedState, resolution);
    }
}
