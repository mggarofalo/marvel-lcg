using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps the complete printed text available when a dense face exceeds its fixed card frame.</summary>
internal static class DeckCardPreview
{
    internal static Control Create(BoardCardPresentation presentation)
    {
        var result = new VBoxContainer();
        var card = CardControl.Create(presentation, CardDisplaySize.Full, InterfaceScale.Standard);
        result.AddChild(card);
        var heading = new Label { Text = "Full printed text", Visible = false };
        var text = CardRulesRendering.Create(presentation, InterfaceScale.Standard, 16);
        text.Name = "FullPrintedText";
        text.FitContent = true;
        text.CustomMinimumSize = new(400, 0);
        text.Visible = false;
        text.AddThemeColorOverride("default_color", ClientTheme.ToGodot(VisualSystem.Palette.Text));
        result.AddChild(heading);
        result.AddChild(text);
        card.Ready += () =>
        {
            var rules = card.FindChild("RulesText", true, false) as RichTextLabel;
            bool overflow = rules is not null && rules.GetContentHeight() > rules.Size.Y + 1;
            heading.Visible = text.Visible = overflow;
        };
        return result;
    }
}
