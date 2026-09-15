namespace Marvel.Godot;

/// <summary>
/// Tracks the control currently entitled to dismiss the card preview. The
/// rules do not prescribe preview ownership; this client policy prevents a
/// stale source from closing a preview claimed by a newer source.
/// </summary>
internal sealed class CardPreviewOwnership
{
    private object? owner;

    internal event Action<int?>? Changed;

    internal void Show(object source, int cardId)
    {
        owner = source;
        Changed?.Invoke(cardId);
    }

    internal void Dismiss(object source)
    {
        if (!ReferenceEquals(owner, source)) return;
        owner = null;
        Changed?.Invoke(null);
    }

    internal void Clear()
    {
        if (owner is null) return;
        owner = null;
        Changed?.Invoke(null);
    }
}
