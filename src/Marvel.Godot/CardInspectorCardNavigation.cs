using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Exposes related inspected faces as named direct choices without paging.</summary>
internal sealed class CardInspectorCardNavigation(Main main,
    Action<BoardCardPresentation, Control?, IReadOnlyList<BoardCardPresentation>> show)
{
    internal bool IsVisible { get; private set; }

    internal void Configure(BoardCardPresentation card, Control? inspectedFrom,
        IReadOnlyList<BoardCardPresentation> cards)
    {
        HBoxContainer header = main.cardInspectorTitle.GetParent<HBoxContainer>();
        if (header.GetNodeOrNull<Control>("RelatedFaces") is { } old)
        { header.RemoveChild(old); old.QueueFree(); }
        IsVisible = cards.Count > 1;
        main.cardInspectorTitle.Text = card.Title;
        if (!IsVisible) return;
        var scroll = new ScrollContainer { Name = "RelatedFaces", CustomMinimumSize = new Vector2(280, 42),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FollowFocus = true,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var choices = new HBoxContainer();
        scroll.AddChild(choices);
        for (int index = 0; index < cards.Count; index++)
        {
            BoardCardPresentation face = cards[index];
            string label = Caption(face);
            var button = new Button { Name = $"InspectRelatedFace{index}", Text = label,
                CustomMinimumSize = new Vector2(0, 44), Flat = true, Disabled = face == card,
                AccessibilityName = "Inspect " + label };
            button.Pressed += () => show(face, inspectedFrom, cards);
            choices.AddChild(button);
        }
        header.AddChild(scroll);
        scroll.CustomMinimumSize = new Vector2(Math.Min(600, choices.GetCombinedMinimumSize().X), 44);
    }

    private static string Caption(BoardCardPresentation card)
    {
        string? stage = card.PrintedStats.FirstOrDefault(value => value.Name == "Stage")?.Value;
        return stage is null ? card.Title : $"{card.Title} · Stage {stage}";
    }
}
