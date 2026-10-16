namespace Marvel.Rules.Play;

/// <summary>The mutually exclusive lifecycle states of one agenda-owned invocation.</summary>
internal enum AbilityExecutionState { Running, Suspended, Completing, Complete, Abandoned }
