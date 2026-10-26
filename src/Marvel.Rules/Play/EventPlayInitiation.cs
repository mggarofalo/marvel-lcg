namespace Marvel.Rules.Play;

/// <summary>The cancellation receipt retained by one paid event's initiation.</summary>
public sealed class EventPlayInitiation
{
    /// <summary>Whether an interrupt canceled the event before its effects resolved.</summary>
    public bool Cancelled { get; private set; }

    internal void Cancel() => Cancelled = true;
}
