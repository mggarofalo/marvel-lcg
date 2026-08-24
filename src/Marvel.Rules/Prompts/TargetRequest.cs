namespace Marvel.Rules.Prompts;

/// <summary>
/// What still has to be chosen before an affordance can resolve.
/// </summary>
/// <param name="Legal">Every object that may be chosen.</param>
/// <param name="Min">Fewest that must be chosen.</param>
/// <param name="Max">Most that may be chosen.</param>
/// <param name="Groups">
/// Complete legal selections, when the rule groups its candidates instead of
/// taking them all. Empty for every ordinary rule.
/// </param>
/// <param name="MustIncludeTraits">
/// Traits the selection has to contain, as <c>t_</c> keys.
/// </param>
/// <param name="Rule">
/// The named selection rule, when one applies. Empty otherwise.
/// </param>
/// <param name="IsSearch">
/// Whether choosing means looking through a hidden zone, which a client should
/// present as a search rather than as picking something already on the table.
/// </param>
/// <remarks>
/// <para>
/// <paramref name="Groups"/> exists because <paramref name="Legal"/> and the
/// min/max cannot express every rule. <c>VillainAndMinionsEngagedWithYou</c>
/// pools every player's minions but accepts exactly one villain plus one
/// player's whole group, so a flat candidate list plus a count is not a legal
/// selection and a client obeying it would build an illegal one. When
/// <paramref name="Groups"/> is non-empty it is authoritative and the flat list
/// is only a hint for highlighting.
/// </para>
/// <para>
/// Measured over 6,351 sampled options: a target request is present on
/// <b>86.5%</b>. Two thirds of those offer exactly one legal target, so the
/// common case is a choice with one answer — but 20% offer two or more, which
/// is why the list travels rather than being collapsed by the engine.
/// <paramref name="Groups"/> and <paramref name="Rule"/> appear on 0.3%, and
/// <paramref name="IsSearch"/> on 2.8%.
/// </para>
/// </remarks>
public sealed record TargetRequest(
    IReadOnlyList<int> Legal,
    int Min,
    int Max,
    IReadOnlyList<IReadOnlyList<int>>? Groups = null,
    IReadOnlyList<string>? MustIncludeTraits = null,
    string Rule = "",
    bool IsSearch = false)
{
    /// <summary>Whether the selection rule constrains it beyond a count.</summary>
    public bool IsGrouped => Groups is { Count: > 0 };
}
