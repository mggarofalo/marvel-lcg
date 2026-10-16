using Marvel.Protocol;
using Godot;
using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns board rendering and response presentation.</summary>
internal sealed class MainBoardController : IDisposable
{
    private readonly Main main;
    private EngineResponse? displayedResponse;
    private readonly BoardResponseTransition transition;
    internal bool ResponseStaged => transition.Pending;
    internal EngineResponse? PresentedGame => displayedResponse ?? main.CurrentGame;
    private readonly BoardCardInspectorController cardInspector;
    private readonly BoardRelationshipOverlayController relationships;
    private readonly BoardRenderLifetime renderLifetime = new();
    private readonly MainTabletopController tabletop;
    private readonly TableInputGestureBoundary inputBoundary = new();
    private readonly ForcedAbilityController forcedAbilities;

    internal MainBoardController(Main main)
    {
        this.main = main;
        transition = new BoardResponseTransition(main);
        forcedAbilities = new ForcedAbilityController(main);
        relationships = new BoardRelationshipOverlayController(main);
        tabletop = new MainTabletopController(main);
        cardInspector = new BoardCardInspectorController(main, tabletop);
    }

    internal void ResetForSession()
    {
        transition.Cancel();
        main.eventController.PresentAttack(null);
        forcedAbilities.Clear();
        main.eventController.ClearTimeline();
        HandPresentationOrder.Clear();
        SourcePresentationOrder.Clear();
        cardInspector.Reset();
        renderLifetime.Advance();
        main.SkipEventPresentation();
        displayedResponse = null;
        tabletop.ResetForGame();
    }

    internal void CancelPendingPresentation()
    {
        transition.Cancel();
        main.SkipEventPresentation();
    }

    internal void RenderGame(
        EngineResponse response,
        bool resetEvents = false,
        bool preserveEvents = false,
        string operation = EngineProtocol.Resolve,
        DecisionReceiptContext? acceptedReceipt = null)
    {
        transition.Cancel();
        main.SkipEventPresentation();
        var options = new BoardResponsePresentation.Options(resetEvents, preserveEvents, operation, acceptedReceipt);
        if (transition.TryBegin(response, displayedResponse, options,
            () => ApplyResponse(response, options with { AlreadyPresented = true }))) return;
        ApplyResponse(response, options);
    }

