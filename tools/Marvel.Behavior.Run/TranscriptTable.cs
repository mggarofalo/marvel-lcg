namespace Marvel.Behavior.Run;

internal sealed record TranscriptTable(
    IReadOnlyList<string> Header,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);
