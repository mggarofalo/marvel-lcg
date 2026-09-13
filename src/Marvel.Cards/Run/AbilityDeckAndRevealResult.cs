using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record AbilityDeckAndRevealResult(
    bool IsHandled, bool ResolveEffect, AbilityRevealRequest? Reveal,
    ImmutableDictionary<string, long> Values)
{
    internal static AbilityDeckAndRevealResult Handled { get; } =
        new(true, false, null, ImmutableDictionary<string, long>.Empty);
    internal static AbilityDeckAndRevealResult NotHandled { get; } =
        new(false, false, null, ImmutableDictionary<string, long>.Empty);
}
