namespace Marvel.Godot;

/// <summary>Applies focus or event emphasis consistently to every visible copy of an object.</summary>
internal static class BoardCardEmphasis
{
    internal static void Apply(IReadOnlyDictionary<int, List<CardControl>> controls,
        IEnumerable<int> ids, BoardControlReveal reveal, bool highlight)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> selected = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches.Where(InteractionControl.IsUsable))
            {
                if (highlight) match.SetHighlighted(selected.Contains(key));
                else match.SetPresented(selected.Contains(key));
                if (selected.Contains(key)) reveal.Ensure(key);
            }
        }
    }
}
