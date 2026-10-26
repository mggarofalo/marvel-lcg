using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorOdinsAngerCardTests
{
    private static World Board()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        World.MoveToTop(world.Seats[0].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[0].IdentityCard = world.CreateCard("01001a,01001b", world.Seats[0].Hero);
        world.Seats[1].IdentityCard = world.CreateCard("06001a,06001b", world.Seats[1].Hero);
        return world;
    }

    [Fact]
    [Rule("rr:obligation.1")]
    [Rule("rr:obligation.4")]
    public void RevealerGivesObligationToNonFirstOdinsonWhoMayFlipAndExhaustToRemove()
    {
        // "Abilities on obligations that use the words 'you' or 'your' apply only to the player whose play area the obligation is in."
        var world = Board();
        var runner = AuthoredCards.Runner();
        var card = new RedSkullFixture(world, runner).Reveal("06026", 0);
        var flip = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(1, flip.Player);
        Assert.Equal(PlayArea.Of(1), card.Area.PlayArea);
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[0].Id), []);
        var choose = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(1, choose.Player);
        Sequence.Answer(world, world.Facts, runner, choose, Decision.Take(choose.Affordances[0].Id), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal("06001b", world.Seats[1].IdentityCard.FaceId);
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(DeckType.RemovedArea, card.Area.Type);
    }

    [Theory]
    [InlineData("hand")]
    [InlineData("play")]
    [InlineData("absent")]
    public void DecliningFlipDiscardsOnlyOwnHammerThenStunsAndDiscardsEvenWithoutHammer(string location)
    {
        var world = Board();
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[1].IdentityCard;
        var area = location == "hand" ? world.Seats[1].Hand : world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), hero.ObjectId, cardOwner: 1);
        var hammer = location == "absent" ? null : world.CreateCard("06009", area);
        var card = new RedSkullFixture(world, runner).Reveal("06026", 0);
        var flip = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(flip.Affordances[^1].Id), []);
        var choose = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(1, choose.Player);
        Sequence.Answer(world, world.Facts, runner, choose, Decision.Take(choose.Affordances[^1].Id), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        if (hammer is not null) Assert.Equal(DeckType.DiscardPile, hammer.Area.Type);
        Assert.True(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal("06001a", hero.FaceId);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.True(hero.Ready);
    }
}
