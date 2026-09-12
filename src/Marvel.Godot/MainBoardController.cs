using Godot;
using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns board rendering and the card-inspection interaction.</summary>
internal sealed class MainBoardController
{
    private readonly Main main;

    internal MainBoardController(Main main)
    {
        this.main = main;
    }
    internal void RenderGame(
        EngineResponse response,
        bool resetEvents = false,
        bool preserveEvents = false,
        GameProgressPresentation? priorProgress = null,
        string operation = EngineProtocol.Resolve)
    {
        Outcome previousOutcome = main.CurrentGame?.World?.Outcome ?? Outcome.Unfinished;
        HashSet<int> priorHistory = main.CurrentGame?.History?.Entries
            .Select(entry => entry.Cursor)
            .ToHashSet() ?? [];
        main.CurrentGame = response;
        WorldDescriptor world = response.World!;
        RenderCurrentResponse(response, world);
        IReadOnlyList<EventPresentation> reportNarrative = UpdateEvents(
            response, world, previousOutcome, priorHistory, resetEvents, preserveEvents, operation);
        FinishRender(response, world, priorProgress, operation, reportNarrative);
    }

    private void RenderCurrentResponse(EngineResponse response, WorldDescriptor world)
    {
        RenderBoard(world);
        main.syncStatus.Visible = true;
        main.syncStatus.Text = $"✓ Synced · r{response.Revision}";
        main.synchronize.Visible = true;
        main.RenderPromptSummary(response.Prompt, world);
        main.decisions.Render(response.Prompt, world);
    }

    private IReadOnlyList<EventPresentation> UpdateEvents(
        EngineResponse response,
        WorldDescriptor world,
        Outcome previousOutcome,
        IReadOnlySet<int> priorHistory,
        bool resetEvents,
        bool preserveEvents,
        string operation)
    {
        if (preserveEvents)
        {
            main.RenderEvents();
            return [];
        }
        EventBatchPresentation presented = EventCuePlanner.Plan(response.Events, world, previousOutcome);
        if (resetEvents)
        {
            main.events.Reset(presented.History);
        }
        else
        {
            main.events.Append(presented.History);
        }
        main.RenderEvents();
        HistoryEntryDescriptor[] completedActions = CompletedActions(response, priorHistory, operation);
        main.RenderLastResult(Highlights(response, presented, completedActions), resetEvents);
        main.PresentEvents(presented.Cues);
        return ReportNarrative(response, presented, completedActions);
    }

    private static HistoryEntryDescriptor[] CompletedActions(
        EngineResponse response, IReadOnlySet<int> priorHistory, string operation) =>
        operation == EngineProtocol.Resolve
            ? response.History?.Entries
                .Where(entry => !priorHistory.Contains(entry.Cursor))
                .ToArray() ?? []
            : [];

    private static IReadOnlyList<EventPresentation> Highlights(
        EngineResponse response,
        EventBatchPresentation presented,
        HistoryEntryDescriptor[] completedActions)
    {
        if (response.History?.ActionOpen == true)
        {
            return [];
        }
        HistoryEntryDescriptor? completed = completedActions.LastOrDefault(entry =>
            entry.Summary.Contains(" played ", StringComparison.Ordinal));
        return completed is null
            ? presented.Highlights
            : PresentAction(completed.Details.Prepend(completed.Summary));
    }

    private static IReadOnlyList<EventPresentation> ReportNarrative(
        EngineResponse response,
        EventBatchPresentation presented,
        HistoryEntryDescriptor[] completedActions)
    {
        if (response.History?.ActionOpen == true)
        {
            return [];
        }
        return completedActions.Length == 0
            ? presented.History
            : PresentAction(completedActions.SelectMany(entry =>
                entry.Details.Prepend(entry.Summary)));
    }

    private static EventPresentation[] PresentAction(IEnumerable<string> summaries) =>
        summaries.Select(summary => new EventPresentation(
            summary, "Action", [], EventMotionKind.State)).ToArray();

