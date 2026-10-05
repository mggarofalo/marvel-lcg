namespace Marvel.Rules.State;

/// <summary>
/// One card. <b>One card, however many faces it has printed on it.</b>
/// </summary>
/// <remarks>
/// <para>
/// An identity is one card with a hero side and an alter-ego side; a main scheme
/// is one card with an <c>A</c> and a <c>B</c> side. Both get one
/// <see cref="ObjectId"/>. Treating a face as a card would shift every id after
/// it, and ids are on the wire.
/// </para>
/// <para>
/// Printed faces and physical ownership survive temporary identities. Active
/// characteristics and quantities belong to <see cref="InstanceState"/>;
/// leaving play detaches that copy before the physical card enters its destination.
/// </para>
/// </remarks>
public sealed class Card
{
    private CardInstanceState state = new();

    /// <summary>The quantities of this copy; a departure detaches this state.</summary>
    public CardInstanceState InstanceState => state;

    /// <summary>Assigns the active characteristics of an in-play copy.</summary>
    public void AssignProfile(EffectiveCardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!DeckTypes.IsInPlay(Area.Type))
            throw new InvalidOperationException("Only an in-play copy can have a temporary profile");
        state.Profile = profile;
    }

    internal Card(int objectId, IReadOnlyList<string> faces, int owner)
    {
        ObjectId = objectId;
        Faces = faces;
        Owner = owner;
    }

    /// <summary>The card's id. Its position in the deal order.</summary>
    public int ObjectId { get; }

    /// <summary>The printed face ids, in the order the engine created them.</summary>
    public IReadOnlyList<string> Faces { get; private set; }

    /// <summary>Which face is currently showing.</summary>
    public int FaceIndex { get; private set; }

    /// <summary>The printed id of the face currently showing.</summary>
    public string FaceId => Faces[FaceIndex];

    /// <summary>Which entry into play this is, starting at one.</summary>
    /// <remarks>
    /// <c>rr:leaves-play.1</c> makes a card that leaves play and returns a new
    /// copy with no memory of its former state. Object ids deliberately remain
    /// stable, so engine bookkeeping that belongs to one in-play copy uses
    /// this generation beside the id rather than allocating another card.
    /// Moving between two in-play areas does not create a new copy.
    /// </remarks>
    public int Incarnation { get; private set; }

    /// <summary>The seat that owns this card, or -1 for the scenario.</summary>
    public int Owner { get; private set; }

    /// <summary>Makes a linked card the property of the player who controls it.</summary>
    /// <remarks>
    /// <c>rr:linked-card-title.4</c> changes ownership, not merely control.
    /// Nothing else changes a card's printed owner after creation.
    /// </remarks>
    public void TransferLinkedOwnership(int player)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        Owner = player;
    }

    /// <summary>Makes a scenario-specific player card the named player's property.</summary>
    /// <remarks>
    /// <c>rr:ownership-and-control.2.2</c> changes ownership when a player takes
    /// control of a campaign- or scenario-specific player card with a player
    /// card back. This is deliberately separate from ordinary control, which
    /// changes an area's play-area coordinate and leaves <see cref="Owner"/>
    /// alone.
    /// </remarks>
    public void TransferScenarioOwnership(int player)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        Owner = player;
    }

    /// <summary>Where the card is.</summary>
    public Area Area { get; private set; } = null!;

    /// <summary>Whether the card is face up.</summary>
    public bool FaceUp { get; private set; } = true;

    /// <summary>Whether this physical card has registered token keys.</summary>
    /// <remarks>
    /// Engine choice: registered keys remain present at zero after departure,
    /// distinguishing an empty pool from one that has never been registered.
    /// This wire-format fact does not retain tokens from the ended copy.
    /// </remarks>
    public bool HasRegisteredTokens { get; private set; }

    /// <summary>Whether the card is ready. <c>is_exhaust</c> is its negation.</summary>
    public bool Ready => state.Ready;

    /// <summary>Damage on the active copy, cleared when it leaves play.</summary>
    /// <remarks>
    /// rr:leaves-play.2.3 returns all tokens to the token pool. Engine choice:
    /// damage is stored separately and contributes to remaining health in the
    /// digest; registered token keys keep their canonical spelling.
    /// </remarks>
    public long Damage => state.Damage;

    /// <summary>Token quantities of the current copy, by canonical key.</summary>
    /// <remarks>Departure clears quantities while preserving registered keys.</remarks>
    public IReadOnlyDictionary<string, long> Tokens => state.Tokens;

    /// <summary>Puts tokens on the card.</summary>
    /// <param name="kind">The digest's key, e.g. <c>k_threat</c>.</param>
    /// <param name="count">How many. Negative removes.</param>
    public void PlaceTokens(string kind, long count)
    {
        state.PlaceTokens(kind, count);
    }

    /// <summary>Turns the card to a named face.</summary>
    /// <param name="faceId">A printed id this card carries.</param>
    /// <exception cref="ArgumentException">The card has no such face.</exception>
    public void TurnTo(string faceId)
    {
        int index = Faces.ToList().IndexOf(faceId);
        FaceIndex = index >= 0
            ? index
            : throw new ArgumentException($"card {ObjectId} has no face '{faceId}'", nameof(faceId));
    }

    /// <summary>Puts damage on the card.</summary>
    /// <remarks>
    /// Clamped at zero for the same reason tokens are: healing more than a
    /// character has taken leaves it undamaged rather than over-healed
    /// (<c>rr:heal</c>).
    /// </remarks>
    /// <param name="amount">How much. Negative heals.</param>
    public void TakeDamage(long amount) => state.TakeDamage(amount);

    /// <summary>Exhausts the card. <c>rr:exhaust-ready</c>.</summary>
    public void Exhaust() => state.Exhaust();

    /// <summary>Readies the card. <c>rr:exhaust-ready</c>.</summary>
    public void Refresh() => state.Refresh();

    /// <summary>Clears state that cannot survive this card leaving play.</summary>
    internal void ResetForNewCopy()
    {
        state = state.NewCopy();
    }

    /// <summary>Turns the card face up where it lies.</summary>
    /// <remarks>
    /// Revealing is not moving. An encounter card is revealed while it sits in
    /// the pile it was dealt to and only then goes to the discard, and the two
    /// are separate events because a client animates them separately.
    /// </remarks>
    public void TurnFaceUp() => FaceUp = true;

    /// <summary>Turns the card face down where it lies.</summary>
    /// <remarks>
    /// The other half of <see cref="TurnFaceUp"/>, and a card in play can need
    /// it: Spectrum's <c>21001a</c> reads "choose a <b>facedown</b> energy form
    /// upgrade → flip that card faceup to change to that energy form", so all
    /// three of her permanents are in play at once with at most one showing.
    /// <c>rr:identity.4</c> is the same idea on the identity card — a facedown
    /// side is out of play.
    /// </remarks>
    public void TurnFaceDown() => FaceUp = false;

    internal void MovedTo(Area area)
    {
        bool wasInPlay = Area is not null && DeckTypes.IsInPlay(Area.Type);
        bool entersPlay = DeckTypes.IsInPlay(area.Type);
        if (wasInPlay && !entersPlay)
        {
            // rr:leaves-play.1: "there is no memory of its previous state".
            // Historical occurrences retain their own facts; the physical
            // card in its destination does not retain the old copy's state.
            ResetForNewCopy();
        }
        if (entersPlay && !wasInPlay)
        {
            if (Area is not null)
            {
                // Entry starts a fresh copy, including cards whose out-of-play
                // quantities were explicitly changed by an effect.
                ResetForNewCopy();
            }
            Incarnation++;
        }

        bool movesWithinPlay = wasInPlay && entersPlay;
        Area = area;
        if (!movesWithinPlay) FaceUp = !DeckTypes.FaceDownOnEntry(area.Type);
        HasRegisteredTokens |= DeckTypes.GrantsTokenPool(area.Type);
    }

    // Preflight can project a departure without creating a new incarnation,
    // turning a face, or registering token pools. The projection is always
    // restored before control returns to gameplay code.
    internal void ProjectTo(Area area) => Area = area;
}
