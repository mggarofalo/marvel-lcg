namespace Marvel.Behavior.Run;

internal sealed record TranscriptScenario(
    string Name,
    string Obligation,
    IReadOnlyList<string> CoveredObligations,
    IReadOnlyList<string> Authorities,
    IReadOnlyList<TranscriptStep> Steps,
    TranscriptLocation Location);
