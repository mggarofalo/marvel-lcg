namespace Marvel.Rules.State;

/// <summary>
/// One place cards can be, with an identity of its own.
/// </summary>
/// <remarks>
/// <para>
/// <b>An area is identified, not described.</b> <c>(zone, owner, host)</c> does
/// not name an area uniquely — measured over 6,554 corpus steps it collides on
/// 5,969 of them — so the area carries an <see cref="Id"/> that the fold hands
/// out. MARVEL-175 and <c>docs/event-stream.md</c>.
/// </para>
/// <para>
/// <see cref="Owner"/> is <b>whose place this is</b>, which is not the same
/// question as who controls a card sitting in it. The two agree 98.1% of the
/// time and the 1.9% is five named rules — see <c>docs/state-digest-v2.md</c>.
/// </para>
/// </remarks>
public sealed class Area
{
    private readonly List<Card> cards = [];
    private readonly List<Card> removed = [];

    internal Area(int id, DeckType type, int owner, int relatedPlayer, int host)
    {
        Id = id;
        Type = type;
        Owner = owner;
        RelatedPlayer = relatedPlayer;
        Host = host;
    }

    /// <summary>This area's identity, unique within the world.</summary>
    public int Id { get; }

    /// <summary>What kind of place this is.</summary>
    public DeckType Type { get; }

    /// <summary>Who owns this place, or -1 for the scenario.</summary>
    /// <remarks>
    /// A card created here takes this as its owner, which is the engine's rule
    /// in <c>CardFactory.GenerateCard</c>. It is <b>not</b> the same question as
    /// <see cref="RelatedPlayer"/>: a player's nemesis pile is <i>theirs</i> and
    /// is owned by the scenario, which is why the digest records an obligation
    /// as owner -1 while it sits in a pile that plainly belongs to a seat.
    /// </remarks>
    public int Owner { get; }

    /// <summary>Whose place this is, or -1 when it belongs to no seat.</summary>
    /// <remarks>
    /// The Python engine's <c>related_player</c>. Measured earlier in MARVEL-163:
    /// reading <c>GetOwner()</c> alone answers -1 for every player's engagement
    /// area, which mislabelled 380 of 621 ambiguous steps.
    /// </remarks>
    public int RelatedPlayer { get; }

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
