using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

[Collection("Source presentation state")]
public sealed class SourceCopyIdentityTests
{
    [Fact]
    public void DuplicateMarkersFollowPhysicalCopiesThroughReorderingExhaustionAndReplacement()
    {
        const int seat = 916;
        BoardCardPresentation first = Card(41), second = Card(57);
        var initial = SourceCopyIdentity.Describe(seat, [second, first]);
        Assert.Equal("copy 1", initial[41]);
        Assert.Equal("copy 2", initial[57]);
        SourcePresentationOrder.Store(seat, [57, 41]);
        var refreshed = SourcePresentationOrder.Arrange(seat, [first, second with { Status = "EXHAUSTED" }]);
        Assert.Equal([57, 41], refreshed.Select(card => card.TargetId));
        Assert.Equal(initial.OrderBy(value => value.Key), SourceCopyIdentity.Describe(seat, refreshed).OrderBy(value => value.Key));
        var replaced = SourceCopyIdentity.Describe(seat, [second, Card(93)]);
        Assert.Equal("copy 2", replaced[57]);
        Assert.Equal("copy 3", replaced[93]);
        Assert.Equal("", SourceCopyIdentity.Describe(917, [second])[57]);
    }

    private static BoardCardPresentation Card(int id) => new(id, 1, false, "Powered Gauntlets", "UPGRADE", "", "", []);
}
