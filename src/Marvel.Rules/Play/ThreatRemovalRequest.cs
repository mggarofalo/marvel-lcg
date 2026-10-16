using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One scheme's threat removal, its permission override and causal attribution.</summary>
public sealed record ThreatRemovalRequest(
    Card Scheme, long Amount, string Trigger, string Verb, int By = -1,
    int OverridesCannotFrom = -1)
{
    /// <summary>The occurrence retaining a defeat across nested procedure scheduling.</summary>
    public Occurrence? RecordDefeatOn { get; init; }
}
