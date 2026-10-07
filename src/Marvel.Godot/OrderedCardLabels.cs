using System.Globalization;

namespace Marvel.Godot;

/// <summary>Gives offered card copies stable local labels, separate from their chosen position.</summary>
internal static class OrderedCardLabels
{
    // These are presentation identifiers for this prompt, not engine or printed card values.
    internal static string Copy(IReadOnlyList<int> offered, int target)
    {
        int index = offered.ToList().IndexOf(target);
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(target));
        string label = string.Empty;
        for (int value = index + 1; value > 0; value = (value - 1) / 26)
            label = (char)('A' + (value - 1) % 26) + label;
        return label;
    }

    internal static string Position(IReadOnlyList<int> selected, int target)
    {
        int index = selected.ToList().IndexOf(target);
        return index < 0 ? "+" : (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    internal static string Sequence(IReadOnlyList<int> offered, IReadOnlyList<int> selected) =>
        string.Join(" → ", selected.Select(target => Copy(offered, target)));
}
