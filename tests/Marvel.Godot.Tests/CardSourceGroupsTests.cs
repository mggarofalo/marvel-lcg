using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardSourceGroupsTests
{
    [Fact]
    public void PhysicalSourceUsesItsRelationRatherThanStorageHostOrAffectedRecipients()
    {
        BoardCardPresentation controlled = Source(2, "Controlled", null);
        BoardCardPresentation attached = Source(3, "Attached", 20) with
        { Persistent = Source(3, "Attached", 20).Persistent! with { Contributions = [new(30, "ATK", "Add", 1, null)] } };
        var area = new BoardAreaPresentation(1, "Stored", "", [controlled, attached], []) { Host = 10 };
        Assert.Empty(CardSourceGroups.Attached([area], 10));
        Assert.Empty(CardSourceGroups.Attached([area], 30));
        Assert.Equal(3, Assert.Single(CardSourceGroups.Attached([area], 20)).TargetId);
        Assert.Equal(2, Assert.Single(CardSourceGroups.Controlled([area], 0)).TargetId);
    }

    [Fact]
    public void ConcealedRemovedAndRepeatedCopiesDoNotCreateLiveRows()
    {
        BoardCardPresentation live = Source(2, "Attached", 20);
        BoardCardPresentation hidden = Source(3, "Attached", 20) with { Concealed = true };
        var area = new BoardAreaPresentation(1, "", "", [live, hidden, live], [Source(4, "Attached", 20)]);
        Assert.Equal(2, Assert.Single(CardSourceGroups.Attached([area], 20)).TargetId);
        Assert.Empty(CardSourceSummary.Lines(hidden));
    }

    [Fact]
    public void ControlledCharactersRemainPhysicalCardsAndOrphanAttachmentsRemainReachable()
    {
        Assert.False(CardSourceGroups.IsLocalSource(Source(2, "Controlled", null) with { Kind = "HERO" }));
        Assert.False(CardSourceGroups.IsLocalSource(Source(3, "Controlled", null) with { Kind = "ALLY" }));
        var area = new BoardAreaPresentation(1, "", "", [Source(4, "Attached", 20)], [])
        { Host = 20, Zone = "UpgradesArea", Prominence = BoardAreaProminence.Live };
        Assert.Single(SpatialTableObjectRenderer.Unplaced([area], []));
        Assert.Empty(SpatialTableObjectRenderer.Unplaced([area], [20]));
    }

    private static BoardCardPresentation Source(int id, string relation, int? host) =>
        new(id, 1, false, "Duplicate title", "", "UPGRADE", "READY", [])
        { Persistent = new(new("synthetic", "Duplicate title", id, false), new(relation, host, 0), [], [], false) };
}
