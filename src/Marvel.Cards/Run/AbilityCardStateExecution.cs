using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Executes card effects whose only authority is live card state.</summary>
/// <remarks>The runner retains prompts, agenda continuations, and resolution-ledger
/// bookkeeping. This owner receives immutable expression bindings and emits only
/// committed state changes and semantic events.</remarks>
internal static class AbilityCardStateExecution
{
    // The structural owner supplies an already-validated, ordered selection;
    // this owner alone performs the resulting deck and card-state mutations.
    internal static void ChooseTopForHand(
        IReadOnlyList<Card> top, Card selected, AbilityCardStateContext context)
    {
        var deck = context.World.Seats[context.Player].Deck;
        var hand = context.World.Seats[context.Player].Hand;
        foreach (var card in top)
        {
            if (ReferenceEquals(card, selected))
            {
                World.MoveToTop(card, hand);
                context.Events.Add(new CardsMoved(
                    Places.Reference(deck), Places.Reference(hand),
                    [new Landing(card.ObjectId, hand.Cards.Count - 1)])
                { Trigger = context.Trigger, Verb = "Add_To_Hand" });
            }
            else
            {
                Rules.Play.Discard.Card(context.World, card, context.Trigger, context.Events);
            }
        }
    }

    internal static void ShuffleDiscardIntoDeck(
        IReadOnlyList<Card> selected, AbilityCardStateContext context)
    {
        var deck = context.World.Seats[context.Player].Deck;
        foreach (var card in selected)
            World.MoveToTop(card, deck);
        context.World.Shuffle(deck);
    }

    internal static void DiscardCards(IReadOnlyList<Card> cards, string verb, AbilityCardStateContext context)
    {
        foreach (var card in cards)
            Rules.Play.Discard.Card(context.World, card, verb, context.Events);
    }

