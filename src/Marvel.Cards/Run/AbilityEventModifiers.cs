using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Live event modifiers, read when a cost or effect executes.</summary>
internal static class AbilityEventModifiers
{
    internal static long Amount(World world, Card source, string kind) =>
        AbilityAmounts.SaturatingSum(0, Effects(world, source, kind).Select(effect => effect.Amount));

    internal static IReadOnlyList<ContinuousEffect> Effects(World world, Card source, string kind)
    {
        if (world.Facts.Kind(source.FaceId) != CardKind.Event) return [];
        return [.. world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, kind, StringComparison.Ordinal)
            && effect.Affects == source.ObjectId)];
    }
}
