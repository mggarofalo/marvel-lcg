using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Persistent engine-described contributions occupy the idle action footer.</summary>
internal static class SourceTileMeaning
{
    internal static void Add(VBoxContainer body, BoardCardPresentation card, InterfaceScale scale)
    {
        var footer = new VBoxContainer { Name = "SourcePassiveMeaning",
            CustomMinimumSize = new Vector2(0, 20), MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(footer);
        CardSourceStrip.AddContributions(footer, card, showRecipients: false, scale);
        if (footer.GetChildCount() > 0)
        {
            footer.AddThemeConstantOverride("separation", 0);
            foreach (Node child in footer.GetChildren())
                if (child is Control unit) unit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return;
        }
        string meaning = CardSourceSummary.TileMeaning(card);
        bool printed = meaning.Length == 0;
        if (printed) meaning = card.Concealed ? "" : card.RulesText;
        if (meaning.Length == 0) { footer.Hide(); return; }
        int fontSize = CompactTableTypography.Size(12, scale);
        RichTextLabel label = CardRulesRendering.Create(
            printed ? card : card with { RulesText = meaning, RulesMarkup = "" }, InterfaceScale.Standard, fontSize);
        label.Name = "SourceMeaning";
        label.AccessibilityName = (printed ? "Printed rules: " : "") + meaning;
        label.CustomMinimumSize = new Vector2(0, Mathf.Ceil(CardTypography.Body.GetHeight(fontSize)) + 2);
        label.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        footer.AddChild(label);
        SourceRuleExcerpt.Bind(label);
    }

    internal static void ShowIdle(CardControl card, bool visible)
    {
        if (card.FindChild("SourcePassiveMeaning", true, false) is Control footer)
            footer.Visible = visible;
    }
}
