using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Every physical attachment remains directly inspectable in one continuous collection.</summary>
internal static class CardSourceCollection
{
    internal static VBoxContainer Create(BoardCardPresentation[] cards, float width,
        BoardRenderResult result, InterfaceScale scale, string relationship)
    {
        var stack = new VBoxContainer { Name = "SourceCollection", CustomMinimumSize = new Vector2(width, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        stack.AddThemeConstantOverride("separation", 3);
        stack.AddChild(new Label { Text = $"{relationship} · {cards.Length}", ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore });
        var rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        stack.AddChild(rows);
        result.Inspector.Register(cards);
        foreach (BoardCardPresentation card in cards)
        {
            CardControl control = SourceTableauTile.CreateCard(card, width, scale, compact: true);
            control.Name = $"ProceduralCard{card.TargetId}";
            rows.AddChild(control);
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
            rows.SortChildren += () => Callable.From(() =>
            { if (InteractionControl.IsUsable(control)) result.UpdateRestingPose(control); }).CallDeferred();
        }
        return stack;
    }
}
