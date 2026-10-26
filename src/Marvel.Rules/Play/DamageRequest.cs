using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One source's damage assignment and its causal event identity.</summary>
public sealed record DamageRequest(
    Card Source, Card Target, long Amount, string Trigger, string Verb, int By = -1)
{
    /// <summary>The cause retaining the defeat when nested procedures change the current frame.</summary>
    public Occurrence? RecordDefeatOn { get; init; }
    /// <summary>The live activation accepting this boost damage placement, when present.</summary>
    public int? ActivationId { get; init; }
    /// <summary>Bypasses Tough for this damage instance while preserving its status cards.</summary>
    public bool IgnoresTough { get; init; }
    internal bool PreventAllDamage { get; init; }
}
