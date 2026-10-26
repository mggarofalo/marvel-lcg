using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MediaCoverageCardTests
{
    [Theory]
    [InlineData(0, false, 1)]
    [InlineData(1, false, 2)]
    [InlineData(2, false, 3)]
    [InlineData(1, true, 1)]
    public void ActualAttachmentsRepeatActualRevealAndDepartureEndsTheConstant(int copies, bool departed, int expected)
    {
        var f = RedSkullFixture.Create(hero: false, players: 2);
        f.World.Seats[1].IdentityCard.TurnTo("01010a");
        for (int index = 0; index < copies; index++)
        {
            var attachment = f.Reveal("02049", 1);
            f.Finish();
            Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, attachment.Area.Host);
            if (departed) World.MoveToTop(attachment, f.World.AreaOf(DeckType.EncounterDiscardPile));
        }
        // Shocker deals one damage to each hero; only seat one is in hero form.
        var revealed = f.Reveal("01103", 1);
        f.Finish();
        Assert.Equal(expected, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, revealed.Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void OnlyTheAttachedControllerInAlterEgoCanPayMentalToRemove(int player)
    {
        var f = RedSkullFixture.Create(hero: false, players: 2);
        var attachment = f.Reveal("02049", player);
        f.Finish();
        var identity = f.World.Seats[player].IdentityCard;
        identity.TurnTo(player == 0 ? "01001a" : "01010a");
        var payment = f.World.CreateCard("01089", f.World.Seats[player].Hand);
        Assert.DoesNotContain(f.Runner.Actions(f.World, player), action => action.Card == attachment.ObjectId);
        identity.TurnTo(player == 0 ? "01001b" : "01010b");
        var action = Assert.Single(f.Runner.Actions(f.World, player), item => item.Card == attachment.ObjectId);
        Assert.DoesNotContain(f.Runner.Actions(f.World, 1 - player), item => item.Card == attachment.ObjectId);
        f.Runner.ExecuteAction(f.World, new PlayerAction(action, [payment.ObjectId], []));
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }
}
