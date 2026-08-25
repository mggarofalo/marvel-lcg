namespace Marvel.Rules.State;

/// <summary>
/// A grouping over play areas that cards cannot reach across.
/// </summary>
/// <remarks>
/// <para>
/// The Once and Future Kang, <c>pack:mc11:game-areas</c>:
/// </para>
/// <blockquote>
/// Cards and components in one game area cannot affect another game area […]
/// Players cannot attack or defend enemies in other game areas, and they cannot
/// target any game elements in the other game areas. While the players are in
/// separate game areas, they continue to use the same encounter deck and
/// encounter discard pile.
/// </blockquote>
/// <para>
/// Three properties follow, and all three are load-bearing:
/// </para>
/// <list type="bullet">
/// <item>
/// <b>It contains play areas, not cards.</b> Stage 3A goes "directly in front
/// of your play area" (<c>pack:mc11:areas</c>). So this holds
/// <see cref="PlayArea"/> values and a card's game area is looked up through
/// the play area it sits in.
/// </item>
/// <item>
/// <b>It is a visibility partition, not a partition of the world.</b> The
/// encounter deck stays shared across every game area, so this must never be
/// used to decide where a card physically is — only what may reach it.
/// </item>
/// <item>
/// <b>It holds any number of players, including none.</b> Kang gives each
/// player their own, which makes one the tempting model. God of Lies does not:
/// <c>pack:mc55:game-areas</c> has "a collection of 1 to 4 players who work as
/// a team to fight the villain in their game area", and puts Loki himself in "a
/// neutral game area that is outside of any group's game area" with no players
/// in it at all.
/// </item>
/// </list>
/// <para>
/// <b>An ordinary game has exactly one</b>, containing every play area, and
/// nothing in the rules distinguishes that from having none. Every predicate
/// here is therefore trivially true in the one-area case, which is what makes
/// the model cost nothing when unused.
/// </para>
/// <para>
/// <b>Not an event, and not a card tag.</b> The legacy engine tags every card
/// with <c>card.game_area</c> and keeps one deck per zone regardless; that is an
/// implementation shortcut rather than the rule. PR #115 modelled a split as 47
/// cards changing a tag and was reverted: the unit is a player joining a game
/// area. See <c>docs/event-stream.md</c>, "Play areas and game areas".
/// </para>
/// </remarks>
public sealed class GameArea
{
    private readonly HashSet<PlayArea> playAreas = [];

    internal GameArea(int id) => Id = id;

    /// <summary>This game area's identity, unique within the world.</summary>
    public int Id { get; }

    /// <summary>The play areas grouped into this one.</summary>
    public IReadOnlyCollection<PlayArea> PlayAreas => playAreas;

    /// <summary>Whether a play area is part of this game area.</summary>
    /// <param name="area">The play area.</param>
    public bool Contains(PlayArea area) => playAreas.Contains(area);

    internal bool Add(PlayArea area) => playAreas.Add(area);

    internal bool Remove(PlayArea area) => playAreas.Remove(area);

    /// <inheritdoc/>
    public override string ToString() =>
        $"game area {Id} [{string.Join(", ", playAreas.Select(area => area.ToString()))}]";
}