    private void FinishRender(
        EngineResponse response,
        WorldDescriptor world,
        GameProgressPresentation? priorProgress,
        string operation,
        IReadOnlyList<EventPresentation> reportNarrative)
    {
        main.transcript.RecordResponse(operation, response, reportNarrative);
        // A synchronized snapshot is authoritative but is not a new
        // transition, so it does not alter the diagnostic chronology.
        main.ApplyProgress(GameProgressPresentation.FromSynchronization(
            response,
            priorProgress ?? main.currentProgress));
        main.pageScroll.ScrollVertical = 0;
        main.pageScroll.SetDeferred("scroll_vertical", 0);
        main.RefreshSynchronizeAvailability();
        if (response.Prompt is null && world.Outcome != Outcome.Unfinished)
        {
            main.CallDeferred(Main.MethodName.RevealOutcome);
        }
    }

    internal void RenderBoard(WorldDescriptor world)
    {
        main.boardPresentation = BoardPresentation.From(world);
        main.boardRender = BoardRenderer.Render(
            main.boardAreas,
            main.boardPresentation,
            main.handRail,
            main.handHeading,
            main.interfaceScale,
            main.expandedAreas,
            main.art);
        main.boardRender.CardActivated += (card, control) => ToggleCardInspector(card, control);
        HideCardInspector();
    }

    internal void PreviewHandCard(int? id)
    {
        if (main.cardInspectorPinned)
        {
            return;
        }

        if (id is null)
        {
            HideCardInspector();
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
            HideCardInspector();
            return;
        }

        ShowCardInspector(card, source, pinned: true);
    }

