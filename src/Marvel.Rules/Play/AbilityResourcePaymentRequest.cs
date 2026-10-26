using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Identifies an ability's cost separately from playing its source card.</summary>
public sealed record AbilityResourcePaymentRequest(
    Card Source, int Player, IReadOnlyList<int> Paying, long Cost, string Required)
{
    /// <summary>Exact constrained components accepted for this payment.</summary>
    public Marvel.Rules.Prompts.CostOption? Price { get; init; }
    /// <summary>The player's accepted resource declarations.</summary>
    public IReadOnlyList<ResourceAllocation> Allocations { get; init; } = [];
}
