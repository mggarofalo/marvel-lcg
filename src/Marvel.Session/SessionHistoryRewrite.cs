using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Rewrites verified session-history units in a requested order.</summary>
public static class SessionHistoryRewrite
{

    /// <summary>
    /// Rebuilds a complete active trace from durable inputs in a proposed order.
    /// </summary>
    /// <remarks>
    /// Reordering is a product operation. Derived prompts, events, RNG counts,
    /// fingerprints, results, positions and information signals are generated
    /// again; none are copied from the prior order.
    /// </remarks>
    public static RewrittenTrace Rewrite(
        SessionSave save,
        SessionCompatibility expected,
        Func<SessionSetup, ReplayOpenedGame> open,
        IReadOnlyList<int> sourceOrder)
    {
        _ = SessionReplay.Verify(save, expected, open);
        ArgumentNullException.ThrowIfNull(sourceOrder);
        RequireRewriteOrder(save, sourceOrder);

        Game game = SessionReplayVerification.Replay(save, 0, open, requireExposures: true).Game;
        var rewritten = new List<JournalUnit>(sourceOrder.Count);
        int frontier = 0;
        foreach (int sourceIndex in sourceOrder)
        {
            JournalUnit unit = RewriteUnit(save.Units[sourceIndex], sourceIndex, game);
            rewritten.Add(unit);
            if (unit.Exposures.Count > 0)
            {
                frontier = rewritten.Count;
            }
        }

        return new RewrittenTrace(game, rewritten, frontier);
    }

    internal static void RequireRewriteOrder(
        SessionSave save,
        IReadOnlyList<int> sourceOrder)
    {
        if (sourceOrder.Count != save.Cursor
            || !sourceOrder.Order().SequenceEqual(Enumerable.Range(0, save.Cursor)))
        {
            throw new SessionSaveException(
                "rewrite order is not a permutation of the active trace");
        }
    }

    internal static JournalUnit RewriteUnit(JournalUnit source, int sourceIndex, Game game)
    {
        if (source.Status != "complete")
        {
            throw new ReplayDivergenceException(
                $"unit {sourceIndex} is not complete for rewriting");
        }

        int active = game.Active;
        int round = game.Round;
        string phase = game.Phase.ToString();
        string? role = null;
        var steps = new List<JournalStep>(source.Decisions.Count);
        IReadOnlyList<InformationExposure> exposures = [];
        for (int decisionIndex = 0; decisionIndex < source.Decisions.Count; decisionIndex++)
        {
            RequireRewriteContinuation(game, sourceIndex, decisionIndex);
            SessionReplay.RewrittenDecision rewritten = RewriteDecision(
                source.Decisions[decisionIndex], sourceIndex, decisionIndex, game, exposures,
                deriveRole: role is null);
            role ??= rewritten.Role;
            exposures = rewritten.Exposures;
            steps.Add(rewritten.Step);
        }

        RequireRewriteBoundary(game, sourceIndex);
        return new JournalUnit(
            RewrittenRole(game, role, sourceIndex),
            "complete",
            source.Decisions[0].Decision.Actor,
            active,
            round,
            phase,
            steps,
            exposures);
    }

    internal static void RequireRewriteContinuation(Game game, int unitIndex, int decisionIndex)
    {
        if (decisionIndex > 0 && (game.Pending is null || game.IsRootPrompt))
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} reached a boundary before its dependent decisions ended");
        }
    }

    internal static SessionReplay.RewrittenDecision RewriteDecision(
        JournalStep input,
        int unitIndex,
        int decisionIndex,
        Game game,
        IReadOnlyList<InformationExposure> exposures,
        bool deriveRole)
    {
        Prompt prompt = game.Pending ?? throw new ReplayDivergenceException(
            $"unit {unitIndex} decision {decisionIndex} has no prompt");
        Decision decision = input.Decision.Resolve(prompt);
        string? role = deriveRole ? SessionReplay.UnitRole(game, prompt, decision) : null;
        long rngBefore = game.State.Random.Generator.WordsConsumed;
        var resolved = game.Resolve(decision);
        IReadOnlyList<InformationExposure> rewrittenExposures = InformationFrontier.Merge(
            exposures,
            InformationFrontier.Classify(
                game.State.Players,
                rngBefore,
                game.State.Random.Generator.WordsConsumed,
                resolved.Information,
                resolved.Events,
                game.Pending));
        JournalStep step = JournalStep.From(
            input.Decision.Actor,
            prompt,
            decision,
            resolved.Events,
            game.State.Random.Generator.WordsConsumed,
            SessionReplay.Fingerprint(game),
            SessionReplay.Result(game));
        return new SessionReplay.RewrittenDecision(role, step, rewrittenExposures);
    }

    internal static void RequireRewriteBoundary(Game game, int unitIndex)
    {
        if (game.Pending is not null && !game.IsRootPrompt)
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} did not reach its complete boundary");
        }
    }

    internal static string RewrittenRole(Game game, string? role, int unitIndex) =>
        game.Pending is null
            ? "terminal"
            : role ?? throw new ReplayDivergenceException(
                $"unit {unitIndex} has no root decision");
}
