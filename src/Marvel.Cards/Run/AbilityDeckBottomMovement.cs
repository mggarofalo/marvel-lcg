using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Places one selected card on the resolver's deck without consuming randomness.</summary>
internal static class AbilityDeckBottomMovement
{
    internal static AdmissionResolution Resolution(
        AbilityCardSelection selection, AbilityAdmissionScope cast) =>
        AbilityInitiationPrimitives.Find(selection, cast) is { } card
            && SideSchemeDeparture.Allowed(cast.World, card)
                ? AdmissionResolution.Full : AdmissionResolution.None;

    internal static AbilityDeckAndRevealResult Run(
        AbilityCardSelection selection, AbilityDeckAndRevealContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var found = evaluation.Result(evaluation.Find(selection));
        foreach (var observation in found.Information) context.World.RecordInformation(observation);
        if (found.Value is not { } card || !SideSchemeDeparture.Allowed(context.World, card))
            return new(true, false, null, ImmutableDictionary<string, long>.Empty);
        var deck = context.World.Seats[context.Player].Deck;
        var from = card.Area;
        var ending = context.World.Effects.PreflightConstantsEnding(card);
        using var departure = ending.Begin();
        if (DeckTypes.IsInPlay(from.Type))
            Discard.Attachments(context.World, card, context.Trigger, context.Events);
        CardPlacement.MoveToBottom(card, deck);
        // Engine choice: movement uses a semantic verb and bottom-first landing index.
        context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(deck),
            [new Landing(card.ObjectId, 0)]) { Trigger = context.Trigger, Verb = "Place_On_Bottom" });
        if (from.Host >= 0)
            context.Events.Add(new CardDetached(card.ObjectId, from.Host)
            { Trigger = context.Trigger, Verb = "Place_On_Bottom" });
        ending.Complete(context.Trigger, context.Events);
        return new(true, true, null, ImmutableDictionary<string, long>.Empty);
    }
}
