namespace Marvel.Tests;

/// <summary>
/// The Rules Reference clause a test holds the engine to.
/// </summary>
/// <remarks>
/// <para>
/// A test asserts something about the game. This says <i>which published rule</i>
/// it is asserting, by citing an id from
/// <c>datasets/rules-reference/index.json</c> — <c>rr:forced.4</c>, the fourth
/// clause of the FORCED entry, and so on.
/// </para>
/// <para>
/// The citation is checked. <see cref="RuleCitations"/> fails the build when an
/// id names no clause, which is the difference between this and the comment it
/// replaces: a comment citing <c>rr:villain-phase.2b</c> stays readable and
/// convincing long after the Rules Reference has renumbered or removed it.
/// </para>
/// <para>
/// What it buys is the other direction. Every clause is enumerated, so the
/// clauses <b>nothing cites</b> are enumerable too, and "what has this engine
/// never been held to?" becomes a list rather than a thing to be discovered by
/// mutating code and seeing what survives.
/// </para>
/// <para>
/// Only tests carry citations. A citation on the implementation would name a
/// rule without claiming anything about it; the claim is the assertion.
/// </para>
/// </remarks>
/// <param name="id">A citation id, e.g. <c>rr:ability.step.2.a</c>.</param>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = false)]
internal sealed class RuleAttribute(string id) : Attribute
{
    /// <summary>The cited id, as it appears in the Rules Reference index.</summary>
    public string Id { get; } = id;

    /// <summary>Why this test is the right place to hold that clause, when it is not obvious.</summary>
    public string? Note { get; set; }
}
