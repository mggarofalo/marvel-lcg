namespace Marvel.Behavior.Run;

internal sealed class TranscriptAssertionException : Exception
{
    public TranscriptAssertionException(string message)
        : base(message)
    {
    }
}
