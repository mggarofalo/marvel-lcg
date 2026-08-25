namespace Marvel.Rules.Timing;

/// <summary>Which of an occurrence's two windows is open.</summary>
public enum WindowKind
{
    /// <summary>Before the occurrence resolves — <c>rr:interrupt.3</c>.</summary>
    Interrupt,

    /// <summary>After it has resolved — <c>rr:response</c>.</summary>
    Response,
}

/// <summary>
/// One thing happening in the game, and the two windows around it.
/// </summary>
/// <remarks>
/// <para>
/// A triggering condition is "a specific occurrence that takes place in the
/// game" (<c>rr:triggering-condition</c>). This is that occurrence, and it
/// exists as an object rather than a moment for one reason: two rules are about
/// an occurrence's <i>identity</i> and cannot be written without it.
/// </para>
/// <para>
/// <b>Once each.</b> <c>rr:triggering-condition.1</c> — each interrupt and each
/// response may be triggered only once per occurrence of its triggering
/// condition, though <c>rr:triggering-condition.1.1</c> lets two copies of a
/// card each trigger. So the bookkeeping is per card, not per printed face.
/// </para>
/// <para>
/// <b>One window, however many conditions.</b>
/// <c>rr:triggering-condition.2</c> — a single game occurrence that creates
/// several triggering conditions, such as one attack that both damages a
/// character and defeats it, is handled with a single interrupt window and a
/// single response window. An engine that opened a window per condition would
/// let one interrupt fire twice against what the rules call one moment.
/// </para>
/// </remarks>
/// <param name="Id">Distinguishes this occurrence from another of the same shape.</param>
/// <param name="Conditions">
/// Every triggering condition this occurrence creates. More than one is the
/// <c>rr:triggering-condition.2</c> case, and they still share these windows.
/// </param>
public sealed record Occurrence(int Id, IReadOnlyList<string> Conditions)
{
    private readonly HashSet<(WindowKind Window, int Card)> spent = [];

    /// <summary>An occurrence creating a single triggering condition.</summary>
    /// <param name="id">Distinguishes this occurrence from another of the same shape.</param>
    /// <param name="condition">What happened.</param>
    public Occurrence(int id, string condition)
        : this(id, [condition])
    {
    }

    /// <summary>Whether a card's ability may still be triggered in this window.</summary>
    /// <param name="window">Which window.</param>
    /// <param name="card">The object id of the card carrying the ability.</param>
    public bool MayTrigger(WindowKind window, int card) => !spent.Contains((window, card));

    /// <summary>Record that a card's ability has been triggered in this window.</summary>
    /// <remarks>
    /// Keyed on the card's object id, so two copies of the same printed card
    /// each get a turn — <c>rr:triggering-condition.1.1</c>.
    /// </remarks>
    /// <param name="window">Which window.</param>
    /// <param name="card">The object id of the card carrying the ability.</param>
    /// <returns>False when it had already been triggered.</returns>
    public bool Trigger(WindowKind window, int card) => spent.Add((window, card));
}
