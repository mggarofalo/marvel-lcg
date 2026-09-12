using Marvel.Rules.Play;
using Marvel.Session;
using static Marvel.Sim.SimulationHarnessSupport;

namespace Marvel.Sim;

internal sealed class SimulationFailureReplay(
    SimulationReplaySession session, FailureRecord failure)
{
    internal void Reproduce()
    {
        ValidateSeed();
        if (session.Game is null) ReproduceSetup();
        else ReproduceActive();
    }

    private void ValidateSeed()
    {
        if (failure.Game < 0 || failure.Game >= session.Header.Seeds.Count)
            throw new ReplayDivergenceException(
                $"failure names game index {failure.Game} outside the seed plan");
        RequireEqual(session.Header.Seeds[failure.Game], failure.Seed,
            $"game {failure.Game} failure seed");
    }

    private void ReproduceSetup()
    {
        RequireEqual(session.CurrentGame + 1, failure.Game, "setup failure game index");
        ValidateSetupRecord();
        try
        {
            _ = Open(session.Data, session.Simulation, failure.Seed);
        }
        catch (Exception error)
        {
            RequireFailure(failure, error, "setup");
            session.CountSetupFailure(failure);
            session.Diagnostics.WriteLine(
                $"reproduced expected setup failure in game {failure.Game}: "
                + $"{error.GetType().Name}: {error.Message}");
            return;
        }
        throw new ReplayDivergenceException(
            $"game {failure.Game} did not reproduce recorded setup failure");
    }

    private void ValidateSetupRecord()
    {
        if (failure.LastGoodDigest is not null || failure.Decision is not null)
            throw new ReplayDivergenceException(
                $"game {failure.Game} has no start but records gameplay state");
        RequireEqual(0, failure.Round, $"game {failure.Game} setup round");
        RequireEqual(Json(new PolicyMetrics(0, 0, 0, 0)), Json(failure.Metrics),
            $"game {failure.Game} setup metrics");
        RequireNullablePrompt(null, failure.Prompt, $"game {failure.Game} setup prompt");
        RequireEqual(0, failure.RecentSteps.Count,
            $"game {failure.Game} setup recent-step count");
    }

    private void ReproduceActive()
    {
        ValidateActiveRecord();
        session.CountActiveFailure(failure);
        if (failure.Category == "decision_limit") ReproduceDecisionLimit();
        else if (failure.Category == "policy_error") ReproducePolicyError();
        else if (failure.Decision is null) ReproduceTerminalError();
        else ReproduceResolutionError();
    }

    private void ValidateActiveRecord()
    {
        var game = session.Game!;
        RequireGame(game, session.CurrentGame, failure.Game);
        RequireEqual(session.CurrentSteps, failure.Step,
            $"game {session.CurrentGame} failure step");
        RequireEqual(failure.LastGoodDigest, game.State.Digest().Canonical(),
            $"game {session.CurrentGame} last-good digest");
        RequireNullablePrompt(failure.Prompt, ExpectedFailurePrompt(failure, game),
            $"game {session.CurrentGame} failure prompt");
        RequireEqual(Json(failure.RecentSteps), Json(session.Recent),
            $"game {session.CurrentGame} recent steps");
        RequireEqual(game.Round, failure.Round,
            $"game {session.CurrentGame} failure round");
        RequireEqual(Json(session.Policy!.Metrics), Json(failure.Metrics),
            $"game {session.CurrentGame} failure metrics");
    }

    private void ReproduceDecisionLimit()
    {
        var game = session.Game!;
        string expected = $"decision limit {session.Header.DecisionLimit} reached at "
            + $"'{game.Pending?.Label}'";
        RequireEqual("decision_limit", failure.Category,
            $"game {session.CurrentGame} failure category");
        RequireEqual(typeof(SimulationRunException).FullName, failure.Exception,
            $"game {session.CurrentGame} failure type");
        RequireEqual(expected, failure.Message,
            $"game {session.CurrentGame} failure message");
        RequireEqual(failure.PostFailureDigest, game.State.Digest().Canonical(),
            $"game {session.CurrentGame} post-failure digest");
        session.CloseGame();
    }

    private void ReproducePolicyError()
    {
        try
        {
            var generated = session.Policy!.Answer(session.Game!);
            _ = DecisionSelector.From(session.Game!.Pending!, generated);
        }
        catch (Exception error)
        {
            RequireFailure(failure, error, "policy");
            session.CloseGame();
            return;
        }
        throw new ReplayDivergenceException(
            $"game {session.CurrentGame} did not reproduce recorded policy failure");
    }

    private void ReproduceTerminalError()
    {
        var game = session.Game!;
        if (failure.Category != "engine_exception" || game.Pending is not null)
            throw new ReplayDivergenceException(
                $"game {session.CurrentGame} records category '{failure.Category}' "
                + "without an attempted decision");
        var terminal = new SimulationRunException(
            $"game ended without a terminal outcome: {game.State.Result}");
        RequireFailure(failure, terminal, "terminal");
        session.CloseGame();
    }

    private void ReproduceResolutionError()
    {
        var game = session.Game!;
        if (game.Pending is null)
            throw new ReplayDivergenceException(
                $"game {session.CurrentGame} failure has no pending decision");
        try
        {
            var decision = failure.Decision!.Resolve(
                DurableDecision.SimulationActor(game.Pending, failure.Decision),
                game.Pending, failure.Targets, failure.Resources,
                failure.Values, failure.Allocations);
            game.Resolve(decision);
        }
        catch (Exception error)
        {
            RequireFailure(failure, error, "resolve");
            RequireEqual(failure.PostFailureDigest, game.State.Digest().Canonical(),
                $"game {session.CurrentGame} post-failure digest");
            session.Diagnostics.WriteLine(
                $"reproduced expected failure in game {session.CurrentGame}: "
                + $"{error.GetType().Name}: {error.Message}");
            session.CloseGame();
            return;
        }
        throw new ReplayDivergenceException(
            $"game {session.CurrentGame} did not reproduce recorded failure");
    }

    private static string Json<T>(T value) =>
        System.Text.Json.JsonSerializer.Serialize(value, RecordJson.Options);
}
