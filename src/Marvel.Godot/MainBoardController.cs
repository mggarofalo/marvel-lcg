using Godot;
using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns board rendering and response presentation.</summary>
internal sealed class MainBoardController : IDisposable
{
    private readonly Main main;
    private readonly BoardCardInspectorController cardInspector;
    private readonly BoardRelationshipOverlayController relationships;
    private readonly BoardRenderLifetime renderLifetime = new();
    private readonly MainTabletopController tabletop;

    internal MainBoardController(Main main)
    {
        this.main = main;
        relationships = new BoardRelationshipOverlayController(main);
        tabletop = new MainTabletopController(main);
        cardInspector = new BoardCardInspectorController(main, tabletop);
    }

    internal void RenderGame(
        EngineResponse response,
        bool resetEvents = false,
        bool preserveEvents = false,
        GameProgressPresentation? priorProgress = null,
        string operation = EngineProtocol.Resolve)
    {
        int renderGeneration = renderLifetime.Advance();
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
        main.layoutController.ApplyResponsivePlayLayout();
        FinishRender(response, world, priorProgress, operation, reportNarrative, renderGeneration);
    }
    private void RenderCurrentResponse(
        EngineResponse response,
        WorldDescriptor world,
        int renderGeneration)
    {
        // The settled table remains the primary record of how the game ended.
        // A null terminal prompt disables actions without hiding inspection or history.
        main.board.Visible = true;
        RenderBoard(world, response.Prompt, renderGeneration);
        main.syncStatus.Visible = true;
        main.syncStatus.Text = $"✓ Synced · r{response.Revision}";
        main.synchronize.Visible = true;
        main.RenderPromptSummary(response.Prompt, world);
        main.decisions.Render(response.Prompt, world, response.Revision);
        main.decisions.BindMulliganTargets(main.boardRender);
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
        main.GetNode<PanelContainer>("Margin/Shell/Content/Play/Board/HandShelf").Visible = true;
        BoardRenderResult rendered = tabletop.Render(prompt, viewport)
            ?? BoardRenderer.Render(
                main.boardAreas, main.boardPresentation, main.handRail, main.handHeading,
                main.interfaceScale, main.expandedAreas, main.art);
        main.boardRender = rendered;
        rendered.CardActivated += cardInspector.Toggle;
        rendered.CardPreviewEntered += cardInspector.PreviewCardAfterDelay;
        rendered.CardPreviewExited += cardInspector.LeaveCardPreview;
        rendered.IsCurrent = () => ReferenceEquals(main.boardRender, rendered)
            && IsCurrentRender(renderGeneration ?? renderLifetime.Current);
        relationships.Bind(rendered);
        main.decisions.BindMulliganTargets(rendered);
        if (main.cardInspectorPinned
            && (main.inspectedCardId is not { } inspected
                || rendered.ControlFor(inspected) is null))
        {
            cardInspector.Hide();
        }
    }
    internal void FocusAnchors(IReadOnlyList<int> ids) => tabletop.FocusAnchors(ids);

    internal void FocusEventAnchors(IReadOnlyList<int> ids) => tabletop.FocusAnchors(ids);
    internal void RerenderForViewport(Vector2 viewport) => tabletop.RerenderForViewport(viewport);

    internal void PreviewHandCard(int? id) => cardInspector.PreviewHandCard(id);
    internal void ToggleCardInspector(BoardCardPresentation card, Control? source) =>
        cardInspector.Toggle(card, source);
    internal void ShowCardInspector(BoardCardPresentation card, Control? source, bool pinned) =>
        cardInspector.Show(card, source, pinned);
    internal void Input(InputEvent input) => cardInspector.Input(input);
    internal void ScheduleCardInspectorHide() => cardInspector.ScheduleHide();
    internal void BindCardInspectorFocus(Control control) => cardInspector.BindFocus(control);
    internal bool CardInspectorHasFocus() => cardInspector.HasFocus();
    internal void HideCardInspector() => cardInspector.Hide();
    public void Dispose()
    {
        renderLifetime.Dispose(main.boardRender);
        relationships.Dispose();
    }

    private bool IsCurrentRender(int generation) => renderLifetime.IsCurrent(generation)
        && main.IsInsideTree();
}
