using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaStrengthInNumbersTests
{
    [Fact]
    [Rule("rr:exhausted.2")]
    public void NoReadyAllyMeansNoMeaningfulEventOffer()
    {
        // An exhausted card "cannot exhaust again until it is readied".
        Card? source = null;
        var (game, _) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            source = board.CreateCard("03017", board.Seats[0].Hand);
            board.CreateCard("03012", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)).Exhaust();
        });
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
    }

    [Fact]
    [Rule("rr:cost.7")]
    public void InvalidCostSelectionsLeaveTheEventAndAllCardsUnchanged()
    {
        // rr:cost.7: players "must pay costs with cards and/or game elements they control."
        Card? source = null;
        Card? ally = null;
        Card? other = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            source = board.CreateCard("03017", board.Seats[0].Hand);
            ally = board.CreateCard("03012", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("03013", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            other = board.CreateCard("03020", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        }, twoPlayers: true);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        int[][] rejected = [[], [source!.ObjectId], [ally!.ObjectId, ally.ObjectId], [other!.ObjectId]];
        string before = world.Digest().Canonical();
        foreach (int[] targets in rejected)
        {
            Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(action.Id, targets)));
            Assert.Equal(before, world.Digest().Canonical());
        }
        game.Resolve(new Decision(action.Id, [ally.ObjectId]));
        Assert.False(ally.Ready);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [Rule("rr:cost.1")]
    public void ActualEventExhaustsSelectedAlliesBeforeDrawingAndDiscarding(int count, bool hero)
    {
        // 03017: "Exhaust any number of allies you control → draw 1 card for each ally exhausted this way."
        // rr:cost.1 distinguishes costs and effects in a "pay cost resolve effect" format.
        Card? source = null;
        Card[] allies = [];
        Card? foreign = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            source = board.CreateCard("03017", board.Seats[0].Hand);
            board.CreateCard("01073", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            string[] faces = ["03012", "03013", "03020", "03002"];
            allies = [.. faces.Select(face => board.CreateCard(face,
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)))];
            allies[3].Exhaust();
            foreign = board.CreateCard("01020", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        }, hero: hero, twoPlayers: true);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        Assert.DoesNotContain(source!.ObjectId, action.Targets!.Legal);
        Assert.DoesNotContain(allies[3].ObjectId, action.Targets.Legal);
        Assert.DoesNotContain(foreign!.ObjectId, action.Targets.Legal);
        int before = world.Seats[0].Hand.Cards.Count;
        game.Resolve(new Decision(action.Id, [.. allies.Take(count).Select(card => card.ObjectId)]));
        Assert.Equal(before - 1 + count, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.All(allies.Take(count), card => Assert.False(card.Ready));
        Assert.All(allies.Skip(count).Take(3 - count), card => Assert.True(card.Ready));
        Assert.True(foreign.Ready);
    }
}
