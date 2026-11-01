using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Describes a checked entry-counter instruction and its payment receipt.</summary>
internal static class AbilityEntryCounterDescription
{
    internal static string? Caption(AbilityEffect effect, string? description) =>
        description is not null && Match(effect) is { } placement
            ? $"Place {placement.Counter} counters" : null;

    internal static string? Summary(AbilityResolutionExecution execution, World world, Card source,
        CompiledCardAbility ability, int player)
    {
        if (ability.Trigger.Timing != AbilityType.Interrupt || Match(ability.Effect) is not { } placement
            || EntryOccurrence(world, source, player) is not { } occurrence) return null;
        long cap = Cap(placement);
        var cast = new AbilityResolutionState(world, source, occurrence, player, []);
        long amount = Math.Min(cap, Energy(placement)
            ? AbilityEntryResourceQuery.EnergyExcess(cast.ExpressionContext())
            : AbilityEntryResourceQuery.Excess(cast.ExpressionContext()));
        string title = EffectiveCards.Title(source, world.Facts);
        string suffix = amount == 1 ? "counter" : "counters";
        return $"Place {amount} {placement.Counter} {suffix} on {title} "
            + $"for overpaid {Kind(placement)}resources (maximum {cap}). Declining this interrupt places no counters."
            + HealthText(world, execution.program, source, placement.Counter, title);
    }

    internal static string? PlaySummary(World world, AbilityProgram program, Card source)
    {
        var ability = AbilityProgramQueries.ActiveOn(world, program, source).FirstOrDefault(IsSimpleEntry);
        if (ability is null || Match(ability.Effect) is not { } placement) return null;
        string title = EffectiveCards.Title(source, world.Facts);
        return $"Optional entry interrupt: place 1 {placement.Counter} counter on {title} "
            + $"per {Kind(placement)}resource overpaid (maximum {Cap(placement)})."
            + HealthText(world, program, source, placement.Counter, title);
    }

    private static bool IsSimpleEntry(CompiledCardAbility ability)
    {
        if (ability.Trigger is not { Event: Steps.CardEntersPlay, Timing: AbilityType.Interrupt, Subject: "this",
            Actor: null, Target: null, Form: null, Also: null, Player: null, Would: false }) return false;
        if (ability.When is not null || ability.Cost is not null || ability.Limit is not null) return false;
        return ability.Maximum is null && ability.Labels.IsEmpty && Match(ability.Effect) is not null;
    }

    private static Occurrence? EntryOccurrence(World world, Card source, int player)
    {
        if (world.Windows.Current?.Occurrence is not { } occurrence
            || !occurrence.Is(Steps.CardEntersPlay) || occurrence.Subject != source.ObjectId
            || occurrence.Entry.EntryPayment is not { } payment) return null;
        return payment.MatchesPending(source, player) || payment.MatchesEntered(source, player)
            ? occurrence : null;
    }

    private static string HealthText(World world, AbilityProgram program, Card source, string counter, string title) =>
        AbilityProgramQueries.ActiveOn(world, program, source).Any(other => GrantsHealth(other, counter))
            ? $" While this card's text is active, each {counter} counter gives {title} +1 hit point." : string.Empty;

    private static bool GrantsHealth(CompiledCardAbility ability, string counter)
    {
        if (ability.Trigger.Timing != AbilityType.Constant || ability.When is not null) return false;
        if (ability.Effect is not AbilityEffect.GrantField
        {
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
            EachCard: false, Until: null, Field: "health", Amount: AbilityNumber.Counters
            { Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This } } counters,
        }) return false;
        return counters.Counter == counter;
    }

    private static bool Energy(AbilityCounterEffect.PlaceCounters placement) =>
        ((AbilityNumber.Minimum)placement.Count).Operands[1] is AbilityNumber.ResolutionValue
            { Kind: AbilityResolutionNumber.EntryEnergyExcess };

    private static string Kind(AbilityCounterEffect.PlaceCounters placement) => Energy(placement) ? "energy " : string.Empty;

    private static long Cap(AbilityCounterEffect.PlaceCounters placement) =>
        ((AbilityNumber.Constant)((AbilityNumber.Minimum)placement.Count).Operands[0]).Value;

    // Only this checked shape has a completed entry-payment receipt and a fixed cap.
    private static AbilityCounterEffect.PlaceCounters? Match(AbilityEffect effect)
    {
        if (effect is not AbilityCounterEffect.PlaceCounters
        {
            Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
            Count: AbilityNumber.Minimum { Operands.Length: 2 } minimum,
        } placement) return null;
        if (minimum.Operands[0] is not AbilityNumber.Constant { Value: > 0 }) return null;
        return minimum.Operands[1] is AbilityNumber.ResolutionValue
            { Kind: AbilityResolutionNumber.EntryResourceExcess or AbilityResolutionNumber.EntryEnergyExcess }
            ? placement : null;
    }
}
