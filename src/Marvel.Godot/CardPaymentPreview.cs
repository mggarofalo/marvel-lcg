using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Routes payment inspection through the table's single card inspector.</summary>
internal sealed class CardPaymentPreview(Main main)
{
    internal static BoardCardPresentation? Card(BoardPresentation? board, int id) =>
        board?.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId == id && !card.Concealed);

    internal void ShowSource(int? id)
    {
        if (main.cardInspectorPinned) return;
        if (id is { } exact && Card(main.boardPresentation, exact) is { } card)
            main.ShowCardInspector(card, main.boardRender?.ControlFor(exact), pinned: false);
        else main.ScheduleCardInspectorHide();
    }
}
