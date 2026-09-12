using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.CardPayment;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardControlTransfer;
using static Marvel.Rules.Play.CardEntry;
using static Marvel.Rules.Play.CardPlay;

namespace Marvel.Rules.Play;

/// <summary>Evaluates play restrictions and legal attachment targets.</summary>
public static class CardPlayLegality
{

    /// <summary>
    /// <c>rr:initiating-abilities.step.2</c> — the play restrictions.
    /// </summary>
    /// <remarks>
    /// Only the ones the rules state generally. A card's own restrictions are
    /// its text, which is <c>src/Marvel.Cards</c>'s business, and a card that
    /// has one is not filtered here — it fails when its ability runs.
    /// </remarks>
    /// <summary>
    /// Whether a team-up card's two characters are both on the table —
    /// <c>rr:team-up</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "A card with the team-up keyword cannot be played unless <b>both</b> of
    /// the named friendly characters <i>(identity or ally)</i> are in play",
    /// which <c>.1</c> writes out as two separate requirements — one character
    /// matching name 1 and one matching name 2.
    /// </para>
    /// <para>
    /// <b>"Friendly" is every player's, not yours.</b> <c>rr:friendly</c> is
    /// one sentence — "a blanket term that refers to cards <b>the players</b>
    /// control" — so at a table the other player's Wasp is the Wasp this card
    /// needs. Twenty-eight cards print the keyword and every one of them names
    /// two heroes, which is the shape a solo game cannot satisfy without an
    /// ally.
    /// </para>
    /// <para>
    /// <c>rr:team-up.2</c>: "an ally counts as a named character if
    /// <b>either its title or subtitle</b> matches", so both are checked and
    /// on identities as well — an identity's two faces print different titles
    /// and only the faceup one is in play (<c>rr:identity.4</c>).
    /// </para>
    /// <para>
    /// The deck-building half of <c>rr:team-up.1</c> — "you cannot include this
    /// card in your deck unless your alter-ego or hero title matches" — is not
    /// here. The decks are given to this engine already built, and a rule about
    /// what may go in one has nothing to check at play time.
    /// </para>
    /// </remarks>
    internal static bool TeamedUp(World world, ICardFacts facts, Card card)
    {
        if (Characteristics.IsLost(world, card, "team-up")
            || !facts.Attributes(card.FaceId).TryGetValue("TeamUp", out string? named))
        {
            return true;
        }

        var characters = world.Areas
            .Where(area => area.Type is DeckType.HeroArea or DeckType.AlliesArea)
            .SelectMany(area => area.Cards)
            .ToList();

        return named
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .All(name => characters.Any(character => Matches(facts, character, name)));
    }

    /// <summary>Whether one character in play is the character a name means.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:team-up.2</c> for the plain case: "an ally counts as a named
    /// character if <b>either its title or subtitle</b> matches". Only the
    /// faceup side of an identity is in play (<c>rr:identity.4</c>), so a
    /// player who has flipped down is not the hero the card names.
    /// </para>
    /// <para>
    /// <b>A slash names one character by two of its names.</b> "Heart of the
    /// Panther" prints <i>Team-Up (Black Panther/T'Challa and Black
    /// Panther/Shuri)</i>, because two identities share the hero title Black
    /// Panther and the alter-ego is what tells them apart. No card is titled
    /// "Black Panther/T'Challa", so the notation has to be read rather than
    /// matched.
    /// </para>
    /// <para>
    /// It is read against every one of the identity's faces, and
    /// <c>rr:unique-icon.1.2</c> is why that is not a liberty: the rules
    /// already use an identity's <b>alter-ego title</b> as one of its
    /// identifying names — "the identity with the T'Challa alter-ego, the
    /// T'Challa ally, and the Black Panther ally with the subtitle 'T'Challa'
    /// are all considered to match". Reading only the faceup side would make
    /// the notation name nothing at all, since neither face carries both
    /// halves.
    /// </para>
    /// </remarks>
    internal static bool Matches(ICardFacts facts, Card character, string name)
    {
        var halves = name.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (halves.Length == 1)
        {
            return Named(facts, character.FaceId, name);
        }

        return halves.All(half => character.Faces.Any(face => Named(facts, face, half)));
    }

    internal static bool Named(ICardFacts facts, string faceId, string name) =>
        string.Equals(facts.Title(faceId), name, StringComparison.Ordinal)
        || string.Equals(facts.Subtitle(faceId), name, StringComparison.Ordinal);

