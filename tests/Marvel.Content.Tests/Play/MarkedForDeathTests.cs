using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MarkedForDeathTests : CoreCardTestBase
{
    [Theory]
    [InlineData("04001a", DeckType.HandsArea)]
    [InlineData("04001b", DeckType.HandsArea)]
    [InlineData("04001a", DeckType.PlayerDeck)]
    [InlineData("04001b", DeckType.PlayerDeck)]
    [InlineData("04001a", DeckType.DiscardPile)]
    [InlineData("04001b", DeckType.DiscardPile)]
    [InlineData("04001a", DeckType.AlliesArea)]
    [InlineData("04001b", DeckType.AlliesArea)]
    public void CapturesOnlyClintsIdentitySpecificCopyAndReturnsOnlyThatCaptive(string identity, DeckType area)
    {
        // ruling:6d6264d6620980b3: the identity-specific copy is out of play beneath
        // the scheme; only that copy returns when the scheme is defeated.
        var world = Hero(identity, 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("04028", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        var captive = world.CreateCard("04004", world.AreaOf(area, PlayArea.Of(0), cardOwner: 0));
        var basic = world.CreateCard("01083", world.Seats[0].Hand);
        var other = world.CreateCard("01083", world.Seats[1].Hand);
        world.CreateCard("01088", world.Seats[0].Deck);
        var events = runner.ExecuteReveal(world, scheme, 1).Events.ToList();
        var order = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Order, order.Asking);
        var offered = Assert.Single(order.Affordances);
        Sequence.Answer(world, Cards, runner, order, Decision.Take(offered.Id, offered.Targets!.Legal, []), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(captive, Assert.Single(BeneathCards.Of(world, scheme)));
        Assert.True(captive.FaceUp);
        Assert.False(DeckTypes.IsInPlay(captive.Area.Type));
        Assert.Same(world.Seats[0].Hand, basic.Area);
        Assert.Same(world.Seats[1].Hand, other.Area);

        Defeat.Scheme(world, Cards, scheme, "test", events, by: 1, recordOn: new Occurrence(99, []));
        Sequence.Finish(world, Cards, runner, events);
        Assert.Same(world.Seats[0].Hand, captive.Area);
        Assert.Same(world.Seats[0].Hand, basic.Area);
        Assert.Same(world.Seats[1].Hand, other.Area);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Empty(BeneathCards.Of(world, scheme));
    }
}
