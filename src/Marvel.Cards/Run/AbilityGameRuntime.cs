using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Mutable interpreter work owned by one game. Agenda activation ids are only
// meaningful inside that game. Definitions remain in the immutable program.
internal sealed class AbilityGameRuntime(AbilityProgram program) : AbilityRuntime
{
    private readonly Dictionary<int, List<ActivationEffect>> activationEffects = [];

    internal static AbilityGameRuntime For(World world, AbilityProgram program)
    {
        if (world.AbilityRuntime is null)
            world.BindAbilityRuntime(new AbilityGameRuntime(program));
        if (world.AbilityRuntime is not AbilityGameRuntime runtime
            || !ReferenceEquals(runtime.Program, program))
            throw new InvalidOperationException("the game belongs to a different ability program");
        return runtime;
    }

    private AbilityProgram Program => program;

    internal void AfterActivation(int activation, ActivationEffect effect)
    {
        if (!activationEffects.TryGetValue(activation, out var waiting))
            activationEffects[activation] = waiting = [];
        waiting.Add(effect);
    }

    // Consume before executing so a callback cannot execute the same work twice.
    // The ordered list preserves the order in which abilities registered it.
    internal IReadOnlyList<ActivationEffect> CompleteActivation(int activation) =>
        activationEffects.Remove(activation, out var waiting) ? waiting : [];
}
