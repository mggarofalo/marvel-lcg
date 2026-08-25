namespace Marvel.Rules.Fold;

/// <summary>
/// One answer to one prompt. The fold's input.
/// </summary>
/// <param name="Affordance">
/// Which of the prompt's affordances is being taken, by
/// <c>Affordance.Id</c>, or <c>-1</c> to decline.
/// </param>
/// <param name="Targets">
/// The objects chosen for it, in the order they were chosen. Empty when the
/// affordance takes no targets, and empty on a decline.
/// </param>
/// <param name="Resources">
/// The generators spent to pay for it, by <c>ResourceSource.Effect</c>. Empty
/// when the affordance is free, and empty on a decline.
/// </param>
/// <remarks>
/// <para>
/// The Python engine's answer is a JSON <c>CommandDescriptor</c> and its decline
/// is the empty object <c>{}</c> — which is what
/// <c>tools/determinism/headless.py</c> returns for every prompt, and therefore
/// the only input the recorded fixtures exercise. This type is the same thing
/// without the string: <c>id</c>, <c>targets</c> and <c>resources</c>.
/// </para>
/// <para>
/// <b>Paying is a decision, and it is not the targeting decision.</b>
/// <c>rr:initiating-abilities</c> separates them into different steps — step 2
/// checks play restrictions, step 3 determines the cost, step 5 pays it — and
/// the numbers say the same: <c>docs/affordances.md</c> measures 22.1% of priced
/// affordances offering five ways to pay and 21.6% offering six, against 7.3%
/// offering exactly one. <c>CostOption.Sources</c> has modelled the menu since
/// MARVEL-169; this is where the answer goes.
/// </para>
/// <para>
/// It is also not new information. <c>tools/affordances/verify.py</c> already
/// holds every recorded payment against that menu — "every generator it spent is
/// in <c>CostOption.Sources</c>" — so a C# engine without this field could not
/// replay a recorded payment at all, whatever else changed.
/// </para>
/// <para>
/// <b>The order of <paramref name="Targets"/> is kept.</b> Several rules care
/// which was chosen first — the order minions activate in, the order cards go
/// back on top of a deck — so collapsing this to a set would lose a decision the
/// player made.
/// </para>
/// <para>
/// <b>Declining is not nothing.</b> Answering the main-turn prompt with a
/// decline ends the turn, which is progress in the game's terms even though the
/// board is untouched at the instant the next prompt is asked. Measured over
/// 2,966 recorded transitions, 1,031 left the digest unchanged and 320 of those
/// were declines. See <c>docs/no-op-decisions.md</c>.
/// </para>
/// </remarks>
public sealed record Decision(
    int Affordance,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int>? Resources = null)
{
    /// <summary>Take nothing. The engine's empty command.</summary>
    public static readonly Decision Decline = new(-1, []);

    /// <summary>Whether this answer takes no affordance.</summary>
    public bool IsDecline => Affordance < 0;

    /// <summary>The generators spent, never null.</summary>
    public IReadOnlyList<int> Spent => Resources ?? [];

    /// <summary>Takes an affordance with no targets.</summary>
    /// <param name="affordance">The affordance's id, from the prompt that offered it.</param>
    public static Decision Take(int affordance) => new(affordance, []);

    /// <summary>Takes an affordance, paying for it with these generators.</summary>
    /// <param name="affordance">The affordance's id, from the prompt that offered it.</param>
    /// <param name="targets">The objects chosen for it, in order.</param>
    /// <param name="paying">
    /// The generators spent, by <c>ResourceSource.Effect</c>. Order is not kept:
    /// <c>rr:modifiers.2</c> treats a cost's modifiers as simultaneous, and
    /// nothing in <c>rr:initiating-abilities</c> step 5 makes the order of
    /// payment observable.
    /// </param>
    public static Decision Take(
        int affordance, IReadOnlyList<int> targets, IReadOnlyList<int> paying) =>
        new(affordance, targets, paying);
}
