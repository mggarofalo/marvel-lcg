using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Engine-local immutable arguments for one resource payment instruction.</summary>
internal sealed record CardResourcePaymentRequest
{
    internal required IReadOnlyList<Area> Hands { get; init; }
    internal required IReadOnlyList<int> Paying { get; init; }
    internal required long Cost { get; init; }
    internal required string Required { get; init; }
    internal required int Itself { get; init; }
    internal required int Payer { get; init; }
    internal Card? PayingFor { get; init; }
    internal Card? AbilitySource { get; init; }
    internal IReadOnlyDictionary<int, int>? ResourcePayers { get; init; }
}
