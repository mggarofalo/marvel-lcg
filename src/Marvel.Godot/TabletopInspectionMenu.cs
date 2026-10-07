using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Lists authorized current cards and opens their shared inspector.</summary>
internal static class TabletopInspectionMenu
{
    internal static void Bind(MenuButton drawer, IReadOnlyList<BoardAreaPresentation> areas,
        BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art)
    {
        drawer.FocusMode = Control.FocusModeEnum.All;
        drawer.Text += " ▾";
        var choices = new List<(TabletopAreaObject Pile, int Index)>();
        PopupMenu menu = drawer.GetPopup();
        foreach (BoardAreaPresentation area in areas)
        {
            TabletopAreaObject pile = TabletopAreaObject.From(area with { Removed = [] });
            for (int index = 0; index < pile.InspectionOrder.Count; index++)
            {
                menu.AddItem($"{index + 1} · {CardStatePresentation.Summary(pile.InspectionOrder[index])}", choices.Count);
                choices.Add((pile, index));
            }
        }
        drawer.Disabled = choices.Count == 0;
        menu.IdPressed += id =>
        {
            if (result.IsCurrent?.Invoke() != true) return;
            var choice = choices[(int)id];
            TabletopPileInspector.Show(drawer, choice.Pile, result, scale, art, choice.Index);
        };
    }
}
