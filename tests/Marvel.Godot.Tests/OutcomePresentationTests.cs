using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class OutcomePresentationTests
{
    [Theory]
    [InlineData(Outcome.PlayersWin, "Victory", "The players won.")]
    [InlineData(Outcome.VillainWins, "Defeat", "The villain won.")]
    [InlineData(Outcome.PlayersLose, "Defeat", "The players lost.")]
    public void ResultNamesTheSuppliedOutcomeWithoutInventingItsCause(
        Outcome outcome, string heading, string result)
    {
        OutcomePresentation view = OutcomePresentation.From(outcome);
        Assert.Equal(heading, view.Heading);
        Assert.Equal(result, view.Result);
        Assert.Equal("A detailed cause is not supplied for this result.", view.Detail);
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
