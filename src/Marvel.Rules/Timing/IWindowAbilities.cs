using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>What a card is waiting to do, and what happens when it does.</summary>
public interface IWindowAbilities : IAbilityDescriptions
{
    /// <summary>The abilities on the board waiting to act in this window.</summary>
    /// <param name="world">The world.</param>
    /// <param name="occurrence">What is happening.</param>
    /// <param name="window">Which of its two windows is open.</param>
    IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window);

    /// <summary>Resolves one ability that was waiting in a window.</summary>
    /// <remarks>
    /// <b>The payment is the player's, and a window is where it arrives.</b>
    /// <c>rr:initiating-abilities.step.5</c> pays before step 6 resolves, and
    /// nothing in that sequence is about which tier the ability sits in — a
    /// response with a cost is priced by <see cref="IAbilityDescriptions.Describe"/>, paid from the
    /// answer, and resolved here. A forced ability passes an empty list,
    /// because nobody was asked.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="occurrence">What it is timed to.</param>
    /// <param name="ability">Which ability, from <see cref="Waiting"/>.</param>
    /// <param name="paying">
    /// The generators the player spent, by <c>ResourceSource.Effect</c>.
    /// </param>
    /// <param name="chosen">
    /// The objects the player chose for it, in the order they were chosen —
    /// including any a <i>cost</i> asked for, which <c>rr:cost</c> makes a
    /// choice like any other.
    /// </param>
    IReadOnlyList<GameEvent> Resolve(
        World world,
        Occurrence occurrence,
        PendingAbility ability,
        IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen);

    /// <summary>Resolves a window ability with explicit variable and icon decisions.</summary>
    /// <param name="world">The world.</param>
    /// <param name="occurrence">What it is timed to.</param>
    /// <param name="ability">Which ability is resolving.</param>
    /// <param name="paying">The selected generators.</param>
    /// <param name="chosen">The chosen game elements.</param>
    /// <param name="values">Numerical variables defined for the cost.</param>
    /// <param name="allocations">Generated icons assigned to cost components.</param>
    IReadOnlyList<GameEvent> Resolve(
        World world,
        Occurrence occurrence,
        PendingAbility ability,
        IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<Play.ResourceAllocation>? allocations = null);
}
