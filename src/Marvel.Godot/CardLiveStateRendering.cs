using System.Globalization;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders an authorized snapshot as upright readable state, outside printed rules.</summary>
internal static class CardLiveStateRendering
{
    internal static VBoxContainer Create(BoardCardPresentation card, float width, bool compact, bool showProgress = true,
        InterfaceScale scale = InterfaceScale.Percent100)
    {
        var stack = new VBoxContainer { Name = "LiveState", CustomMinimumSize = new Vector2(width, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.Fill };
        stack.AddThemeConstantOverride("separation", compact ? 1 : 6);
        if (card.Concealed) return stack;
        CardProgressValue? progress = CardProgressValue.From(card);
        if (showProgress && progress is not null && card.Fields.Any(field => field.Name == progress.FieldName))
            AddProgress(stack, card, progress, compact);
        AddStatuses(stack, card, width, compact, scale);
        return stack;
    }

    private static void AddStatuses(VBoxContainer stack, BoardCardPresentation card, float width, bool compact,
        InterfaceScale scale)
    {
        var grid = new HFlowContainer { Name = "LiveStatuses", CustomMinimumSize = new Vector2(width, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 2);
        foreach (var entry in CardStatusEntries.From(card))
        {
            var cell = new VBoxContainer {
                MouseFilter = Control.MouseFilterEnum.Ignore };
            if (StatusBadge.Supports(entry.Name)) cell.AddChild(StatusBadge.Create(entry.Name, entry.Text, entry.Count, compact, scale));
            else AddLabel(cell, entry.Text, $"LiveStatus{grid.GetChildCount()}", compact ? 12 : 16).AutowrapMode = TextServer.AutowrapMode.Off;
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
        if (card.DamageKnown && card.Damage > 0) parts.Add($"{card.Damage.ToString(CultureInfo.InvariantCulture)} damage");
        parts.AddRange(CardStatusEntries.From(card).Select(entry => entry.Text));
        return string.Join(" · ", parts);
    }

    private static void AddProgress(VBoxContainer stack, BoardCardPresentation card, CardProgressValue progress, bool compact)
    {
        var row = new HBoxContainer { Name = "LiveProgress", MouseFilter = Control.MouseFilterEnum.Ignore };
        var meaningCell = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        var valueCell = new VBoxContainer();
        string meaning = progress.IsThreat ? "Threat" : "HP";
        Label caption = AddLabel(meaningCell, meaning, "LiveProgressMeaning", compact ? 10 : 14);
        caption.AutowrapMode = TextServer.AutowrapMode.Off;
        Label value = AddLabel(valueCell, progress.Value, "LiveProgressValue", compact ? 17 : 30);
        value.AutowrapMode = TextServer.AutowrapMode.Off;
        row.AddThemeConstantOverride("separation", 5);
        row.AddChild(meaningCell);
        row.AddChild(valueCell);
        stack.AddChild(row);
        value.AccessibilityName = $"{meaning}: {progress.Value}";
        if (progress.MaximumModified)
            valueCell.AddChild(new ColorRect { Name = "ModifiedMaximum", Color = ClientTheme.ToGodot(CardVisualTokens.Modified),
                CustomMinimumSize = new Vector2(0, 2), MouseFilter = Control.MouseFilterEnum.Ignore });
    }

    internal static Label AddLabel(VBoxContainer stack, string text, string name, int fontSize)
    {
        var label = new Label { Name = name, Text = text, AccessibilityName = text,
            AutowrapMode = TextServer.AutowrapMode.Word, MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.Fill };
        label.AddThemeConstantOverride("line_spacing", 0);
        label.AddThemeFontOverride("font", CardTypography.Bold);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        stack.AddChild(label);
        return label;
    }
}
