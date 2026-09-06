using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Executes effects that inspect or change decks and prepare reveals.</summary>
internal static class AbilityDeckAndRevealExecution
{
    internal static AbilityDeckAndRevealResult Run(
        AbilityEffect effect, AbilityDeckAndRevealContext context)
    {
        return effect switch
        {
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Reveal } reveal =>
                RevealCard(Find(reveal.Selection, context), context),
            AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.RevealTop } =>
                RevealCard(EncounterDeck.TakeTop(
                    context.World, context.Trigger, context.Events), context),
            AbilityEffect.DealEncounterCard deal => DealEncounterCard(deal, context),
            AbilityEffect.DealEncounterCards deal => DealEncounterCards(deal, context),
            AbilityEffect.CreateDrones drones => CreateDrones(drones, context),
            AbilityEffect.DiscardUntil discard => DiscardUntil(discard, context),
            AbilityEffect.RecoverDiscardedByResource recovery =>
                RecoverDiscardedByResource(recovery, context),
            AbilityEffect.Shuffle shuffle => Shuffle(shuffle, context),
            AbilityEffect.ShuffleInto shuffle => ShuffleInto(shuffle, context),
            AbilityEffect.Search search => Search(search, context),
            AbilityEffect.PutIntoPlay placement => PutIntoPlay(placement, context),
            _ => AbilityDeckAndRevealResult.NotHandled,
        };
    }

    private static AbilityDeckAndRevealResult RevealCard(
        Card? card, AbilityDeckAndRevealContext context)
    {
        var reveal = PrepareReveal(card, context);
        return new(true, reveal is not null, reveal,
            ImmutableDictionary<string, long>.Empty);
    }

    /// <summary>Moves a card now so the executor can schedule its reveal procedure.</summary>
    /// <remarks>
    /// Revealing is not dealing: <c>rr:deal</c> leaves a card facedown for the
    /// villain phase, while <c>rr:reveal</c> opens its own interrupt and response
    /// windows. The move precedes the scheduled procedure so later text sees
    /// the card in the revealing area rather than its former deck or pile.
    /// </remarks>
    private static AbilityRevealRequest? PrepareReveal(
        Card? card, AbilityDeckAndRevealContext context)
    {
        if (card is null) return null;
        var from = card.Area;
        var revealing = context.World.AreaOf(DeckType.RevealingArea);
        World.MoveToTop(card, revealing);
        context.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(revealing),
            [new Landing(card.ObjectId, revealing.Cards.Count - 1)])
        {
            Trigger = context.Trigger,
            Verb = "Reveal",
        });
        int player = context.Player >= 0 ? context.Player : context.World.FirstPlayer;
        return new AbilityRevealRequest(card.ObjectId, player);
    }

    /// <summary>Deals a specific encounter card facedown — <c>rr:deal</c>.</summary>
    private static AbilityDeckAndRevealResult DealEncounterCard(
        AbilityEffect.DealEncounterCard deal, AbilityDeckAndRevealContext context)
    {
        Rules.Play.Deal.EncounterCard(
            context.World,
            Find(deal.Card, context) ?? throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' cannot find the encounter card to deal"),
            Player(deal.Player, context),
            context.Trigger,
            context.Events);
        return AbilityDeckAndRevealResult.Handled;
    }

    /// <summary>Deals facedown encounter cards in player order — <c>rr:deal</c>.</summary>
    private static AbilityDeckAndRevealResult DealEncounterCards(
        AbilityEffect.DealEncounterCards deal, AbilityDeckAndRevealContext context)
    {
        var players = Players(deal.Players, context).ToList();
        for (long dealt = 0; dealt < deal.Count; dealt++)
        {
            foreach (int player in players)
            {
                if (Rules.Play.Deal.EncounterCard(
                        context.World, player, context.Trigger, context.Events) is null)
                    return AbilityDeckAndRevealResult.Handled;
            }
        }
        return AbilityDeckAndRevealResult.Handled;
    }

    private static AbilityDeckAndRevealResult CreateDrones(
        AbilityEffect.CreateDrones drones, AbilityDeckAndRevealContext context)
    {
        foreach (int player in Players(drones.Players, context))
        {
            for (long created = 0; created < drones.Count; created++)
                FacedownDrones.EngageTop(
                    context.World, player, context.Trigger, "Create_Drone", context.Events);
        }
        return AbilityDeckAndRevealResult.Handled;
    }

    /// <summary>Discards one at a time until the bounded search matches.</summary>
    /// <remarks>
    /// <c>rr:discard.4</c> preserves order when one effect discards several
    /// cards. <see cref="EncounterDeck.DiscardUntil"/> also bounds an absent
    /// match by the cards available across the deck and discard pile.
    /// </remarks>
    private static AbilityDeckAndRevealResult DiscardUntil(
        AbilityEffect.DiscardUntil discard, AbilityDeckAndRevealContext context)
    {
        var found = EncounterDeck.DiscardUntil(
            context.World, context.World.Facts, discard.Kind,
            context.Trigger, context.Events, discard.Trait);
        if (found is null) return AbilityDeckAndRevealResult.Handled;
        if (discard.PutIntoPlayForFirstPlayer)
        {
            AbilityCardStateExecution.PutIntoPlay(
                found, context.World.FirstPlayer, CardStateContext(context));
            return AbilityDeckAndRevealResult.Handled;
        }
        var reveal = PrepareReveal(found, context);
        return new(true, reveal is not null, reveal,
            ImmutableDictionary<string, long>.Empty);
    }

    private static AbilityDeckAndRevealResult RecoverDiscardedByResource(
        AbilityEffect.RecoverDiscardedByResource recovery,
        AbilityDeckAndRevealContext context)
    {
        var hand = context.World.Seats[context.Player].Hand;
        foreach (var card in context.Discarded.Where(card =>
                     Resources.GeneratedBy(card.FaceId, context.World.Facts)
                         .Contains(recovery.Resource)))
        {
            var from = card.Area;
            World.MoveToTop(card, hand);
            context.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(hand),
                [new Landing(card.ObjectId, hand.Cards.Count - 1)])
            {
                Trigger = context.Trigger,
                Verb = "Add_To_Hand",
            });
        }
        return AbilityDeckAndRevealResult.Handled;
    }

    /// <summary>Shuffles one named deck — <c>rr:shuffle</c>.</summary>
    /// <remarks>
    /// This is a separate effect after an answered search choice because
    /// <c>rr:search.3</c> shuffles upon completion of that search.
    /// </remarks>
    private static AbilityDeckAndRevealResult Shuffle(
        AbilityEffect.Shuffle shuffle, AbilityDeckAndRevealContext context) =>
        new(true, context.World.Shuffle(Area(shuffle.Area, context)), null,
            ImmutableDictionary<string, long>.Empty);

    /// <summary>Moves every selected card, then shuffles once — <c>rr:shuffle</c>.</summary>
    /// <remarks>The single shuffle is part of the seeded RNG wire format.</remarks>
    private static AbilityDeckAndRevealResult ShuffleInto(
        AbilityEffect.ShuffleInto shuffle, AbilityDeckAndRevealContext context)
    {
        var deck = Area(shuffle.Deck, context);
        bool applied = false;
        foreach (var card in Every(shuffle.Cards, context))
        {
            var from = card.Area;
            World.MoveToTop(card, deck);
            context.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(deck),
                [new Landing(card.ObjectId, deck.Cards.Count - 1)])
            {
                Trigger = context.Trigger,
                Verb = "Shuffle_Into",
            });
            applied = true;
        }
        applied |= context.World.Shuffle(deck);
        return new(true, applied, null, ImmutableDictionary<string, long>.Empty);
    }

    /// <summary>Searches without moving inspected cards — <c>rr:search.2</c>.</summary>
    /// <remarks>
    /// A found card leaves before <c>rr:search.3</c> shuffles the searched deck.
    /// Several matches require the player choice in <c>rr:search.1</c>, which
    /// remains an explicit unsupported boundary.
    /// </remarks>
    private static AbilityDeckAndRevealResult Search(
        AbilityEffect.Search search, AbilityDeckAndRevealContext context)
    {
        var areas = search.Areas.Select(where => Area(where, context)).ToList();
        var found = areas.SelectMany(area => area.Cards)
            .Where(card => string.Equals(
                card.FaceId, search.Face, StringComparison.Ordinal))
            .ToList();
        if (found.Count > 1)
        {
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' searched and found {found.Count} copies of "
                + $"'{search.Face}'; rr:search.1 gives the player that choice and asking is "
                + "not implemented");
        }
        context.World.RecordInformation(InformationKind.Search);
        var reveal = found.Count == 1 ? PrepareReveal(found[0], context) : null;
        bool applied = reveal is not null;
        foreach (var deck in areas.Where(area => area.Type == DeckType.EncounterDeck))
            applied |= context.World.Shuffle(deck);
        return new(true, applied, reveal,
            ImmutableDictionary<string, long>.Empty.Add("found", found.Count));
    }

    /// <summary>Puts a card into play without triggering When Revealed.</summary>
    /// <remarks>
    /// <c>rr:when-revealed-abilities.2</c> suppresses that ability, while
    /// <c>rr:enters-play</c> still applies the card's enters-play rules.
    /// </remarks>
    private static AbilityDeckAndRevealResult PutIntoPlay(
        AbilityEffect.PutIntoPlay placement, AbilityDeckAndRevealContext context)
    {
        var card = Find(placement.Card, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would put a card into play that is not there");
        if (placement.PrintedDestination)
        {
            Reveal.Resolve(context.World, context.World.Facts, card,
                context.World.FirstPlayer, context.Events, verb: "Put_Into_Play");
            return new(true, true, null, ImmutableDictionary<string, long>.Empty);
        }
        AbilityCardStateExecution.PutIntoPlay(card, context.Player, CardStateContext(context));
        return AbilityDeckAndRevealResult.Handled;
    }

    private static AbilityCardStateContext CardStateContext(
        AbilityDeckAndRevealContext context) =>
        new(context.Expressions, context.Trigger, context.Events,
            context.CardPlayAbilities, context.Readiness,
            new AbilityCardStateResult());

    private static Card? Find(
        AbilityCardSelection selection, AbilityDeckAndRevealContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        return Publish(evaluation.Result(evaluation.Find(selection)), context.World);
    }

    private static IReadOnlyList<Card> Every(
        AbilityCardSelection selection, AbilityDeckAndRevealContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        return Publish(evaluation.Result(evaluation.Every(selection)), context.World);
    }

    private static Area Area(
        AbilitySearchArea area, AbilityDeckAndRevealContext context) => area switch
    {
        AbilitySearchArea.EncounterDeck => context.World.AreaOf(DeckType.EncounterDeck),
        AbilitySearchArea.EncounterDiscardPile =>
            context.World.AreaOf(DeckType.EncounterDiscardPile),
        AbilitySearchArea.ScenarioSetAside => context.World.AreaOf(DeckType.AsideDeck),
        AbilitySearchArea.YourDeck => context.World.Seats[context.Player].Deck,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };

    private static IEnumerable<int> Players(
        AbilityPlayerSelection players, AbilityDeckAndRevealContext context) => players switch
    {
        AbilityPlayerSelection.AllPlayers => context.World.PlayerOrder,
        AbilityPlayerSelection.OnePlayer one =>
            [new AbilityExpressionEvaluation(context.Expressions,
                new AbilitySelectorEvaluation(context.Expressions.Bindings)).Seat(one.Player)],
        _ => throw new InvalidOperationException("Unknown compiled player selection"),
    };

    private static int Player(
        AbilityPlayer player, AbilityDeckAndRevealContext context) =>
        new AbilityExpressionEvaluation(context.Expressions,
            new AbilitySelectorEvaluation(context.Expressions.Bindings)).Seat(player);

    private static T Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information)
            world.RecordInformation(observation);
        return result.Value;
    }
}

internal sealed record AbilityDeckAndRevealContext(
    AbilityExpressionContext Expressions, string Trigger, List<GameEvent> Events,
    ICardPlayAbilities CardPlayAbilities, ICardReadinessAbilities Readiness,
    ImmutableArray<Card> Discarded)
{
    internal World World => Expressions.World;
    internal Card Source => Expressions.Source;
    internal int Player => Expressions.Player;
}

internal sealed record AbilityRevealRequest(int Card, int Player);

internal sealed record AbilityDeckAndRevealResult(
    bool IsHandled, bool ResolveEffect, AbilityRevealRequest? Reveal,
    ImmutableDictionary<string, long> Values)
{
    internal static AbilityDeckAndRevealResult Handled { get; } =
        new(true, false, null, ImmutableDictionary<string, long>.Empty);
    internal static AbilityDeckAndRevealResult NotHandled { get; } =
        new(false, false, null, ImmutableDictionary<string, long>.Empty);
}
