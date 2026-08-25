using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>What a card is waiting to do, and what happens when it does.</summary>
/// <remarks>
/// The seam between the timing rules and the cards. Everything in
/// <see cref="Offering"/> is the Rules Reference and none of it knows what any
/// card says.
/// </remarks>
public interface IWindowAbilities
{
    /// <summary>The abilities on the board waiting to act in this window.</summary>
    /// <param name="world">The world.</param>
    /// <param name="occurrence">What is happening.</param>
    /// <param name="window">Which of its two windows is open.</param>
    IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window);

    /// <summary>Resolves one ability that was waiting in a window.</summary>
    /// <param name="world">The world.</param>
    /// <param name="occurrence">What it is timed to.</param>
    /// <param name="ability">Which ability, from <see cref="Waiting"/>.</param>
    IReadOnlyList<GameEvent> Resolve(World world, Occurrence occurrence, PendingAbility ability);

    /// <summary>How to describe one ability to a player who may take it.</summary>
    /// <param name="world">The world.</param>
    /// <param name="ability">The ability being offered.</param>
    Affordance Describe(World world, PendingAbility ability);
}

/// <summary>
/// Working a window: resolving what is forced, and asking only where there is
/// something to ask.
/// </summary>
/// <remarks>
/// <para>
/// <b>Most windows ask nobody anything.</b> Every occurrence in the game opens
/// two, and on an ordinary board almost none of them has an eligible ability in
/// it. A window that finds nothing closes in silence: an engine that asked "any
/// interrupts?" before every threat token would be asking a question that cannot
/// be answered any way but one.
/// </para>
/// <para>
/// So the three cases are kept apart:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Nothing eligible for a player</b> — that player is skipped, with no
///       prompt. This is not the same as declining: they were never asked,
///       because there was nothing to ask about.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>A forced ability</b> — resolved, and the player is <i>told</i> rather
///       than asked. <c>rr:forced.1</c>: forced interrupts and responses "must be
///       resolved when their triggering conditions are met". There is no choice
///       to present, so what reaches the client is an event: this happened.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>An optional ability</b> — offered, always. An interrupt with one
///       ability in it is still a real choice, because <c>rr:ability.11</c>
///       makes declining the other answer. The prompt is cancellable for exactly
///       that reason.
///     </description>
///   </item>
/// </list>
/// </remarks>
public static class Offering
{
    /// <summary>
    /// Carry a window as far as it goes without a player's answer.
    /// </summary>
    /// <remarks>
    /// Returns the prompt the window is waiting on, or <c>null</c> when it
    /// finished on its own — which is the common case by a wide margin.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="abilities">What the cards are waiting to do.</param>
    /// <param name="occurrence">What is happening.</param>
    /// <param name="kind">Which window.</param>
    /// <param name="events">Where to record what resolved.</param>
    public static Prompt? Work(
        World world,
        IWindowAbilities abilities,
        Occurrence occurrence,
        WindowKind kind,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(events);

        if (world.Windows.Current is not { } open
            || open.Occurrence != occurrence || open.Kind != kind)
        {
            world.Windows.Open(occurrence, kind);
        }

        // A window is walked until nothing more happens, not once: resolving a
        // forced ability changes the board, and rr:interrupt.5 is about
        // *further* abilities.
        while (true)
        {
            var tiers = AbilityWindow.Tiers(
                abilities.Waiting(world, occurrence, kind), kind, occurrence);

            var forced = Forced(tiers);
            if (forced.Count == 1)
            {
                occurrence.Trigger(kind, forced[0].Card);
                events.AddRange(abilities.Resolve(world, occurrence, forced[0]));

                // rr:forced.6 -- each resolves as completely as possible before
                // the next initiates, so the board is re-read rather than the
                // rest of this tier being applied from a stale list.
                continue;
            }

            if (forced.Count > 1)
            {
                // rr:forced.5 -- the first player decides the order, regardless
                // of who controls the cards.
                return Ordering(world, abilities, occurrence, kind, forced);
            }

            if (Ask(world, abilities, occurrence, kind, tiers) is { } prompt)
            {
                return prompt;
            }

            return null;
        }
    }

    /// <summary>
    /// The abilities in this window one seat may take.
    /// </summary>
    /// <remarks>
    /// <c>rr:interrupt.1</c> and <c>rr:response.1</c>: "Players can only trigger
    /// interrupt / response abilities on cards they control or on encounter
    /// cards." An ability with no controller is on an encounter card, and
    /// <c>rr:ability.8</c> lets any player use it.
    /// </remarks>
    /// <param name="tiers">The window's tiers.</param>
    /// <param name="seat">Whose opportunity it is.</param>
    public static IReadOnlyList<PendingAbility> Eligible(
        IReadOnlyList<AbilityTier> tiers, int seat)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        return
        [
            .. tiers
                .SelectMany(tier => AbilityWindow.Split(tier).Optional)
                .Where(ability => ability.Player == seat || ability.Player < 0),
        ];
    }

    private static IReadOnlyList<PendingAbility> Forced(IReadOnlyList<AbilityTier> tiers)
    {
        // Only the earliest tier that has anything forced in it: rr:forced.4
        // orders the tiers, and a later tier does not initiate while an earlier
        // one still has something waiting.
        foreach (var tier in tiers)
        {
            var mandatory = AbilityWindow.Split(tier).Mandatory;
            if (mandatory.Count > 0)
            {
                return mandatory;
            }
        }

        return [];
    }

    private static Prompt? Ask(
        World world,
        IWindowAbilities abilities,
        Occurrence occurrence,
        WindowKind kind,
        IReadOnlyList<AbilityTier> tiers)
    {
        // Every player in turn, starting wherever the window's opportunity
        // currently sits. A seat with nothing eligible is skipped rather than
        // asked -- being asked a question with one possible answer is not being
        // given a choice.
        for (int asked = 0; asked < world.Players; asked++)
        {
            if (world.Windows.Current is not { } window)
            {
                return null;
            }

            var eligible = Eligible(tiers, window.Asking);
            if (eligible.Count > 0)
            {
                return new Prompt(
                    Player: window.Asking,
                    Asking: Question.Opportunity,
                    When: eligible.Min(ability => AbilityTypes.PriorityOf(ability.Type)),
                    Trigger: occurrence.Conditions[0],
                    Label: Label(kind, occurrence),

                    // rr:ability.11 -- declining is the other answer, which is
                    // what makes an offer of one ability a real choice.
                    Cancellable: true,
                    Affordances: [.. eligible.Select(ability => abilities.Describe(world, ability))]);
            }

            if (world.Windows.Pass())
            {
                return null;
            }
        }

        return null;
    }

    private static Prompt Ordering(
        World world,
        IWindowAbilities abilities,
        Occurrence occurrence,
        WindowKind kind,
        IReadOnlyList<PendingAbility> forced) =>
        new(
            Player: world.FirstPlayer,
            Asking: Question.Order,
            When: AbilityTypes.PriorityOf(forced[0].Type),
            Trigger: occurrence.Conditions[0],
            Label: $"{Label(kind, occurrence)}: in what order?",

            // rr:forced.1 -- they must all resolve. The choice is the order and
            // nothing else, so declining is not an answer.
            Cancellable: false,
            Affordances: [.. forced.Select(ability => abilities.Describe(world, ability))]);

    private static string Label(WindowKind kind, Occurrence occurrence) =>
        $"{kind} to {string.Join(" and ", occurrence.Conditions)}";
}
