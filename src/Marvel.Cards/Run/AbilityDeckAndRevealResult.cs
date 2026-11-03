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
    internal EncounterEntryRequest? Entry { get; init; }
    /// <summary>The number of encounter cards a batched minion entry discards.</summary>
    internal long? MinionBatch { get; init; }
    internal static AbilityDeckAndRevealResult Handled =>
        new(true, false, null, ImmutableDictionary<string, long>.Empty);
    internal static AbilityDeckAndRevealResult NotHandled =>
        new(false, false, null, ImmutableDictionary<string, long>.Empty);
}
