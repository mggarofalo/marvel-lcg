using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text needed to remove threat.</summary>
// Completing a main scheme is part of threat removal; the ensuing stage must
// reveal its card text, so this cohesive port includes that nested operation.
public interface IThreatCardAbilities : IEncounterCardAbilities
{
    bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1);
}
#pragma warning restore CS1591
