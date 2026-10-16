using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityChoiceProgressionTests
{
    [Rule("rr:choose-option.1")]
    [Rule("rr:exhausted")]
    [Fact]
    public void SonicBoomFinishesWhenNoResourcesOrReadyCharactersCanBeChosen()
    {
        // An encounter option requiring targets cannot be chosen without valid
        // targets. An exhausted character cannot be exhausted again.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        seat.IdentityCard.Exhaust();
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var sonicBoom = world.CreateCard("01123", world.AreaOf(DeckType.RevealingArea));
        string before = world.Digest().Canonical();

        AuthoredCards.Runner().WhenRevealed(world, sonicBoom, 0);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Theory]
    [InlineData(24)]
    [InlineData(26)]
    public void InterrogationRoomResponseAlwaysPublishesAResolvableChoice(uint seed)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino_expert", ["spider_man"]), cards),
            ["Spider-Man"], seed, runner);
        var game = Game.Begin(world, cards, runner);
        var policy = new ActingPolicy(checked((int)seed), 1000);
        int decisions = 0;
        while (game.Pending is { } prompt)
        {
            Assert.True(++decisions < 5000);
            Assert.True(prompt.Cancellable || prompt.Affordances.Count > 0);
            game.Resolve(policy.Answer(prompt));
        }
        Assert.True(decisions > 0);
    }
}
