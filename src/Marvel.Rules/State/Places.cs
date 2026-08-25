using Marvel.Rules.Events;

namespace Marvel.Rules.State;

/// <summary>
/// Rules that resolve by <i>where a card is</i> rather than by what it says.
/// </summary>
/// <remarks>
/// <para>
/// Two published scenarios need a card's place to change what its own text
/// means, and neither of them changes the text. Fear No Evil's Protection
/// Racket gives every player their own main scheme, so "the main scheme" means
/// a different card depending on who says it. The Once and Future Kang splits
/// the table into game areas that cards cannot reach across, so whether an
/// ability may touch a card depends on where both of them are.
/// </para>
/// <para>
/// <b>The point of putting them here is that no card knows which scenario it is
/// in.</b> The rulebooks state these in terms of place, so an engine that
/// models place gets them for every card at once, and one that special-cases the
/// scenario gets them for the cards somebody remembered. Fear No Evil makes the
/// argument itself: a crisis icon on a side scheme "prevents threat from being
/// removed from any main scheme" precisely <i>because</i> a side scheme is in no
/// player's play area — the exception is a consequence of placement rather than
/// a clause about crisis icons.
/// </para>
/// <para>
/// <b>Every rule here is trivially true in an ordinary game.</b> One game area
/// holding every play area, one main scheme in the villain's play area: every
/// predicate answers the same thing it would if none of this existed. That is
/// the property to preserve — the cost of modelling these is meant to be paid
/// only by the scenarios that need them.
/// </para>
/// <para>
/// <b>Not verifiable against the corpus, and that is measured rather than
/// assumed.</b> The v2 digest cannot see a play area (MARVEL-174): creating a
/// game area on the legacy engine and moving 47 cards into it left the digest
/// byte-identical. Kang reaches a second game area in 0 of 3,462 steps across
/// all 42 recorded scenes, and <c>py_src</c> has no Fear No Evil cards at all.
/// So these are held against the published rules, quoted at each one, and the
/// tests cite the rule they come from.
/// </para>
/// </remarks>
public static class Places
{
    /// <summary>How an area is named on the wire.</summary>
    /// <remarks>
    /// <para>
    /// <b><c>AreaRef.Owner</c> is the play area, not the card's controller and
    /// not <c>Area.CardOwner</c>.</b> The wire type has said so in prose since
    /// MARVEL-163; this is the one conversion, so the two cannot drift into
    /// disagreeing about which of three seat-shaped numbers it carries.
    /// </para>
    /// <para>
    /// <c>Id</c> travels because a zone name and an owner do not identify an
    /// area: measured over 6,554 corpus steps, <c>(zone, owner, host)</c>
    /// collides on 5,969 of them.
    /// </para>
    /// </remarks>
    /// <param name="area">The area.</param>
    public static AreaRef Reference(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        return new AreaRef(
            Zone: area.Type.ToString(),
            Owner: area.PlayArea.Player,
            Host: area.Host,
            Id: area.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Which play area a card is in.</summary>
    /// <remarks>
    /// <c>rr:play-area.3</c>: "A card cannot be in more than one play area at a
    /// time." A card is in an area, an area sits in one play area, so this is
    /// total and single-valued by construction rather than by a check.
    /// </remarks>
    /// <param name="card">The card.</param>
    public static PlayArea PlayAreaOf(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Area.PlayArea;
    }

    /// <summary>Which game area a card is in, or <c>null</c> when it is in none.</summary>
    /// <param name="world">The world.</param>
    /// <param name="card">The card.</param>
    public static GameArea? GameAreaOf(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.GameAreaOf(PlayAreaOf(card));
    }

    /// <summary>Whether one card may affect another at all.</summary>
    /// <remarks>
    /// <para>
    /// <c>pack:mc11:game-areas</c>: "Cards and components in one game area
    /// cannot affect another game area […] Players cannot attack or defend
    /// enemies in other game areas, and they cannot target any game elements in
    /// the other game areas." <c>pack:mc55:game-areas</c> says the same for God
    /// of Lies' Epic Multiplayer Mode.
    /// </para>
    /// <para>
    /// <b>A card in no game area reaches everything.</b> Kang's stage 2B
    /// "remains in play in a central location and its text remains active for
    /// all players, though it is not part of any other game area"
    /// (<c>pack:mc11:areas</c>) — and mc11 names exactly that card as the
    /// exception to the partition. So the exception is a consequence of where
    /// 2B sits, and needs no flag on the card.
    /// </para>
    /// <para>
    /// <b>The target direction is inferred, not quoted.</b> The rules say a card
    /// in no game area affects everyone; they do not say in as many words
    /// whether everyone can affect it. This treats reach as symmetric, which
    /// keeps 2B thwartable by the players racing it. If a scenario ever
    /// distinguishes the two directions, this is the line that has to split.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="source">The card doing the affecting.</param>
    /// <param name="target">The card being affected.</param>
    public static bool CanAffect(World world, Card source, Card target)
    {
        ArgumentNullException.ThrowIfNull(world);
        var from = GameAreaOf(world, source);
        var to = GameAreaOf(world, target);
        return from is null || to is null || ReferenceEquals(from, to);
    }

    /// <summary>Which seats "each player" means, said by this card.</summary>
    /// <remarks>
    /// <para>
    /// <c>pack:mc11:rules-clarifications</c>:
    /// </para>
    /// <blockquote>
    /// Q: How do I resolve encounter card effects that refer to "each player"
    /// while I am in my own game area? A: "Each player" refers to each player in
    /// the same game area. If you are the only person in your game area, then
    /// "each player" refers only to you.
    /// </blockquote>
    /// <para>
    /// The second sentence is the one worth keeping: the answer is not "the
    /// other players in your game area", it is "the players in your game area",
    /// which includes you and can be just you. An implementation that read it as
    /// "the others" would be wrong by one in the case the clarification exists
    /// to settle.
    /// </para>
    /// <para>
    /// A card in no game area reaches everybody, for the same reason as
    /// <see cref="CanAffect"/> — which is what makes Kang's 2B "active for all
    /// players".
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="source">The card that said "each player".</param>
    public static IReadOnlyList<int> EachPlayer(World world, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        var area = GameAreaOf(world, source);
        var seats = new List<int>();
        for (int seat = 0; seat < world.Players; seat++)
        {
            if (area is null || area.Contains(PlayArea.Of(seat)))
            {
                seats.Add(seat);
            }
        }

        return seats;
    }

    /// <summary>Which cards "the main scheme" means, said by this card.</summary>
    /// <remarks>
    /// <para>
    /// <c>pack:mc60:separate-main-schemes</c>:
    /// </para>
    /// <blockquote>
    /// Each main scheme is in the play area of the player who chose it. […]
    /// Cards in a player's play area (identities, allies, upgrades, minions,
    /// etc.) that refer to "the main scheme" refer only to the main scheme in
    /// the same play area. Cards that are not in any player's play area (the
    /// villain, side schemes, and environments) that refer to "the main scheme"
    /// apply to all main schemes.
    /// </blockquote>
    /// <para>
    /// <b>"Not in any player's play area" is exactly the villain's play area</b>,
    /// which is not a coincidence: <c>rr:play-area.2</c> defines the villain's
    /// play area as holding "any encounter cards in play that have not entered a
    /// player's play area (such as side schemes or environments)" — the same
    /// three examples Fear No Evil lists. The two rulebooks describe one
    /// partition, so the rule needs one test.
    /// </para>
    /// <para>
    /// <b>An event a player plays resolves from their play area</b>, because
    /// their hand and discard pile are in it (<c>rr:play-area.1</c>). So Fear No
    /// Evil's separate sentence about events and treacheries needs no separate
    /// case here — it falls out of where the card is.
    /// </para>
    /// <para>
    /// <b>The narrowing applies only when the source's play area actually holds
    /// a main scheme, and that condition is not in the rulebook.</b> Fear No
    /// Evil's sentence presupposes its own setup, where every player has one. In
    /// an ordinary game the single main scheme is in the <i>villain's</i> play
    /// area (<c>rr:play-area.2</c>: "The villain's play area contains the
    /// villain deck, main scheme deck…"), so reading the sentence literally
    /// would answer <i>nothing</i> for every ally and upgrade in every ordinary
    /// game ever played. The condition is what makes one rule cover both, and it
    /// is local — no scan of the whole board, no flag saying which scenario this
    /// is.
    /// </para>
    /// <para>
    /// The game-area partition is applied on top. Both rules say what a card
    /// cannot reach, so the answer is the intersection; in an ordinary game
    /// neither narrows anything.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="source">The card that said "the main scheme".</param>
    public static IReadOnlyList<Card> MainSchemes(World world, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        var from = PlayAreaOf(source);
        var inPlay = new List<Card>();
        bool narrow = false;
        foreach (var card in world.Cards)
        {
            if (card.Area.Type != DeckType.MainSchemesArea)
            {
                continue;
            }

            inPlay.Add(card);
            narrow |= from.IsPlayers && PlayAreaOf(card) == from;
        }

        var found = new List<Card>();
        foreach (var card in inPlay)
        {
            // Narrowed to this play area only when this play area has one --
            // otherwise every ally in an ordinary game would refer to nothing.
            if (narrow && PlayAreaOf(card) != from)
            {
                continue;
            }

            if (CanAffect(world, source, card))
            {
                found.Add(card);
            }
        }

        return found;
    }
}
