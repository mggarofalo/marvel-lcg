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
    Occurrence Occurrence, WindowKind Kind, int Asking, int Passed)
{
    /// <summary>Whether every player has passed on this window's optional "would" interrupts.</summary>
    /// <remarks>
    /// rr:would gives "would" interrupts "a higher timing priority" than interrupts
    /// to the same condition without it, so optional ones are offered first and the
    /// others only after every player has passed on them.
    /// </remarks>
    public bool PastWould { get; init; }
}
