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
/// the single easiest thing to get wrong here, and it is not hypothetical: the
/// state digest records the <i>card's</i> controller in its <c>owner</c> field,
/// and the two genuinely differ. A side scheme controlled by player 3 sits in
/// the scenario's side-scheme area alongside cards with no controller at all.
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
/// <b>An area is not a table.</b> When a scenario splits the board — The Once
/// and Future Kang, and newer scenarios — the areas stay shared: every main
/// scheme sits in one <c>MainSchemesArea</c> whatever board it belongs to. So
/// <see cref="Id"/> addresses a deck and says nothing about which table to draw
/// it on. That lives on the card, and moves through
/// <see cref="CardsChangedBoard"/>. A client laying out two tables splits an
/// area's contents by card, not by <see cref="AreaRef"/>.
/// </para>
/// <para>
/// <b>Why <see cref="Id"/> exists.</b> The three fields above were the whole
/// type until MARVEL-163 replayed the corpus against engine state and counted.
/// They are not a key: two areas share a triple for <c>AsideDeck</c> — a
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
