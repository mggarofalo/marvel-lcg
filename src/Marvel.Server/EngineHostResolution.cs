using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.View;
using System.Diagnostics;
using System.Security.Cryptography;

using static Marvel.Server.EngineHostResolution;
using static Marvel.Server.EngineHostHistory;
using static Marvel.Server.EngineHostLifecycle;

namespace Marvel.Server;

/// <summary>Owns resolution operations for an engine host.</summary>
internal static class EngineHostResolution
{
    internal static EngineResponse Resolve(this EngineHost host, EngineRequest request, RequestExecution execution)
    {
        if (!ValidResolveRequest(request))
        {
            return Failed(
                request, "invalid_request",
                "resolve requires decision and does not accept game");
        }

        if (!host.authority.TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        if (request.Decision!.Targets is null)
        {
            return Failed(request, "invalid_request", "decision.targets is required");
        }

        if (request.ExpectedRevision != access.Session.Revision)
        {
            return Failed(
                request,
                "stale_decision",
                "the decision was composed for an earlier table revision");
        }

        if (access.Session.Game.Pending is not { } pending)
        {
            return Failed(request, "not_your_turn", "this capability cannot answer the pending prompt");
        }

        Decision decision = request.Decision.ToDomain();
        if (!TryActor(request, pending, decision, access,
                out int actor, out EngineResponse? rejected))
            return rejected!;
        if (!Legal(actor, pending, decision))
            return Failed(request, "invalid_decision", "the decision is not legal for that seat");
        return CommitResolution(host, request, execution, access, pending, decision, actor);
    }

    private static bool ValidResolveRequest(EngineRequest request) =>
        request.Decision is not null && request.Game is null && request.Viewer is null
        && request.ExpectedRevision is >= 0 && request.Cursor is null && request.Order is null;

    private static bool TryActor(
        EngineRequest request, Prompt pending, Decision decision, SessionAccess access,
        out int actor, out EngineResponse? rejected)
    {
        Affordance? selected = decision.IsDecline ? null
            : pending.Affordances.SingleOrDefault(option => option.Id == decision.Affordance);
        actor = selected is not null && selected.Verb == Game.ActionVerb
            ? selected.AnchorPlayer : pending.Player;
        if (!decision.IsDecline && selected is null)
            rejected = Failed(request, "invalid_decision", "the selected affordance is not pending");
        else if (!access.Scope.Includes(actor))
            rejected = Failed(request, "not_your_turn", "this capability cannot submit that decision");
        else if (!AvailableTo(access.Session.Game, actor, decision))
            rejected = Failed(request, "invalid_decision", "the decision is not available to that seat");
        else rejected = null;
        return rejected is null;
    }

    private static bool AvailableTo(Game game, int actor, Decision decision)
    {
        Prompt? authorized = game.PromptFor(actor);
        return authorized is not null && (decision.IsDecline
            || authorized.Affordances.Any(option => option.Id == decision.Affordance));
    }

    private static bool Legal(int actor, Prompt pending, Decision decision)
    {
        try
        {
            _ = DurableDecision.From(actor, pending, decision).Resolve(pending);
            return true;
        }
        catch (Exception failure) when (failure is InvalidOperationException
            or ReplayDivergenceException) { return false; }
    }

    private static EngineResponse CommitResolution(
        EngineHost host, EngineRequest request, RequestExecution execution,
        SessionAccess access, Prompt pending, Decision decision, int actor)
    {
        try { return BuildAndCommit(host, request, execution, access, pending, decision, actor); }
        catch (Exception failure)
        {
            if (!PersistenceFailure(failure))
                return Failed(request, "game_aborted",
                    "the candidate decision failed and the prior game remains authoritative");
            if (failure is ReplayDivergenceException) execution.MarkReplayDiverged();
            return Failed(request, "save_failed",
                "the decision was not committed and the prior game remains authoritative");
        }
    }

    private static EngineResponse BuildAndCommit(
        EngineHost host, EngineRequest request, RequestExecution execution,
        SessionAccess access, Prompt pending, Decision decision, int actor)
    {
            Game candidate = execution.ObserveReplay(() => SessionReplay.Verify(
                access.Session.Save, host.compatibility, host.ReplayOpen));
            Prompt candidatePrompt = candidate.Pending
                ?? throw new ReplayDivergenceException("candidate has no pending prompt");
            JournalReplay.RequirePrompt(
                PromptRecord.From(pending), candidatePrompt, "live prompt");
            Decision replayDecision = DurableDecision.From(actor, pending, decision)
                .Resolve(candidatePrompt);
            bool root = candidate.IsRootPrompt;
            int active = candidate.Active;
            int round = candidate.Round;
            string phase = candidate.Phase.ToString();
            string role = SessionReplay.UnitRole(candidate, candidatePrompt, replayDecision);
            long rngBefore = candidate.State.Random.Generator.WordsConsumed;
            var resolved = candidate.Resolve(replayDecision);
            IReadOnlyList<InformationExposure> exposures = InformationFrontier.Classify(
                candidate.State.Players,
                rngBefore,
                candidate.State.Random.Generator.WordsConsumed,
                resolved.Information,
                resolved.Events,
                candidate.Pending);
            var step = JournalStep.From(
                actor,
                candidatePrompt,
                replayDecision,
                resolved.Events,
                candidate.State.Random.Generator.WordsConsumed,
                SessionReplay.Fingerprint(candidate),
                SessionReplay.Result(candidate));
            SessionSave proposed = host.Stamp(Append(
                access.Session.Save,
                step,
                root,
                candidate.IsRootPrompt || candidate.Pending is null,
                role,
                actor,
                active,
                round,
                phase,
                candidate.Pending,
                exposures));
            var transaction = new SessionTransaction(candidate, proposed);
            return transaction.CommitAndPublish(
                access.Session,
                host.authority.Snapshot(access.Session),
                stored => execution.ObservePersistence(() => host.store.Commit(stored)),
                (verified, committed) => AuthorizedSessionProjector.Succeeded(
                    request,
                    verified,
                    resolved.Prompt,
                    resolved.Events,
                    access.Scope,
                    revision: committed.Revision,
                    history: host.projector.History(committed, access.Scope, verified)));
    }

    private static bool PersistenceFailure(Exception failure) => failure is IOException
        or UnauthorizedAccessException or SessionSaveException or ReplayDivergenceException;

}
