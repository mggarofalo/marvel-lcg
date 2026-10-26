using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic research reveals use the actual authored attachment row.
public sealed class AllTiedUpAttachmentTests
{
    [Theory]
    [InlineData(0, "01001a")]
    [InlineData(0, "01001b")]
    [InlineData(1, "01010a")]
    [InlineData(1, "01010b")]
    [Rule("rr:attach-to")]
    [Rule("rr:cannot")]
    public void RevealAttachesToTheRevealingIdentityInEitherForm(int player, string face)
    {
        // rr:attach-to: "must be attached to ... the specified game element as it enters play."
        // 02048: "Attach to your identity card"; "cannot ready or change form."
        var f = RedSkullFixture.Create(hero: false, players: 2);
        Card identity = f.World.Seats[player].IdentityCard;
        identity.TurnTo(face);
        var attachment = f.Reveal("02048", player);
        f.Finish();
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Equal(identity.ObjectId, attachment.Area.Host);
        Assert.Equal(PlayArea.Of(player), attachment.Area.PlayArea);
        Assert.Equal(face, identity.FaceId);
        Assert.False(ReadinessPermissions.CanReady(f.World, identity));
        Assert.False(FormChangePermissions.CanChange(f.World, identity));
        Card other = f.World.Seats[1 - player].IdentityCard;
        Assert.True(ReadinessPermissions.CanReady(f.World, other));
        Assert.True(FormChangePermissions.CanChange(f.World, other));
        Assert.Contains("02048", AuthoredCards.Book.Authored);
    }
}
