namespace Marvel.Rules.Play;

/// <summary>
/// An occurrence's identity.
/// </summary>
/// <remarks>
/// <c>rr:ability</c> puts an interrupt window before every occurrence and a
/// response window after it, and <see cref="Agenda"/> is where that shape
/// lives. What is left here is the one thing an occurrence needs that a step
/// cannot work out for itself: a number that tells it apart from every other.
/// </remarks>
public static class Moment
{
    /// <summary>An occurrence's id, unique within a game.</summary>
    /// <remarks>
    /// <c>rr:triggering-condition.1</c> is per occurrence, so two threat
    /// placements in the same round must not share an id or the second would
    /// find every interrupt already spent.
    /// </remarks>
    /// <param name="round">Which round.</param>
    /// <param name="step">Which step of the phase — the Rules Reference's number.</param>
    /// <param name="index">Which repetition within that step, e.g. which player.</param>
    public static int Id(int round, int step, int index) =>
        ((round * 100) + step) * 100 + index;
}
