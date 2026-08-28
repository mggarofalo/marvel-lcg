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
/// <see cref="Faces"/> can be <i>replaced</i> and not merely flipped: the Ultron
/// scenario turns a player card into a facedown drone in place, keeping the
/// object id. Nothing at setup does that, but the model has to allow it.
/// </para>
/// </remarks>
public sealed class Card
{
    private readonly Dictionary<string, long> tokens = new(StringComparer.Ordinal);

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
    public int Owner { get; }

    /// <summary>Where the card is.</summary>
    public Area Area { get; private set; } = null!;

    /// <summary>Whether the card is face up.</summary>
    public bool FaceUp { get; private set; } = true;

    /// <summary>
    /// Whether this card has ever held its token pools.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not the same as being in play, and the digest keeps the
    /// difference.</b> A card acquires its token pools when it enters play and
    /// <i>never gives them back</i>: the recorded milestone board shows
    /// <c>01105</c> with no <c>k_threat</c> key while it sits in the encounter
    /// deck, and with <c>k_threat: 0</c> once it has been revealed — still
    /// there two steps later, from the discard pile.
    /// </para>
    /// <para>
    /// Absent and zero are different in a digest, so this is the difference
    /// between a card that never had a threat pool and one whose pool is empty.
    /// </para>
    /// </remarks>
    public bool HasRegisteredTokens { get; private set; }

    /// <summary>Whether the card is ready. <c>is_exhaust</c> is its negation.</summary>
    public bool Ready { get; private set; } = true;

    /// <summary>
    /// Damage on the card. <c>rr:damage</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not a token pool, and that is measured rather than chosen.</b> The
    /// digest records a character's remaining <c>health</c> and no damage key
    /// at all — <c>StateFields</c> says so in as many words — so damage is what
    /// is subtracted from printed hit points, not something counted beside
    /// them. Putting it in <see cref="Tokens"/> would register a key the
    /// recorded boards do not have.
    /// </para>
    /// <para>
    /// <c>rr:damage.4</c>: damage stays on a character until it is healed or
    /// the character leaves play, which is why this is state on the card rather
    /// than a number an attack carries.
    /// </para>
    /// </remarks>
    public long Damage { get; private set; }

    /// <summary>
    /// Tokens sitting on this card, by the digest's own key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Threat on a scheme, damage counters, and anything else the engine keys
    /// with a <c>k_</c> prefix. Live state, unlike everything printed — the
    /// main scheme's <c>k_threat</c> starts at its <c>StartingThreat</c> and
    /// climbs every villain phase.
    /// </para>
    /// <para>
    /// <b>Absent and zero are different, and the digest keeps the
    /// distinction.</b> Which <c>k_</c> keys a card registers is decided by its
    /// kind and whether it is in play (<c>StateFields.Keys</c>); this only says
    /// how many are there. A card out of play registers none of them however
    /// many this holds.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, long> Tokens => tokens;

    /// <summary>Puts tokens on the card.</summary>
    /// <param name="kind">The digest's key, e.g. <c>k_threat</c>.</param>
    /// <param name="count">How many. Negative removes.</param>
    public void PlaceTokens(string kind, long count)
    {
        ArgumentNullException.ThrowIfNull(kind);
        long total = (tokens.TryGetValue(kind, out long held) ? held : 0) + count;

        // Clamped rather than allowed negative: "remove 2 threat" from a scheme
        // holding 1 removes 1, and a scheme holding -1 threat would complete on
        // the wrong turn.
        tokens[kind] = Math.Max(0, total);
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
    public void TakeDamage(long amount) => Damage = Math.Max(0, Damage + amount);

    /// <summary>Exhausts the card. <c>rr:exhaust-ready</c>.</summary>
    public void Exhaust() => Ready = false;

    /// <summary>Readies the card. <c>rr:exhaust-ready</c>.</summary>
    public void Refresh() => Ready = true;

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
        if (DeckTypes.IsInPlay(area.Type)
            && (Area is null || !DeckTypes.IsInPlay(Area.Type)))
        {
            Incarnation++;
        }

        Area = area;
        FaceUp = !DeckTypes.FaceDownOnEntry(area.Type);
        HasRegisteredTokens |= DeckTypes.GrantsTokenPool(area.Type);
    }
}
