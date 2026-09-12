namespace Marvel.Rules.Timing;

/// <summary>
/// What resolves, and in what order, in the window around an occurrence.
/// </summary>
/// <remarks>
/// <para>
/// The tiers come from <c>rr:ability</c>; see <see cref="TimingPriority"/>.
/// This adds the two rules about moving between them.
/// </para>
/// <para>
/// <c>rr:forced.4</c> — for any given triggering condition, forced interrupts
/// initiate before non-forced interrupts, and forced responses before non-forced
/// responses. That is already the tier order, so it needs no separate code; what
/// needs stating is that the ordering is <b>by tier and not by player</b>. A
/// forced interrupt belonging to the last player still goes ahead of the first
/// player's optional one.
/// </para>
/// <para>
/// <c>rr:forced.6</c> — each forced ability resolves as completely as possible
/// before the next one triggered by the same condition may initiate. So a tier
/// is walked one ability at a time with the board re-read between them, never
/// gathered up and applied together.
/// </para>
/// </remarks>
public static class AbilityWindow
{
    /// <summary>
    /// What is waiting in one window, grouped into the tiers that resolve in
    /// order.
    /// </summary>
    /// <remarks>
    /// Abilities not belonging to this window are dropped, as are those on a
    /// card that has already been triggered in it
    /// (<c>rr:triggering-condition.1</c>). Empty tiers are dropped too: an
    /// engine that walked all eight every time would ask about windows nothing
    /// is waiting in.
    /// </remarks>
    /// <param name="pending">Everything eligible, in any order.</param>
    /// <param name="window">Which window is open.</param>
    /// <param name="occurrence">The occurrence, which remembers what has fired.</param>
    public static IReadOnlyList<AbilityTier> Tiers(
        IEnumerable<PendingAbility> pending, WindowKind window, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(occurrence);

        var byTier = new SortedDictionary<TimingPriority, List<PendingAbility>>();
        foreach (var ability in pending)
        {
            if (!BelongsIn(ability.Type, window))
            {
                continue;
            }

            if (!occurrence.MayTrigger(window, ability.Card))
            {
                continue;
            }

            var priority = AbilityTypes.PriorityOf(ability.Type);
            if (!byTier.TryGetValue(priority, out var tier))
            {
                byTier[priority] = tier = [];
            }

            tier.Add(ability);
        }

        return [.. byTier.Select(pair => new AbilityTier(pair.Key, pair.Value))];
    }

    /// <summary>
    /// The abilities in a tier that the game resolves without asking, and those
    /// a player may decline.
    /// </summary>
    /// <remarks>
    /// The split is <c>rr:ability.11</c>: unless prefaced by "Forced", every
    /// interrupt and response is optional. A tier never mixes the two, because
    /// forced and non-forced are different tiers — but the split is stated here
    /// rather than assumed, so that a type added to the wrong tier shows up as a
    /// failing test instead of as an ability nobody is ever offered.
    /// </remarks>
    /// <param name="tier">One tier.</param>
    public static (IReadOnlyList<PendingAbility> Mandatory, IReadOnlyList<PendingAbility> Optional)
        Split(AbilityTier tier) =>
        ([.. tier.Abilities.Where(a => AbilityTypes.IsMandatory(a.Type))],
         [.. tier.Abilities.Where(a => !AbilityTypes.IsMandatory(a.Type))]);

    private static bool BelongsIn(AbilityType type, WindowKind window) => window switch
    {
        WindowKind.Interrupt => AbilityTypes.IsInterrupt(type),
        WindowKind.Response => AbilityTypes.IsResponse(type),
        _ => false,
    };
}
