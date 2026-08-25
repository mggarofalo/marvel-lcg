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
/// <b>Thrown before anything is applied.</b> A caller that catches this still
/// holds a world in the state it was in before the fold, so the boundary is a
/// place the engine stops rather than a place it half-finishes.
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
