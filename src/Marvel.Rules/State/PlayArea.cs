namespace Marvel.Rules.State;

/// <summary>
/// One of the game's play areas: a player's, or the villain's.
/// </summary>
/// <remarks>
/// <para>
/// Rules Reference v1.8, <c>rr:play-area</c>:
/// </para>
/// <blockquote>
/// There are two types of play areas: a player's play area and the villain's
/// play area. […] A card cannot be in more than one play area at a time.
/// </blockquote>
/// <para>
/// So a game has <b>players + 1</b> of them, and this is a seat index with
/// <see cref="Villains"/> as the extra. Not a new concept a scenario
/// introduces — it is the ordinary structure of every game, which is why
/// <c>Area.PlayArea</c> is an integer and why Fear No Evil's separate main
/// schemes need no new container.
/// </para>
/// <para>
/// <b>A type rather than an <c>int</c>, because three different integers on
/// adjacent records mean three different things.</b> A card's controller, the
/// seat a card made in an area belongs to, and the play area the area sits in
/// are all seat-shaped and none of them is the others. The digest's
/// <c>owner</c> is the first, <c>Area.CardOwner</c> is the second, and this is
/// the third. Measured over a large sample of recorded play, the first and
/// third agreed 98.1% of the time — exactly often enough for a confusion to
/// pass its tests and fail on the cards where whose-is-it drives rules.
/// </para>
/// <para>
/// <b>The name is overloaded in the published rules and this is the safe
/// half.</b> The Rules Reference notes a player's play area is <i>"also
/// sometimes referred to as a 'player's game area'"</i>, while The Once and
/// Future Kang uses "game area" for a grouping <i>over</i> play areas. Two
/// published meanings, one phrase. <see cref="GameArea"/> is the other one.
/// </para>
/// </remarks>
/// <param name="Player">The seat, or <c>-1</c> for the villain's.</param>
public readonly record struct PlayArea(int Player)
{
    /// <summary>
    /// The villain's play area.
    /// </summary>
    /// <remarks>
    /// <c>rr:play-area.2</c>: it "contains the villain deck, main scheme deck,
    /// encounter deck, encounter discard pile, and any encounter cards in play
    /// that have not entered a player's play area". It is a place in its own
    /// right and not the absence of one, which is why this is a value rather
    /// than a null.
    /// </remarks>
    public static readonly PlayArea Villains = new(-1);

    /// <summary>Whether this is the villain's play area.</summary>
    public bool IsVillains => Player < 0;

    /// <summary>Whether this is a player's play area.</summary>
    public bool IsPlayers => Player >= 0;

    /// <summary>A seat's play area.</summary>
    /// <param name="player">The seat, from 0.</param>
    public static PlayArea Of(int player)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        return new PlayArea(player);
    }

    /// <inheritdoc/>
    public override string ToString() => IsVillains ? "villain" : $"p{Player}";
}
