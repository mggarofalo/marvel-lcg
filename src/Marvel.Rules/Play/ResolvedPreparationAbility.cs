using System.Collections.Immutable;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Initiation facts retained until one Preparation ability finishes.</summary>
/// <remarks>The receipt representation is an engine choice; rr:resolve.2 determines completion.</remarks>
public sealed record ResolvedPreparationAbility(
    int Source, string Face, int Incarnation, int Controller, int Player,
    ImmutableArray<string> Traits, PendingAbility Ability)
{
    /// <summary>Original actor facts, captured before payment moves the source.</summary>
    public OccurrenceCard? SourceFacts { get; init; }
}
