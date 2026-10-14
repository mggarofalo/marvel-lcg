using Godot;

namespace Marvel.Godot;

/// <summary>Shows only complete shaped lines and explicitly identifies an abbreviated card face.</summary>
internal static class SourceRuleExcerpt
{
    internal static void Bind(RichTextLabel rules)
    {
        rules.VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping;
        // Disclosure has its own horizontal space; even a one-line tile retains meaningful text.
        using var inset = new StyleBoxEmpty { ContentMarginRight = 14 };
        rules.AddThemeStyleboxOverride("normal", inset);
        Label hint = CardSourceStrip.Label("…", "SourceRulesDisclosure", 10, CardTypography.Body);
        hint.AutowrapMode = TextServer.AutowrapMode.Off;
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
        float hintHeight = Mathf.Ceil(hint.GetCombinedMinimumSize().Y);
        hint.Position = new Vector2(Math.Max(0, rules.Size.X - 12), Math.Max(0, rules.Size.Y - hintHeight));
        hint.Size = new Vector2(12, hintHeight);
        float available = rules.Size.Y;
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
