using Godot;

namespace Marvel.Godot;

/// <summary>Tooltips add information; visible captions and accessible names stand on their own.</summary>
internal static class TooltipPolicy
{
    internal static void RemoveRestatement(Button button)
    {
        if (!Restates(button.Text, button.TooltipText)) return;
        if (string.IsNullOrEmpty(button.AccessibilityName)) button.AccessibilityName = button.Text;
        button.TooltipText = "";
    }

    internal static bool Restates(string visible, string tooltip)
    {
        if (string.IsNullOrWhiteSpace(visible) || string.IsNullOrWhiteSpace(tooltip)) return false;
        static string Normalize(string value) => string.Join(" ", value.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries)).Trim().TrimEnd('.', ':');
        return string.Equals(Normalize(visible), Normalize(tooltip), StringComparison.OrdinalIgnoreCase);
    }
}
