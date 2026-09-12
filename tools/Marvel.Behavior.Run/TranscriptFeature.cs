namespace Marvel.Behavior.Run;

internal sealed record TranscriptFeature(
    string Name,
    IReadOnlyList<TranscriptScenario> Scenarios,
    TranscriptLocation Location);
