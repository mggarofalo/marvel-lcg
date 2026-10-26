using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Doctor Strange's five physical Invocations and their separate lifecycle.</summary>
public sealed class InvocationDeck
{
    private static readonly ImmutableArray<string> Faces = ["09032", "09033", "09034", "09035", "09036"];
    private readonly World world;
    private readonly Card[] cards;
    private bool duringGame;

    private InvocationDeck(World world, int player, IReadOnlyList<Card> cards)
    {
        this.world = world;
        this.cards = [.. cards];
        Player = player;
        // Engine choice: reuse the existing v3 zone names, with player ownership.
        Deck = world.AreaOf(DeckType.AdditionalDeck, PlayArea.Of(player), cardOwner: player);
        Discard = world.AreaOf(DeckType.AdditionalDiscardPile, PlayArea.Of(player), cardOwner: player);
    }

    /// <summary>The player whose identity owns this deck.</summary>
    public int Player { get; }

    /// <summary>The bottom-first Invocation draw pile; only its top is public during play.</summary>
    public Area Deck { get; }

    /// <summary>The separate, faceup Invocation discard pile.</summary>
    public Area Discard { get; }

    /// <summary>The public top, or null until every player's mulligan has finished.</summary>
    public Card? PublicTop => duringGame && Deck.Cards.Count > 0 ? Deck.Cards[^1] : null;

    /// <summary>Validates, binds and separately shuffles the five set-aside physical cards.</summary>
    public static InvocationDeck Initialize(
        World world, int player, IReadOnlyList<Card> cards, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Seats.Count);
        var seat = world.Seats[player];
        if (seat.InvocationDeck is not null || world.Areas.Any(area =>
                area.PlayArea == PlayArea.Of(player)
                && area.Type is DeckType.AdditionalDeck or DeckType.AdditionalDiscardPile))
            throw new InvalidOperationException("the player's additional deck areas are already initialized");
        ValidateIdentity(world, seat);
        ValidateCards(world, player, cards);
        var invocation = new InvocationDeck(world, player, cards);
        seat.InvocationDeck = invocation;
        // pack:mc08:the-invocation-deck: "shuffle all five ... Invocation cards together".
        foreach (var card in cards) Move(card, invocation.Deck, trigger, "Setup", events);
        invocation.Shuffle(trigger, events);
        return invocation;
    }

    /// <summary>Whether this bound physical card is the owned public top in its draw pile.</summary>
    public bool CanDiscardTop(Card card) => duringGame && cards.Contains(card)
        && card.Owner == Player && ReferenceEquals(card, PublicTop)
        && card.Area == Deck && Deck.Cards.Contains(card);

    /// <summary>Discards the exact current public top without touching the player deck.</summary>
    public void DiscardTop(Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);
        if (!CanDiscardTop(card))
            throw new ArgumentException("only this player's current public Invocation can be discarded", nameof(card));
        global::Marvel.Rules.Play.Discard.Card(world, card, trigger, events);
    }

    internal static InvocationDeck? ForCard(World world, Card card)
    {
        var invocation = world.Seats.Select(seat => seat.InvocationDeck)
            .FirstOrDefault(deck => deck is not null && deck.cards.Contains(card));
        if (invocation is not null && card.Owner != invocation.Player)
            throw new InvalidOperationException("an Invocation must retain its player's ownership");
        return invocation;
    }

    /// <summary>Return the resolved physical card from its discard or recycled deck.</summary>
    public void ReturnToTop(Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);
        if (!duringGame || !cards.Contains(card) || card.Owner != Player
            || card.Area != Discard && card.Area != Deck)
            throw new ArgumentException("only this player's resolved Invocation can return", nameof(card));
        // ruling:b07d53e12625ab23: expose/recycle before returning this same copy.
        // A recycled copy can occupy any position, so move it within its deck too.
        if (PublicTop is { } previous)
        {
            previous.TurnFaceDown();
            events.Add(new CardsFlipped([previous.ObjectId], false) { Trigger = trigger, Verb = "Conceal" });
        }
        Move(card, Deck, trigger, "Return", events);
        RefreshTop(trigger, events);
    }

    internal void BeginGame(string trigger, List<GameEvent> events)
    {
        duringGame = true;
        RefreshTop(trigger, events);
    }

    internal void RefreshTop(string trigger, List<GameEvent> events)
    {
        // pack:mc08:the-invocation-deck: "If the Invocation deck is ever empty,
        // shuffle the Invocation discard pile back ... There is no penalty".
        if (Deck.Cards.Count == 0 && Discard.Cards.Count > 0)
        {
            foreach (var card in Discard.Cards.ToArray()) Move(card, Deck, trigger, "Reset", events);
            Shuffle(trigger, events);
        }
        if (!duringGame || Deck.Cards.Count == 0 || Deck.Cards[^1].FaceUp) return;
        var top = Deck.Cards[^1];
        top.TurnFaceUp();
        world.RecordInformation(InformationKind.Reveal);
        events.Add(new CardsFlipped([top.ObjectId], true)
        {
            Trigger = trigger,
            Verb = "Reveal",
            SubjectFaces = OccurrenceFaces.For(top),
        });
    }

    private static void ValidateIdentity(World world, Seat seat)
    {
        var identity = seat.IdentityCard;
        if (identity is null || !world.Cards.Contains(identity) || identity.Owner != seat.Index
            || identity.Area != seat.Hero || identity.Faces.Count != 2
            || !identity.Faces.Contains("09001a") || !identity.Faces.Contains("09001b"))
            throw new ArgumentException("an Invocation deck requires this player's Doctor Strange identity");
    }

    private static void ValidateCards(World world, int player, IReadOnlyList<Card> cards)
    {
        if (cards.Count != Faces.Length || cards.Distinct().Count() != Faces.Length)
            throw new ArgumentException("an Invocation deck requires five distinct physical cards", nameof(cards));
        foreach (var face in Faces)
        {
            if (cards.Count(card => card is not null && card.Faces.Count == 1 && card.FaceId == face) != 1)
                throw new ArgumentException("an Invocation deck requires exactly one of each printed Invocation", nameof(cards));
        }
        ValidateSources(world, player, cards);
    }

    private static void ValidateSources(World world, int player, IReadOnlyList<Card> cards)
    {
        foreach (var card in cards)
        {
            if (!world.Cards.Contains(card) || card.Owner != player || card.Area.Type != DeckType.AsideDeck
                || card.Area.PlayArea != PlayArea.Of(player) || !card.Area.Cards.Contains(card))
                throw new ArgumentException("Invocations must be this world's player-owned set-aside cards", nameof(cards));
        }
    }

    private static void Move(Card card, Area destination, string trigger, string verb, List<GameEvent> events)
    {
        var from = card.Area;
        World.MoveToTop(card, destination);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(destination),
            [new Landing(card.ObjectId, destination.Cards.Count - 1)]) { Trigger = trigger, Verb = verb });
    }

    private void Shuffle(string trigger, List<GameEvent> events)
    {
        world.Shuffle(Deck);
        events.Add(new AreaReordered(Places.Reference(Deck), [.. Deck.Cards.Select(card => card.ObjectId)])
        { Trigger = trigger, Verb = "Shuffle" });
    }
}
