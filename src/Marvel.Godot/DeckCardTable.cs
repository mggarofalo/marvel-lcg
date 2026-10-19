using Godot;

namespace Marvel.Godot;

/// <summary>Persistent keyed card rows preserve focus, scroll and selection across quantity edits.</summary>
internal sealed partial class DeckCardTable : ScrollContainer
{
    private readonly VBoxContainer rows = new() { Name = "Rows", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Dictionary<string, DeckCardRow> byFace = new(StringComparer.Ordinal);
    private string[] order = [];
    internal InterfaceScale UiScale { get; set; } = InterfaceScale.Standard;
    internal DeckRowSelection Selection { get; } = new();
    internal Action? SelectionChanged { get; set; }
    internal Action<string>? Inspected { get; set; }
    internal Action<string, int>? QuantityRequested { get; set; }
    internal Func<Control, Variant>? BeginTransfer { get; set; }
    internal Func<Variant, bool>? AcceptTransfer { get; set; }
    internal Action<Variant>? CompleteTransfer { get; set; }
    internal Action? EndTransfer { get; set; }
    internal string[] SelectedFaces => Selection.Selected.Where(index => index < order.Length).Select(index => order[index]).ToArray();
    internal int ItemCount => order.Length;

    public DeckCardTable()
    {
        HorizontalScrollMode = ScrollMode.Disabled;
        FollowFocus = true;
        AddChild(rows);
    }

    internal void Render(IReadOnlyList<DeckTableEntry> entries)
    {
        string[] next = entries.Select(entry => entry.Face).ToArray();
        bool reordered = !order.SequenceEqual(next);
        string? anchor = Selection.Anchor >= 0 && Selection.Anchor < order.Length ? order[Selection.Anchor] : null;
        var retained = SelectedFaces.ToHashSet(StringComparer.Ordinal);
        foreach (string face in order.Except(next, StringComparer.Ordinal))
        {
            var removed = byFace[face];
            rows.RemoveChild(removed);
            removed.QueueFree();
            byFace.Remove(face);
        }
        order = next;
        if (reordered) Selection.Clear();
        for (int index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var row = Row(entry.Face);
            row.Index = index;
            if (reordered) rows.MoveChild(row, index);
            row.Update(entry);
            if (reordered && retained.Contains(entry.Face)) Selection.Select(index);
        }
        if (reordered) Selection.Anchor = Array.IndexOf(order, anchor);
        PaintSelection();
    }

    private DeckCardRow Row(string face)
    {
        if (byFace.TryGetValue(face, out var existing)) return existing;
        var row = new DeckCardRow(this, face);
        byFace.Add(face, row);
        rows.AddChild(row);
        return row;
    }

    internal void SelectRow(int index, bool additive, bool range)
    {
        Selection.Click(index, additive, range);
        PaintSelection();
        Inspected?.Invoke(order[index]);
        SelectionChanged?.Invoke();
    }

    internal void SelectAll()
    {
        for (int index = 0; index < order.Length; index++) Selection.Select(index);
        PaintSelection();
        SelectionChanged?.Invoke();
    }

    internal void DeselectAll() { Selection.Clear(); PaintSelection(); SelectionChanged?.Invoke(); }

    internal void Reveal(IReadOnlyList<string> faces)
    {
        Selection.Clear();
        for (int index = 0; index < order.Length; index++)
            if (faces.Contains(order[index])) Selection.Select(index);
        PaintSelection();
        string? first = order.FirstOrDefault(faces.Contains);
        if (first is not null) EnsureControlVisible(byFace[first]);
        SelectionChanged?.Invoke();
    }

    private void PaintSelection()
    {
        for (int index = 0; index < order.Length; index++) byFace[order[index]].Select(Selection.Contains(index));
    }

    internal bool Key(int index, InputEventKey key)
    {
        bool additive = key.CtrlPressed || key.MetaPressed;
        if (additive && key.Keycode == global::Godot.Key.A) { SelectAll(); return true; }
        if (key.Keycode is global::Godot.Key.Space or global::Godot.Key.Enter) { SelectRow(index, additive, key.ShiftPressed); return true; }
        if (key.Keycode is not (global::Godot.Key.Up or global::Godot.Key.Down)) return false;
        int next = Math.Clamp(index + (key.Keycode == global::Godot.Key.Up ? -1 : 1), 0, order.Length - 1);
        byFace[order[next]].GrabFocus();
        if (!additive || key.ShiftPressed) SelectRow(next, additive, key.ShiftPressed);
        return true;
    }

    public override bool _CanDropData(Vector2 position, Variant data) => AcceptTransfer?.Invoke(data) ?? false;
    public override void _DropData(Vector2 position, Variant data) => CompleteTransfer?.Invoke(data);
}
