namespace Marvel.Behavior.Run;

internal enum TranscriptFailureKind
{
    Validation,
    UnknownStep,
    AmbiguousStep,
    Assertion,
    Execution,
}
