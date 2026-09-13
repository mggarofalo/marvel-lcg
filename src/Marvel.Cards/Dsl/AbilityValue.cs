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
/// This syntax tree retains the shape of the authored JSON. Semantic lowering
/// maps supported operations to the closed instruction types in
/// <see cref="AbilityProgram"/>. Cards compose those operations as inert data;
/// adding a card needs no new CLR type, while adding an operation requires an
/// engine implementation before its spelling is admitted.
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
