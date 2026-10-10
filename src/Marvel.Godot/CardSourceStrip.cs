using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shared native content for a host-width tab or an intentionally wider source ledger.</summary>
internal static class CardSourceStrip
{
    internal const float LedgerWidth = 250;
    internal const float RailWidth = 17;
    internal const float BodyInset = RailWidth + 7;

    internal static Control Create(BoardCardPresentation card, float width)
    {
        MarginContainer margin = Frame(card, width, out VBoxContainer stack);
        AddTitle(stack, card.Title);
        Label type = Label(card.Kind, "SourceType", 9, CardTypography.Body);
        type.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Secondary));
        stack.AddChild(type);
        AddContributions(stack, card);
        foreach (string line in CardSourceSummary.Lines(card))
        {
            RichTextLabel text = CardRulesRendering.Create(card with { RulesText = line, RulesMarkup = "" }, InterfaceScale.Standard, 11);
            text.Name = "SourceMeaning";
            text.FitContent = true;
            text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            stack.AddChild(text);
        }
        return margin;
    }

    internal static MarginContainer Frame(BoardCardPresentation card, float width, out VBoxContainer stack)
    {
        var margin = new MarginContainer { Name = "SourceStrip", MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(width - 8, 0) };
        margin.AddThemeConstantOverride("margin_left", (int)BodyInset - 4);
        margin.AddThemeConstantOverride("margin_right", 9);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 7);
        stack = new VBoxContainer { Name = "SourceContents", MouseFilter = Control.MouseFilterEnum.Ignore };
        stack.AddThemeConstantOverride("separation", 2);
        margin.AddChild(stack);
        margin.Draw += () => DrawRail(margin, card);
        margin.Resized += margin.QueueRedraw;
        return margin;
    }

    internal static void AddTitle(VBoxContainer stack, string title)
    {
        Label label = Label(title, "SourceTitle", 14, CardTypography.Title);
        label.Uppercase = true;
        stack.AddChild(label);
    }

    internal static void AddContributions(VBoxContainer stack, BoardCardPresentation card, bool showRecipients = true)
    {
        if (card.Persistent is not { } source || source.Contributions.Count == 0) return;
        var flow = new HFlowContainer { Name = "SourceContributions", MouseFilter = Control.MouseFilterEnum.Ignore };
        flow.AddThemeConstantOverride("h_separation", 8);
        stack.AddChild(flow);
        foreach (var group in source.Contributions.GroupBy(row => (row.Attribute, row.Operation, row.Amount)))
        {
            CardContributionDescriptor value = group.First();
            var unit = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            unit.AddThemeConstantOverride("separation", 3);
            flow.AddChild(unit);
            string? glyph = CardGlyphRendering.Stat(value.Attribute);
            if (glyph is not null)
            {
                TextureRect icon = CardGlyphRendering.Create(glyph, "SourceStatIcon", new Rect2(0, 0, 12, 12), CardFaceStyle.Ink);
                icon.CustomMinimumSize = new Vector2(12, 12);
                unit.AddChild(icon);
            }
            Label contribution = Label(CardSourceSummary.Contribution(value), "SourceContribution", 11, CardTypography.Bold);
            contribution.AutowrapMode = TextServer.AutowrapMode.Off;
            unit.AddChild(contribution);
        }
        int recipients = source.Contributions.Select(value => value.TargetId).Distinct().Count();
        if (showRecipients && (source.Relation.Kind != "Attached" || source.Contributions.Any(value => value.TargetId != source.Relation.HostId)))
            stack.AddChild(Label($"Applied to {recipients} {(recipients == 1 ? "card" : "cards")}", "SourceRecipients", 10, CardTypography.Body));
    }

    internal static Label Label(string text, string name, int size, Font font)
    {
        var label = new Label { Name = name, Text = text, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", CardFaceStyle.Ink);
        label.AddThemeConstantOverride("line_spacing", 0);
        return label;
    }

    private static void DrawRail(Control surface, BoardCardPresentation card)
    {
        float height = surface.Size.Y;
        surface.DrawColoredPolygon([new(0, 0), new(RailWidth, 0), new(7, height), new(0, height)], CardFaceStyle.Accent(card));
        surface.DrawColoredPolygon([new(0, 0), new(RailWidth - 5, 0), new(2, height), new(0, height)], CardFaceStyle.Ink);
    }
}
