using Marvel.Protocol;
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

        Decision decision = request.Decision;
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
        actor = selected is null ? pending.Player : DecisionAssessment.ActingSeat(pending, selected);
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

    private static bool Legal(int actor, Prompt pending, Decision decision) =>
        DecisionAssessment.Assess(pending, decision, actor).IsAccepted;

    private static EngineResponse CommitResolution(
        EngineHost host, EngineRequest request, RequestExecution execution,
        SessionAccess access, Prompt pending, Decision decision, int actor)
    {
        try { return BuildAndCommit(host, request, execution, access, pending, decision, actor); }
        catch (Exception failure)
        {
            if (!PersistenceFailure(failure))
                return Failed(request, "candidate_failed",
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
            ReplayCheckpoint? checkpoint = access.Session.TakeCheckpoint();
            CandidateReplay replay = execution.ObserveReplay(() => SessionReplay.BeginCandidate(
                access.Session.Save, host.compatibility, host.ReplayOpen, checkpoint));
            Game candidate = replay.Game;
            Prompt candidatePrompt = candidate.Pending
                ?? throw new ReplayDivergenceException("candidate has no pending prompt");
            JournalReplay.RequirePrompt(
                PromptRecord.From(pending), candidatePrompt, "live prompt");
            Decision replayDecision = DurableDecision.From(actor, pending, decision)
                .Resolve(candidatePrompt);
            // Ledger choice: an initial setup sequence opens its own history unit.
            // Later dependent decisions must still extend an existing open unit.
            bool root = candidate.IsRootPrompt || access.Session.Save.Cursor == 0;
            int active = candidate.Active;
            int round = candidate.Round;
            string phase = candidate.Phase.ToString();
            string role = SessionReplay.UnitRole(candidate, candidatePrompt, replayDecision);
            long rngBefore = candidate.State.Random.Generator.WordsConsumed;
            var resolved = replay.Resolve(actor, replayDecision, root);
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
            return PublishVerified(host, request, execution, access, replay, proposed, resolved);
    }

    /// <summary>
    /// The live game advanced through the committed decision, or a fresh game
    /// that verifies the whole committed save when the live game cannot
    /// reproduce it.
    /// </summary>
    /// <remarks>
    /// Presentation reads the live game, and a read can leave hidden state behind
    /// -- an area a lookup allocates is one -- that a fresh replay never makes.
    /// Such a live game may disagree with the checkpoint's derivation, which was
    /// never read; the save follows the derivation, and the replacement verifies
    /// it from setup.
    /// </remarks>
    private static Game AdvancePublished(
        EngineHost host, Game live, CandidateReplay replay, SessionSave proposed)
    {
        try
        {
            replay.VerifyCommitted(live, proposed);
            return live;
        }
        catch (ReplayDivergenceException)
        {
            return SessionReplay.Verify(proposed, host.compatibility, host.ReplayOpen);
        }
    }

    /// <summary>
    /// Publishes the live game advanced through the new decision and keeps the
    /// candidate private as the next decision's checkpoint.
    /// </summary>
    /// <remarks>
    /// Ledger choice: an independent game must reproduce the candidate's decision
    /// before the response is projected or the save committed. Advancing the live
    /// game changes it, so any failure from here on restores the live game by
    /// verifying the live save before the failure is reported.
    /// </remarks>
    private static EngineResponse PublishVerified(
        EngineHost host, EngineRequest request, RequestExecution execution,
        SessionAccess access, CandidateReplay replay, SessionSave proposed, Resolution resolved)
    {
        SessionSave live = access.Session.Save;
        try
        {
            Game published = AdvancePublished(host, access.Session.Game, replay, proposed);
            var history = replay.History(proposed);
            var transaction = new SessionTransaction(published, proposed, history)
            {
                Checkpoint = replay.Checkpoint(proposed),
            };
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
                    history: AuthorizedSessionProjector.History(committed, access.Scope, verified, history)));
        }
        catch
        {
            access.Session.Recover(SessionReplay.Verify(live, host.compatibility, host.ReplayOpen));
            throw;
        }
    }

    private static bool PersistenceFailure(Exception failure) => failure is IOException
        or UnauthorizedAccessException or SessionSaveException or ReplayDivergenceException;

}
