using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Combines committed child results without predicting later execution.</summary>
internal static class AbilityCompletion
{
    internal static AbilityStructuralOutcome? Combine(
        AbilityStructuralOutcome? left, AbilityStructuralOutcome? right) =>
        left is null ? right : right is null ? left : left == right ? left : AbilityStructuralOutcome.Partial;

    internal static AbilityStructuralOutcome Amount(long applied, long requested) =>
        applied <= 0 || requested <= 0 ? AbilityStructuralOutcome.None
        : applied >= requested ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.Partial;

    private static AbilityStructuralOutcome Moved(IEnumerable<GameEvent> committed, string verb, long requested) =>
        Amount(committed.OfType<CardsMoved>().Where(item => item.Verb == verb).Sum(item => (long)item.Cards.Count), requested);

    internal static AbilityStructuralOutcome? Observe(
        AbilityEffect effect, IReadOnlyList<GameEvent> events, int start, long requested)
    {
        var committed = events.Skip(start);
        return ObserveFieldChanges(effect.OperationName(), committed.OfType<FieldSet>(), requested)
            ?? effect.OperationName() switch
        {
            "placeOnBottomOfDeck" => Moved(committed, "Place_On_Bottom", requested),
            // Equivalent mutant: Salvage (16033), the only placeOnTopOfDeck row, reads no
            // completion of its placement, so observing another verb here survives.
            "placeOnTopOfDeck" => Moved(committed, "Place_On_Top", requested),
            "placeThreat" => AbilityStructuralOutcome.None,
            "giveStatus" => Amount(committed.OfType<CardAttached>().Count(item => item.Verb == "Give_Status"), requested),
            "draw" => Amount(committed.OfType<CardsMoved>().Where(item => item.Verb == "Draw")
                .Sum(item => (long)item.Cards.Count), requested),
            "replaceStatus" => committed.OfType<CardAttached>().Any(item => item.Verb == "Replace_Status")
                ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None,
            "removeStatuses" => committed.OfType<CardsMoved>().Any(item => item.Verb == "Remove_Status")
                ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None,
            "discard" => committed.OfType<CardsMoved>().Any(item => item.Verb == "Discard")
                ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None,
            "changeForm" => committed.OfType<CardsFlipped>().Any(item => item.Verb == "Change_Form")
                ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None,
            _ => null,
        };
    }

    private static AbilityStructuralOutcome? ObserveFieldChanges(
        string operation, IEnumerable<FieldSet> fields, long requested) => operation switch
    {
        "moveActiveCounter" => Amount(fields.Count(item =>
            item.Field == "active_villain" && item.From == 0 && item.To == 1), requested),
        "exhaust" => Amount(fields.Count(item =>
            item.Field == "is_exhaust" && item.From == 0 && item.To == 1), requested),
        "ready" => Amount(fields.Count(item =>
            item.Field == "is_exhaust" && item.From == 1 && item.To == 0), requested),
        _ => null,
    };

    internal static long Requested(AbilityResolutionExecution execution, AbilityEffect effect,
        AbilityResolutionState cast) => effect switch
    {
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.MoveActiveCounter } => 1,
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PlaceOnBottomOfDeck or AbilityCardInstruction.PlaceOnTopOfDeck } => 1,
        AbilityEffect.Draw draw => draw.Count * (long)(draw.Players is AbilityPlayerSelection.AllPlayers ? EachPlayerEffects.Players(cast.World, cast.Source).Count : 1),
        AbilityEffect.GiveStatus status => execution.Every(status.Cards, cast).Count,
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust } action =>
            execution.Every(action.Selection, cast).Count(card => card.Ready),
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Ready } action =>
            execution.Every(action.Selection, cast).Count(card => !card.Ready
                && execution.readinessAbilities.CanReady(cast.World, card, cast.Source)),
        _ => 0,
    };
}

