using System.Collections.Immutable;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

/// <summary>One ability whose executable syntax has been lowered completely.</summary>
public sealed record CompiledCardAbility(
    string Card, string Name, AbilityTrigger Trigger, AbilityEffect Effect,
    AbilityCost? Cost, AbilityCondition? When, long? Limit, bool AnyPlayer,
    ImmutableArray<string> Labels, string PrintedResources, AbilityMaximum? Maximum,
    AbilityEffectAddress Address)
{
    /// <summary>Printed limitation on the cost that consumes this ability's resources.</summary>
    public Marvel.Rules.Prompts.ResourceRestriction ResourceRestriction { get; init; }
}
