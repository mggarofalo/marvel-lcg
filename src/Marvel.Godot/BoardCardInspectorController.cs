using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns card-inspection previews, pinned detail, and its input behavior.</summary>
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
        layout = new CardInspectorLayout(main);
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
        IReadOnlyList<BoardCardPresentation>? stages = null)
    {
        int inspectorGeneration = checked(++main.cardInspectorGeneration);
        previewSource = pinned ? null : source;
        main.inspectedCardId = card.TargetId;
        main.cardInspector.SetMeta("inspected_card_anchor", card.TargetId ?? -1);
        int? sourceId = source is CardControl sourceCard
            ? sourceCard.TargetId
            : card.TargetId;
        ClearContent();
        InterfaceScale inspectionScale = CardInspectorFocus.FittedScale(
            card, main.interfaceScale, main.Size.Y);
        Control detail = CardStateDetails.Wrap(CardControl.Create(
            card, CardDisplaySize.Full, inspectionScale, main.art), card, beside: true,
            inspect: valueSource => Show(valueSource, source, pinned: true, stages: [card, valueSource]));
        detail.FocusMode = Control.FocusModeEnum.All;
        CardInspectorFocus.IgnoreMouseRecursively(detail);
        main.cardInspectorContent.AddChild(detail);
        cardNavigation.Configure(card, source, pinned
            ? stages ?? main.boardRender?.Inspector.For(card.TargetId) ?? []
            : []);
        HBoxContainer inspectorHeader = main.cardInspectorTitle.GetParent<HBoxContainer>();
        inspectorHeader.Visible = pinned;
        main.cardInspectorTitle.Visible = pinned && cardNavigation.IsVisible;
        main.cardInspectorClose.Visible = pinned;
        ConfigureFrame();
        layout.BindMeasurement(detail, card, source, pinned);
        layout.Position(card, source, pinned);
        Present(detail, sourceId, source, pinned, inspectorGeneration);
    }

    internal void Refresh(BoardPresentation board, BoardRenderResult rendered)
    {
        BoardCardPresentation? current = BoardInspectorSequences.Current(board, main.inspectedCardId);
        Control? source = current?.TargetId is { } id ? rendered.ControlFor(id) : null;
        if (!main.cardInspectorPinned || current is null || source is null) inspector.Hide();
        else Show(current, source, pinned: true);
    }

    internal void Input(InputEvent input) =>
        MainBoardInputRouter.Route(main, inspector, cardNavigation, input);

    internal void ScheduleHide() => inspector.ScheduleHide();
    internal void BindFocus(Control control) => inspector.BindFocus(control);
    internal bool HasFocus() => inspector.HasFocus();
    internal void Hide() => inspector.Hide();

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

        main.cardInspector.MouseFilter = pinned
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;
        // The backdrop shares the inspector's full viewport bounds. Keep it
        // transparent while previewing so a hover cannot replace the decision
        // control that opened the preview as the pointer's GUI owner.
        main.cardInspectorBackdrop.MouseFilter = pinned
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;
        main.cardInspectorBackdrop.Visible = pinned && !main.decisions.PaymentModalOpen;
        main.cardInspector.Visible = true;
        if (pinned)
        {
            inspector.RememberSource(sourceId, source);
            Callable.From(() => inspector.FocusDetail(inspectorGeneration)).CallDeferred();
        }
    }
}
