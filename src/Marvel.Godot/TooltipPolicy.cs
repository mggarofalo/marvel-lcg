using Godot;

namespace Marvel.Godot;

/// <summary>Tooltips add information; visible captions and accessible names stand on their own.</summary>
internal static class TooltipPolicy
{
    internal const string PinDetails = "Click or press Enter to pin card details.";

    internal static void Inspect(Control control, string title)
    {
        control.TooltipText = PinDetails;
        control.AccessibilityName = "Inspect " + title;
        control.AccessibilityDescription = PinDetails;
    }

    // This authored policy is for text-only references; icon action controls
    // retain their operation, symbol decoding and unavailable explanations.
    internal static void ClippedCaption(Button button, string caption)
    {
        void Refresh()
        {
            using var line = new TextLine();
            line.AddString(button.Text, button.GetThemeFont("font"), button.GetThemeFontSize("font_size"));
            float available = button.Size.X - button.GetThemeStylebox("normal").GetMinimumSize().X;
            button.TooltipText = ClippedCaption(caption, button.ClipText && line.GetSize().X > available);
        }
        button.Resized += Refresh;
        button.Ready += Refresh;
    }

    internal static string ClippedCaption(string caption, bool clipped) => clipped ? caption : string.Empty;

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
