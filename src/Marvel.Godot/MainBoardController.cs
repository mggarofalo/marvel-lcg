using Godot;
using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns board rendering and the card-inspection interaction.</summary>
internal sealed class MainBoardController : IDisposable
{
    private readonly Main main;
    private readonly CardInspectorFocus inspector;
    private readonly BoardRelationshipOverlayController relationships;
    private readonly InteractionGeneration renderGeneration = new();
    private readonly MainTabletopController tabletop;

    internal MainBoardController(Main main)
    {
        this.main = main;
        inspector = new CardInspectorFocus(main);
        relationships = new BoardRelationshipOverlayController(main);
        tabletop = new MainTabletopController(main);
    }

    internal void RenderGame(
        EngineResponse response,
        bool resetEvents = false,
        bool preserveEvents = false,
        GameProgressPresentation? priorProgress = null,
        string operation = EngineProtocol.Resolve)
    {
        int renderGeneration = this.renderGeneration.Advance();
        Outcome previousOutcome = main.CurrentGame?.World?.Outcome ?? Outcome.Unfinished;
        HashSet<int> priorHistory = main.CurrentGame?.History?.Entries
            .Select(entry => entry.Cursor)
            .ToHashSet() ?? [];
        if (!string.Equals(main.CurrentGame?.GameId, response.GameId, StringComparison.Ordinal))
        {
            tabletop.ResetForGame();
        }
        main.CurrentGame = response;
        WorldDescriptor world = response.World!;
        RenderCurrentResponse(response, world, renderGeneration);
        IReadOnlyList<EventPresentation> reportNarrative = BoardResponsePresentation.Update(
            main,
            response, previousOutcome, priorHistory, resetEvents, preserveEvents, operation);
        FinishRender(response, world, priorProgress, operation, reportNarrative, renderGeneration);
    }

    private void RenderCurrentResponse(
        EngineResponse response,
        WorldDescriptor world,
        int renderGeneration)
    {
        RenderBoard(world, response.Prompt, renderGeneration);
        main.syncStatus.Visible = true;
        main.syncStatus.Text = $"✓ Synced · r{response.Revision}";
        main.synchronize.Visible = true;
        main.RenderPromptSummary(response.Prompt, world);
        main.decisions.Render(response.Prompt, world, response.Revision);
        main.decisions.BindMulliganTargets(main.boardRender);
        main.layoutController.ApplyResponsivePlayLayout();
    }

    private void FinishRender(
        EngineResponse response,
        WorldDescriptor world,
        GameProgressPresentation? priorProgress,
        string operation,
        IReadOnlyList<EventPresentation> reportNarrative,
        int renderGeneration)
    {
        main.transcript.RecordResponse(operation, response, reportNarrative);
        // A synchronized snapshot is authoritative but is not a new
        // transition, so it does not alter the diagnostic chronology.
        main.ApplyProgress(GameProgressPresentation.FromSynchronization(
            response,
            priorProgress ?? main.currentProgress));
        main.pageScroll.ScrollVertical = 0;
        Callable.From(() => ResetPageScroll(renderGeneration)).CallDeferred();
        main.RefreshSynchronizeAvailability();
        if (response.Prompt is null && world.Outcome != Outcome.Unfinished)
        {
            Callable.From(() =>
            {
                if (IsCurrentRender(renderGeneration))
                {
                    main.RevealOutcome();
                }
            }).CallDeferred();
        }
    }

    private void ResetPageScroll(int renderGeneration)
    {
        if (IsCurrentRender(renderGeneration) && InteractionControl.IsUsable(main.pageScroll))
        {
            main.pageScroll.ScrollVertical = 0;
        }
    }

    internal void RenderBoard(
        WorldDescriptor world, Prompt? prompt = null, int? renderGeneration = null)
    {
        prompt ??= main.CurrentGame?.Prompt;
        main.boardPresentation = BoardPresentation.From(world);
        // The render target is already sized when a response arrives, while a
        // newly shown Control can still be waiting for its container layout.
        // Choose the opening surface from that settled canvas, not its
        // transient child size.
        Vector2 viewport = main.GetViewportRect().Size;
        BoardRenderResult rendered = tabletop.Render(prompt, viewport)
            ?? BoardRenderer.Render(
                main.boardAreas, main.boardPresentation, main.handRail, main.handHeading,
                main.interfaceScale, main.expandedAreas, main.art);
        main.boardRender = rendered;
        rendered.CardActivated += (card, control) => ToggleCardInspector(card, control);
        rendered.IsCurrent = () => ReferenceEquals(main.boardRender, rendered)
            && IsCurrentRender(renderGeneration ?? this.renderGeneration.Current);
        relationships.Bind(rendered, main.boardPresentation.Relationships);
        main.decisions.BindMulliganTargets(rendered);
        inspector.Hide();
    }

