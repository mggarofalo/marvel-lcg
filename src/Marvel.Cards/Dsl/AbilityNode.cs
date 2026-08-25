namespace Marvel.Cards.Dsl;

/// <summary>
/// A value in an ability tree: a number, a word, a list, or another node.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four cases, and no fifth.</b> A card is data, so there is nothing here
/// that can hold a delegate, a lambda, or a reference to compiled code —
/// <c>docs/migration.md</c> makes that a trust boundary rather than a style
/// preference: everything a player can author or download has to be inert.
/// </para>
/// <para>
/// This is the JSON the sketches in <c>docs/card-dsl.md</c> are written in,
/// given types. It is deliberately <i>not</i> a record per node kind: the
/// measured vocabulary is around a hundred actions and three hundred triggers,
/// and a C# type per node would make the vocabulary grow by compiling rather
/// than by authoring — which is the thing this exists to stop.
/// </para>
/// </remarks>
public abstract record AbilityValue
{
    /// <summary>A number, e.g. the <c>1</c> in "draw 1 card".</summary>
    /// <param name="Value">The number.</param>
    public sealed record Number(long Value) : AbilityValue;

    /// <summary>
    /// A bare word: a name, a binding, or a keyword.
    /// </summary>
    /// <remarks>
    /// The DSL's identifiers all arrive this way — <c>this</c>, <c>tough</c>,
    /// <c>trigger.player</c>. What a word means is decided by where it sits,
    /// which is how <c>docs/card-dsl.md</c>'s sketches read.
    /// </remarks>
    /// <param name="Value">The word.</param>
    public sealed record Word(string Value) : AbilityValue;

    /// <summary>A list of values.</summary>
    /// <param name="Values">The items, in order.</param>
    public sealed record List(IReadOnlyList<AbilityValue> Values) : AbilityValue;

    /// <summary>
    /// A map of named values — a JSON object, unchanged.
    /// </summary>
    /// <remarks>
    /// <b>There is no separate "node" case, and that is the point.</b> A node is
    /// a map with exactly one entry, <i>read as one when a node is what the
    /// interpreter wants</i>. Deciding at parse time is not possible:
    /// <c>{"not": {"hasStatus": …}}</c> holds a node and
    /// <c>{"hasStatus": {"card": …, "status": …}}</c> holds two fields, and both
    /// are objects inside an object. A parser that guessed would have to know
    /// the vocabulary, which would make adding a node a change to the reader as
    /// well as to the interpreter.
    /// </remarks>
    /// <param name="Entries">The entries, by name.</param>
    public sealed record Map(IReadOnlyDictionary<string, AbilityValue> Entries) : AbilityValue
    {
        /// <summary>One entry, or null.</summary>
        /// <param name="name">The entry's name.</param>
        public AbilityValue? Entry(string name) =>
            Entries.TryGetValue(name, out var value) ? value : null;
    }
}

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
