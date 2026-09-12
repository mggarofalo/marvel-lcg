using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

using static Marvel.Session.SessionReplay;

internal static class SessionReplayVerification
{

    internal static SessionReplay.ReplayResult Replay(
        SessionSave save,
        int unitCount,
        Func<SessionSetup, ReplayOpenedGame> open,
        bool requireExposures,
        List<HistoryUnitInspection>? history = null)
    {
        ReplayOpenedGame opened = open(save.Setup);
        Game game = opened.Game;
        RequireInitialReplay(save.Initial, opened, game);
        var derived = new List<IReadOnlyList<InformationExposure>>(unitCount);
        for (int unitIndex = 0; unitIndex < unitCount; unitIndex++)
        {
            derived.Add(ReplayUnit(
                save.Units[unitIndex], unitIndex, game, requireExposures, history));
        }

        RequireReplayFrontier(save, unitCount, requireExposures);
        return new SessionReplay.ReplayResult(game, derived);
    }

    internal static void RequireInitialReplay(
        InitialRecord initial,
        ReplayOpenedGame opened,
        Game game)
    {
        JournalReplay.RequireEvents(initial.Events, opened.SetupEvents, "initial events");
        JournalReplay.RequireRng(
            initial.RngWords,
            game.State.Random.Generator.WordsConsumed,
            "initial RNG");
        JournalReplay.RequireFingerprint(
            initial.StateDigest,
            game.State.Digest().Canonical(),
            "initial state");
    }

    internal static IReadOnlyList<InformationExposure> ReplayUnit(
        JournalUnit unit,
        int unitIndex,
        Game game,
        bool requireExposures,
        List<HistoryUnitInspection>? history)
    {
        RequireUnitPosition(unit, unitIndex, game);
        ReplayHistory? inspection = CreateReplayHistory(history, unit);
        IReadOnlyList<InformationExposure> exposures = [];
        string? derivedRole = null;
        for (int decisionIndex = 0; decisionIndex < unit.Decisions.Count; decisionIndex++)
        {
            ReplayUnitDecision(
                unit,
                unitIndex,
                decisionIndex,
                game,
                inspection,
                ref derivedRole,
                ref exposures);
        }

        RequireUnitBoundary(unit, unitIndex, game, derivedRole);
        AppendReplayHistory(inspection, history, unit, unitIndex);
        RequireReplayExposures(requireExposures, unit, unitIndex, exposures);
        return exposures;
    }

    internal static ReplayHistory? CreateReplayHistory(
        List<HistoryUnitInspection>? history,
        JournalUnit unit) =>
        history is not null && unit.Status == "complete" ? new ReplayHistory() : null;

    internal static void ReplayUnitDecision(
        JournalUnit unit,
        int unitIndex,
        int decisionIndex,
        Game game,
        ReplayHistory? inspection,
        ref string? derivedRole,
        ref IReadOnlyList<InformationExposure> exposures)
    {
        RequireReplayContinuation(game, unitIndex, decisionIndex);
        JournalStep step = unit.Decisions[decisionIndex];
        Prompt prompt = game.Pending ?? throw new ReplayDivergenceException(
            $"unit {unitIndex} decision {decisionIndex} has no prompt");
        string context = $"unit {unitIndex} decision {decisionIndex}";
        JournalReplay.RequirePrompt(step.Prompt, prompt, $"{context} prompt");
        Decision decision = step.Decision.Resolve(prompt);
        ObserveReplayRoot(inspection, unit, step, decisionIndex, game, prompt, decision);
        ObserveReplayResources(inspection, step, decision, game);
        DeriveReplayRole(unit, step, unitIndex, decisionIndex, game, prompt, decision,
            ref derivedRole);
        SessionReplay.ReplayDecision resolved = ResolveReplayDecision(
            step, decision, context, game, exposures);
        exposures = resolved.Exposures;
        ObserveReplayEvents(inspection, resolved.Events);
    }

    internal static void ObserveReplayRoot(
        ReplayHistory? inspection,
        JournalUnit unit,
        JournalStep step,
        int decisionIndex,
        Game game,
        Prompt prompt,
        Decision decision)
    {
        if (decisionIndex == 0)
        {
            inspection?.ObserveRoot(unit, step, game, prompt, decision);
        }
    }

    internal static void DeriveReplayRole(
        JournalUnit unit,
        JournalStep step,
        int unitIndex,
        int decisionIndex,
        Game game,
        Prompt prompt,
        Decision decision,
        ref string? derivedRole)
    {
        if (decisionIndex != 0)
        {
            return;
        }

        RequireRootMetadata(unit, step, unitIndex);
        derivedRole = UnitRole(game, prompt, decision);
    }

