using System.Collections.Immutable;

using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>The immutable player selections accepted for an arrow-cost payment.</summary>
internal sealed record AbilityPaymentSelection(
    ImmutableArray<int> Paying, ImmutableArray<int> Chosen,
    ImmutableDictionary<string, long>? Values, bool ResourcesPaidByEvent, ImmutableArray<ResourceAllocation> Allocations)
{
    internal static AbilityPaymentSelection Capture(IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, IReadOnlyDictionary<string, long>? values = null,
        bool resourcesPaidByEvent = false, IReadOnlyList<ResourceAllocation>? allocations = null) =>
        new([.. paying], [.. chosen], values?.ToImmutableDictionary(StringComparer.Ordinal), resourcesPaidByEvent, [.. allocations ?? []]);
}

