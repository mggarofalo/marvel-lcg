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
