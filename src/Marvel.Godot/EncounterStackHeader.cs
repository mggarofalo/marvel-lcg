using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps exposed encounter stack titles and live quantities current at receipt impact.</summary>
internal static class EncounterStackHeader
{
    internal static PanelContainer Create(BoardCardPresentation card, float width)
    {
        var header = new PanelContainer { Name = "SchemeStackHeader", Size = new Vector2(width, 60),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        using var ink = new StyleBoxFlat { BgColor = CardFaceStyle.Ink,
            ContentMarginLeft = 7, ContentMarginRight = 7, ContentMarginTop = 3, ContentMarginBottom = 3 };
        header.AddThemeStyleboxOverride("panel", ink);
        var copy = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        copy.AddThemeConstantOverride("separation", 0);
        header.AddChild(copy);
        Add(copy, card.Title, "StackTitle", 14, CardTypography.Title);
        if (CardProgressValue.From(card) is { IsThreat: true } threat)
            Add(copy, $"Threat {threat.Value}", "StackThreat", 11, CardTypography.Body);
        string statuses = string.Join(" · ", CardStatusEntries.From(card).Select(entry => entry.Text));
        if (statuses.Length > 0)
            Add(copy, statuses, "StackStatuses", 11, CardTypography.Body).AutowrapMode = TextServer.AutowrapMode.Word;
        return header;
    }

    internal static void Refresh(CardControl card, BoardCardPresentation presentation)
    {
        if (card.GetNodeOrNull<Control>("EncounterStackOverlay/SchemeStackHeader") is not { } old) return;
        Node parent = old.GetParent();
        parent.RemoveChild(old);
        old.QueueFree();
        parent.AddChild(Create(presentation, card.CustomMinimumSize.X));
    }

    private static Label Add(VBoxContainer parent, string text, string name, int size, Font font)
    {
        Label label = CardSourceStrip.Label(text, name, size, font);
        label.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        parent.AddChild(label);
        return label;
    }
}
