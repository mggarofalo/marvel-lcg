using Marvel.Core.Digest;

namespace Marvel.Rules.State;

/// <summary>
/// Every card in a game, and every place they can be.
/// </summary>
/// <remarks>
/// <para>
/// The first argument of the fold. Cards are held in a flat list indexed by
/// <c>object_id</c>, which is also their creation order — the id allocator is a
/// counter and ids are never reused, so the list is append-only and
/// <c>cards[i].ObjectId == i</c> always.
/// </para>
/// <para>
/// <b>Nothing is ever removed from it.</b> A card removed from the game moves to
/// the removed area and is still recorded, so the set of ids in a digest is
/// always <c>0..highest</c>. Dropping one would renumber nothing but would make
/// the digest disagree.
/// </para>
/// </remarks>
public sealed class World
{
    private readonly List<Card> cards = [];
    private readonly List<Area> areas = [];
    private readonly ICardFacts facts;

    /// <summary>Creates an empty world.</summary>
    /// <param name="facts">The printed card data this game is played with.</param>
    /// <param name="players">How many players are in the game.</param>
    public World(ICardFacts facts, int players)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        this.facts = facts;
        Players = players;
    }

    /// <summary>The seat value meaning "the scenario", not a player.</summary>
    public const int Scenario = -1;

    /// <summary>How many players are in the game.</summary>
    public int Players { get; }

    /// <summary>Every card, ascending by <see cref="Card.ObjectId"/>.</summary>
    public IReadOnlyList<Card> Cards => cards;

    /// <summary>Every area, in the order they were made.</summary>
    public IReadOnlyList<Area> Areas => areas;

    /// <summary>The seat holding the first player token.</summary>
    public int FirstPlayer { get; internal set; }

    /// <summary>Makes an area.</summary>
    /// <param name="type">What kind of place it is.</param>
    /// <param name="owner">Who owns it, or -1 for the scenario.</param>
    /// <param name="relatedPlayer">Whose place it is, or -1.</param>
    /// <param name="host">The card it is bound to, or -1.</param>
    public Area CreateArea(DeckType type, int owner = -1, int relatedPlayer = -1, int host = -1)
    {
        var area = new Area(areas.Count, type, owner, relatedPlayer, host);
        areas.Add(area);
        return area;
    }

    /// <summary>Makes a card and puts it in an area.</summary>
    /// <remarks>
    /// The id is the next one, so the order these calls are made in <b>is</b> the
    /// wire format. See <c>Marvel.Content.Setup.Dealer</c>.
    /// </remarks>
    /// <param name="spec">Comma-separated face ids. One card, however many faces.</param>
    /// <param name="into">Where it starts. Its owner becomes the card's owner.</param>
    public Card CreateCard(string spec, Area into)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(into);

        // The engine's rule: a card belongs to whoever owns the place it was
        // made in, falling back to the scenario. Not to the seat that asked for
        // it -- an obligation is dealt for a player and owned by the scenario.
        var card = new Card(cards.Count, spec.Split(','), into.Owner);
        cards.Add(card);
        into.Append(card);
        return card;
    }

    /// <summary>Moves a card to the end (the top) of an area.</summary>
    /// <param name="card">The card.</param>
    /// <param name="destination">Where it goes.</param>
    public static void MoveToTop(Card card, Area destination)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(destination);
        card.Area.Remove(card);
        destination.Append(card);
    }

    /// <summary>The state digest of this world.</summary>
    /// <remarks>
    /// One record per card, ascending by id, nothing excluded — not the rules
    /// pseudo-card, not id 0, not the middle of a deck.
    /// </remarks>
    public StateDigest Digest()
    {
        var positions = new Dictionary<int, (string Zone, int Index)>();
        foreach (var area in areas)
        {
            string zone = area.Type.ToString();
            for (int index = 0; index < area.Cards.Count; index++)
            {
                positions[area.Cards[index].ObjectId] = (zone, index);
            }

            for (int index = 0; index < area.Removed.Count; index++)
            {
                positions[area.Removed[index].ObjectId] = (zone + "/removed", index);
            }
        }

        var records = new List<CardRecord>(cards.Count);
        foreach (var card in cards)
        {
            // `/absent` should not happen. It is emitted rather than raised
            // because an oracle that can crash while computing itself is worse
            // than one with a visible anomaly.
            var (zone, index) = positions.TryGetValue(card.ObjectId, out var found)
                ? found
                : (card.Area.Type + "/absent", -1);

            bool inPlay = IsInPlay(card.Area.Type);
            records.Add(new CardRecord(
                Id: card.ObjectId,
                Card: card.FaceId,
                Zone: zone,
                Owner: card.Owner,
                Index: index,
                Host: card.Area.Host,
                FaceUp: card.FaceUp,
                Fields: StateFields.For(
                    card, facts, Players, inPlay,
                    hasFirstPlayerToken: card.Owner == FirstPlayer
                                         && card.Area.Type == DeckType.HeroArea)));
        }

        return new StateDigest(records);
    }

    private static bool IsInPlay(DeckType type) => type is
        DeckType.UpgradesArea or DeckType.AlliesArea or DeckType.SupportsArea or
        DeckType.EngagedEnemiesArea or DeckType.HeroArea or DeckType.ObligationsArea or
        DeckType.MainSchemesArea or DeckType.SideSchemesArea or DeckType.VillainArea or
        DeckType.EnvironmentArea or DeckType.EvidenceArea or DeckType.RuleArea;
}
