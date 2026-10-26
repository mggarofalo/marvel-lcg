namespace Marvel.Rules.Prompts;

/// <summary>A component's shortage under one engine-suggested partial allocation.</summary>
public sealed record ResourceComponentProgress(int Component, int Required, int Remaining,
    string RemainingResources, bool Printed, bool DistinctTypes);
