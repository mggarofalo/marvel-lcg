using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>Builds visibility-safe responses and retained-history descriptions.</summary>
internal sealed class AuthorizedSessionProjector(
    SessionCompatibility compatibility,
    Func<SessionSetup, ReplayOpenedGame> replayOpen)
{
    public static EngineResponse Succeeded(
        EngineRequest request,
        Game? game = null,
        Prompt? prompt = null,
        IReadOnlyList<GameEvent>? events = null,
        ViewScope? scope = null,
        string? capability = null,
        IReadOnlyList<SeatInvitation>? invitations = null,
        long revision = 0,
        HistoryDescriptor? history = null)
    {
        IReadOnlyList<GameEvent> safeEvents = events ?? [];
        if (game is null || scope is null)
        {
            return new EngineResponse(
                EngineProtocol.Version, request.RequestId, request.GameId,
                capability,
                Prompt: null,
                Events: [],
                Invitations: invitations,
                Revision: revision,
                History: history);
        }

        Prompt? scopedPrompt = scope.SoleSeat is int seat
            ? game.PromptFor(seat)
            : prompt;
        VisibleResult visible = WorldProjection.For(
            game.State, scopedPrompt, safeEvents, scope);
        return new EngineResponse(
            EngineProtocol.Version, request.RequestId, request.GameId,
            capability,
            visible.Prompt,
            visible.Events,
            visible.World,
            Invitations: invitations,
            Revision: revision,
            History: history);
    }

    public HistoryDescriptor History(SessionSave save, ViewScope scope, Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        WorldDescriptor world = WorldProjection.For(game.State, null, [], scope).World;
        var entries = new List<HistoryEntryDescriptor>(save.Cursor);
        foreach (HistoryUnitInspection unit in SessionReplay.InspectActiveHistory(
                     save, compatibility, replayOpen))
        {
            Outcome? outcome = Enum.TryParse(unit.Outcome, out Outcome parsed)
                ? parsed
                : null;
            if (!scope.Includes(unit.Actor))
            {
                string summary = $"{unit.ActorName} completed an action.";
                if (outcome is { } terminal)
                {
                    summary += $" {EventPresenter.Terminal(terminal).Summary}";
                }
                entries.Add(new HistoryEntryDescriptor(unit.Cursor, summary, []));
                continue;
            }

            var facts = new ActionHistoryFacts(
                unit.Cursor,
                unit.ActorName,
                unit.Role,
                unit.Phase,
                unit.Verb,
                unit.Action,
                unit.Subject,
                unit.ResourceGeneratorIds,
                unit.ResourceGenerators,
                outcome);
            ActionHistoryPresentation presented = ActionHistoryPresenter.PresentEntry(
                facts, unit.Events, world);
            entries.Add(new HistoryEntryDescriptor(
                unit.Cursor, presented.Summary, presented.Details));
        }

        bool actionOpen = save.Units.Any(unit => unit.Status != "complete");
        if (actionOpen)
        {
            return new HistoryDescriptor(save.Cursor, [], [], entries, ActionOpen: true);
        }

        int[] undo = Enumerable.Range(save.EditFrontier, save.Cursor - save.EditFrontier)
            .Where(target => EditableBy(
                save.Units.Skip(target).Take(save.Cursor - target), scope))
            .ToArray();
        int[] redo = Enumerable.Range(save.Cursor + 1, save.Units.Count - save.Cursor)
            .Where(target => EditableBy(
                save.Units.Skip(save.Cursor).Take(target - save.Cursor), scope))
            .ToArray();
        return new HistoryDescriptor(save.Cursor, undo, redo, entries, ActionOpen: false);
    }

    public static bool EditableBy(IEnumerable<JournalUnit> units, ViewScope scope)
    {
        int[] actors = units
            .SelectMany(unit => unit.Decisions
                .Select(step => step.Decision.Actor)
                .Prepend(unit.InitiatingSeat))
            .Distinct()
            .ToArray();
        return actors.Length == 1 && scope.Includes(actors[0]);
    }
}
