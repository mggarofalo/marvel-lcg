namespace Marvel.Rules.Prompts;

/// <summary>A numerical value the player must define while initiating a cost.</summary>
/// <param name="Name">The printed variable, such as <c>X</c>.</param>
/// <param name="Min">The smallest legal definition.</param>
/// <param name="Max">The largest legal definition on the current board.</param>
public readonly record struct VariableRequest(string Name, long Min, long Max)
{
    /// <summary>Whether a proposed definition is inside the offered range.</summary>
    public bool Allows(long value) => value >= Min && value <= Max;
}
