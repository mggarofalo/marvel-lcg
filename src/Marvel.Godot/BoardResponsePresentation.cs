using Marvel.Server;
using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Projects one accepted response into chronology, cues, and last-action evidence.</summary>
internal static class BoardResponsePresentation
{
    internal static IReadOnlyList<EventPresentation> Update(
        Main main, EngineResponse response, Outcome previousOutcome,
        IReadOnlySet<int> priorHistory, bool resetEvents, bool preserveEvents, string operation)
    {
        if (preserveEvents)
        {
            main.RenderEvents();
            return [];
        }
        WorldDescriptor world = response.World!;
        EventBatchPresentation presented = EventCuePlanner.Plan(response.Events, world, previousOutcome);
        if (resetEvents) main.events.Reset(presented.History);
        else main.events.Append(presented.History);
        main.RenderEvents();
        HistoryEntryDescriptor[] completed = Completed(response, priorHistory, operation);
        main.RenderLastResult(Highlights(response, presented, completed), resetEvents);
        main.PresentEvents(presented.Cues);
        return Narrative(response, presented, completed);
    }

    private static HistoryEntryDescriptor[] Completed(
        EngineResponse response, IReadOnlySet<int> priorHistory, string operation) =>
        operation == EngineProtocol.Resolve
            ? response.History?.Entries.Where(entry => !priorHistory.Contains(entry.Cursor)).ToArray() ?? [] : [];

    private static IReadOnlyList<EventPresentation> Highlights(
        EngineResponse response, EventBatchPresentation presented, HistoryEntryDescriptor[] completed)
    {
        if (response.History?.ActionOpen == true) return [];
        HistoryEntryDescriptor? action = completed.LastOrDefault(entry => entry.Summary.Contains(" played ", StringComparison.Ordinal));
        return action is null ? presented.Highlights : Present(action.Details.Prepend(action.Summary));
    }

    private static IReadOnlyList<EventPresentation> Narrative(
        EngineResponse response, EventBatchPresentation presented, HistoryEntryDescriptor[] completed) =>
        response.History?.ActionOpen == true ? [] : completed.Length == 0
            ? presented.History : Present(completed.SelectMany(entry => entry.Details.Prepend(entry.Summary)));

    private static EventPresentation[] Present(IEnumerable<string> summaries) => summaries.Select(summary =>
        new EventPresentation(summary, "Action", [], EventMotionKind.State)).ToArray();
}
