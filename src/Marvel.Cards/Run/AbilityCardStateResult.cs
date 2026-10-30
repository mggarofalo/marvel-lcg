using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed class AbilityCardStateResult
{
    internal List<Card> Discarded { get; } = [];
    internal Dictionary<string, long> Values { get; } = new(StringComparer.Ordinal);
    /// <summary>A ready instruction whose observed would-ready window the resolver must schedule.</summary>
    internal CardReadyBatch? InterruptedReady { get; set; }
}
