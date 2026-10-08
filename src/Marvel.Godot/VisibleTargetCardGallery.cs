using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows paginated visible target cards with explicit draft toggles and separate inspection.</summary>
internal sealed class VisibleTargetCardGallery(DecisionPanel panel)
{
    private readonly TargetCardPages pages = new();
    private DecisionComposer? draft;
    private int capacity;
    private InterfaceScale scale;

    internal static bool IsChoice(Prompt? prompt) => prompt?.PublicKind == PublicDecisionKind.VisibleCardSelection;

    internal void RefreshLayout()
    {
        if (!panel.CompleteChoicesOpen || !ReferenceEquals(draft, panel.composer)) return;
        Vector2 viewport = panel.GetViewportRect().Size;
        if (capacity != CardChoiceLayout.Capacity(viewport) || scale != CardChoiceLayout.CardScale(viewport))
            panel.Rebuild();
    }

    internal void Add(int generation)
    {
        draft = panel.composer!;
        Vector2 viewport = panel.GetViewportRect().Size;
        capacity = CardChoiceLayout.Capacity(viewport);
        scale = CardChoiceLayout.CardScale(viewport);
        pages.Refresh(draft, capacity);
        BoardPresentation board = BoardPresentation.From(panel.world!);
        IReadOnlyList<BoardCardPresentation> cards = pages.ReadableCards(board);
        var inspection = new SearchChoiceInspection(panel, cards, board, generation);
        var row = new HBoxContainer { Name = "VisibleTargetCards", Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 24);
        panel.AddContent(row);
        TableDraftBinding operations = panel.BindTableDraft(draft, generation);
        foreach (int target in pages.Current)
        {
            BoardCardPresentation? card = cards.FirstOrDefault(candidate => candidate.TargetId == target);
            VisibleTargetChoiceCard.Add(panel, row, target, card, scale, inspection, operations);
        }
        AddSelection(cards);
        AddNavigation(generation);
    }

    private void AddSelection(IReadOnlyList<BoardCardPresentation> cards)
    {
        string selected = string.Join(", ", draft!.Targets.Select(id =>
            cards.FirstOrDefault(card => card.TargetId == id)?.Title ?? "Card"));
        var label = new Label
        {
            Name = "VisibleTargetSelection", AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = draft.Targets.Count == 0 ? "No cards selected. Use a card's select control; open its face to inspect."
                : $"{draft.Targets.Count} selected: {selected}",
        };
        panel.AddContent(label);
    }

    private void AddNavigation(int generation)
    {
        if (pages.Count == 1) return;
        var row = new HBoxContainer { Name = "VisibleTargetPages", Alignment = BoxContainer.AlignmentMode.Center };
        AddPageButton(row, "PreviousTargetPage", "‹", -1, pages.Page > 0, generation);
        row.AddChild(new Label { Text = $"{pages.Page + 1} / {pages.Count}", VerticalAlignment = VerticalAlignment.Center });
        AddPageButton(row, "NextTargetPage", "›", 1, pages.Page + 1 < pages.Count, generation);
        panel.AddContent(row);
    }

    private void AddPageButton(HBoxContainer row, string name, string symbol, int offset, bool enabled, int generation)
    {
        var button = new Button { Name = name, Text = symbol, Disabled = !enabled || panel.submitting,
            CustomMinimumSize = new Vector2(56, 44),
            AccessibilityName = offset < 0 ? "Previous cards" : "Next cards" };
        button.Pressed += () =>
        {
            if (draft is null || !panel.IsCurrentDraft(draft, generation)) return;
            pages.Move(offset);
            panel.Rebuild();
        };
        row.AddChild(button);
    }
}
