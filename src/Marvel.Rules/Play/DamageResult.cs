namespace Marvel.Rules.Play;

/// <summary>Damage facts that survive a recipient leaving play.</summary>
public sealed record DamageResult(Damage.Outcome Outcome, long Dealt, long Taken);