    internal static bool Permitted(
        World world, ICardFacts facts, Seat seat, Card card,
        IReadOnlyList<int>? targets = null, ICardPlayAbilities? abilities = null,
        bool outOfPlayPermission = false)
    {
        if (!PermittedLocation(seat, card, outOfPlayPermission)) return false;

        // `rr:dash-value.1`: a dash cost cannot be paid; the card may only
        // enter play through another effect. The dataset represents that both
        // as a literal dash and, on card types never normally played, as no
        // Cost field at all.
        if (!Resources.HasPlayableCost(card.FaceId, facts))
        {
            return false;
        }

        // `rr:play-put-into-play.1` and `rr:form-change-form.7`: cards with the
        // text "[type] form only" can only be played by a player whose identity
        // is in that form.
        if (facts.RequiredForm(card.FaceId) is { } form
            && !Forms.In(world, seat, facts, form))
        {
            return false;
        }

        // `rr:team-up` -- "a card with the team-up keyword cannot be played
        // unless both of the named friendly characters (identity or ally) are
        // in play".
        if (!TeamedUp(world, facts, card))
        {
            return false;
        }

        // `rr:unique-icon.4.1`: a matching unique player card cannot be
        // played or put into play while its counterpart is in play.
        if (Uniqueness.IsBlocked(world, facts, card))
        {
            return false;
        }

        if (!WithinPerPlayerLimit(
                world, facts, seat, card, targets, abilities ?? world.CardPlayAbilities))
        {
            return false;
        }

        // `rr:player-turn.2` lists what may be played from hand as a turn
        // option: "an ally, upgrade, support, or player side scheme card".
        // **A resource card is not among them** -- `rr:resource-card` says its
        // "primary function is to be discarded from a player's hand to generate
        // resources", and the pool agrees: `01088` Energy prints no cost at all.
        //
        // An event is played a different way, through `rr:player-turn.5.d`
        // ("trigger an Action ability on an event card in their hand, by
        // playing that event"), which is why it is here and not offered by
        // `Price`.
        return PlayableKind(facts.Kind(card.FaceId));
    }

    private static bool PermittedLocation(Seat seat, Card card, bool outOfPlayPermission) =>
        outOfPlayPermission
            ? card.Owner == seat.Index && !DeckTypes.IsInPlay(card.Area.Type)
            : card.Area == seat.Hand;

    private static bool PlayableKind(CardKind kind) =>
        kind is CardKind.Ally or CardKind.Upgrade or CardKind.Support or CardKind.Event;

    /// <summary>Checks a printed “Max N per player” against the destination controller.</summary>
    public static bool WithinPerPlayerLimit(
        World world, ICardFacts facts, Seat seat, Card card, IReadOnlyList<int>? targets,
        ICardPlayAbilities abilities)
    {
        long maximum = facts.PrintedValue(card.FaceId, "MaxPerUnit", world.Players);
        if (maximum <= 0)
        {
            return true;
        }

        // The extractor preserves which printed unit the maximum names.
        // Synthetic facts written before that field existed mean "per player";
        // this is an engine compatibility choice, not a Rules Reference term.
        string unit = facts.Attributes(card.FaceId)
            .GetValueOrDefault("MaxPerUnitKind", "player");
        IReadOnlyList<int>? eligible = facts.Kind(card.FaceId) == CardKind.Upgrade
            ? abilities.AttachmentTargets(world, card)
            : null;
        if (string.Equals(unit, "player", StringComparison.Ordinal))
            return WithinPlayerLimit(world, facts, seat, card, targets, eligible, maximum);

        // `rr:max-maximum.4`: every non-player unit is an attachment-host
        // maximum. AttachmentTargets has already applied the printed host
        // restriction (ally, enemy, scheme, and so on); this check asks the
        // separate question of whether that host already has this title.
        if (eligible is null && targets is not { Count: > 0 })
        {
            // A generic offer can precede attachment-target selection. There
            // is no host maximum to test until the ability layer supplies a
            // legal host; Play calls this method again with the chosen target.
            return true;
        }

        IEnumerable<int> hosts = targets is { Count: > 0 } ? targets : eligible!;
        return hosts.Any(host =>
            (eligible is null || eligible.Contains(host))
            && CountAttached(world, facts, card, host) < maximum);
    }

    private static bool WithinPlayerLimit(
        World world, ICardFacts facts, Seat seat, Card card,
        IReadOnlyList<int>? targets, IReadOnlyList<int>? eligible, long maximum)
    {
        if (eligible is null) return CountControlled(world, facts, card, seat.Index) < maximum;
        IEnumerable<int> hosts = targets is { Count: > 0 } ? targets : eligible;
        return hosts.Any(host => eligible.Contains(host)
            && world.Cards[host].Area.PlayArea is { IsPlayers: true } area
            && CountControlled(world, facts, card, area.Player) < maximum);
    }

    /// <summary>Attachment targets that also satisfy the card's printed maximum.</summary>
    public static IReadOnlyList<int>? LegalAttachmentTargets(
        World world, ICardFacts facts, Seat seat, Card card, ICardPlayAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(abilities);

        var targets = abilities.AttachmentTargets(world, card);
        return targets is null
            ? null
            : [.. targets.Where(target =>
                WithinPerPlayerLimit(world, facts, seat, card, [target], abilities))];
    }

    internal static int CountControlled(
        World world, ICardFacts facts, Card card, int controller)
    {
        string title = facts.Title(card.FaceId);
        return world.Areas
            .Where(area => area.PlayArea == PlayArea.Of(controller))
            .SelectMany(area => area.Cards)
            .Count(inPlay => DeckTypes.IsInPlay(inPlay.Area.Type)
                && string.Equals(facts.Title(inPlay.FaceId), title, StringComparison.Ordinal));
    }

    internal static int CountAttached(
        World world, ICardFacts facts, Card card, int host)
    {
        string title = facts.Title(card.FaceId);
        return world.Areas
            .Where(area => area.Host == host && DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Count(attached => string.Equals(
                facts.Title(attached.FaceId), title, StringComparison.Ordinal));
    }
}
