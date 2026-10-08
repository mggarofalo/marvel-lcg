namespace Marvel.View;

/// <summary>One already-applied authorized calculation step. HP denotes maximum health, never healing.</summary>
public sealed record CardContributionDescriptor(int TargetId, string Attribute, string Operation, long Amount, CardValueDuration? Duration);