    internal void FocusAnchors(IReadOnlyList<int> ids) => tabletop.FocusAnchors(ids);

    internal void FocusEventAnchors(IReadOnlyList<int> ids) => tabletop.FocusAnchors(ids);

    internal void PreviewHandCard(int? id)
    {
        if (main.cardInspectorPinned)
        {
            return;
        }

        if (id is null)
        {
            inspector.Hide();
            return;
        }

        BoardCardPresentation? card = main.boardPresentation?.Areas
            .Where(area => area.Zone == "HandsArea")
            .SelectMany(area => area.Cards)
            .FirstOrDefault(candidate => candidate.TargetId == id);
        Control? source = main.boardRender?.ControlFor(id.Value);
        if (card is null || source is null)
        {
            return;
        }

        ShowCardInspector(card, source, pinned: false);
    }

    internal void ToggleCardInspector(BoardCardPresentation card, Control? source)
    {
        if (card.Concealed)
        {
            return;
        }

        if (main.cardInspector.Visible && main.inspectedCardId == card.TargetId)
        {
            inspector.Hide();
            return;
        }

        ShowCardInspector(card, source, pinned: true);
    }

    internal void ShowCardInspector(
        BoardCardPresentation card, Control? source, bool pinned)
    {
        int inspectorGeneration = checked(++main.cardInspectorGeneration);
        main.inspectedCardId = card.TargetId;
        int? sourceId = source is CardControl sourceCard
            ? sourceCard.TargetId
            : card.TargetId;
        ClearInspectorContent();
        InterfaceScale inspectionScale = CardInspectorFocus.FittedScale(
            card, main.interfaceScale, main.Size.Y);
        CardControl detail = CardControl.Create(
            card, CardDisplaySize.Full, inspectionScale, main.art);
        detail.FocusMode = Control.FocusModeEnum.All;
        CardInspectorFocus.IgnoreMouseRecursively(detail);
        main.cardInspectorContent.AddChild(detail);
        ConfigureInspectorFrame();
        PositionInspector(card, source, pinned);
        ShowInspector(detail, sourceId, pinned, inspectorGeneration);
    }

    private void ClearInspectorContent()
    {
        foreach (Node child in main.cardInspectorContent.GetChildren())
        {
            main.cardInspectorContent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ConfigureInspectorFrame()
    {
        main.cardInspectorScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        main.cardInspectorScroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        main.cardInspectorFrame.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }

    private void PositionInspector(BoardCardPresentation card, Control? source, bool pinned)
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

    private void ShowInspector(
        Control detail,
        int? sourceId,
        bool pinned,
        int inspectorGeneration)
    {
        main.cardInspectorPinned = pinned;
        if (pinned)
        {
            CardInspectorFocus.RestoreMouseRecursively(main.cardInspectorFrame);
        }
        else
        {
            // A hover preview is informational only; it must never cover a
            // decision target that the pointer is travelling toward.
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
        main.cardInspectorBackdrop.Visible = pinned;
        main.cardInspectorClose.Visible = false;
        main.cardInspector.Visible = true;
        if (pinned)
        {
            inspector.RememberSource(sourceId);
            Callable.From(() => inspector.FocusDetail(inspectorGeneration)).CallDeferred();
        }
    }

    internal void Input(InputEvent input) => MainBoardInputRouter.Route(main, inspector, input);
    internal void ScheduleCardInspectorHide() => inspector.ScheduleHide();
    internal void BindCardInspectorFocus(Control control) => inspector.BindFocus(control);
    internal bool CardInspectorHasFocus() => inspector.HasFocus();
    internal void HideCardInspector() => inspector.Hide();
    public void Dispose() => relationships.Dispose();
    private bool IsCurrentRender(int generation) => main.IsInsideTree()
        && generation == renderGeneration.Current;
}
