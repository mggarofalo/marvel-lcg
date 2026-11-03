using Marvel.Client;
using Xunit;

namespace Marvel.Godot.Tests;

// The table states a finished game once: its heading and reason chip, not a second notice.
public sealed class TableNoticeTests
{
    private static GameProgressPresentation Progress(GameProgressKind kind) =>
        new(kind, "Title.", "Why it happened.", "STATUS", LocksDecisions: false);

    [Theory]
    [InlineData(GameProgressKind.PlayersWin)]
    [InlineData(GameProgressKind.VillainWins)]
    [InlineData(GameProgressKind.PlayersLose)]
    public void AFinishedGameShowsNoTableNoticeButKeepsItsWordsForThePanel(GameProgressKind kind)
    {
        Assert.Equal("", TableSidebarContent.TableNotice(Progress(kind)));
        Assert.Equal("Title.\nWhy it happened.", TableSidebarContent.Notice(Progress(kind)));
    }

    [Theory]
    [InlineData(GameProgressKind.DecisionRejected, "Title.\nWhy it happened.")]
    [InlineData(GameProgressKind.Resolving, "Title.")]
    [InlineData(GameProgressKind.AwaitingDecision, "")]
    public void AnOperationalNoticeStaysOnTheTable(GameProgressKind kind, string expected) =>
        Assert.Equal(expected, TableSidebarContent.TableNotice(Progress(kind)));

    [Fact]
    public void TheStagedRedrawAreaIsAShortChipWithItsMeaningOnHover()
    {
        Assert.Equal("Redraw", TableWording.StagedForRedraw.Label);
        Assert.Contains("replaced when you confirm", TableWording.StagedForRedraw.Tooltip);
    }

    [Fact]
    public void AForcedReceiptStatesItsStateAndNextDecisionInAFewWords()
    {
        Assert.Equal("In progress", ForcedAbilityWording.InProgress.Label);
        Assert.Equal("Next: Choose a target", ForcedAbilityWording.Next("Choose a target"));
    }
}
