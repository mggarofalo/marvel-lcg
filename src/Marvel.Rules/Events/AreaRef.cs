namespace Marvel.Rules.Events;

/// <summary>
/// Which ordered list of cards an event is talking about.
/// </summary>
/// <param name="Zone">The <c>DeckType</c> member name, e.g. <c>HandsArea</c>.</param>
/// <param name="Owner">
/// The player the <b>area</b> belongs to, or <c>-1</c> for the scenario.
/// </param>
/// <param name="Host">The card the area hangs off, or <c>-1</c>.</param>
/// <remarks>
/// <para>
/// A zone name alone does not identify an area. <c>HandsArea</c> names as many
/// areas as there are players, and <c>UpgradesArea</c> as many as there are
/// hosts, so an event that says only "to <c>HandsArea</c>" is ambiguous the
/// moment a second player exists.
/// </para>
/// <para>
/// <b><see cref="Owner"/> is the area's owner, not the card's controller.</b>
/// This distinction is the single easiest thing to get wrong here, and it is
/// not hypothetical: the state digest records the <i>card's</i> controller in
/// its <c>owner</c> field, and the two genuinely differ. A side scheme
/// controlled by player 3 sits in the scenario's side-scheme area alongside
/// cards with no controller at all, so grouping the digest's records by
/// <c>(zone, owner, host)</c> splits one area into two and renumbers across the
/// join.
/// </para>
/// <para>
/// The engine has real area objects and can fill this in exactly. A consumer
/// reconstructing areas from a digest cannot — see
/// <c>docs/event-stream.md</c>, "Why the digest cannot verify position".
/// </para>
/// </remarks>
public readonly record struct AreaRef(string Zone, int Owner, int Host)
{
    /// <summary>An area owned by the scenario rather than a player.</summary>
    public static AreaRef Scenario(string zone) => new(zone, -1, -1);

    /// <summary>An area belonging to a player.</summary>
    public static AreaRef Player(string zone, int player) => new(zone, player, -1);

    /// <summary>An area hanging off a card, such as its upgrades.</summary>
    public static AreaRef On(string zone, int owner, int host) => new(zone, owner, host);

    /// <inheritdoc/>
    public override string ToString() =>
        Host >= 0 ? $"{Zone}[p{Owner}]@c{Host}" : $"{Zone}[p{Owner}]";
}
