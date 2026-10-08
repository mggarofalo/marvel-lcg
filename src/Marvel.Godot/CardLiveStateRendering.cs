using System.Globalization;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders an authorized snapshot as upright readable state, outside printed rules.</summary>
internal static class CardLiveStateRendering
{
    internal static VBoxContainer Create(BoardCardPresentation card, float width, bool compact)
    {
        var stack = new VBoxContainer { Name = "LiveState", CustomMinimumSize = new Vector2(width, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.Fill };
        stack.AddThemeConstantOverride("separation", compact ? 1 : 6);
        if (card.Concealed) return stack;
        CardProgressValue? progress = CardProgressValue.From(card);
        if (progress is not null && card.Fields.Any(field => field.Name == progress.FieldName))
            AddProgress(stack, card, progress, compact);
        AddStatuses(stack, card, width, compact);
        return stack;
    }

    private static void AddStatuses(VBoxContainer stack, BoardCardPresentation card, float width, bool compact)
    {
        var grid = new GridContainer { Name = "LiveStatuses", Columns = 2,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 2);
        foreach (var entry in CardStatusEntries.From(card))
        {
            var cell = new VBoxContainer { CustomMinimumSize = new Vector2((width - 4) / 2, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            AddLabel(cell, entry.Text, $"LiveStatus{grid.GetChildCount()}", compact ? 12 : 16);
            grid.AddChild(cell);
        }
        if (grid.GetChildCount() > 0) stack.AddChild(grid);
        else grid.Free();
    }

    internal static string Description(BoardCardPresentation card)
    {
        if (card.Concealed) return card.Title;
        var parts = new List<string> { card.Title };
        CardProgressValue? progress = CardProgressValue.From(card);
        if (progress is not null) parts.Add($"{(progress.IsThreat ? "Threat" : "Hit points")} {progress.Value}");
        if (card.Damage > 0) parts.Add($"{card.Damage.ToString(CultureInfo.InvariantCulture)} damage");
        parts.AddRange(CardStatusEntries.From(card).Select(entry => entry.Text));
        return string.Join(" · ", parts);
    }

    private static void AddProgress(VBoxContainer stack, BoardCardPresentation card, CardProgressValue progress, bool compact)
    {
        Label value = AddLabel(stack, progress.Value, "LiveProgressValue", compact ? 17 : 30);
        string meaning = progress.IsThreat ? "Threat / threshold" : "HP remaining / max";
        if (!progress.Value.Contains('/')) meaning = progress.IsThreat ? "Threat" : "Hit points";
        AddLabel(stack, meaning, "LiveProgressMeaning", compact ? 10 : 14);
        value.AccessibilityName = $"{meaning}: {progress.Value}";
        if (card.Damage > 0)
            AddLabel(stack, $"{card.Damage.ToString(CultureInfo.InvariantCulture)} damage", "LiveDamage", compact ? 10 : 16);
        AddMaximum(stack, card, progress, compact);
    }

    private static void AddMaximum(VBoxContainer stack, BoardCardPresentation card, CardProgressValue progress, bool compact)
    {
        if (progress.MaximumModified && card.EffectiveValues.TryGetValue("HP", out CardEffectiveValue? maximum))
        {
            var line = new ColorRect { Name = "ModifiedMaximum", CustomMinimumSize = new Vector2(0, 2),
                Color = ClientTheme.ToGodot(CardVisualTokens.Modified), MouseFilter = Control.MouseFilterEnum.Ignore };
            stack.AddChild(line);
            AddLabel(stack, $"Max {maximum.CurrentValue.ToString(CultureInfo.InvariantCulture)} modified", "LiveMaximum", compact ? 11 : 14);
        }
    }

    internal static Label AddLabel(VBoxContainer stack, string text, string name, int fontSize)
    {
        var label = new Label { Name = name, Text = text, AccessibilityName = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.Fill };
        label.AddThemeConstantOverride("line_spacing", 0);
        label.AddThemeFontOverride("font", CardTypography.Bold);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        stack.AddChild(label);
        return label;
    }
}
