using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card actions and their player-facing descriptions.</summary>
public interface ICardActionAbilities : IAbilityDescriptions
{
    IReadOnlyList<PendingAbility> Actions(World world, int player);
    IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null);
    IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, Occurrence occurrence,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null);
}
#pragma warning restore CS1591
