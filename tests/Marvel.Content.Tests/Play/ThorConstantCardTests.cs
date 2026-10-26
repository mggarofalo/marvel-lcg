using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorConstantCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("06001a", 4)]
    [InlineData("06001b", 5)]
    public void AsgardAndHelmetApplyOnlyToControllerInBothForms(string face, int handSize)
    {
        var world = ThorActualFixture.Board(face, 2);
        world.Abilities = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        var other = world.Seats[1].IdentityCard;
        var asgard = world.CreateCard("06007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var helmet = world.CreateCard("06010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(handSize + 1, Modified(world, hero, "hand_size"));
        Assert.Equal(19, DamagePlacement.Health(world, world.Facts, hero));
        Assert.Equal(5, Modified(world, other, "hand_size"));
        Assert.Equal(10, DamagePlacement.Health(world, world.Facts, other));
        for (int i = 0; i < 10; i++) world.CreateCard("01088", world.Seats[0].Deck);
        PhaseEnd.DrawToHandSize(world, world.Facts, []);
        Assert.Equal(handSize + 1, world.Seats[0].Hand.Cards.Count);
        Discard.Card(world, asgard, "test", []);
        Discard.Card(world, helmet, "test", []);
        Assert.Equal(handSize, Modified(world, hero, "hand_size"));
        Assert.Equal(14, DamagePlacement.Health(world, world.Facts, hero));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MjolnirNamesThorAcrossControllersAndEndsOnDeparture(bool exhausted)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        world.Seats[1].IdentityCard = world.CreateCard("06001a,06001b", world.Seats[1].Hero);
        world.Abilities = AuthoredCards.Runner();
        var thor = world.Seats[1].IdentityCard;
        World.MoveToTop(world.Seats[0].IdentityCard, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(0)));
        world.Seats[0].IdentityCard = world.CreateCard("01001a,01001b", world.Seats[0].Hero);
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        if (exhausted) hammer.Exhaust();
        Assert.Equal(3, Modified(world, thor, "attack"));
        Assert.True(Traits.Has(world, thor, "AERIAL", world.Facts));
        Assert.Equal(2, Modified(world, world.Seats[0].IdentityCard, "attack"));
        thor.TurnTo("06001b");
        Assert.Equal(0, Modified(world, thor, "attack"));
        Assert.False(Traits.Has(world, thor, "AERIAL", world.Facts));
        thor.TurnTo("06001a");
        Assert.Equal(3, Modified(world, thor, "attack"));
        Discard.Card(world, hammer, "test", []);
        Assert.Equal(2, Modified(world, thor, "attack"));
        Assert.False(Traits.Has(world, thor, "AERIAL", world.Facts));
    }

    [Theory]
    [InlineData("06001a", false)]
    [InlineData("06001b", false)]
    [InlineData("06001a", true)]
    public void ActualLadySifPlayReadiesNamedIdentityAfterOptionalResponse(string face, bool decline)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        World.MoveToTop(world.Seats[0].IdentityCard, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(0)));
        world.Seats[0].IdentityCard = world.CreateCard("01001a,01001b", world.Seats[0].Hero);
        var named = world.CreateCard("06001a,06001b", world.Seats[1].Hero);
        named.TurnTo(face);
        world.Seats[1].IdentityCard = named;
        named.Exhaust();
        world.Seats[0].IdentityCard.Exhaust();
        var sif = world.CreateCard("06002", world.Seats[0].Hand);
        var first = world.CreateCard("06022", world.Seats[0].Hand);
        var second = world.CreateCard("06023", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], sif, [first.ObjectId, second.ObjectId], []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var response = Assert.Single(prompt.Affordances, a => a.AnchorId == sif.ObjectId);
        Assert.False(named.Ready);
        Sequence.Answer(world, world.Facts, runner, prompt, decline ? Decision.Decline : Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(!decline, named.Ready);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(DeckType.AlliesArea, sif.Area.Type);
    }
}
