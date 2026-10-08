using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Measures live state alongside inspection, or below a bounded choice-gallery face.</summary>
internal static class CardStateDetails
{
    internal static Control Wrap(CardControl face, BoardCardPresentation card, bool beside,
        Action<BoardCardPresentation>? inspect = null)
    {
        VBoxContainer state = CardLiveStateRendering.Create(card, beside ? 224 : face.CustomMinimumSize.X, compact: false);
        if (inspect is not null) CardValueSourceInspection.Add(state, card, inspect);
        if (state.GetChildCount() == 0)
        {
            state.Free();
            return face;
        }
        BoxContainer details = beside ? new HBoxContainer() : new VBoxContainer();
        details.Name = "CardStateDetails";
        details.AddThemeConstantOverride("separation", 12);
        details.AddChild(face);
        state.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        var panel = new PanelContainer { Name = "StatePanel", SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
        using var style = new StyleBoxFlat { BgColor = CardFaceStyle.Ink,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 8, ContentMarginBottom = 8 };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(state);
        details.AddChild(panel);
        return details;
    }
}
