using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal sealed class OptionalDamageInterrupts(IWindowAbilities inner)
    : IWindowAbilities
{
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window) =>
        [.. inner.Waiting(world, occurrence, window).Where(ability =>
            window != WindowKind.Interrupt
            || !AbilityTypes.IsMandatory(ability.Type))];

    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen) =>
        inner.Resolve(world, occurrence, ability, paying, chosen);

    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null) =>
        inner.Resolve(world, occurrence, ability, paying, chosen, values, allocations);

    public Affordance Describe(World world, PendingAbility ability) =>
        inner.Describe(world, ability);
}
