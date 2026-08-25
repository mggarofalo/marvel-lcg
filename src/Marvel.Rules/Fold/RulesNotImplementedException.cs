namespace Marvel.Rules.Fold;

/// <summary>
/// The fold reached a rule this engine does not have yet.
/// </summary>
/// <remarks>
/// <para>
/// A type of its own rather than <see cref="NotImplementedException"/>, because
/// in a rules engine being ported one rule at a time these are a large,
/// enumerable and expected category, and a test needs to tell "the villain phase
/// is not written yet" from "somebody left a stub in".
/// </para>
/// <para>
/// <b>Where it is thrown from decides what the caller still holds, and the two
/// cases are different.</b> A decision the fold refuses — taking an affordance
/// rather than declining one — is refused before the world is touched, so the
/// caller keeps the board they had. A rule reached <i>part-way through</i> a
/// phase cannot promise that: the villain phase places threat and discards a
/// boost card before it ever reveals the card it cannot resolve, and unwinding
/// that would need a snapshot.
/// </para>
/// <para>
/// So treat the world as <b>unusable</b> after catching one from inside a
/// phase. Re-folding from a snapshot plus inputs is how the engine recovers
/// anyway (<c>docs/presentation-layer.md</c>), which is why this is a
/// documented property rather than a transaction.
/// </para>
/// </remarks>
public sealed class RulesNotImplementedException : Exception
{
    /// <summary>Creates the exception.</summary>
    public RulesNotImplementedException()
        : base("this engine does not have that rule yet")
    {
    }

    /// <summary>Creates the exception.</summary>
    /// <param name="message">What is missing.</param>
    public RulesNotImplementedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception.</summary>
    /// <param name="message">What is missing.</param>
    /// <param name="innerException">The cause.</param>
    public RulesNotImplementedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