    internal static void ObserveReplayResources(
        ReplayHistory? inspection,
        JournalStep step,
        Decision decision,
        Game game) =>
        inspection?.ObserveResources(step, decision, game);

    internal static void ObserveReplayEvents(
        ReplayHistory? inspection,
        IReadOnlyList<GameEvent> events) =>
        inspection?.ObserveEvents(events);

    internal static void AppendReplayHistory(
        ReplayHistory? inspection,
        List<HistoryUnitInspection>? history,
        JournalUnit unit,
        int unitIndex)
    {
        if (inspection is not null)
        {
            inspection.Append(history!, unit, unitIndex);
        }
    }

    internal static void RequireReplayExposures(
        bool requireExposures,
        JournalUnit unit,
        int unitIndex,
        IReadOnlyList<InformationExposure> exposures)
    {
        if (requireExposures)
        {
            RequireExposures(unit.Exposures, exposures, $"unit {unitIndex} exposure");
        }
    }

    internal static void RequireUnitPosition(JournalUnit unit, int unitIndex, Game game)
    {
        if (unit.Decisions is null or { Count: 0 }
            || unit.ActiveSeat != game.Active
            || unit.Round != game.Round
            || !string.Equals(unit.Phase, game.Phase.ToString(), StringComparison.Ordinal))
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} engine position diverged");
        }
    }

    internal static void RequireReplayContinuation(Game game, int unitIndex, int decisionIndex)
    {
        if (decisionIndex > 0 && (game.Pending is null || game.IsRootPrompt))
        {
            throw new ReplayDivergenceException($"unit {unitIndex} crossed a root boundary");
        }
    }

    internal static void RequireRootMetadata(JournalUnit unit, JournalStep step, int unitIndex)
    {
        if (unit.InitiatingSeat != step.Decision.Actor)
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} root metadata diverged");
        }
    }

    internal static SessionReplay.ReplayDecision ResolveReplayDecision(
        JournalStep step,
        Decision decision,
        string context,
        Game game,
        IReadOnlyList<InformationExposure> exposures)
    {
        long rngBefore = game.State.Random.Generator.WordsConsumed;
        var resolved = game.Resolve(decision);
        JournalReplay.RequireEvents(step.Events, resolved.Events, $"{context} events");
        JournalReplay.RequireRng(
            step.RngWords,
            game.State.Random.Generator.WordsConsumed,
            $"{context} RNG");
        JournalReplay.RequireFingerprint(
            step.StateFingerprint, Fingerprint(game), $"{context} state");
        if (!Equals(step.Result, Result(game)))
        {
            throw new ReplayDivergenceException($"{context} result diverged");
        }

        IReadOnlyList<InformationExposure> merged = InformationFrontier.Merge(
            exposures,
            InformationFrontier.Classify(
                game.State.Players,
                rngBefore,
                game.State.Random.Generator.WordsConsumed,
                resolved.Information,
                resolved.Events,
                game.Pending));
        return new SessionReplay.ReplayDecision(resolved.Events, merged);
    }

    internal static void RequireUnitBoundary(
        JournalUnit unit,
        int unitIndex,
        Game game,
        string? derivedRole)
    {
        bool reachedBoundary = game.Pending is null || game.IsRootPrompt;
        if ((unit.Status == "complete") != reachedBoundary)
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} completion status diverged");
        }

        string expectedRole = ReplayedRole(game, derivedRole, unitIndex);
        if (!string.Equals(unit.Role, expectedRole, StringComparison.Ordinal))
        {
            throw new ReplayDivergenceException($"unit {unitIndex} role diverged");
        }
    }

    internal static string ReplayedRole(Game game, string? role, int unitIndex) =>
        game.Pending is null
            ? "terminal"
            : role ?? throw new ReplayDivergenceException(
                $"unit {unitIndex} has no root decision");

    internal static void RequireReplayFrontier(
        SessionSave save,
        int unitCount,
        bool requireExposures)
    {
        if (!requireExposures || unitCount != save.Units.Count)
        {
            return;
        }

        int frontier = save.Units
            .Select((unit, index) => unit.Exposures.Count > 0 ? index + 1 : 0)
            .DefaultIfEmpty(0)
            .Max();
        if (frontier != save.EditFrontier)
        {
            throw new ReplayDivergenceException("information frontier diverged");
        }
    }
}
