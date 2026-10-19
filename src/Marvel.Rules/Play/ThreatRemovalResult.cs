namespace Marvel.Rules.Play;

/// <summary>Committed removal and whether its side-scheme defeat still has scheduled work.</summary>
public readonly record struct ThreatRemovalResult(long Removed, bool Suspended);
