using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TableAttachmentCaptionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ConcealedBoostCaptionNamesItsKnownRoleAndCountWithoutLongAnonymousTitle(int count)
    {
        // Synthetic passive projection: visibility is already applied; no
        // hidden face identity or printed text is available to the renderer.
        BoardCardPresentation[] cards = [new(null, count, true,
            $"{count} concealed encounter cards", "", "", "", [])];
        var area = new BoardAreaPresentation(1, "Boost", "", cards, []) { Zone = "BoostCardsDeck" };

        Assert.Equal($"Boost · {count}\nFace down", SpatialTableHostAttachments.Caption(area, cards));
    }

    [Fact]
    public void ReadableAttachmentRetainsItsExactSourceName()
    {
        BoardCardPresentation[] cards = [new(2, 1, false, "Enhanced Ivory Horn", "", "Attachment", "", [])];
        var area = new BoardAreaPresentation(1, "Attachments", "", cards, []);

        Assert.Equal("Enhanced Ivory Horn", SpatialTableHostAttachments.Caption(area, cards));
    }

    [Fact]
    public void MixedAttachmentRegionRemainsInspectableWithoutCallingEveryFaceHidden()
    {
        BoardCardPresentation[] cards = [new(2, 1, false, "Enhanced Ivory Horn", "", "Attachment", "", []),
            new(null, 1, true, "1 concealed encounter card", "", "", "", [])];
        var area = new BoardAreaPresentation(1, "Attachments", "", cards, []);

        Assert.Equal("Cards\n2", SpatialTableHostAttachments.Caption(area, cards));
    }
}
