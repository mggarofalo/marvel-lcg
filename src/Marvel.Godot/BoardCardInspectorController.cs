using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns card-inspection previews, pinned detail, and its input behavior.</summary>
internal sealed class BoardCardInspectorController
{
    private readonly Main main;
    private readonly CardInspectorFocus inspector;
    private readonly CardInspectorStageNavigation stageNavigation;
    private readonly MainTabletopController tabletop;
    private int previewGeneration;

    internal BoardCardInspectorController(Main main, MainTabletopController tabletop)
    {
        this.main = main;
        this.tabletop = tabletop;
        inspector = new CardInspectorFocus(main);
        stageNavigation = new CardInspectorStageNavigation(
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

    internal void PreviewCardAfterDelay(BoardCardPresentation card, Control source)
    {
        if (card.Concealed || main.cardInspectorPinned)
        {
            return;
        }
        int generation = checked(++previewGeneration);
        main.GetTree().CreateTimer(0.32).Timeout += () =>
        {
            if (generation == previewGeneration
                && !main.cardInspectorPinned
                && InteractionControl.IsUsable(source)
                && PointerIsWithin(source))
            {
                Show(card, source, pinned: false);
            }
        };
    }

    private bool PointerIsWithin(Control source)
    {
        Control? hovered = main.GetViewport().GuiGetHoveredControl();
        return hovered == source || (hovered is not null && source.IsAncestorOf(hovered));
    }

    internal void LeaveCardPreview(Control _)
    {
        previewGeneration++;
        if (!main.cardInspectorPinned)
        {
            inspector.ScheduleHide();
        }
    }

    internal void Toggle(BoardCardPresentation card, Control? source)
    {
        previewGeneration++;
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
        main.inspectedCardId = card.TargetId;
        int? sourceId = source is CardControl sourceCard
            ? sourceCard.TargetId
            : card.TargetId;
        ClearContent();
        InterfaceScale inspectionScale = CardInspectorFocus.FittedScale(
            card, main.interfaceScale, main.Size.Y);
        CardControl detail = CardControl.Create(
            card, CardDisplaySize.Full, inspectionScale, main.art);
        if (!pinned)
        {
            PreparePreview(detail);
        }
        detail.FocusMode = Control.FocusModeEnum.All;
        CardInspectorFocus.IgnoreMouseRecursively(detail);
        main.cardInspectorContent.AddChild(detail);
        stageNavigation.Configure(card, source, pinned
            ? stages ?? main.boardRender?.Inspector.For(card.TargetId) ?? []
            : []);
        HBoxContainer inspectorHeader = main.cardInspectorTitle.GetParent<HBoxContainer>();
        inspectorHeader.Visible = pinned;
        main.cardInspectorTitle.Visible = pinned && stageNavigation.IsVisible;
        main.cardInspectorClose.Visible = pinned;
        ConfigureFrame();
        Position(card, source, pinned);
        Present(detail, sourceId, pinned, inspectorGeneration);
    }

    private static void PreparePreview(CardControl detail)
    {
        if (detail.FindChild("IllustrationRegion", true, false) is not PanelContainer art)
        {
            return;
        }

        bool hasIllustration = art.FindChild("Illustration", true, false) is TextureRect;
        art.Visible = hasIllustration;
        art.CustomMinimumSize = hasIllustration ? new Vector2(0, 96) : Vector2.Zero;
        art.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
    }

    internal void Input(InputEvent input) =>
        MainBoardInputRouter.Route(main, inspector, stageNavigation, input);

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
        main.cardInspectorFrame.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }

    private void Position(BoardCardPresentation card, Control? source, bool pinned)
    {
        Control detail = (Control)main.cardInspectorContent.GetChild(0);
        Vector2 detailSize = detail.GetCombinedMinimumSize();
        float width = detailSize.X;
        float height = Math.Min(main.Size.Y - 48, detailSize.Y);
        Control? currentSource = InteractionControl.IsUsable(source)
            ? source
            : card.TargetId is { } target ? main.boardRender?.ControlFor(target) : null;
        Rect2 sourceRect = currentSource?.GetGlobalRect() ?? new Rect2(
            main.GetViewport().GetMousePosition(), Vector2.Zero);
        if (!pinned)
        {
            height = Math.Min(height, Math.Max(160, sourceRect.Position.Y - 24));
        }

        main.cardInspectorFrame.CustomMinimumSize = Vector2.Zero;
        main.cardInspectorFrame.Size = new Vector2(width, Math.Max(pinned ? 240 : 160, height));
        Vector2 anchor = sourceRect.Position + sourceRect.Size / 2;
        FloatingPanelPosition position = pinned
            ? VisualSystem.PlaceFloatingPanel(
                Mathf.RoundToInt(main.Size.X),
                Mathf.RoundToInt(main.Size.Y),
                Mathf.RoundToInt(anchor.X),
                Mathf.RoundToInt(anchor.Y),
                Mathf.RoundToInt(width),
                Mathf.RoundToInt(height))
            : new FloatingPanelPosition(
                Mathf.RoundToInt(Mathf.Clamp(
                    anchor.X - width / 2, 12, Math.Max(12, main.Size.X - width - 12))),
                Mathf.RoundToInt(Mathf.Max(12, sourceRect.Position.Y - height - 12)));
        main.cardInspectorFrame.Position = new Vector2(position.X, position.Y);
    }

    private void Present(Control detail, int? sourceId, bool pinned, int inspectorGeneration)
    {
        main.cardInspectorPinned = pinned;
        if (pinned)
        {
            CardInspectorFocus.RestoreMouseRecursively(main.cardInspectorFrame);
        }
        else
        {
            // The preview itself is a stable hover bridge from the source.
            // Its contents do not accept actions, while the frame keeps the
            // preview alive until the pointer leaves both objects.
            CardInspectorFocus.IgnoreMouseRecursively(main.cardInspectorFrame, interactiveRules: false);
            main.cardInspectorFrame.MouseFilter = Control.MouseFilterEnum.Stop;
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
        main.cardInspectorBackdrop.Visible = pinned;
        main.cardInspector.Visible = true;
        if (pinned)
        {
            inspector.RememberSource(sourceId);
            Callable.From(() => inspector.FocusDetail(inspectorGeneration)).CallDeferred();
        }
    }
}
