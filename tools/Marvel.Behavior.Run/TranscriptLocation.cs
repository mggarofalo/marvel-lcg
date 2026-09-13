namespace Marvel.Behavior.Run;

internal sealed record TranscriptLocation(string Path, int Line, int Column)
{
    public override string ToString() => $"{Path}:{Line}:{Column}";
}
