namespace Marvel.Rules.Timing;

/// <summary>One ability waiting in a window.</summary>
/// <param name="Card">The object id of the card carrying it.</param>
/// <param name="Type">Its bold timing trigger.</param>
/// <param name="Player">
/// The seat that would resolve it, or <c>-1</c> for an ability on an encounter
/// card that no player has claimed. <c>rr:ability.8</c> lets any player use an
/// optional ability on an encounter card, so who resolves it is settled when it
/// is offered, not when it is collected.
/// </param>
/// <param name="Ordinal">
/// Which ability at this timing on the card is waiting, in printed/data order.
/// Most cards have one and therefore use zero.
/// </param>
public readonly record struct PendingAbility(
    int Card, AbilityType Type, int Player, int Ordinal = 0);

/// <summary>Everything waiting at one priority, which resolves before the next.</summary>
/// <param name="Priority">The tier.</param>
/// <param name="Abilities">What is waiting in it.</param>
/// <remarks>
/// A tier holding more than one ability is a <b>decision</b>, not an ordering
/// this code can make: <c>rr:forced.5</c> gives the choice to the first player,
/// and <c>rr:simultaneous-resolution</c> says the same of any two effects
/// sharing a bold trigger. Returning the group rather than a sorted list is what
/// keeps that choice visible instead of quietly resolving it by object id.
/// </remarks>
public readonly record struct AbilityTier(TimingPriority Priority, IReadOnlyList<PendingAbility> Abilities);

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
