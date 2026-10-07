using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ResolvingCardRegionTests
{
    [Fact]
    public void VisibleBoostTakesTheResolvingSlotWhileItsSuspendedCauseRemainsInspectable()
    {
        // Synthetic projected areas: visibility has already been enforced by Marvel.View.
        var cause = Area(1, "RevealingArea", new(10, 1, false, "Assault", "", "", "", []));
        var boost = Area(2, "BoostingArea", new(11, 1, false, "Android Efficiency", "", "", "", []));
        var hand = Area(3, "HandsArea", new(12, 1, false, "Emergency", "", "", "", []));

        var resolving = SpatialTableZones.Resolving([cause, hand, boost]);

        Assert.Equal([boost, cause], resolving);
        Assert.Equal([11, 10], resolving.SelectMany(SpatialTableZones.Current).Select(card => card.TargetId));
        Assert.Empty(SpatialTableObjectRenderer.Unplaced([cause, boost]));
    }

    [Fact]
    public void ConcealedBoostKeepsOnlyItsSuppliedBackAndCount()
    {
        var back = new BoardCardPresentation(null, 1, true, "Face down", "", "", "", [])
        { Back = "Encounter" };
        var boost = Area(2, "BoostingArea", back);

        var card = Assert.Single(SpatialTableZones.Resolving([boost]).SelectMany(SpatialTableZones.Current));

        Assert.Same(back, card);
        Assert.Null(card.TargetId);
        Assert.Null(card.FaceId);
        Assert.Empty(card.RulesText);
        Assert.Equal("Encounter", card.Back);
    }

    private static BoardAreaPresentation Area(int id, string zone, BoardCardPresentation card) =>
        new(id, zone, "", [card], []) { Zone = zone, Prominence = BoardAreaProminence.Live };
}
