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
        // Wrapped text changes its minimum height after the container receives its width.
        header.MinimumSizeChanged += () => header.Size = new Vector2(width, 60);
        Populate(header, card);
        return header;
    }

    private static void Populate(PanelContainer header, BoardCardPresentation card)
    {
        var copy = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        copy.AddThemeConstantOverride("separation", 0);
        header.AddChild(copy);
        Add(copy, card.Title, "StackTitle", 14, CardTypography.Title);
        if (CardProgressValue.From(card) is { IsThreat: true } threat)
            Add(copy, $"Threat {threat.Value}", "StackThreat", 11, CardTypography.Body);
        string statuses = string.Join(" · ", CardStatusEntries.From(card).Select(entry => entry.Text));
        if (statuses.Length > 0)
            Add(copy, statuses, "StackStatuses", 11, CardTypography.Body).AutowrapMode = TextServer.AutowrapMode.Word;
    }

    internal static void Refresh(CardControl card, BoardCardPresentation presentation)
    {
        if (card.GetNodeOrNull<PanelContainer>("EncounterStackOverlay/SchemeStackHeader") is not { } header) return;
        // The header owns the stack's resize subscription, so receipt impact retains it.
        foreach (Node child in header.GetChildren())
        {
            header.RemoveChild(child);
            child.QueueFree();
        }
        Populate(header, presentation);
        header.Size = new Vector2(card.CustomMinimumSize.X, 60);
    }

    private static Label Add(VBoxContainer parent, string text, string name, int size, Font font)
    {
        Label label = CardSourceStrip.Label(text, name, size, font);
        label.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        parent.AddChild(label);
        return label;
    }
}
