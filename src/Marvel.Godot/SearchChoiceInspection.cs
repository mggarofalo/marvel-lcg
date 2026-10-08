using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Opens read-only candidate comparison within one live search draft.</summary>
internal sealed class SearchChoiceInspection
{
    private readonly DecisionPanel panel;
    private readonly DecisionComposer draft;
    private readonly int generation;
    private readonly BoardRenderResult result;
    private readonly TabletopAreaObject collection;

    internal SearchChoiceInspection(DecisionPanel panel, IReadOnlyList<BoardCardPresentation> candidates,
        BoardPresentation board, int generation)
    {
        this.panel = panel;
        draft = panel.composer!;
        this.generation = generation;
        result = new BoardRenderResult { IsCurrent = IsCurrent };
        result.Inspector.Register(board.Areas.SelectMany(area => area.Cards).Where(card => !card.Concealed).ToArray());
        collection = TabletopAreaObject.From(new(-1, "Card choices", "", candidates.Reverse().ToArray(), []));
    }

    internal void Bind(CardControl face, BoardCardPresentation card)
    {
        face.TooltipText = $"Inspect {card.Title}";
        face.AccessibilityName = $"Inspect {card.Title}";
        face.GuiInput += input =>
        {
            if (!IsCurrent()) return;
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }
                || input.IsActionPressed("ui_accept"))
            {
                face.AcceptEvent();
                Main main = ClientSceneHost.MainFor(panel);
                int index = collection.InspectionOrder.ToList().FindIndex(candidate => candidate.TargetId == card.TargetId);
                TabletopPileInspector.Show(face, collection, result, main.interfaceScale, main.art, index, allowActions: false);
            }
        };
        face.TreeExiting += () => TabletopPileInspector.CloseFor(result);
    }

    private bool IsCurrent() => InteractionControl.IsUsable(panel) && !panel.submitting
        && panel.CompleteChoicesOpen && panel.IsCurrentDraft(draft, generation);
}
