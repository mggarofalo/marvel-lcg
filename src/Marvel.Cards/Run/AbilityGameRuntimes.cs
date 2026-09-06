using System.Runtime.CompilerServices;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Per-world delayed activation work. Weak keys keep a shared interpreter from
// retaining abandoned games; each value preserves authored registration order.
internal sealed class AbilityGameRuntimes
{
    private readonly ConditionalWeakTable<World, AbilityGameRuntime> games = new();

    internal void AfterActivation(World world, int activation, ActivationEffect effect)
    {
        ArgumentNullException.ThrowIfNull(world);
        games.GetValue(world, static _ => new AbilityGameRuntime())
            .AfterActivation(activation, effect);
    }

    internal IReadOnlyList<ActivationEffect> CompleteActivation(World world, int activation)
    {
        ArgumentNullException.ThrowIfNull(world);
        return games.TryGetValue(world, out var runtime)
            ? runtime.CompleteActivation(activation)
            : [];
    }
}
