using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The remaining choices in one defeat interrupt window.</summary>
public sealed record DefeatWindow(Occurrence? Origin = null, IReadOnlyList<PendingAbility>? Abilities = null,
    IReadOnlyList<int>? Passed = null, AbilityType? Tier = null);
