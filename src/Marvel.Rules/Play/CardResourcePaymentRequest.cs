using Marvel.Rules.State;

namespace Marvel.Rules.Play;

#pragma warning disable CS1591

/// <summary>Engine-local arguments captured on commitment for one resource payment instruction.</summary>
public sealed record CardResourcePaymentRequest
{
    public required IReadOnlyList<Area> Hands { get; init; }
    public required IReadOnlyList<int> Paying { get; init; }
    public required long Cost { get; init; }
    public required string Required { get; init; }
    public required int Itself { get; init; }
    public required int Payer { get; init; }
    public Marvel.Rules.Prompts.CostOption? Price { get; init; }
    public IReadOnlyList<ResourceAllocation> Allocations { get; init; } = [];
    public Card? PayingFor { get; init; }
    public Card? AbilitySource { get; init; }
    public IReadOnlyDictionary<int, int>? ResourcePayers { get; init; }
}
