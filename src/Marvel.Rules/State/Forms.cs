using Marvel.Rules.Play;

namespace Marvel.Rules.State;

/// <summary>
/// What form a player is in — read off the board, never stored.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:identity</c>: "A player's identity card is a double-sided card that
/// represents their hero on one side and their alter-ego on the other. <b>The
/// side that is face up indicates the form</b> <i>(hero or alter-ego)</i> that
/// player is currently in." So form is a question the board already answers.
/// Anything storing it would be a second copy that can disagree with the card,
/// and <c>rr:form-change-form.3</c> guarantees the two ways of changing it do
/// not go through one place.
/// </para>
/// <para>
/// <b>Not a boolean, and not two options.</b> <c>rr:form-change-form.6</c>:
/// cards with the "[type] form" keyword grant an identity forms that are "in
/// addition to the identity's alter-ego and hero forms, and they come with
/// their own conditions for changing into them". Measured over the 4,344-card
/// pool, three such types exist on nine faces the engine has —
/// <c>energy</c> (Spectrum's Gamma, Photon and Pulsar), <c>mass</c> (Vision's
/// Intangible and Dense, Shadowcat's Solid and Phased) and <c>suit</c> (Nick
/// Fury's Assault and Stealth). Every one of them is a <b>set-aside permanent
/// on a separate card</b>, never a face of the identity.
/// </para>
/// <para>
/// Those forms <i>coexist</i> with hero form rather than replacing it:
/// <c>21002</c> Gamma reads "Spectrum gets +2 ATK" and "<b>Hero</b> Response",
/// which only parses if Spectrum is in hero form while an energy form is
/// faceup. So a player is in a <b>set</b> of forms, and this returns a set.
/// </para>
/// <para>
/// <b>Nothing here is emitted into the state digest yet, on purpose.</b>
/// <c>docs/state-digest-v2.md</c> reserves an <c>f_&lt;name&gt;</c> namespace for
/// form keys and says they "come from game data, so the key set is open-ended
/// and a port cannot enumerate it from a fixed schema" — which is this type's
/// claim in the digest's words. But no recording shows one, so which card
/// carries the key (the identity, or the card granting the form) and what its
/// value counts are both unknown, and the digest is a wire format where a
/// guessed key changes every game outcome. Naming a form is a rules question
/// and is answered here; putting one on the wire waits for a recording.
/// </para>
/// <para>
/// A second hero face on the identity card is a different thing again and is
/// <i>not</i> a keyword form: Ant-Man <c>12001a/c</c>, Wasp <c>13001a/c</c> and
/// Angel <c>42001a</c>/<c>42001c</c> Archangel are foldable three-sided cards
/// under <c>rr:flip.1</c>. See <see cref="Change"/>, which refuses them by name.
/// </para>
/// </remarks>
public static class Forms
{
    /// <summary>Hero form. <c>rr:hero-hero-form</c>.</summary>
    public const string Hero = "hero";

    /// <summary>Alter-ego form. <c>rr:alter-ego-alter-ego-form</c>.</summary>
    public const string AlterEgo = "alter-ego";

    /// <summary>
    /// The form a single face puts a player in, or null if it is not an
    /// identity face.
    /// </summary>
    /// <param name="faceId">A printed card id.</param>
    /// <param name="facts">The printed card data.</param>
    public static string? OfFace(string faceId, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Kind(faceId) switch
        {
            CardKind.Hero => Hero,
            CardKind.AlterEgo => AlterEgo,
            _ => null,
        };
    }

