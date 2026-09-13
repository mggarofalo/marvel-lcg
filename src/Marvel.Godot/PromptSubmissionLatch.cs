namespace Marvel.Godot;

/// <summary>Accepts one decision attempt for each rendered prompt revision.</summary>
internal sealed class PromptSubmissionLatch
{
    private long? revision;
    private bool submitted;

    internal bool IsSubmitted => submitted;

    internal void Render(long currentRevision)
    {
        if (revision != currentRevision)
        {
            revision = currentRevision;
            submitted = false;
        }
    }

    internal bool TrySubmit(long currentRevision)
    {
        if (revision != currentRevision || submitted)
        {
            return false;
        }

        submitted = true;
        return true;
    }

    internal void AllowRetry(long currentRevision)
    {
        if (revision == currentRevision)
        {
            submitted = false;
        }
    }
}
