namespace Marvel.Rules.Events;

/// <summary>
/// Which ordered list of cards an event is talking about.
/// </summary>
/// <param name="Zone">The <c>DeckType</c> member name, e.g. <c>HandsArea</c>.</param>
/// <param name="Owner">
/// The player whose board the <b>area</b> sits on, or <c>-1</c> for the
/// scenario.
/// </param>
/// <param name="Host">The card the area hangs off, or <c>-1</c>.</param>
/// <param name="Id">
/// The area's own identity. Empty only when an area was described rather than
/// identified — see the remarks.
/// </param>
/// <remarks>
/// <para>
/// A zone name alone does not identify an area. <c>HandsArea</c> names as many
/// areas as there are players, and <c>UpgradesArea</c> as many as there are
/// hosts, so an event that says only "to <c>HandsArea</c>" is ambiguous the
/// moment a second player exists.
/// </para>
/// <para>
/// <b><see cref="Owner"/> is the area's, not the card's controller.</b> This is
/// the single easiest thing to get wrong here, because the two agree
/// <b>98.1%</b> of the time — the Rules Reference moves a character to its new
/// controller's play area when control changes, so for characters the play area
/// follows control.
/// </para>
/// <para>
/// The exceptions are all named rules and all load-bearing. A minion engaged
/// with you is in your play area and controlled by the scenario. A player side
/// scheme is yours and is "placed next to the main scheme in the villain's play
/// area" — measured, the side-scheme area holds controllers <c>-1</c>, <c>0</c>
/// and <c>2</c> simultaneously. An upgrade on a card in the villain's play area
/// is yours and is not in your play area. Reading one field as the other passes
/// nearly every test and fails on precisely the cards where whose-is-it drives
/// rules.
/// </para>
/// <para>
/// <b>And it is not <c>Deck2.GetOwner()</c> either.</b> The minions engaged
/// with a player are <i>owned</i> by the scenario and <i>sit</i> in front of
/// that player; the engine records the second as <c>play_area</c>. Reading only
/// the first answers <c>-1</c> for every player's engagement area at once, and
/// that mistake alone accounted for 380 of the 621 ambiguous steps in the first
/// measurement of MARVEL-163.
/// </para>
/// <para>
/// <b><see cref="Owner"/> is a play area, and that is load-bearing for more
/// than multiplayer.</b> The Rules Reference defines exactly two kinds — a
/// player's and the villain's — and a card is in exactly one. Scenarios lean on
/// that: Fear No Evil's Protection Racket gives each player their own main
/// scheme, <i>"in the play area of the player who chose it"</i>, and resolves
/// "the main scheme" relative to the play area of whatever card said it. That
/// needs no new field here — <c>AreaRef("MainSchemesArea", owner: 2, …)</c>
/// already says it — and no new datum on a card. It needs the engine to resolve
/// by place.
/// </para>
/// <para>
/// The Once and Future Kang's <i>game areas</i> are a different concept wearing
/// a confusingly similar name: a grouping <b>over</b> play areas that cards
/// cannot affect across. That grouping is not expressible here and should not
/// be — it belongs to the engine's state, and a player joins one. See
/// <c>docs/event-stream.md</c>, "Play areas and game areas", and MARVEL-175.
/// </para>
/// <para>
/// <b>Why <see cref="Id"/> exists.</b> The three fields above were the whole
/// type until MARVEL-163 counted how often they collide. They are not a key:
/// two areas share a triple for <c>AsideDeck</c> — a
/// three-hero game has three set-aside nemesis decks, all owned by the
/// scenario, all hanging off nothing — and again for <c>RemovedArea</c>. So the
/// triple describes an area and <see cref="Id"/> addresses it. An engine has
/// real area objects and fills this in exactly; a consumer reconstructing areas
/// from a digest cannot, and leaves it empty. See <c>docs/event-stream.md</c>.
/// </para>
/// </remarks>
public readonly record struct AreaRef(string Zone, int Owner, int Host, string Id = "")
{
    /// <summary>An area owned by the scenario rather than a player.</summary>
    public static AreaRef Scenario(string zone, string id = "") => new(zone, -1, -1, id);

    /// <summary>An area belonging to a player.</summary>
    public static AreaRef Player(string zone, int player, string id = "") =>
        new(zone, player, -1, id);

    /// <summary>An area hanging off a card, such as its upgrades.</summary>
    public static AreaRef On(string zone, int owner, int host, string id = "") =>
        new(zone, owner, host, id);

    /// <summary>Whether this addresses an area rather than merely describing one.</summary>
    public bool IsIdentified => Id.Length > 0;

    /// <inheritdoc/>
    public override string ToString()
    {
        string place = Host >= 0 ? $"{Zone}[p{Owner}]@c{Host}" : $"{Zone}[p{Owner}]";
        return IsIdentified ? $"{place}#{Id}" : place;
    }
}