    private void ApplyResponse(EngineResponse response, BoardResponsePresentation.Options options)
    {
        int renderGeneration = renderLifetime.Advance();
        Outcome previousOutcome = displayedResponse?.World?.Outcome ?? Outcome.Unfinished;
        HashSet<int> priorHistory = displayedResponse?.History?.Entries
            .Select(entry => entry.Cursor)
            .ToHashSet() ?? [];
        if (!string.Equals(displayedResponse?.GameId, response.GameId, StringComparison.Ordinal))
        {
            HandPresentationOrder.Clear();
            SourcePresentationOrder.Clear();
            cardInspector.Reset();
            tabletop.ResetForGame();
        }
        displayedResponse = response;
        WorldDescriptor world = response.World!;
        RenderCurrentResponse(response, world, renderGeneration);
        IReadOnlyList<EventPresentation> reportNarrative = BoardResponsePresentation.Update(
            main,
            response, previousOutcome, priorHistory,
            options);
        main.layoutController.ApplyResponsivePlayLayout();
        FinishRender(response, world, options.Operation, reportNarrative, renderGeneration);
    }
    private void RenderCurrentResponse(
        EngineResponse response,
        WorldDescriptor world,
        int renderGeneration)
    {
        // The settled table remains the primary record of how the game ended.
        // A null terminal prompt disables actions without hiding inspection or history.
        main.setupPanel.Visible = false;
        main.board.Visible = true;
        RenderBoard(world, response.Prompt, renderGeneration);
        main.syncStatus.Visible = true;
        main.syncStatus.Text = "Synced";
        main.syncStatus.SetMeta("response_revision", response.Revision);
        main.synchronize.Visible = true;
        main.RenderPromptSummary(response.Prompt, world);
        main.decisions.Render(response.Prompt, world, response.Revision);
        main.decisions.BindMulliganTargets(main.boardRender);
    }
    private void FinishRender(
        EngineResponse response,
        WorldDescriptor world,
        string operation,
        IReadOnlyList<EventPresentation> reportNarrative,
        int renderGeneration)
    {
        main.transcript.RecordResponse(operation, response, reportNarrative);
        // A synchronized snapshot is authoritative but is not a new
        // transition, so it does not alter the diagnostic chronology.
        main.ApplyProgress(main.lifecycle.Progress!);
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
        Action<BoardRenderResult>? restoreChoice = main.decisions.BoardChoices.Capture(renderGeneration is null);
        if (ResponseStaged && displayedResponse is { World: { } prior })
        {
            world = prior;
            prompt = displayedResponse.Prompt;
        }
        else prompt ??= main.CurrentGame?.Prompt;
        main.boardPresentation = BoardPresentation.From(world);
        // The render target is already sized when a response arrives, while a
        // newly shown Control can still be waiting for its container layout.
        // Choose the opening surface from that settled canvas, not its
        // transient child size.
        Vector2 viewport = main.GetViewportRect().Size;
        main.GetNode<PanelContainer>("Margin/Shell/Content/Play/Board/HandShelf").Visible = true;
        BoardRenderResult rendered = tabletop.Render(prompt, viewport)
            ?? RenderCompactBoard();
        main.boardRender = rendered;
        TableSidebarContent.Render(main, rendered, prompt, tabletop.InspectedSeat);
        rendered.BindMotion(() => main.eventMotion.ButtonPressed);
        rendered.RegisterContextualWorld(world);
        rendered.PresentLastResult(main.lastResultSummary.Text);
        rendered.CardActivated += cardInspector.Toggle;
        rendered.CardPreviewEntered += cardInspector.PreviewCard;
        rendered.CardPreviewExited += cardInspector.LeaveCardPreview;
        rendered.SourceArrangementStarted += cardInspector.HidePreview;
        rendered.IsCurrent = () => ReferenceEquals(main.boardRender, rendered)
            && !ResponseStaged && IsCurrentRender(renderGeneration ?? renderLifetime.Current);
        relationships.Bind(rendered);
        main.decisions.BindMulliganTargets(rendered);
        cardInspector.Refresh(main.boardPresentation!, rendered);
        transition.PresentNextDecision(rendered);
        BoardChoiceContinuity.Restore(restoreChoice, rendered);
    }
    private BoardRenderResult RenderCompactBoard()
    {
        BoardRenderResult rendered = BoardRenderer.Render(
            main.boardAreas, main.boardPresentation!, main.handRail, main.handHeading,
            main.interfaceScale, main.expandedAreas, main.art);
        var actions = new HFlowContainer
        {
            Name = "ContextualActionObjects", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var dock = new VBoxContainer { Name = "CompactDecisionDock",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        dock.AddChild(actions);
        var complete = new Button { Name = "CompleteChoiceSheet", Text = ChoiceDisclosure.Label(main.CurrentGame?.Prompt),
            CustomMinimumSize = new Vector2(148, 44),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            Visible = main.CurrentGame?.Prompt is not null };
        dock.AddChild(complete);
        TableCompactButtonStyle.Apply(complete);
        main.boardAreas.AddChild(dock);
        main.boardAreas.MoveChild(dock, 0);
        rendered.RegisterContextualActions(actions);
        rendered.RegisterCompleteChoices(complete);
        return rendered;
    }

    internal void FocusAnchors(IReadOnlyList<int> ids) => tabletop.FocusAnchors(ids);

    internal void FocusEventAnchors(IReadOnlyList<int> ids) => tabletop.FocusAnchors(ids);
    internal void RerenderForViewport(Vector2 viewport) => tabletop.RerenderForViewport(viewport);

    internal void PreviewHandCard(int? id) => cardInspector.PreviewHandCard(id);
    internal void ToggleCardInspector(BoardCardPresentation card, Control? source) =>
        cardInspector.Toggle(card, source);
    internal void ShowCardInspector(BoardCardPresentation card, Control? source, bool pinned) =>
        cardInspector.Show(card, source, pinned);
    internal void Input(InputEvent input)
    {
        if (ResponseStaged && StagedResponseInput.Consume(main, input)) return;
        if (main.CurrentGame is not null && inputBoundary.Consume(input, main.GetViewport().GuiGetHoveredControl()))
        {
            main.GetViewport().SetInputAsHandled();
            return;
        }
        cardInspector.Input(input);
    }
    internal void RouteAbilityNotificationInput(InputEvent input) => forcedAbilities.Input(input);
    internal void RecordAbilityNotifications(EngineResponse response, IReadOnlyList<EventPresentation> cues,
        bool accepted, bool reset) => forcedAbilities.Record(response, cues, accepted, reset);
    internal void ScheduleCardInspectorHide() => cardInspector.ScheduleHide();
    internal void BindCardInspectorFocus(Control control) => cardInspector.BindFocus(control);
    internal bool CardInspectorHasFocus() => cardInspector.HasFocus();
    internal void HideCardInspector() => cardInspector.Hide();
    public void Dispose()
    {
        transition.Dispose();
        forcedAbilities.Dispose();
        renderLifetime.Dispose(main.boardRender);
        relationships.Dispose();
    }

    private bool IsCurrentRender(int generation) => renderLifetime.IsCurrent(generation)
        && InteractionControl.IsUsable(main);
}
