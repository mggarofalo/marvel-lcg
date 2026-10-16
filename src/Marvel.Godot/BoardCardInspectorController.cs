using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns card-inspection previews, pinned detail, and its input behavior.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The scene tree owns the inspector layout node.")]
internal sealed class BoardCardInspectorController
{
    private readonly Main main;
    private readonly CardInspectorLayout layout;
    private readonly CardInspectorFocus inspector;
    private readonly CardInspectorCardNavigation cardNavigation;
    private readonly MainTabletopController tabletop;
    private Control? previewSource;

    internal BoardCardInspectorController(Main main, MainTabletopController tabletop)
    {
        this.main = main;
        main.AddChild(new CardInspectorTransition { Name = "InspectorExpansion" });
        layout = new CardInspectorLayout(main);
        main.AddChild(layout);
        this.tabletop = tabletop;
        inspector = new CardInspectorFocus(main);
        cardNavigation = new CardInspectorCardNavigation(
            main, (card, source, stages) => Show(card, source, pinned: true, stages: stages));
    }

    internal void PreviewHandCard(int? id)
    {
        if (main.cardInspectorPinned)
        {
            return;
        }

        if (id is null)
        {
            inspector.ScheduleHide();
            return;
        }

        BoardCardPresentation? card = main.boardPresentation?.Areas
            .Where(area => area.Zone == "HandsArea")
            .SelectMany(area => area.Cards)
            .FirstOrDefault(candidate => candidate.TargetId == id);
        Control? source = HandSource(id.Value, card);
        if (card is null || source is null)
        {
            return;
        }

        Show(card, source, pinned: false);
    }

    internal void PreviewCard(BoardCardPresentation card, Control source)
    {
        if (!card.Concealed && !main.cardInspectorPinned && InteractionControl.IsUsable(source))
            Show(card, source, pinned: false);
    }

    internal void LeaveCardPreview(Control source)
    {
        if (!main.cardInspectorPinned && ReferenceEquals(source, previewSource))
        {
            inspector.ScheduleHide();
        }
    }

    internal void Toggle(BoardCardPresentation card, Control? source)
    {
        if (card.Concealed)
        {
            return;
        }

        if (main.cardInspector.Visible && main.cardInspectorPinned
            && main.inspectedCardId == card.TargetId)
        {
            inspector.Hide();
            return;
        }

        Show(card, source, pinned: true);
    }

    internal void Show(
        BoardCardPresentation card,
        Control? source,
        bool pinned,
        IReadOnlyList<BoardCardPresentation>? stages = null,
        int? maximumScale = null)
    {
        int inspectorGeneration = checked(++main.cardInspectorGeneration);
        previewSource = pinned ? null : source;
        main.inspectedCardId = card.TargetId;
        main.cardInspector.SetMeta("inspected_card_anchor", card.TargetId ?? -1);
        int? sourceId = source is CardControl sourceCard
            ? sourceCard.TargetId
            : card.TargetId;
        cardNavigation.Configure(card, source, pinned
            ? stages ?? main.boardRender?.Inspector.For(card.TargetId) ?? []
            : []);
        HBoxContainer inspectorHeader = main.cardInspectorTitle.GetParent<HBoxContainer>();
        inspectorHeader.Visible = pinned;
        main.cardInspectorTitle.Visible = false;
        main.cardInspectorClose.Visible = pinned;
        main.cardInspectorFrame.SetMeta("bounded_inspection", false);
        main.cardInspectorFrame.SetMeta("inspection_overlay", false);
        main.cardInspectorScroll.Visible = true;
        ConfigureFrame();
        Control detail = CreateFittedDetail(card, source, pinned, maximumScale);
        detail.FocusMode = Control.FocusModeEnum.All;
        CardInspectorFocus.IgnoreMouseRecursively(detail);
        layout.Track(detail, card, source, pinned, () => Show(card, source, pinned, stages,
            detail.GetMeta("inspection_scale").AsInt32() - 10));
        layout.Position(card, source, pinned);
        Present(detail, sourceId, source, pinned, inspectorGeneration);
    }

