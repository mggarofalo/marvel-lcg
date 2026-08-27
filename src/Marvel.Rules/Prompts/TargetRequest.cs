namespace Marvel.Rules.Prompts;

/// <summary>
/// What still has to be chosen before an affordance can resolve.
/// </summary>
/// <param name="Legal">Every object that may be chosen.</param>
/// <param name="Min">
/// Fewest that must be chosen. <b>Meaningless when <see cref="Groups"/> is
/// non-empty</b> — see the remarks.
/// </param>
/// <param name="Max">Most that may be chosen. Same caveat.</param>
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
/// <b>A grouped request's <see cref="Min"/> and <see cref="Max"/> describe the
/// pooled candidate list, not a legal selection, and a client that enforced
/// them would reject legal play.</b> Measured in MARVEL-164: the bot played
/// Explosive Arrow — <i>"choose a player → deal 3 damage to the villain and
/// each minion engaged with that player"</i> — against a player with one
/// minion. Two groups were offered, <c>[villain, minion A]</c> and
/// <c>[villain, minion B]</c>; the flat range said <c>[3, 3]</c>, because three
/// cards were in the pool; the legal selection had two. Use
/// <see cref="Allows"/> rather than reading the fields.
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

    /// <summary>Whether <paramref name="selection"/> is a legal answer.</summary>
    /// <remarks>
    /// The two readings are mutually exclusive on purpose. When groups are
    /// present they are authoritative and the count is not applied at all;
    /// when they are absent the flat list and the count are. Encoding it here
    /// rather than describing it is deliberate: the rule was stated in prose
    /// on this type from the start, and MARVEL-164 still found a real
    /// selection that prose would have rejected.
    /// </remarks>
    public bool Allows(IReadOnlyList<int> selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        // `rr:choose-game-element.3.1`: "The same target cannot be chosen
        // multiple times this way." This applies before either representation
        // below: a grouped selection does not make two references to one
        // object into two targets, and a flat request's count must not be met
        // by repeating one legal id.
        if (selection.Distinct().Count() != selection.Count)
        {
            return false;
        }

        if (IsGrouped)
        {
            return Groups!.Any(group => selection.All(group.Contains));
        }

        return selection.Count >= Min
            && selection.Count <= Max
            && selection.All(Legal.Contains);
    }
}
