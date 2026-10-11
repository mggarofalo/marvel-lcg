using System.Globalization;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Countable small counters and bounded explicit large values, using supplied names only.</summary>
internal static class SourceCounterPips
{
    internal const int MaximumPips = 6;

    internal static Control Create(BoardFieldPresentation value, bool showName)
    {
        bool numeric = int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count);
        bool pips = numeric && count is >= 0 and <= MaximumPips;
        string name = $"{value.Name}: {value.Value}";
        var group = new HBoxContainer { Name = "SourceCounter", AccessibilityName = name,
            TooltipText = name, MouseFilter = Control.MouseFilterEnum.Pass };
        group.AddThemeConstantOverride("separation", 1);
        string abbreviation = showName ? value.Name[..Math.Min(3, value.Name.Length)].ToUpperInvariant() : "";
        Label label = CardSourceStrip.Label(pips && count > 0 ? abbreviation : $"{abbreviation} {value.Value}".Trim(),
            "SourceCounterValue", 10, CardTypography.Body);
        label.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.CustomMinimumSize = new Vector2(0, 16);
        label.AccessibilityName = name;
        group.AddChild(label);
        if (pips && count > 0)
        {
            var marks = new Control { Name = "SourceCounterPips", CustomMinimumSize = new Vector2(count * 7, 16),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            // Dots display the supplied count; multiple kinds retain their engine-named labels.
            marks.Draw += () => Draw(marks, count);
            group.AddChild(marks);
        }
        group.SetMeta("counter_name", value.Name);
        group.SetMeta("counter_value", value.Value);
        group.SetMeta("pip_count", pips ? count : -1);
        return group;
    }

    private static void Draw(Control marks, int count)
    {
        for (int index = 0; index < count; index++)
        {
            Vector2 center = new(index * 7 + 3, 8);
            marks.DrawCircle(center, 2.5f, CardFaceStyle.Paper, antialiased: true);
        }
    }
}
