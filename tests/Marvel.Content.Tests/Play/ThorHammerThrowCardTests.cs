using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorHammerThrowCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HammerThrowPaysThreeAndExhaustsThenReturnsAfterDamageOrStun(bool stunned)
    {
        // Synthetic board uses actual printed cards and the complete authored book.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var doubleResource = world.CreateCard("06022", world.Seats[0].Hand);
        var singleResource = world.CreateCard("01003", world.Seats[0].Hand);
        if (stunned) Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [doubleResource.ObjectId, singleResource.ObjectId], [enemy.ObjectId]));        if (!stunned)
        {
            var target = Sequence.Work(world, world.Facts, runner, [])!;
            Sequence.Answer(world, world.Facts, runner, target, Decision.Take(enemy.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleResource.Area.Type);
        Assert.Equal(DeckType.DiscardPile, singleResource.Area.Type);
        Assert.Equal(stunned ? DeckType.UpgradesArea : DeckType.HandsArea, hammer.Area.Type);
        if (stunned) Assert.False(hammer.Ready);
        Assert.Equal(stunned ? 3 : 2, StateFields.Modified(world, world.Seats[0].IdentityCard, "attack", world.Facts, world.Players));
        Assert.Equal(stunned ? 0 : 5, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(stunned ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(stunned, Traits.Has(world, world.Seats[0].IdentityCard, "AERIAL", world.Facts));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void HammerThrowCannotPayWithAnExhaustedHammerOrInsufficientResources()
    {
        // Synthetic board uses actual printed cards and the complete authored book.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var resource = world.CreateCard("06022", world.Seats[0].Hand);
        var before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [resource.ObjectId], []));
        Assert.Equal(before, world.Digest().Canonical());
        world.CreateCard("01003", world.Seats[0].Hand);
        Assert.Contains(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        hammer.Exhaust();
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == card.ObjectId);
    }
    [Fact]
    [Rule("rr:and")]
    public void HammerReturnsBeforeLaterDefeatResponseAndItsNestedChoice()
    {
        // Synthetic board uses actual printed cards and the complete authored book.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var chase = world.CreateCard("06013", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 4);
        var side = world.CreateCard("01129", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 3);
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("06022", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        var target = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(minion.ObjectId), []);
        var response = Sequence.Work(world, world.Facts, runner, [])!;
        // rr:and: effects joined by and resolve simultaneously.
        Assert.Equal(DeckType.HandsArea, hammer.Area.Type);
        Assert.Same(world.Seats[0].Hand, hammer.Area);
        Assert.False(Traits.Has(world, world.Seats[0].IdentityCard, "AERIAL", world.Facts));
        var offer = Assert.Single(response.Affordances, a => a.AnchorId == chase.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(offer.Id), []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(DeckType.HandsArea, hammer.Area.Type);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(scheme.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Same(world.Seats[0].Hand, hammer.Area);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, chase.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

}
