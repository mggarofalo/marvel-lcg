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
/// <b>Not a boolean, and not two options.</b> <c>rr:form-change-form.6</c>
/// permits a card with a "[type] form" keyword to grant a form in addition to
/// hero or alter-ego form. The runtime is Core Set only, but the representation
/// supports that later rules pattern without changing the form model.
/// </para>
/// <para>
/// Additional forms are not emitted into the state digest. The reserved
/// <c>f_&lt;name&gt;</c> namespace is a wire-format boundary that must be specified
/// and pinned before a product using it becomes executable.
/// </para>
/// <para>
/// A foldable identity with more than two faces is a different case again.
/// See <see cref="Change"/>, which refuses to choose a destination the general
/// rule does not settle.
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

    /// <summary>Flips a keyword-form card and schedules the form-change window.</summary>
    /// <remarks>
    /// <c>rr:form-change-form.6.2</c> makes this a form change for triggered
    /// effects but excludes it from the identity's once-per-turn flip limit.
    /// Accordingly this schedules <see cref="Steps.FormChanged"/> and never
    /// writes <see cref="Seat.FormChangedInRound"/>.
    /// </remarks>
    public static void ChangeAdditional(
        World world, Seat seat, ICardFacts facts, Card formCard,
        bool faceUp, int round, string trigger, List<Events.GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(formCard);
        ArgumentNullException.ThrowIfNull(events);

        if (!DeckTypes.IsInPlay(formCard.Area.Type)
            || formCard.Owner != seat.Index
            || facts.FormKeyword(formCard.FaceId) is null)
        {
            throw new Play.RulesNotImplementedException(
                $"card {formCard.ObjectId} does not grant '{seat.Name}' an additional form");
        }

        if (formCard.FaceUp == faceUp)
        {
            return;
        }

        if (faceUp)
        {
            formCard.TurnFaceUp();
        }
        else
        {
            formCard.TurnFaceDown();
        }

        events.Add(new Events.CardsFlipped([formCard.ObjectId], faceUp)
        {
            Trigger = trigger, Verb = "Change_Form",
        });
        world.Agenda.Then(new PhaseStep(
            Steps.FormChanged,
            round,
            0,
            Index: seat.Index,
            Subject: formCard.ObjectId,
            Seat: seat.Index));
    }

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
            // have flipped any time the faceup side of the card changes". The
            // general rule does not settle which face a change arrives at.
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
