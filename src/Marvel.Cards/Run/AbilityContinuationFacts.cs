using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed record AbilityContinuationFacts(
    bool HasPath, ImmutableArray<AbilityContinuationFrame> Frames)
{
    internal static AbilityContinuationFacts Empty { get; } = new(false, []);
}
