namespace Marvel.Godot;

/// <summary>Chooses the stable attached control to receive focus after a draft refresh.</summary>
internal static class BoardInteractionFocus
{
    internal static BoardInteractionFocusKey? Restore(
        BoardInteractionFocusKey requested,
        IEnumerable<BoardInteractionFocusKey> offered)
    {
        ArgumentNullException.ThrowIfNull(offered);
        BoardInteractionFocusKey[] ordered = [.. offered
            .Distinct()
            .OrderBy(key => key.CardId)
            .ThenBy(key => key.Intent)];
        if (ordered.Contains(requested))
        {
            return requested;
        }

        BoardInteractionFocusKey? sameCard = ordered
            .Where(key => key.CardId == requested.CardId)
            .Cast<BoardInteractionFocusKey?>()
            .FirstOrDefault();
        return sameCard ?? (ordered.Length > 0 ? ordered[0] : null);
    }
}
