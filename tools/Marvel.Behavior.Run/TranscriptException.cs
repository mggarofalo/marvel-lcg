namespace Marvel.Behavior.Run;

internal sealed class TranscriptException : Exception
{
    public TranscriptException(string message)
        : this(TranscriptFailureKind.Validation, message)
    {
    }

    public TranscriptException(
        TranscriptFailureKind kind,
        string message,
        Exception? innerException = null,
        string? worldDigest = null)
        : base(message, innerException)
    {
        Kind = kind;
        WorldDigest = worldDigest;
    }

    public TranscriptFailureKind Kind { get; }

    public string? WorldDigest { get; }
}
