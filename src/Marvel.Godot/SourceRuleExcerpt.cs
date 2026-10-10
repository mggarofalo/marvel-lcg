using Godot;

namespace Marvel.Godot;

/// <summary>Shows only complete shaped lines and explicitly identifies an abbreviated card face.</summary>
internal static class SourceRuleExcerpt
{
    internal static void Bind(RichTextLabel rules)
    {
        rules.VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping;
        Label hint = CardSourceStrip.Label("… Inspect card", "SourceRulesDisclosure", 10, CardTypography.Body);
        hint.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Secondary));
        rules.AddChild(hint);
        void Refresh() => Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(rules) && rules.IsInsideTree()) Fit(rules, hint);
        }).CallDeferred();
        rules.Ready += Refresh;
        rules.Resized += Refresh;
    }

    private static void Fit(RichTextLabel rules, Label hint)
    {
        bool abbreviated = rules.GetContentHeight() > rules.Size.Y;
        hint.Visible = abbreviated;
        hint.Position = new Vector2(0, Math.Max(0, rules.Size.Y - 13));
        hint.Size = new Vector2(rules.Size.X, 13);
        float available = abbreviated ? hint.Position.Y : rules.Size.Y;
        int characters = -1;
        for (int line = 0; line < rules.GetLineCount(); line++)
        {
            if (rules.GetLineOffset(line) + rules.GetLineHeight(line) <= available) continue;
            characters = rules.GetLineRange(line).X;
            break;
        }
        rules.VisibleCharacters = characters;
    }
}
