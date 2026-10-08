using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Pages authorized search results while retaining the single prompt-bound draft.</summary>
internal sealed class SearchChoiceGallery(DecisionPanel panel)
{
    internal static bool IsChoice(Prompt? prompt) =>
        prompt?.PublicKind is PublicDecisionKind.CardSearch or PublicDecisionKind.CardLook
            or PublicDecisionKind.MinionActivationOrder or PublicDecisionKind.SpecialAbilityNext;

    private DecisionComposer? draft;
    private int page;
    private int capacity;
    private InterfaceScale cardScale;

    internal void RefreshLayout()
    {
        if (!panel.CompleteChoicesOpen || !ReferenceEquals(draft, panel.composer)) return;
        Vector2 viewport = panel.GetViewportRect().Size;
        if (capacity != CardChoiceLayout.Capacity(viewport) || cardScale != CardChoiceLayout.CardScale(viewport))
            panel.Rebuild();
    }

    internal void Add(PromptPresentation prompt, int generation)
    {
        if (!ReferenceEquals(draft, panel.composer)) { draft = panel.composer; page = 0; }
        Vector2 viewport = panel.GetViewportRect().Size;
        capacity = CardChoiceLayout.Capacity(viewport);
        cardScale = CardChoiceLayout.CardScale(viewport);
        int pages = Math.Max(1, (prompt.Affordances.Count + capacity - 1) / capacity);
        page = Math.Clamp(page, 0, pages - 1);
        var row = new HBoxContainer { Name = "SearchCards", Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 24);
        panel.AddContent(row);
        BoardPresentation board = BoardPresentation.From(panel.world!);
        IReadOnlyList<BoardCardPresentation> cards = SearchChoiceCandidates.From(prompt, board);
        var inspection = new SearchChoiceInspection(panel, cards, board, generation);
        foreach (AffordancePresentation option in prompt.Affordances.Skip(page * capacity).Take(capacity))
        {
            BoardCardPresentation? card = cards.FirstOrDefault(candidate => candidate.TargetId == option.CardAnchorId);
            SearchChoiceCard.Add(panel, row, option, card, generation, cardScale, inspection);
        }
        AddNavigation(pages, generation);
    }

    private void AddNavigation(int pages, int generation)
    {
        if (pages == 1) return;
        var row = new HBoxContainer { Name = "SearchPages", Alignment = BoxContainer.AlignmentMode.Center };
        AddPageButton(row, "PreviousSearchPage", "‹", page > 0, -1, generation);
        row.AddChild(new Label { Text = $"{page + 1} / {pages}", VerticalAlignment = VerticalAlignment.Center });
        AddPageButton(row, "NextSearchPage", "›", page + 1 < pages, 1, generation);
        panel.AddContent(row);
    }

    private void AddPageButton(HBoxContainer row, string name, string symbol, bool enabled, int offset, int generation)
    {
        var button = new Button { Name = name, Text = symbol, Disabled = !enabled || panel.submitting,
            CustomMinimumSize = new Vector2(56, 44),
            AccessibilityName = offset < 0 ? "Previous cards" : "Next cards" };
        button.Pressed += () =>
        {
            if (draft is null || !panel.IsCurrentDraft(draft, generation)) return;
            page += offset;
            panel.Rebuild();
        };
        row.AddChild(button);
    }
}
