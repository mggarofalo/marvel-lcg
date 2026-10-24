using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaShieldTossTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(2, true)]
    [Rule("rr:cost.5")]
    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:stun-stunned.5")]
    public void ActualEventRetainsDiscardCountAndReturnsShieldBeforeCountedAttack(int count, bool stunned)
    {
        // rr:cost.5: multiple costs "must be paid simultaneously"; Stun does not waive them.
        // rr:choose-game-element.3.1: choose "as many as are available" up to the specified count.
        Card? toss = null;
        Card? shield = null;
        Card[] held = [];
        Card[] minions = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            var seat = board.Seats[0];
            toss = board.CreateCard("03006", seat.Hand);
            shield = board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), seat.IdentityCard.ObjectId, 0));
            held = [.. Enumerable.Range(0, 4).Select(_ => board.CreateCard("03003", seat.Hand))];
            minions = [board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
                board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)))];
            if (stunned) Statuses.Give(board, seat.IdentityCard, Statuses.Stunned);
        }, twoPlayers: true);
        var play = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == toss!.ObjectId && offer.PlaysCard);
        Assert.DoesNotContain(toss!.ObjectId, play.Targets!.Legal);
        Assert.DoesNotContain(shield!.ObjectId, play.Targets.Legal);
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(play.Id, [])));
        Assert.Equal(before, world.Digest().Canonical());
        game.Resolve(new Decision(play.Id, [.. held.Take(count).Select(card => card.ObjectId)]));
        Assert.Same(world.Seats[0].Hand, shield.Area);
        Assert.All(held.Take(count), card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (!stunned)
        {
            var attack = Assert.Single(game.Pending!.Affordances);
            Assert.Equal(Math.Min(3, count), attack.Targets!.Min);
            int[] targets = [.. minions.Select(card => card.ObjectId), villain.ObjectId];
            game.Resolve(new Decision(attack.Id, [.. targets.Take(count)]));
        }
        AssertDamage(minions, villain, count, stunned);
        Assert.Equal(DeckType.DiscardPile, toss.Area.Type);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(Statuses.Afflicted(world, world.Facts, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, -1)]
    [InlineData(true, 1)]
    public void RequiresHeroFormAndControlledShieldInPlay(bool hero, int shieldOwner)
    {
        Card? toss = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            toss = board.CreateCard("03006", board.Seats[0].Hand);
            if (shieldOwner >= 0) board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(shieldOwner), cardOwner: shieldOwner));
        }, hero: hero, twoPlayers: true);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == toss!.ObjectId && offer.PlaysCard);
        Assert.Same(world.Seats[0].Hand, toss!.Area);
    }

    private static void AssertDamage(Card[] minions, Card villain, int count, bool stunned)
    {
        for (int index = 0; index < 2; index++)
            Assert.Equal(!stunned && count > index ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, minions[index].Area.Type);
        Assert.Equal(!stunned && count >= 3 ? 4 : 0, villain.Damage);
    }
}
