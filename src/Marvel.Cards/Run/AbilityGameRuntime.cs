using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Mutable interpreter work owned by one game. Agenda activation ids are only
// meaningful inside that game. Definitions remain in the immutable program.
internal sealed class AbilityGameRuntime
{
    private readonly Dictionary<int, List<ActivationEffect>> activationEffects = [];

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

internal sealed record ActivationEffect(
    int Source, int Player, AbilityType? Tier, AbilityEffect Effect, int Altered,
    int AbilityActor);
