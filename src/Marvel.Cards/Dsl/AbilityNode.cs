namespace Marvel.Cards.Dsl;

/// <summary>
/// One named operation in an ability tree, and its arguments.
/// </summary>
/// <remarks>
/// <para>
/// In JSON a node is an object with exactly one key: <c>{"gainSurge": 1}</c>,
/// <c>{"if": {"test": …, "then": …}}</c>. The one key is the
/// <see cref="Kind"/> and everything under it is the
/// <see cref="Argument"/>, whatever shape it has.
/// </para>
/// <para>
/// The interpreter switches on <see cref="Kind"/> and asks the argument for
/// what it needs — a field, a list, a word, a nested node. A kind nothing knows
/// throws by name, which is what makes an unimplemented card fail loudly at the
/// node rather than quietly at the board.
/// </para>
/// </remarks>
/// <param name="Kind">The operation's name, e.g. <c>seq</c>, <c>giveStatus</c>.</param>
/// <param name="Argument">
/// Everything under that name, exactly as written. A map of fields, a list, a
/// single value, or a single nested node — which of those it is, is the
/// interpreter's business and not the reader's.
/// </param>
public sealed record AbilityNode(string Kind, AbilityValue Argument)
{
    /// <summary>Reads a value as a node: a map with exactly one entry.</summary>
    /// <param name="value">The value.</param>
    /// <exception cref="AbilityException">It is not a single named operation.</exception>
    public static AbilityNode Of(AbilityValue value)
    {
        if (value is not AbilityValue.Map map || map.Entries.Count != 1)
        {
            throw new AbilityException($"{Describe(value)} is not a node");
        }

        var (kind, argument) = map.Entries.First();
        return new AbilityNode(kind, argument);
    }

    /// <summary>One named argument, or null when the node does not carry it.</summary>
    /// <param name="name">The argument's name.</param>
    public AbilityValue? Field(string name) =>
        Argument is AbilityValue.Map map ? map.Entry(name) : null;

    /// <summary>One named argument, or a stated failure.</summary>
    /// <param name="name">The argument's name.</param>
    /// <exception cref="AbilityException">The node does not carry it.</exception>
    public AbilityValue Require(string name) =>
        Field(name) ?? throw new AbilityException($"'{Kind}' needs a '{name}'");

    /// <summary>What a value is, for a message a card author can act on.</summary>
    /// <param name="value">The value.</param>
    public static string Describe(AbilityValue value) => value switch
    {
        AbilityValue.Number number => $"the number {number.Value}",
        AbilityValue.Word word => $"'{word.Value}'",
        AbilityValue.List list => $"a list of {list.Values.Count}",
        AbilityValue.Map map => $"a map of [{string.Join(", ", map.Entries.Keys)}]",
        _ => "nothing",
    };
}
