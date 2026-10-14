using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Persistent engine-described contributions occupy the idle action footer.</summary>
internal static class SourceTileMeaning
{
    internal static void Add(VBoxContainer body, BoardCardPresentation card)
    {
        var footer = new VBoxContainer { Name = "SourcePassiveMeaning",
            CustomMinimumSize = new Vector2(0, 20), MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(footer);
        CardSourceStrip.AddContributions(footer, card, showRecipients: false);
        if (footer.GetChildCount() > 0)
        {
            footer.AddThemeConstantOverride("separation", 0);
            foreach (Node child in footer.GetChildren())
                if (child is Control unit) unit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return;
        }
        IReadOnlyList<string> lines = CardSourceSummary.AbilityLines(card);
        if (lines.Count == 0) { footer.Hide(); return; }
        string meaning = lines[0];
        RichTextLabel label = CardRulesRendering.Create(
            card with { RulesText = meaning, RulesMarkup = "" }, InterfaceScale.Standard, 11);
        label.Name = "SourceMeaning";
        label.CustomMinimumSize = new Vector2(0, 20);
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
