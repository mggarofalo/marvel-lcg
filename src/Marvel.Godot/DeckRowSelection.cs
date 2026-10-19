namespace Marvel.Godot;

/// <summary>Desktop selection semantics, independent of card quantities and legality.</summary>
internal sealed class DeckRowSelection
{
    private readonly SortedSet<int> selected = [];
    private int anchor = -1;
    internal int Anchor { get => anchor; set => anchor = value; }
    internal int[] Selected => selected.ToArray();
    internal bool Contains(int index) => selected.Contains(index);
    internal void Click(int index, bool additive, bool range)
    {
        if (range && anchor >= 0)
        {
            if (!additive) selected.Clear();
            for (int item = Math.Min(anchor, index); item <= Math.Max(anchor, index); item++) selected.Add(item);
            return;
        }
        if (!additive) selected.Clear();
        if (!additive || !selected.Remove(index)) selected.Add(index);
        anchor = index;
    }
    internal void Select(int index) { selected.Add(index); anchor = index; }
    internal void Clear() { selected.Clear(); anchor = -1; }
}
