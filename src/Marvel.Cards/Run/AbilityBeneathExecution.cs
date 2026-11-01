using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityBeneathExecution
{
    /// <summary>The instruction attempted to resolve, and its host had already left play.</summary>
    /// <remarks>
    /// rr:then.1: post-"then" text "must also attempt to resolve". A host can leave play
    /// partway through its own ability, as when the discard before 11021's "then place it
    /// facedown under this card" eliminates the player and step 5 of rr:player-elimination
    /// removes their play area. Nothing can be placed beneath a card that is out of play.
    /// </remarks>
    private static readonly AbilityDeckAndRevealResult Departed =
        new(true, false, null, ImmutableDictionary<string, long>.Empty);

    internal static AbilityDeckAndRevealResult Run(AbilityEffect.StoreDeckTopBeneath store, AbilityDeckAndRevealContext context)
    {
        var selection = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var hostResult = selection.Result(selection.Find(store.Host));
        foreach (var observation in hostResult.Information)
            context.World.RecordInformation(observation);
        var host = hostResult.Value ?? throw new RulesNotImplementedException("stored cards have no bound host");
        if (!DeckTypes.IsInPlay(host.Area.Type)) return Departed;
        var stored = PlayerDeck.StoreTopBeneath(
            context.World, context.Player, store.Count, host, context.Trigger, context.Events);
        return new(true, stored.Count > 0, null, ImmutableDictionary<string, long>.Empty);
    }

    internal static AbilityDeckAndRevealResult Run(AbilityEffect.StoreBeneath store, AbilityDeckAndRevealContext context)
    {
        var selection = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var hostResult = selection.Result(selection.Find(store.Host));
        var cardsResult = selection.Result(selection.Every(store.Cards));
        foreach (var observation in hostResult.Information.Concat(cardsResult.Information))
            context.World.RecordInformation(observation);
        var host = hostResult.Value ?? throw new RulesNotImplementedException("stored cards have no bound host");
        if (!DeckTypes.IsInPlay(host.Area.Type)) return Departed;
        var cards = cardsResult.Value.Where(card => SideSchemeDeparture.Allowed(context.World, card)).ToList();
        int count = Math.Min(store.RandomCount ?? cards.Count, cards.Count);
        for (int index = 0; index < count; index++)
        {
            Card card = store.RandomCount is null ? cards[0] : context.World.Random.Choice(cards);
            cards.Remove(card);
            BeneathCards.Place(context.World, card, host, store.FaceUp, context.Trigger, context.Events);
        }
        return new(true, count > 0, null, ImmutableDictionary<string, long>.Empty);
    }
}
