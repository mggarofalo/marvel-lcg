using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Values already produced by resolution, captured for one expression read.
// These are inputs, not access to the mutable result or payment collections.
internal sealed record AbilityExpressionContext(
    AbilityQueryContext Bindings,
    ImmutableDictionary<string, long> Results, ImmutableArray<Card> Discarded,
    string Payment, long PowerAmount, bool FinalStep, int? ProjectedPlayAreaPlayer)
{
    internal World World => Bindings.World;
    internal Card Source => Bindings.Source;
    internal Occurrence Occurrence => Bindings.Occurrence;
    internal int Player => Bindings.Player;
}
