using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class GameProgressPresentationTests
{
    [Fact]
    public void PendingAndEveryTerminalOutcomeHaveDistinctCopyAndInputPolicy()
    {
        GameProgressPresentation pending = GameProgressPresentation.FromResponse(
            Response(Outcome.Unfinished));
        GameProgressPresentation won = GameProgressPresentation.FromResponse(
            Response(Outcome.PlayersWin));
        GameProgressPresentation villainWon = GameProgressPresentation.FromResponse(
            Response(Outcome.VillainWins));
        GameProgressPresentation playersLost = GameProgressPresentation.FromResponse(
            Response(Outcome.PlayersLose));

        Assert.Equal(GameProgressKind.AwaitingDecision, pending.Kind);
        Assert.False(pending.LocksDecisions);
        Assert.Equal("Victory.", won.Title);
        Assert.Contains("PLAYERS WIN", won.Status);
        Assert.Equal("Defeat.", villainWon.Title);
        Assert.Contains("VILLAIN WINS", villainWon.Status);
        Assert.Contains("encounter", playersLost.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PLAYERS LOSE", playersLost.Status);
        Assert.All([won, villainWon, playersLost], state => Assert.True(state.LocksDecisions));
    }

    [Fact]
    public void AnUnfinishedGameWithoutALocalPromptWaitsForAnotherPlayer()
    {
        GameProgressPresentation waiting = GameProgressPresentation.FromResponse(
            Response(Outcome.Unfinished, hasPrompt: false));

        Assert.Equal(GameProgressKind.WaitingForOtherPlayer, waiting.Kind);
        Assert.Equal("Waiting for another player.", waiting.Title);
        Assert.Contains("GAME IN PROGRESS", waiting.Status);
        Assert.Contains("WAITING FOR ANOTHER PLAYER", waiting.Status);
        Assert.DoesNotContain("GAME COMPLETE", waiting.Status);
        Assert.True(waiting.LocksDecisions);
    }

    [Fact]
    public void RecoveredAndUnconfirmedErrorsHaveDifferentInputPolicies()
    {
        var error = new ClientStartupError("stale_decision", "The prompt changed.");

        GameProgressPresentation recovered = GameProgressPresentation.Recovered(
            Response(Outcome.Unfinished), error);
        GameProgressPresentation unconfirmed = GameProgressPresentation.Unconfirmed(error);
        GameProgressPresentation unavailable = GameProgressPresentation.Unavailable(error);

        Assert.Equal(GameProgressKind.Recovered, recovered.Kind);
        Assert.False(recovered.LocksDecisions);
        Assert.Contains("RESTORED", recovered.Status);
        Assert.Equal(GameProgressKind.Unconfirmed, unconfirmed.Kind);
        Assert.True(unconfirmed.LocksDecisions);
        Assert.Contains("NOT REPEATED", unconfirmed.Status);
        Assert.Equal(GameProgressKind.Unavailable, unavailable.Kind);
        Assert.Contains("PRODUCT ERROR", unavailable.Status);
    }

    [Fact]
    public void ARecoveredTerminalTableStillPresentsTheEnding()
    {
        GameProgressPresentation recovered = GameProgressPresentation.Recovered(
            Response(Outcome.PlayersWin),
            new ClientStartupError("transport_unavailable", "The response was lost."));

        Assert.Equal(GameProgressKind.PlayersWin, recovered.Kind);
        Assert.Equal("Victory.", recovered.Title);
        Assert.Contains("recovered", recovered.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ARecoveredWaitingTableRemainsAnUnfinishedLockedTable()
    {
        GameProgressPresentation recovered = GameProgressPresentation.Recovered(
            Response(Outcome.Unfinished, hasPrompt: false),
            new ClientStartupError("transport_unavailable", "The response was lost."));

        Assert.Equal(GameProgressKind.WaitingForOtherPlayer, recovered.Kind);
        Assert.Contains("recovered", recovered.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("final table", recovered.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GAME COMPLETE", recovered.Status);
        Assert.True(recovered.LocksDecisions);
    }

    private static EngineResponse Response(Outcome outcome, bool hasPrompt = true)
    {
        Prompt? prompt = outcome == Outcome.Unfinished && hasPrompt
            ? new Prompt(
                0,
                Question.TurnOption,
                TimingPriority.Untimed,
                "test",
                "Choose",
                true,
                [new Affordance(1, "Ask", 1, 0, "Choose")])
            : null;
        return new EngineResponse(
            EngineProtocol.Version,
            "request",
            LocalGameSession.GameId,
            Capability: null,
            prompt,
            Events: [],
            World: new WorldDescriptor([], [], [], outcome));
    }
}
