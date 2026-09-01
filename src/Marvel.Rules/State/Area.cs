namespace Marvel.Rules.State;

/// <summary>
/// One place cards can be, with an identity of its own.
/// </summary>
/// <remarks>
/// <para>
/// <b>An area is identified, not described.</b> <c>(zone, owner, host)</c> does
/// not name an area uniquely — over a 6,554-step sample of recorded play it
/// collided on 5,969 of them — so the area carries an <see cref="Id"/> that the
/// engine hands out. the original investigation and <c>docs/event-stream.md</c>.
/// </para>
/// <para>
/// <b>Two seat-shaped fields, and they are different questions.</b>
/// <see cref="PlayArea"/> is where this area <i>sits</i>;
/// <see cref="CardOwner"/> is who a card <i>made</i> here belongs to. They
/// disagree on a player's nemesis pile, which is theirs and is the scenario's
/// property. Neither is the card's controller, which the digest calls
/// <c>owner</c> — that agrees with <see cref="PlayArea"/> 98.1% of the time and
/// the 1.9% is five named rules. See <c>docs/state-digest-v2.md</c>.
/// </para>
/// </remarks>
public sealed class Area
{
    private readonly List<Card> cards = [];
    private readonly List<Card> removed = [];

    internal Area(int id, DeckType type, int cardOwner, PlayArea playArea, int host)
    {
        Id = id;
        Type = type;
        CardOwner = cardOwner;
        PlayArea = playArea;
        Host = host;
    }

    /// <summary>This area's identity, unique within the world.</summary>
    public int Id { get; }

    /// <summary>What kind of place this is.</summary>
    public DeckType Type { get; }

    /// <summary>Who a card created here belongs to, or -1 for the scenario.</summary>
    /// <remarks>
    /// The engine's rule in <c>CardFactory.GenerateCard</c>. It is <b>not</b>
    /// the same question as <see cref="PlayArea"/>: a player's nemesis pile is
    /// <i>theirs</i> and is owned by the scenario, which is why the digest
    /// records an obligation as owner -1 while it sits in a pile that plainly
    /// belongs to a seat.
    /// </remarks>
    public int CardOwner { get; }

    /// <summary>Which play area this area sits in.</summary>
    /// <remarks>
    /// <para>
    /// This is what <c>AreaRef.Owner</c> carries on the wire, and it is not the
    /// card's controller. the original investigation measured the difference: answering with
    /// the controller alone names the scenario for every player's engagement
    /// area, which mislabelled 380 of 621 ambiguous steps — a minion engaged
    /// with you is in <i>your</i> play area and controlled by the scenario.
    /// </para>
    /// <para>
    /// <c>rr:play-area.3</c>: "A card cannot be in more than one play area at a
    /// time." An area sits in exactly one, so every card in it is in that one.
    /// </para>
    /// </remarks>
    public PlayArea PlayArea { get; }

    /// <summary>The card this area is bound to, or -1.</summary>
    public int Host { get; }

    /// <summary>The cards, bottom first. The <b>last</b> element is the top.</summary>
    /// <remarks>
    /// Bottom-first is the engine's order and it is load-bearing: an obligation
    /// put "on top" of the encounter deck is appended, and drawing takes from
    /// the end. Reversing this would reverse every opening hand.
    /// </remarks>
    public IReadOnlyList<Card> Cards => cards;

    /// <summary>Where a detached attachment waits. A zone of its own in the digest.</summary>
    public IReadOnlyList<Card> Removed => removed;

    internal void Append(Card card)
    {
        cards.Add(card);
        card.MovedTo(this);
    }

    internal void Remove(Card card) => cards.Remove(card);

    /// <summary>Takes the top card, or null when empty.</summary>
    internal Card? TakeTop()
    {
        if (cards.Count == 0)
        {
            return null;
        }

        var card = cards[^1];
        cards.RemoveAt(cards.Count - 1);
        return card;
    }

    internal void Replace(IEnumerable<Card> order)
    {
        cards.Clear();
        cards.AddRange(order);
    }
}