    internal static void PutIntoPlay(Card card, int player, AbilityCardStateContext context)
    {
        if (Uniqueness.IsBlocked(context.World, context.World.Facts, card, PlayArea.Of(player)))
        {
            if (card.Area.Type != DeckType.EncounterDiscardPile)
                Rules.Play.Discard.Card(context.World, card, context.Trigger, context.Events);
            return;
        }
        var from = card.Area;
        var into = context.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player));
        World.MoveToTop(card, into);
        context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)]) { Trigger = context.Trigger, Verb = "Put_Into_Play" });
        Reveal.EnterPlay(context.World, context.World.Facts, card, context.Events);
    }
    internal static bool TryRun(AbilityEffect effect, AbilityCardStateContext context)
    {
        switch (effect)
        {
            case AbilityEffect.PlaceCounters counters: PlaceCounters(counters, context); return true;
            case AbilityEffect.RemoveCounters counters: RemoveCounters(counters, context); return true;
            case AbilityEffect.GiveStatus status: GiveStatus(status, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust } exhaust:
                Exhaust(exhaust.Selection, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Ready } ready:
                Ready(ready.Selection, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Discard } discard:
                Discard(discard.Selection, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.RemoveFromGame } removal:
                RemoveFromGame(removal.Selection, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.AddToHand } added:
                AddToHand(added.Selection, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ReturnOwnedToHand } returned:
                ReturnOwnedToHand(returned.Selection, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ReturnToHand } returned:
                ReturnToHand(returned.Selection, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.AttachTo } attachment:
                AttachTo(attachment.Selection, context); return true;
            case AbilityEffect.PlaceAtRandom placement: PlaceAtRandom(placement, context); return true;
            case AbilityEffect.DiscardAtRandom discard: DiscardAtRandom(discard, context); return true;
            case AbilityEffect.DiscardTop discard: DiscardTop(discard, context); return true;
            case AbilityEffect.DiscardHandWithResource discard: DiscardHandWithResource(discard, context); return true;
            default: return false;
        }
    }

    private static void PlaceCounters(AbilityEffect.PlaceCounters counters, AbilityCardStateContext context)
    {
        var card = Find(counters.Card, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the card receiving counters");
        long count = Amount(counters.Count, context);
        if (count < 0) throw new AbilityException("'placeCounters' needs a non-negative 'count'");
        if (count == 0) return;
        string key = "c_" + counters.Counter;
        long before = card.Tokens.GetValueOrDefault(key);
        card.PlaceTokens(key, count);
        context.Events.Add(new FieldSet(card.ObjectId, key, before, before + count)
        { Trigger = context.Trigger, Verb = "Place_Counters" });
    }

    private static void RemoveCounters(AbilityEffect.RemoveCounters removal, AbilityCardStateContext context)
    {
        var card = Find(removal.Card, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the card paying its counter cost");
        AbilityCardOperations.RemoveCounters(context.World, context.Pools, card, removal.Counter,
            removal.Count, context.Trigger, context.Events);
    }

    private static void Exhaust(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        foreach (var card in Every(selection, context))
            AbilityCardOperations.Exhaust(card, context.Trigger, context.Events);
    }

    private static void Ready(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        foreach (var card in Every(selection, context).Where(card => card.Ready == false
                     && context.World.Abilities.CanReady(context.World, card, context.Source)))
        {
            card.Refresh();
            context.Events.Add(new FieldSet(card.ObjectId, "is_exhaust", 1, 0)
            { Trigger = context.Trigger, Verb = "Ready" });
        }
    }

    private static void GiveStatus(AbilityEffect.GiveStatus status, AbilityCardStateContext context)
    {
        foreach (var host in Every(status.Cards, context))
        {
            var created = Reveal.Afflict(context.World, context.World.Facts, host, status.Status,
                context.Trigger, context.Events);
            if (created is not null)
                context.Events.Add(new CardAttached(created.ObjectId, host.ObjectId)
                { Trigger = context.Trigger, Verb = "Give_Status" });
        }
    }

    private static void Discard(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        if (Find(selection, context) is { } target
            && CanRemove(selection, target, context))
            Rules.Play.Discard.CardFromEffect(context.World, context.World.Facts, context.Source, target,
                context.Trigger, context.Events);
    }

    private static void RemoveFromGame(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        var card = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would remove a card that is not there");
        if (!CanRemove(selection, card, context)) return;
        var from = card.Area;
        var removed = context.World.AreaOf(DeckType.RemovedArea);
        var ending = context.World.Effects.PreflightConstantsEnding(card);
        using var departure = ending.Begin();
        if (DeckTypes.IsInPlay(from.Type))
        {
            Rules.Play.Discard.Attachments(context.World, card, context.Trigger, context.Events);
            Rules.Play.Discard.ResetLeavingState(context.World, card, context.Trigger, context.Events);
        }
        World.MoveToTop(card, removed);
        context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(removed),
            [new Landing(card.ObjectId, removed.Cards.Count - 1)])
        { Trigger = context.Trigger, Verb = "Remove_From_Game" });
        ending.Complete(context.Trigger, context.Events);
    }

    private static void AddToHand(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        var card = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the card added to hand");
        MoveToHand(card, context.World.Seats[context.Player].Hand, "Add_To_Hand", context, linked: true);
    }

    private static void ReturnOwnedToHand(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        var card = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the card returned to hand");
        if (card.Owner < 0) throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' returns a card with no owning player");
        MoveToHand(card, context.World.Seats[card.Owner].Hand, "Return", context, linked: false);
    }

    private static void ReturnToHand(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        foreach (var card in Every(selection, context))
        {
            var from = card.Area;
            MoveToHand(card, context.World.Seats[card.Owner].Hand, "Return", context, linked: false);
            card.TurnFaceUp();
            context.Events.Add(new CardDetached(card.ObjectId, from.Host)
            { Trigger = context.Trigger, Verb = "Return" });
        }
    }

    private static void MoveToHand(Card card, Area hand, string verb, AbilityCardStateContext context, bool linked)
    {
        var from = card.Area;
        var ending = context.World.Effects.PreflightConstantsEnding(card);
        using var departure = ending.Begin();
        if (DeckTypes.IsInPlay(from.Type)) Rules.Play.Discard.Attachments(context.World, card, context.Trigger, context.Events);
        if (linked && !Characteristics.IsLost(context.World, card, "linked")
            && context.World.Facts.Attributes(card.FaceId).ContainsKey("Linked")) card.TransferLinkedOwnership(context.Player);
        World.MoveToTop(card, hand);
        context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(hand),
            [new Landing(card.ObjectId, hand.Cards.Count - 1)]) { Trigger = context.Trigger, Verb = verb });
        ending.Complete(context.Trigger, context.Events);
    }

    private static void AttachTo(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        var host = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' attaches to a card that is not there");
        var from = context.Source.Area;
        var onto = context.World.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        World.MoveToTop(context.Source, onto);
        context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(onto),
            [new Landing(context.Source.ObjectId, onto.Cards.Count - 1)]) { Trigger = context.Trigger, Verb = "Attach" });
        context.Events.Add(new CardAttached(context.Source.ObjectId, host.ObjectId) { Trigger = context.Trigger, Verb = "Attach" });
    }

    private static void PlaceAtRandom(AbilityEffect.PlaceAtRandom placement, AbilityCardStateContext context)
    {
        var host = Find(placement.Host, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' places cards on a card that is not there");
        var onto = context.World.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        long count = Amount(placement.Count, context);
        foreach (int player in Players(placement.Players, context))
        {
            var hand = context.World.Seats[player].Hand;
            for (long placed = 0; placed < count && hand.Cards.Count > 0; placed++)
            {
                var card = context.World.Random.Choice(hand.Cards); var from = card.Area;
                World.MoveToTop(card, onto); card.TurnFaceDown();
                context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(onto),
                    [new Landing(card.ObjectId, onto.Cards.Count - 1)]) { Trigger = context.Trigger, Verb = "Place" });
                context.Events.Add(new CardAttached(card.ObjectId, host.ObjectId) { Trigger = context.Trigger, Verb = "Place" });
            }
        }
    }

    private static void DiscardAtRandom(AbilityEffect.DiscardAtRandom discard, AbilityCardStateContext context)
    {
        long count = Amount(discard.Count, context); long discarded = 0; var types = new SortedSet<char>();
        foreach (int player in Players(discard.Players, context))
        {
            var hand = context.World.Seats[player].Hand;
            for (long gone = 0; gone < count && hand.Cards.Count > 0; gone++)
            {
                var card = context.World.Random.Choice(hand.Cards);
                types.UnionWith(Resources.GeneratedBy(card.FaceId, context.World.Facts));
                Rules.Play.Discard.Card(context.World, card, context.Trigger, context.Events);
                discarded++;
            }
        }
        context.Result.Values["discarded"] = discarded; context.Result.Values["resourceTypes"] = types.Count;
    }

    private static void DiscardTop(AbilityEffect.DiscardTop discard, AbilityCardStateContext context)
    {
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

    private static void DiscardHandWithResource(AbilityEffect.DiscardHandWithResource discard, AbilityCardStateContext context)
    {
        foreach (var card in context.World.Seats[context.Player].Hand.Cards.Where(card =>
                     Resources.GeneratedBy(card.FaceId, context.World.Facts).Contains(discard.Resource)).ToList())
        { Rules.Play.Discard.Card(context.World, card, context.Trigger, context.Events); context.Result.Discarded.Add(card); }
        // `result.discarded` is resolution evidence: cards discarded by an
        // earlier node remain visible to later nodes. The owner sees that
        // immutable prior evidence through its expression context and adds
        // only this operation's new discards.
        context.Result.Values["discarded"] = context.Expressions.Discarded.Length
            + context.Result.Discarded.Count;
    }

    private static long Amount(AbilityNumber value, AbilityCardStateContext context)
    {
        var evaluation = new AbilityExpressionEvaluation(context.Expressions, new AbilitySelectorEvaluation(context.Expressions.Bindings));
        return Publish(evaluation.Result(evaluation.Amount(value)), context.World);
    }
    private static Card? Find(AbilityCardSelection selection, AbilityCardStateContext context)
    { var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings); return Publish(evaluation.Result(evaluation.Find(selection)), context.World); }
    private static IReadOnlyList<Card> Every(AbilityCardSelection selection, AbilityCardStateContext context)
    { var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings); return Publish(evaluation.Result(evaluation.Every(selection)), context.World); }
    private static bool CanRemove(AbilityCardSelection selection, Card card, AbilityCardStateContext context) =>
        new AbilitySelectorEvaluation(context.Expressions.Bindings).CanRemove(selection, card);
    private static Area Area(AbilitySearchArea area, AbilityCardStateContext context) => area switch
    {
        AbilitySearchArea.EncounterDeck => context.World.AreaOf(DeckType.EncounterDeck),
        AbilitySearchArea.EncounterDiscardPile => context.World.AreaOf(DeckType.EncounterDiscardPile),
        AbilitySearchArea.ScenarioSetAside => context.World.AreaOf(DeckType.AsideDeck),
        AbilitySearchArea.YourDeck => context.World.Seats[context.Player].Deck,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };
    private static IEnumerable<int> Players(AbilityPlayerSelection players, AbilityCardStateContext context) => players switch
    { AbilityPlayerSelection.AllPlayers => context.World.PlayerOrder, AbilityPlayerSelection.OnePlayer one => [new AbilityExpressionEvaluation(context.Expressions, new AbilitySelectorEvaluation(context.Expressions.Bindings)).Seat(one.Player)], _ => throw new InvalidOperationException("Unknown compiled player selection") };
    private static T Publish<T>(AbilityQueryResult<T> result, World world)
    { foreach (var observation in result.Information) world.RecordInformation(observation); return result.Value; }
}

internal sealed record AbilityCardStateContext(AbilityExpressionContext Expressions, string Trigger,
    List<GameEvent> Events, ICardCounterPools Pools, AbilityCardStateResult Result)
{
    internal World World => Expressions.World;
    internal Card Source => Expressions.Source;
    internal int Player => Expressions.Player;
}

internal sealed class AbilityCardStateResult
{
    internal List<Card> Discarded { get; } = [];
    internal Dictionary<string, long> Values { get; } = new(StringComparer.Ordinal);
}
