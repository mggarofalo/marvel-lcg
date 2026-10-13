using Godot;

namespace Marvel.Godot;

/// <summary>Idempotent drop selection over the current opening-hand draft.</summary>
internal sealed class PointerMulliganDraft
{
    private readonly HashSet<int> legal = [];
    private readonly HashSet<int> selected = [];
    private Action<int>? choose;
    internal bool Active => legal.Count > 0;
    internal bool Contains(int id) => legal.Contains(id);
    internal void Bind(IReadOnlyCollection<int> targets, IReadOnlyCollection<int> staged, Action<int> toggle)
    {
        legal.Clear(); legal.UnionWith(targets);
        SetSelected(staged);
        choose = toggle;
    }
    internal void SetSelected(IReadOnlyCollection<int> staged)
    {
        selected.Clear(); selected.UnionWith(staged);
    }
    internal void Toggle(int id) { if (legal.Contains(id)) choose?.Invoke(id); }
    internal bool Drop(int id, bool onDiscard, bool onHand)
    {
        if (!legal.Contains(id)) return false;
        if (onDiscard)
        {
            if (!selected.Contains(id)) choose?.Invoke(id);
            return true;
        }
        if (onHand && selected.Contains(id))
        {
            choose?.Invoke(id);
            return true;
        }
        return false;
    }
}
