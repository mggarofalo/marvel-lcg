using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreLifecycleAbilityTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:form-change-form.1")]
    [Rule("rr:after")]
    [Fact]
    public void SheHulkRespondsAfterHerHeroFaceIsShowing()
    {
        var world = Board("01019a");
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard(
            "01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(
            1, [Steps.FormChanged], Subject: hero.ObjectId, Player: 0);

        var response = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, hero, 0, choice.Index, Decision.Take(minion.ObjectId), choice.Tier);

        Assert.Equal(2, minion.Damage);
    }

    [Rule("rr:search.1")]
    [Rule("rr:search.3")]
    [Fact]
    public void ShuriSearchesTheControllingPlayersDeckForAnUpgrade()
    {
        var world = Board("01040a");
        var shuri = world.CreateCard(
            "01041", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var upgrade = world.CreateCard("01046", world.Seats[0].Deck);
        world.CreateCard("01044", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(
            1, [Steps.CardEntersPlay], Subject: shuri.ObjectId, Player: 0);

        var response = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, shuri, 0, choice.Index, Decision.Take(upgrade.ObjectId), choice.Tier);

        Assert.Contains(upgrade, world.Seats[0].Hand.Cards);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void HawkeyeGetsFourArrowsBeforeAnsweringAMinionsEntry()
    {
        var world = Board("01040a");
        var hawkeye = world.CreateCard(
            "01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        runner.EntersPlay(world, hawkeye);
        var minion = world.CreateCard(
            "01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var occurrence = new Occurrence(
            1, [Steps.CardEntersPlay], Subject: minion.ObjectId, Player: 0);

        Assert.Equal(4, hawkeye.Tokens["c_arrow"]);
        var response = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);

        Assert.Equal(3, hawkeye.Tokens["c_arrow"]);
        Assert.Equal(2, minion.Damage);
    }

    private static World Board(string identity)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        return world;
    }
}
