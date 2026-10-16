using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The cause and attribution preserved across defeat interrupts.</summary>
public sealed record DefeatCause(int Source = -1, string Trigger = "", string Verb = "", int By = -1, Occurrence? Owner = null);
