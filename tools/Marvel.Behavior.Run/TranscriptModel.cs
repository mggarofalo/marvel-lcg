namespace Marvel.Behavior.Run;

internal enum TranscriptStepKind
{
    Given,
    When,
    Then,
}

internal sealed record TranscriptLocation(string Path, int Line, int Column)
{
    public override string ToString() => $"{Path}:{Line}:{Column}";
}

internal sealed record TranscriptTable(
    IReadOnlyList<string> Header,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

internal sealed record TranscriptStep(
    TranscriptStepKind Kind,
    string Text,
    TranscriptTable? Table,
    TranscriptLocation Location);

internal sealed record TranscriptScenario(
    string Name,
    string Obligation,
    IReadOnlyList<string> Authorities,
    IReadOnlyList<TranscriptStep> Steps,
    TranscriptLocation Location);

internal sealed record TranscriptFeature(
    string Name,
    IReadOnlyList<TranscriptScenario> Scenarios,
    TranscriptLocation Location);

internal enum TranscriptFailureKind
{
    Validation,
    UnknownStep,
    AmbiguousStep,
    Assertion,
    Execution,
}

internal sealed class TranscriptException : Exception
{
    public TranscriptException(string message)
        : this(TranscriptFailureKind.Validation, message)
    {
    }

    public TranscriptException(
        TranscriptFailureKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public TranscriptFailureKind Kind { get; }
}

internal sealed class TranscriptAssertionException : Exception
{
    public TranscriptAssertionException(string message)
        : base(message)
    {
    }
}
