using Marvel.Server;
using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Projects one accepted response into chronology, cues, and last-action evidence.</summary>
internal static class BoardResponsePresentation
{
    internal sealed record Options(bool ResetEvents, bool PreserveEvents, string Operation,
        DecisionReceiptContext? Receipt = null);

    internal static IReadOnlyList<EventPresentation> Update(
        Main main, EngineResponse response, Outcome previousOutcome,
        IReadOnlySet<int> priorHistory, Options options)
    {
        if (options.PreserveEvents)
        {
            main.eventController.RecordTimeline(response, [], reset: true);
            main.SkipEventPresentation();
            main.RenderEvents();
            return [];
        }
        WorldDescriptor world = response.World!;
        EventBatchPresentation presented = EventCuePlanner.Plan(response.Events, world, previousOutcome);
        string? intention = options.Receipt?.ActionVerb is null or Game.EndPhaseVerb
            ? null : options.Receipt.Commitment;
        EventPresentation[] cues = [.. presented.Cues.Select(entry => entry with { Intention = intention })];
        main.eventController.RecordTimeline(response, cues, options.ResetEvents);
        if (options.ResetEvents) main.events.Reset(presented.History);
        else main.events.Append(presented.History);
        main.RenderEvents();
        HistoryEntryDescriptor[] completed = Completed(response, priorHistory, options.Operation);
        main.RenderLastResult(Highlights(response, presented, options.Receipt), options.ResetEvents);
        main.PresentEvents(options.Operation == EngineProtocol.Resolve && !options.ResetEvents
            ? cues : []);
        return Narrative(response, presented, completed);
    }

    private static HistoryEntryDescriptor[] Completed(
        EngineResponse response, IReadOnlySet<int> priorHistory, string operation) =>
        operation == EngineProtocol.Resolve
            ? response.History?.Entries.Where(entry => !priorHistory.Contains(entry.Cursor)).ToArray() ?? [] : [];

    internal static IReadOnlyList<EventPresentation> Highlights(
        EngineResponse response, EventBatchPresentation presented, DecisionReceiptContext? accepted = null) =>
        ResponseReceiptPresenter.Present(response.Events, response.World!, presented, accepted);

    private static IReadOnlyList<EventPresentation> Narrative(
        EngineResponse response, EventBatchPresentation presented, HistoryEntryDescriptor[] completed) =>
        response.History?.ActionOpen == true ? [] : completed.Length == 0
            ? presented.History : Present(completed.SelectMany(entry => entry.Details.Prepend(entry.Summary)));

    private static EventPresentation[] Present(IEnumerable<string> summaries) => summaries.Select(summary =>
        new EventPresentation(summary, "Action", [], EventMotionKind.State)).ToArray();
}
