using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal sealed class OptionalDamageInterrupts(IWindowAbilities inner)
    : IWindowAbilities
{
    public bool ObservesHeroDefense(World world) => inner.ObservesHeroDefense(world);

    public bool ObservesPlayerTurnBeginning(World world) => inner.ObservesPlayerTurnBeginning(world);
    public bool ObservesVillainStepOneCompletion(World world) => inner.ObservesVillainStepOneCompletion(world);

    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window) =>
        [.. inner.Waiting(world, occurrence, window).Where(ability =>
            window != WindowKind.Interrupt
            || !AbilityTypes.IsMandatory(ability.Type))];

    public AbilityExecutionResult Resolve(World world, AbilityExecution invocation, Decision answer) =>
        inner.Resolve(world, invocation, answer);

    public Affordance Describe(World world, PendingAbility ability) =>
        inner.Describe(world, ability);
}
