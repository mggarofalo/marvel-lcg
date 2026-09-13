namespace Marvel.Behavior.Run;

internal sealed record TranscriptStep(
    TranscriptStepKind Kind,
    string Text,
    TranscriptTable? Table,
    TranscriptLocation Location);
