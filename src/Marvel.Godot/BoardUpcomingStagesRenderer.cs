using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders the collapsible future-stage stack for one board area.</summary>
internal static class BoardUpcomingStagesRenderer
{
    internal static void Add(
        VBoxContainer destination,
        BoardCardPresentation[] cards,
        int areaId,
        BoardRenderResult result,
        InterfaceScale scale,
        IDictionary<int, bool> expandedAreas,
        ICardArtProvider? art)
    {
        if (cards.Length == 0)
        {
            return;
        }

        int count = cards.Sum(card => card.Count);
        int stateKey = BoardRenderer.UpcomingStagesStateKey(areaId);
        bool expanded = expandedAreas.TryGetValue(stateKey, out bool remembered)
            && remembered;
        var section = new VBoxContainer
        {
            Name = "UpcomingStages",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        var disclosure = new Button
        {
            Name = "UpcomingStagesDisclosure",
            Text = $"{(expanded ? "▾" : "▸")}  Upcoming stages  ·  {count}",
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = expanded,
            TooltipText = "Show or hide the stages that follow the current stage.",
        };
        var list = new VBoxContainer
        {
            Name = "UpcomingStagesList",
            Visible = expanded,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        disclosure.Pressed += () =>
        {
            expandedAreas[stateKey] = disclosure.ButtonPressed;
            list.Visible = disclosure.ButtonPressed;
            disclosure.Text = $"{(disclosure.ButtonPressed ? "▾" : "▸")}  Upcoming stages  ·  {count}";
        };
        section.AddChild(disclosure);
        section.AddChild(list);
        destination.AddChild(section);

        foreach (BoardCardPresentation card in cards)
        {
            CardControl control = CardControl.Create(card, CardDisplaySize.Board, scale, art);
            control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            list.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card);
        }
    }
}
