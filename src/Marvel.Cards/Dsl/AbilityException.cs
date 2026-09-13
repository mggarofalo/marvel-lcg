namespace Marvel.Cards.Dsl;

/// <summary>An ability's data is wrong, or names something nothing implements.</summary>
/// <remarks>
/// Distinct from <c>RulesNotImplementedException</c>, which is the engine
/// meeting a rule it does not have. This is a card meeting a <i>node</i> nothing
/// has: the same distinction as a malformed program versus an unimplemented
/// library.
/// </remarks>
public sealed class AbilityException : Exception
{
    /// <summary>Says what is wrong with the ability data.</summary>
    /// <param name="message">What is wrong.</param>
    public AbilityException(string message)
        : base(message)
    {
    }

    /// <summary>Says what is wrong, and what caused it.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="inner">What caused it.</param>
    public AbilityException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <summary>An ability failure with nothing to say. Required by the analyzer.</summary>
    public AbilityException()
    {
    }
}
