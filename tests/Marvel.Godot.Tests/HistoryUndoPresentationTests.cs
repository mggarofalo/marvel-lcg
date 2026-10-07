using Marvel.Client;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class HistoryUndoPresentationTests
{
    [Theory]
    [InlineData(HistoryUndoStatus.NoHistory, "no completed action")]
    [InlineData(HistoryUndoStatus.ActionInProgress, "Finish the current action")]
    [InlineData(HistoryUndoStatus.ProtectedHistory, "exposed information or used hidden randomness")]
    [InlineData(HistoryUndoStatus.OtherPlayer, "another player's decisions")]
    public void DisabledUndoNamesTheAuthorizedRestriction(HistoryUndoStatus status, string expected)
    {
        // Synthetic public history metadata: no private journal or card identity reaches the client.
        var history = new HistoryDescriptor(1, [], [], [], false) { UndoStatus = status };
        string description = HistoryUndoPresentation.Describe(false, history, null);
        Assert.Contains(expected, description, StringComparison.Ordinal);
    }


    [Fact]
    public void PendingRequestExplainsTheTemporaryLockInsteadOfAnOlderHistoryRestriction()
    {
        var history = new HistoryDescriptor(1, [], [], [], false)
        { UndoStatus = HistoryUndoStatus.ProtectedHistory };
        var progress = GameProgressPresentation.Undoing();
        Assert.Equal(progress.Description, HistoryUndoPresentation.Describe(false, history, progress));
        Assert.Equal("Undo the latest completed action.", HistoryUndoPresentation.Describe(true, history, null));
    }
}