    private Control CreateFittedDetail(BoardCardPresentation card, Control? source, bool pinned, int? maximumScale)
    {
        InterfaceScale scale = CardInspectorFocus.FittedScale(card, main.interfaceScale, layout.AvailableHeight());
        if (maximumScale is { } maximum) scale = (InterfaceScale)Math.Min((int)scale, Math.Max(50, maximum));
        while (true)
        {
            ClearContent();
            Control detail = CardInspectionContent.Create(card, scale, main.art, beside: false,
                inspect: valueSource => OpenValueSource(valueSource, card, source)).Body;
            detail.SetMeta("inspection_scale", (int)scale);
            main.cardInspectorContent.AddChild(detail);
            layout.Position(card, source, pinned);
            if (maximumScale is not < 50 && layout.PreservesSituation()) return detail;
            if ((int)scale <= 50)
            {
                main.cardInspectorFrame.SetMeta("bounded_inspection", true);
                main.cardInspectorTitle.Text = "Pin to read the full card";
                main.cardInspectorTitle.Visible = true;
                main.cardInspectorTitle.GetParent<HBoxContainer>().Visible = true;
                layout.Position(card, source, pinned);
                return detail;
            }
            // Recreate native controls at the next fitted size; never resample rendered text or icons.
            scale = (InterfaceScale)((int)scale - 10);
        }
    }

    internal void Refresh(BoardPresentation board, BoardRenderResult rendered)
    {
        BoardCardPresentation? current = BoardInspectorSequences.Current(board, main.inspectedCardId);
        Control? source = current?.TargetId is { } id ? rendered.ControlFor(id) : null;
        if (!main.cardInspectorPinned || current is null || source is null) inspector.Hide();
        else Show(current, source, pinned: true);
    }

    internal void Input(InputEvent input) =>
        MainBoardInputRouter.Route(main, inspector, input);

    internal void ScheduleHide() => inspector.ScheduleHide();
    internal void BindFocus(Control control) => inspector.BindFocus(control);
    internal bool HasFocus() => inspector.HasFocus();
    internal void Hide() => inspector.Hide();
    internal void HidePreview() { if (!main.cardInspectorPinned) inspector.Hide(restoreFocus: false); }
    internal void Reset() { previewSource = null; inspector.Hide(restoreFocus: false); }

    private void OpenValueSource(BoardCardPresentation source, BoardCardPresentation host, Control? opener)
    {
        BoardCardPresentation detail = BoardInspectorSequences.Source(main.boardPresentation, source);
        Show(detail, opener, pinned: true, stages: [host, detail]);
    }

    private Control? HandSource(int id, BoardCardPresentation? card)
    {
        Control? source = main.boardRender?.ControlFor(id);
        if (card is not null && source is null)
        {
            tabletop.FocusAnchors([id]);
            source = main.boardRender?.ControlFor(id);
        }

        return source;
    }

    private void ClearContent()
    {
        foreach (Node child in main.cardInspectorContent.GetChildren())
        {
            main.cardInspectorContent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ConfigureFrame()
    {
        main.cardInspectorScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        main.cardInspectorScroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        main.cardInspectorScroll.ScrollHorizontal = 0;
        main.cardInspectorScroll.ScrollVertical = 0;
        using var empty = new StyleBoxEmpty();
        main.cardInspectorFrame.AddThemeStyleboxOverride("panel", empty);
    }


    private void Present(Control detail, int? sourceId, Control? source, bool pinned, int inspectorGeneration)
    {
        main.cardInspectorPinned = pinned;
        if (pinned)
        {
            CardInspectorFocus.RestoreMouseRecursively(main.cardInspectorFrame);
        }
        else
        {
            // Preview lifetime follows pointer geometry without intercepting
            // input intended for another card beneath the full-size face.
            CardInspectorFocus.IgnoreMouseRecursively(main.cardInspectorFrame, interactiveRules: false);
        }

        main.cardInspector.MouseFilter = Control.MouseFilterEnum.Ignore;
        // Only the bounded detail owns pointer input. An outside press is consumed
        // by the focus owner while dismissing, so it cannot commit the card below.
        main.cardInspectorBackdrop.MouseFilter = Control.MouseFilterEnum.Ignore;
        main.cardInspectorBackdrop.Visible = false;
        main.cardInspector.Visible = true;
        main.GetNode<CardInspectorTransition>("InspectorExpansion").Expand(
            main.cardInspectorFrame, source, pinned && main.eventMotion.ButtonPressed,
            () => main.eventMotion.ButtonPressed);
        if (pinned)
        {
            inspector.RememberSource(sourceId, source);
            Callable.From(() => inspector.FocusDetail(inspectorGeneration)).CallDeferred();
        }
    }
}
