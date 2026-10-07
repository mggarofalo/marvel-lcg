using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows authorized minion faces with editable activation positions in one draft.</summary>
internal sealed class MinionOrderGallery(DecisionPanel panel)
{
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

    internal void Add(int generation)
    {
        DecisionComposer composer = panel.composer!;
        if (!ReferenceEquals(draft, composer)) { draft = composer; page = 0; }
        int[] offered = [.. composer.Selected!.Targets!.Legal.Distinct()];
        Vector2 viewport = panel.GetViewportRect().Size;
        capacity = CardChoiceLayout.Capacity(viewport);
        cardScale = CardChoiceLayout.CardScale(viewport);
        int pages = Math.Max(1, (offered.Length + capacity - 1) / capacity);
        page = Math.Clamp(page, 0, pages - 1);
        var row = new HBoxContainer { Name = "MinionOrderCards", Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 24);
        panel.AddContent(row);
        BoardCardPresentation[] cards = [.. BoardPresentation.From(panel.world!).Areas.SelectMany(area => area.Cards)];
        TableDraftBinding operations = panel.BindTableDraft(composer, generation);
        foreach (int target in offered.Skip(page * capacity).Take(capacity))
            AddCard(row, target, cards.FirstOrDefault(card => card.TargetId == target), offered, operations);
        AddSequence(offered, composer.Targets);
        AddNavigation(pages, generation);
    }

    private void AddCard(HBoxContainer row, int target, BoardCardPresentation? card,
        IReadOnlyList<int> offered, TableDraftBinding operations)
    {
        string copy = OrderedCardLabels.Copy(offered, target);
        string name = PromptPresentation.Describe(target, panel.world!);
        bool selected = draft!.Targets.Contains(target);
        var column = new VBoxContainer { Name = $"MinionOrderCopy{copy}" };
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = $"{copy} · {name}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var choose = new Button
        {
            Name = $"Target{target}", Text = OrderedCardLabels.Position(draft.Targets, target),
            AccessibilityName = selected ? $"Remove {name} {copy} from activation order" : $"Add {name} {copy} next",
            ToggleMode = true, ButtonPressed = selected, Disabled = panel.submitting,
            CustomMinimumSize = new Vector2(56, 44),
        };
        choose.TooltipText = choose.AccessibilityName;
        panel.StyleButton(choose, selected ? InteractiveVisualState.Selected : InteractiveVisualState.Legal, compact: true);
        choose.Pressed += () => Toggle(target, operations);
        panel.BindAnchors(choose, target);
        header.AddChild(choose);
        column.AddChild(header);
        row.AddChild(column);
        if (card is null) return;
        CardControl face = CardControl.Create(card, CardDisplaySize.Full, cardScale);
        face.Name = $"MinionOrderCard{target}";
        face.GuiInput += input =>
        {
            if (choose.Disabled) return;
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }
                || input.IsActionPressed("ui_accept"))
            {
                face.AcceptEvent();
                Toggle(target, operations);
            }
        };
        face.SetInteractionCue(selected ? CardInteractionCue.SelectedTarget : CardInteractionCue.LegalTarget);
        face.HideInteractionCue();
        column.AddChild(face);
    }

    private void Toggle(int target, TableDraftBinding operations)
    {
        if (!operations.TryToggleTarget(target)) return;
        panel.NotifyAnchorFocused([target]);
        panel.Rebuild();
    }

    private void AddSequence(IReadOnlyList<int> offered, IReadOnlyList<int> selected)
    {
        var sequence = new Label
        {
            Name = "MinionActivationSequence",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = selected.Count == 0 ? "Choose cards in activation order." : OrderedCardLabels.Sequence(offered, selected),
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = GodotThemeVariations.Heading,
        };
        panel.AddContent(sequence);
    }

    private void AddNavigation(int pages, int generation)
    {
        if (pages == 1) return;
        var row = new HBoxContainer { Name = "MinionOrderPages", Alignment = BoxContainer.AlignmentMode.Center };
        AddPage(row, "PreviousMinionPage", "‹", page > 0, -1, generation);
        row.AddChild(new Label { Text = $"{page + 1} / {pages}", VerticalAlignment = VerticalAlignment.Center });
        AddPage(row, "NextMinionPage", "›", page + 1 < pages, 1, generation);
        panel.AddContent(row);
    }

    private void AddPage(HBoxContainer row, string name, string symbol, bool enabled, int offset, int generation)
    {
        var button = new Button
        {
            Name = name, Text = symbol, Disabled = !enabled || panel.submitting,
            CustomMinimumSize = new Vector2(56, 44),
            AccessibilityName = offset < 0 ? "Previous minions" : "Next minions",
        };
        button.Pressed += () =>
        {
            if (draft is null || !panel.IsCurrentDraft(draft, generation)) return;
            page += offset;
            panel.Rebuild();
        };
        row.AddChild(button);
    }
}