    /// <summary>
    /// Every form a seat is currently in.
    /// </summary>
    /// <remarks>
    /// The faceup side of the identity card, plus every faceup card in play
    /// this player owns that carries a "[type] form" keyword. Both halves are
    /// read fresh: <c>rr:modifiers</c>'s "the game constantly checks" applies
    /// to this as much as to a variable quantity.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="seat">Whose forms.</param>
    /// <param name="facts">The printed card data.</param>
    public static IReadOnlySet<string> Of(World world, Seat seat, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(facts);

        // Sorted, and ordinally. Non-negotiable 1 in `AGENTS.md` forbids
        // "iteration over unordered set/dict where order can affect game
        // state", and a form set is read to decide what a card may do -- a hash
        // set would hand two runs of one game different orders.
        var forms = new SortedSet<string>(StringComparer.Ordinal);
        if (seat.IdentityCard is { } identity && OfFace(identity.FaceId, facts) is { } own)
        {
            // `rr:identity.4` -- the faceup side is in play and the facedown
            // side is out of play, so there is no question of being in both.
            forms.Add(own);
        }

        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                if (card.Owner == seat.Index && card.FaceUp
                    && facts.FormKeyword(card.FaceId) is { } granted)
                {
                    forms.Add(granted);
                }
            }
        }

        return forms;
    }

    /// <summary>Whether a seat is in one particular form.</summary>
    /// <param name="world">The board.</param>
    /// <param name="seat">Whose form.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="form">The form's name.</param>
    public static bool In(World world, Seat seat, ICardFacts facts, string form) =>
        Of(world, seat, facts).Contains(form);

    /// <summary>
    /// Flip a seat's identity card to its other side.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:form-change-form.2</c>: "When a player changes form, <b>only the
    /// form changes.</b> The character retains their sustained damage, status
    /// cards, lasting effects, attached cards, tucked cards, tokens, and
    /// current state <i>(ready or exhausted)</i>." Nothing here touches any of
    /// them, and that is the implementation — a face change on one card, with
    /// every other field of that card left alone.
    /// </para>
    /// <para>
    /// <b>This is the specific rule beating a general one.</b>
    /// <c>rr:flip.2.2</c> says a card whose new face has a <i>different card
    /// type</i> discards its attached cards, tucked cards, status cards and
    /// tokens — and hero and alter-ego are different card types. Applied here
    /// it would throw away exactly what <c>rr:form-change-form.2</c> says is
    /// kept. The form rule is the specific one and wins.
    /// </para>
    /// </remarks>
    /// <param name="seat">Whose identity flips.</param>
    /// <param name="facts">The printed card data.</param>
    /// <returns>The face that is showing afterwards.</returns>
    /// <exception cref="RulesNotImplementedException">
    /// The identity has more than two faces.
    /// </exception>
    public static string Change(Seat seat, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(facts);

        var identity = seat.IdentityCard;
        if (identity.Faces.Count != 2)
        {
            // `rr:flip.1` -- "a foldable, 'three-sided' card is considered to
            // have flipped any time the faceup side of the card changes". Three
            // identities in the pool are built that way: Ant-Man `12001a/c`,
            // Wasp `13001a/c` and Angel `42001a` / Archangel `42001c`. Which
            // hero face a flip from alter-ego arrives at is a choice the
            // rulebook does not settle here, and guessing it would put a wrong
            // stat line on the board -- Archangel prints THW 0 where Angel
            // prints 2.
            throw new RulesNotImplementedException(
                $"'{seat.Name}' has an identity of {identity.Faces.Count} faces "
                + $"({string.Join(", ", identity.Faces)}), and which one a flip arrives at "
                + "is not implemented");
        }

        string was = identity.FaceId;
        identity.TurnTo(identity.Faces[identity.FaceIndex == 0 ? 1 : 0]);
        return was;
    }

    /// <summary>Change form and schedule the occurrence that follows the flip.</summary>
    /// <remarks>
    /// <c>rr:after</c> makes a response wait until the form change has
    /// concluded. The flip is applied first and the agenda step supplies the
    /// interrupt and response windows, so an ability on the newly showing face
    /// is active when the response window is read.
    /// </remarks>
    public static string ChangeAndSchedule(World world, Seat seat, ICardFacts facts, int round)
    {
        ArgumentNullException.ThrowIfNull(world);
        string was = Change(seat, facts);
        world.Agenda.Then(new PhaseStep(
            Steps.FormChanged,
            round,
            0,
            Index: seat.Index,
            Subject: seat.IdentityCard.ObjectId,
            Seat: seat.Index));
        return was;
    }
}
