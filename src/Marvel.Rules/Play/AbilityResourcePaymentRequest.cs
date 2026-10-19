using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Identifies an ability's cost separately from playing its source card.</summary>
public sealed record AbilityResourcePaymentRequest(
    Card Source, int Player, IReadOnlyList<int> Paying, long Cost, string Required);
