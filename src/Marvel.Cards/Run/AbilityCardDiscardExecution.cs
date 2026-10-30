using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityCardStateExecution;

namespace Marvel.Cards.Run;

/// <summary>Executes one explicit multi-card discard instruction and records its receipt.</summary>
internal static class AbilityCardDiscardExecution
{
    internal static void DiscardAtRandom(AbilityEffect.DiscardAtRandom discard, AbilityCardStateContext context)
    {
        long count = Amount(discard.Count, context); long discarded = 0; var types = new SortedSet<char>();
        foreach (int player in Players(discard.Players, context))
        {
            int start = context.Events.Count;
            var hand = context.World.Seats[player].Hand;
            for (long gone = 0; gone < count && hand.Cards.Count > 0; gone++)
            {
                var card = context.World.Random.Choice(hand.Cards);
                types.UnionWith(Resources.GeneratedBy(card.FaceId, context.World.Facts));
                Rules.Play.Discard.Card(context.World, card, context.Trigger, context.Events);
                context.Result.Discarded.Add(card);
                discarded++;
            }
            ActivityBatches.Complete(context.Events, start, "Discard", player);
        }
        context.Result.Values["discarded"] = discarded; context.Result.Values["resourceTypes"] = types.Count;
    }

    internal static void DiscardTop(AbilityEffect.DiscardTop discard, AbilityCardStateContext context)
    {
        // One instruction fixes its quantity before any deck changes or reset work.
        long count = Amount(discard.Count, context);
        if (discard.Players is null && discard.From == AbilitySearchArea.EncounterDeck)
        {
            context.Result.Discarded.AddRange(EncounterDeck.DiscardTop(
                context.World, count, context.Trigger, context.Events));
            return;
        }
        IEnumerable<Area> decks = discard.Players is { } players
            ? Players(players, context).Select(player => context.World.Seats[player].Deck)
            : [Area(discard.From, context)];
        foreach (var deck in decks)
        {
            if (deck.Type == DeckType.PlayerDeck && deck.PlayArea.IsPlayers)
            {
                context.Result.Discarded.AddRange(PlayerDeck.DiscardTop(
                    context.World, deck.PlayArea.Player, count, context.Trigger, context.Events));
                continue;
            }
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' discards from unsupported deck {deck.Type}");
        }
    }

    internal static void DiscardHandWithResource(AbilityEffect.DiscardHandWithResource discard, AbilityCardStateContext context) =>
        DiscardHandWhere(card => Resources.GeneratedBy(card.FaceId, context.World.Facts).Contains(discard.Resource), context);

    internal static void DiscardHandOfKind(AbilityEffect.DiscardHandOfKind discard, AbilityCardStateContext context) =>
        DiscardHandWhere(card => context.World.Facts.Kind(card.FaceId) == discard.Kind, context);

    private static void DiscardHandWhere(Func<Card, bool> matches, AbilityCardStateContext context)
    {
        int start = context.Events.Count;
        foreach (var card in context.World.Seats[context.Player].Hand.Cards.Where(matches).ToList())
        { Rules.Play.Discard.Card(context.World, card, context.Trigger, context.Events); context.Result.Discarded.Add(card); }
        ActivityBatches.Complete(context.Events, start, "Discard", context.Player);
        // `result.discarded` is resolution evidence: cards discarded by an
        // earlier node remain visible to later nodes. The owner sees that
        // immutable prior evidence through its expression context and adds
        // only this operation's new discards.
        context.Result.Values["discarded"] = context.Expressions.Discarded.Length
            + context.Result.Discarded.Count;
    }
}
