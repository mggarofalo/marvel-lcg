using Godot;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Reserves prior table slots while receipt-driven consequences settle before the next decision.</summary>
internal sealed class BoardResponseTransition(Main main) : IDisposable
{
    private readonly PendingResponsePresentation pending = new();
    private EngineResponse? next;
    private bool subscribed;
    internal bool Pending => pending.Pending;

    internal bool TryBegin(EngineResponse response, EngineResponse? previous,
        BoardResponsePresentation.Options options, Action apply)
    {
        if (!Eligible(response, previous, options)) return false;
        EventBatchPresentation batch = EventCuePlanner.Plan(response.Events, response.World!, previous!.World!.Outcome);
        EventPresentation[] cues = BoardResponsePresentation.Cues(batch, options.Receipt);
        if (cues.Length == 0) return false;
        if (!subscribed)
        {
            main.eventController.PlaybackSettled += Settled;
            subscribed = true;
        }
        main.decisions.ClearDecision();
        main.decisions.BindMulliganTargets(main.boardRender);
        pending.Begin(() =>
        {
            next = null;
            main.boardRender?.ClearPendingChoices();
            main.SetMeta("response_staged", false);
            apply();
        });
        next = response;
        main.SetMeta("response_staged", true);
        main.SetMeta("staged_revision", response.Revision);
        if (main.boardRender is { } board) board.IsCurrent = static () => false;
        main.boardController.HideCardInspector();
        main.eventController.RecordTimeline(response, AcceptedActivityOccurrence.From(cues, options.Receipt), false);
        main.eventController.FitTimeline();
        IReadOnlyList<EventPresentation> playback = EventPlaybackPlan.From(
            AcceptedActivityOccurrence.From(cues, options.Receipt), options.Receipt, previous.World);
        main.eventController.PresentAttack(AttackActivityPresentation.DuringTransition(
            previous.World.ActiveAttack, response.World!.ActiveAttack, playback));
        main.PresentEvents(playback);
        main.status.Text = "RESOLVING";
        PresentNextDecision(main.boardRender);
        return true;
    }

    internal void PresentNextDecision(BoardRenderResult? board)
    {
        if (next is not { Prompt: { } prompt, World: { } world } response) return;
        int generation = pending.Generation;
        string heading = PromptPresentation.From(prompt, world).Heading;
        board?.PresentPendingChoices(heading, _ => OpenNextDecision(response, generation));
    }

    private void OpenNextDecision(EngineResponse response, int generation)
    {
        if (!CanOpenNextDecision(response, generation)) return;
        // This control belongs to the accepted prompt. Old card gestures are never forwarded.
        main.eventController.CompleteEventPresentation();
        if (!pending.Complete(generation) || main.decisions.composer?.Prompt != response.Prompt) return;
        if (main.decisions.AbilityWindowOpen || main.decisions.AcknowledgementPending) return;
        Control source = main.boardAreas.FindChild("CompleteChoiceSheet", true, false) as Control ?? main.decisions;
        main.decisions.ShowCompleteChoices(source);
    }

    private bool CanOpenNextDecision(EngineResponse response, int generation) =>
        pending.Pending && generation == pending.Generation && ReferenceEquals(next, response)
        && main.CurrentGame is { } current && current.Revision == response.Revision && current.GameId == response.GameId;

    private bool Eligible(EngineResponse response, EngineResponse? previous, BoardResponsePresentation.Options options) =>
        main.eventMotion.ButtonPressed && options.Operation == EngineProtocol.Resolve
        && !options.ResetEvents && !options.PreserveEvents && response.Error is null
        && previous?.World is not null && previous.GameId == response.GameId
        && response.Revision > previous.Revision;

    private void Settled()
    {
        int generation = pending.Generation;
        Callable.From(() =>
        {
            if (main.IsInsideTree()) pending.Complete(generation);
        }).CallDeferred();
    }

    internal void Cancel()
    {
        pending.Cancel();
        next = null;
        main.boardRender?.ClearPendingChoices();
        main.SetMeta("response_staged", false);
    }

    public void Dispose()
    {
        Cancel();
        if (subscribed) main.eventController.PlaybackSettled -= Settled;
    }
}