    internal void ShowCardInspector(
        BoardCardPresentation card, Control? source, bool pinned)
    {
        main.cardInspectorGeneration++;
        main.inspectedCardId = card.TargetId;
        Control? priorFocus = main.GetViewport()?.GuiGetFocusOwner();
        ClearInspectorContent();
        InterfaceScale inspectionScale = FittedInspectionScale(
            card, main.interfaceScale, main.Size.Y);
        CardControl detail = CardControl.Create(
            card, CardDisplaySize.Full, inspectionScale, main.art);
        detail.FocusMode = Control.FocusModeEnum.All;
        IgnoreMouseRecursively(detail);
        main.cardInspectorContent.AddChild(detail);
        ConfigureInspectorFrame();
        PositionInspector(card, source, pinned);
        ShowInspector(detail, priorFocus, pinned);
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
        Rect2 sourceRect = source?.GetGlobalRect() ?? new Rect2(
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

    private void ShowInspector(Control detail, Control? priorFocus, bool pinned)
    {
        main.cardInspectorPinned = pinned;
        main.cardInspector.MouseFilter = pinned
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;
        main.cardInspectorBackdrop.Visible = pinned;
        main.cardInspectorClose.Visible = false;
        main.cardInspector.Visible = true;
        if (pinned)
        {
            main.cardInspectorReturnFocus = priorFocus;
            Callable.From(detail.GrabFocus).CallDeferred();
        }
        else if (priorFocus is not null && !main.cardInspector.IsAncestorOf(priorFocus))
        {
            Callable.From(priorFocus.GrabFocus).CallDeferred();
        }
    }

    internal void Input(InputEvent @event)
    {
        if (IsInspectorTab(@event))
        {
            if (main.cardInspectorContent.GetChildCount() > 0
                && main.cardInspectorContent.GetChild(0) is Control detail)
            {
                detail.GrabFocus();
            }
            main.GetViewport().SetInputAsHandled();
            return;
        }

        if (IsInspectorCancel(@event))
        {
            HideCardInspector();
            main.GetViewport().SetInputAsHandled();
            return;
        }

        if (IsOutsideInspectorClick(@event))
        {
            HideCardInspector();
            main.GetViewport().SetInputAsHandled();
        }
    }

    private bool IsInspectorTab(InputEvent @event) =>
        main.cardInspector.Visible
        && main.cardInspectorPinned
        && @event is InputEventKey { Keycode: Key.Tab, Pressed: true };

    private bool IsInspectorCancel(InputEvent @event) =>
        main.cardInspector.Visible && @event.IsActionPressed("ui_cancel");

    private bool IsOutsideInspectorClick(InputEvent @event) =>
        main.cardInspector.Visible
        && main.cardInspectorPinned
        && @event is InputEventMouseButton
        {
            ButtonIndex: MouseButton.Left,
            Pressed: true,
        } click
        && !main.cardInspectorFrame.GetGlobalRect().HasPoint(click.Position);

    internal static InterfaceScale FittedInspectionScale(
        BoardCardPresentation card,
        InterfaceScale requested,
        float viewportHeight)
    {
        bool landscape = VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme;
        int baseHeight = landscape ? 400 : 560;
        int availablePercent = (int)MathF.Floor(
            Math.Max(1, viewportHeight - 48) * 100 / baseHeight / 10) * 10;
        int fittedPercent = Math.Clamp(
            Math.Min((int)requested, availablePercent),
            (int)InterfaceScale.Percent50,
            (int)InterfaceScale.Percent150);
        return (InterfaceScale)fittedPercent;
    }

    internal static bool IsInsideCard(Node? node)
    {
        for (Node? current = node; current is not null; current = current.GetParent())
        {
            if (current is CardControl)
            {
                return true;
            }
        }

        return false;
    }

    internal void ScheduleCardInspectorHide()
    {
        int generation = ++main.cardInspectorGeneration;
        main.GetTree().CreateTimer(0.3).Timeout += () =>
        {
            if (generation == main.cardInspectorGeneration
                && !main.cardInspectorPinned
                && !main.cardInspectorHovered
                && !CardInspectorHasFocus())
            {
                main.cardInspectorFrame.FocusMode = Control.FocusModeEnum.None;
                main.cardInspectorScroll.FocusMode = Control.FocusModeEnum.None;
                main.inspectedCardId = null;
                main.cardInspector.Visible = false;
            }
        };
    }

    internal void BindCardInspectorFocus(Control control)
    {
        control.FocusEntered += () => main.cardInspectorGeneration++;
        control.FocusExited += ScheduleCardInspectorHide;
    }

    internal bool CardInspectorHasFocus()
    {
        Control? focused = main.GetViewport()?.GuiGetFocusOwner();
        return focused is not null
            && (focused == main.cardInspectorFrame || main.cardInspectorFrame.IsAncestorOf(focused));
    }

    internal void HideCardInspector()
    {
        Control? returnFocus = main.cardInspectorPinned ? main.cardInspectorReturnFocus : null;
        main.cardInspectorGeneration++;
        main.cardInspectorPinned = false;
        main.cardInspectorHovered = false;
        main.cardInspectorFrame.FocusMode = Control.FocusModeEnum.None;
        main.cardInspectorScroll.FocusMode = Control.FocusModeEnum.None;
        main.inspectedCardId = null;
        main.cardInspectorReturnFocus = null;
        main.cardInspector.Visible = false;
        if (returnFocus is not null
            && GodotObject.IsInstanceValid(returnFocus)
            && returnFocus.IsInsideTree()
            && !returnFocus.IsQueuedForDeletion())
        {
            Callable.From(returnFocus.GrabFocus).CallDeferred();
        }
    }

    internal static void IgnoreMouseRecursively(Node node)
    {
        if (node is RichTextLabel rules)
        {
            rules.MouseFilter = Control.MouseFilterEnum.Stop;
            return;
        }
        if (node is Control control)
        {
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        foreach (Node child in node.GetChildren())
        {
            IgnoreMouseRecursively(child);
        }
    }
}
