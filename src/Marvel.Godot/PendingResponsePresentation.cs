namespace Marvel.Godot;

/// <summary>A generation-bound visual commitment; cancellation never applies its superseded snapshot.</summary>
internal sealed class PendingResponsePresentation
{
    private Action? apply;
    internal int Generation { get; private set; }
    internal bool Pending => apply is not null;

    internal int Begin(Action commit)
    {
        Cancel();
        apply = commit;
        return Generation;
    }

    internal void Cancel()
    {
        Generation++;
        apply = null;
    }

    internal bool Complete(int generation)
    {
        if (generation != Generation || apply is null) return false;
        Action commit = apply;
        apply = null;
        commit();
        return true;
    }
}
