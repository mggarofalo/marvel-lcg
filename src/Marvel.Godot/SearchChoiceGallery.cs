using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Pages authorized search results while retaining the single prompt-bound draft.</summary>
internal sealed class SearchChoiceGallery(DecisionPanel panel)
{
    internal static bool IsChoice(Prompt? prompt) =>
        prompt?.PublicKind is PublicDecisionKind.CardSearch or PublicDecisionKind.CardLook;

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
        BoardCardPresentation[] cards = [.. BoardPresentation.From(panel.world!).Areas
            .SelectMany(area => area.Cards).Where(card => !card.Concealed)];
        foreach (AffordancePresentation option in prompt.Affordances.Skip(page * capacity).Take(capacity))
        {
            BoardCardPresentation? card = cards.FirstOrDefault(candidate => candidate.TargetId == option.CardAnchorId);
            AddChoice(row, option, card, generation, cardScale);
        }
        AddNavigation(pages, generation);
    }

    private void AddChoice(HBoxContainer row, AffordancePresentation option,
        BoardCardPresentation? card, int generation, InterfaceScale scale)
    {
        bool selected = panel.composer!.Selected?.Id == option.Id;
        var choice = new Button
        {
            Name = $"Affordance{option.Id}", Text = selected ? "✓" : "◎",
            AccessibilityName = $"Select {option.SourceName ?? card?.Title ?? option.Label}",
            Disabled = panel.submitting || option.Illegal is not null,
            CustomMinimumSize = new Vector2(44, 44),
        };
        choice.TooltipText = choice.AccessibilityName;
        choice.Pressed += () => panel.SelectAffordance(option.Id, generation);
        if (card is null)
        {
            choice.Text = option.DisplayLabel ?? option.Label;
            row.AddChild(choice);
            return;
        }
        CardControl face = CardControl.Create(card, CardDisplaySize.Full, scale);
        face.Name = $"SearchResult{option.Id}";
        face.GuiInput += input =>
        {
            if (choice.Disabled) return;
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }
                || input.IsActionPressed("ui_accept"))
            {
                face.AcceptEvent();
                panel.SelectAffordance(option.Id, generation);
            }
        };
        row.AddChild(CardStateDetails.Wrap(face, card, beside: false));
        face.SetInteractionCue(selected ? CardInteractionCue.SelectedTarget : CardInteractionCue.LegalTarget);
        face.HideInteractionCue();
        face.GetNode<Control>("CardSurface").AddChild(choice);
        choice.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        choice.Position = new Vector2(face.Size.X - 48, 4);
        CardSymbolButtonStyle.Apply(choice);
        choice.AddThemeColorOverride("font_color", CardFaceStyle.Ink);
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
