using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class OutcomePresentationTests
{
    [Theory]
    [InlineData(Outcome.PlayersWin, "Victory", "The final villain stage was defeated.")]
    [InlineData(Outcome.VillainWins, "Defeat", "The final main scheme was completed.")]
    [InlineData(Outcome.PlayersLose, "Defeat", "The players lost.")]
    public void ResultNamesTheSuppliedOutcomeWithoutInventingItsCause(
        Outcome outcome, string heading, string result)
    {
        OutcomePresentation view = OutcomePresentation.From(outcome);
        Assert.Equal(heading, view.Heading);
        Assert.Equal(result, view.Result);
        Assert.Empty(view.Detail);
    }

    [Theory]
    [InlineData(PlayerLossReason.AllPlayersEliminated, "All players were eliminated.")]
    [InlineData(PlayerLossReason.EncounterCardsExhausted, "The encounter deck and discard pile are both empty.")]
    public void LossExplainsTheRuleOwnedCause(PlayerLossReason reason, string expected)
    {
        var view = OutcomePresentation.From(Outcome.PlayersLose, reason);
        Assert.Equal(expected, view.Result);
        Assert.Empty(view.Detail);
        var response = new EngineResponse(EngineProtocol.Version, "test", "game", null, null, [],
            new WorldDescriptor([], [], [], Outcome.PlayersLose) { LossReason = reason });
        var progress = GameProgressPresentation.FromResponse(response);
        Assert.Equal(expected, progress.Description);
        Assert.True(progress.LocksDecisions);
    }

    [Fact]
    public void PendingOtherSeatIsNotPresentedAsATerminalResult()
    {
        OutcomePresentation view = OutcomePresentation.From(Outcome.Unfinished);
        Assert.Equal("Waiting for another player", view.Heading);
        Assert.Equal("The game is still in progress.", view.Result);
        Assert.Empty(view.Detail);
    }
}
