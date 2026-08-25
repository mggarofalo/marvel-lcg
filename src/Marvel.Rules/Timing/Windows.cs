using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>
/// One window, part-way through being offered round the table.
/// </summary>
/// <param name="Occurrence">What the window is timed to.</param>
/// <param name="Kind">Which of the occurrence's two windows this is.</param>
/// <param name="Asking">The seat holding the opportunity right now.</param>
/// <param name="Passed">
/// How many players have declined in a row. The window closes when every player
/// has — <c>rr:interrupt.5</c>, <c>rr:response.4</c>.
/// </param>
public readonly record struct Window(
    Occurrence Occurrence, WindowKind Kind, int Asking, int Passed);

/// <summary>
/// Where the game is, when it is part-way through resolving something.
/// </summary>
/// <remarks>
/// <para>
/// A stack, because windows nest: an interrupt that plays a card is itself an
/// occurrence with windows of its own, and the outer window is still open
/// underneath. <c>rr:initiating-abilities.3</c> is the rule that makes the
/// nesting outlive its source — the sequence "does not stop from completing if
/// that card leaves play during this sequence".
/// </para>
/// <para>
/// <b>Data, and on the board.</b> The alternative is to suspend the engine
/// mid-call and resume it — an iterator or a blocked thread — and that cannot be
/// written to a save, cannot be diffed against a recorded step, and cannot tell
/// a client that the game is two windows deep. So where the game is, is a value
/// like everything else, and it lives on <see cref="World"/> because a
/// half-resolved occurrence has to be saved with the game.
/// </para>
/// <para>
/// The polling is <c>rr:in-player-order.1</c> exactly: "If a sequence performed
/// in player order does not conclude after each player has performed their part
/// of the sequence once, the sequence of opportunities continues in a clockwise
/// manner until it is complete." So a window is not one pass round the table.
/// It keeps going until nobody takes anything.
/// </para>
/// </remarks>
/// <param name="world">The board this belongs to.</param>
public sealed class Windows(World world)
{
    private readonly List<Window> open = [];

    /// <summary>Whether the game is part-way through resolving something.</summary>
    public bool IsResolving => open.Count > 0;

    /// <summary>How deep the nesting goes.</summary>
    public int Depth => open.Count;

    /// <summary>The innermost open window, which is the one being offered.</summary>
    public Window? Current => open.Count > 0 ? open[^1] : null;

    /// <summary>
    /// Open a window and give the first opportunity to the first player.
    /// </summary>
    /// <remarks>
    /// <c>rr:first-player.4</c> and <c>rr:first-player.5</c> say the same thing
    /// for the two windows: "the first player has the first opportunity to use
    /// an interrupt / a response at each appropriate game moment". Not the
    /// active player, and not whoever the occurrence is happening to.
    /// </remarks>
    /// <param name="occurrence">What is happening.</param>
    /// <param name="kind">Which window.</param>
    public Window Open(Occurrence occurrence, WindowKind kind)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        var window = new Window(occurrence, kind, world.FirstPlayer, Passed: 0);
        open.Add(window);
        return window;
    }

    /// <summary>
    /// The player being asked declines. Moves the opportunity on, and closes the
    /// window if that was the last player left to decline.
    /// </summary>
    /// <remarks>
    /// <c>rr:interrupt.5</c>: "Once <b>all</b> players decide they do not wish
    /// to resolve any (further) interrupts to a triggering condition, (further)
    /// interrupts to that instance of that triggering condition cannot be
    /// used." <c>rr:response.4</c> says it again for responses. So one player
    /// declining is not the end of a window — every player declining in a row
    /// is.
    /// </remarks>
    /// <returns>True when the window closed.</returns>
    public bool Pass()
    {
        var window = Innermost();
        var passed = window.Passed + 1;
        if (passed >= world.Players)
        {
            open.RemoveAt(open.Count - 1);
            return true;
        }

        open[^1] = window with { Asking = Next(window.Asking), Passed = passed };
        return false;
    }

    /// <summary>
    /// The player being asked used an ability, so everyone gets asked again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The count of consecutive declines resets, because the board has changed
    /// and the rule is about <i>further</i> abilities: a player who passed on an
    /// empty board may have something to say now. That is the force of
    /// "(further)" in <c>rr:interrupt.5</c>.
    /// </para>
    /// <para>
    /// The opportunity moves to the next player rather than staying put.
    /// <c>rr:in-player-order.1</c> makes a window a continuing clockwise
    /// sequence of opportunities, so the player who just acted takes their next
    /// turn when it comes round — which it will, as long as anyone is still
    /// acting.
    /// </para>
    /// </remarks>
    public void Used()
    {
        var window = Innermost();
        open[^1] = window with { Asking = Next(window.Asking), Passed = 0 };
    }

    /// <summary>
    /// Close the innermost window without polling the rest of the table.
    /// </summary>
    /// <remarks>
    /// For the case <c>rr:interrupt.4</c> describes: "If an interrupt changes
    /// (via a replacement effect) or cancels an imminent triggering condition,
    /// further interrupts to the original triggering condition cannot be
    /// triggered." The occurrence is no longer going to happen, so there is
    /// nothing left to interrupt.
    /// </remarks>
    public void Close()
    {
        Innermost();
        open.RemoveAt(open.Count - 1);
    }

    // rr:in-player-order.2 -- "next player" always means the next clockwise
    // player in player order.
    private int Next(int seat) => (seat + 1) % world.Players;

    private Window Innermost() => open.Count > 0
        ? open[^1]
        : throw new InvalidOperationException("no window is open");
}
