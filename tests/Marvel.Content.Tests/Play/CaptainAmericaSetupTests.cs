using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Prompts;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaSetupTests : CoreCardTestBase
{
    [Theory]
    [InlineData("deck")]
    [InlineData("discard")]
    [Rule("rr:search.3")]
    public void ActualSetupFindsShieldInEitherPrintedLocationAndShufflesDeck(string location)
    {
        // rr:search.3: "shuffle that entire deck." Printed03001b names deck and discard pile.
        Card? shield = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
            shield = board.CreateCard("03009", location == "deck" ? board.Seats[0].Deck
                : board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0)), hero: false);
        Assert.NotSame(world.Seats[0].Hand, shield!.Area);
        long before = world.Random.Generator.WordsConsumed;
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == shield.ObjectId);
        game.Resolve(Decision.Take(offer.Id));
        Assert.Same(world.Seats[0].Hand, shield.Area);
        Assert.True(world.Random.Generator.WordsConsumed > before);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Fact]
    public void AlreadyHeldShieldIsNotOfferedForSetupSearch()
    {
        Card? shield = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            World.MoveToTop(board.Seats[0].Hand.Cards[0], board.Seats[0].Deck);
            shield = board.CreateCard("03009", board.Seats[0].Hand);
        }, hero: false);
        Assert.Same(world.Seats[0].Hand, shield!.Area);
        Assert.DoesNotContain(game.Pending!.Affordances,
            item => item.AnchorId == shield.ObjectId && item.Verb == Marvel.Cards.Run.AbilityRunner.ChooseVerb);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:search.3")]
    public void ActualSetupStillShufflesWhenNoShieldCanBeRetrieved(bool held)
    {
        // rr:search.3: "shuffle that entire deck." The printed03001b shuffle is unconditional.
        var world = Hero("03001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        for (int index = 0; index < 5; index++) world.CreateCard("01088", world.Seats[0].Deck);
        Card? shield = held ? world.CreateCard("03009", world.Seats[0].Hand) : null;
        long random = world.Random.Generator.WordsConsumed;
        var events = runner.ExecuteSetup(world, world.Seats[0].IdentityCard).Events.ToList();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.True(world.Random.Generator.WordsConsumed > random);
        Assert.Equal(held ? 1 : 0, world.Seats[0].Hand.Cards.Count);
        if (shield is not null) Assert.Same(world.Seats[0].Hand, shield.Area);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
