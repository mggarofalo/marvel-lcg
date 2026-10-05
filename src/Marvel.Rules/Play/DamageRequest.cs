using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One source's damage assignment and its causal event identity.</summary>
public sealed record DamageRequest(
    Card Source, Card Target, long Amount, string Trigger, string Verb, int By = -1);
