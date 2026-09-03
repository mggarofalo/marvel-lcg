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
        Prompt prompt = runner.Choosing(
            world, shuri, 0, choice.Index, choice.Tier)!;
        Assert.True(prompt.ExposesConcealedCandidates);
        runner.Chose(
            world, shuri, 0, choice.Index, Decision.Take(upgrade.ObjectId), choice.Tier);

        Assert.Contains(upgrade, world.Seats[0].Hand.Cards);
    }

    [Rule("rr:search.3")]
    [Fact]
    public void ShurisNoResultOneCardSearchStillRecordsInformation()
    {
        // Searching happens even when no Upgrade matches and a one-card deck
        // needs no Fisher-Yates step, so neither a choice nor RNG can stand in
        // for the knowledge signal.
        var world = Board("01040a");
        var shuri = world.CreateCard(
            "01041", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01044", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(
            1, [Steps.CardEntersPlay], Subject: shuri.ObjectId, Player: 0);

        PendingAbility response = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);
        var resolved = new Resolution(world, Prompt: null, Events: []);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Contains(
            resolved.Information,
            signal => signal.Kind == InformationKind.Search);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void HawkeyeGetsFourArrowsBeforeAnsweringAMinionsEntry()
    {
        var world = Board("01040a");
        var hawkeye = world.CreateCard(
            "01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        Reveal.EnterPlay(world, Cards, hawkeye, [], abilities: runner);
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

    [Rule("rr:cost.1")]
    [Rule("rr:ability.11")]
    [Fact]
    public void HawkeyesOptionalCounterCostDoesNotGiveHimUses()
    {
        // "Unless prefaced by the word Forced, all interrupt and response
        // abilities are optional." The cost arrow distinguishes the arrow
        // removal as a cost from the damage effect. Hawkeye's entry sentence
        // is not Uses, so paying his last arrow leaves him in play.
        var world = Board("01040a");
        var hawkeye = world.CreateCard(
            "01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        Reveal.EnterPlay(world, Cards, hawkeye, [], abilities: runner);
        hawkeye.PlaceTokens("c_arrow", -3);
        var minion = world.CreateCard(
            "01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var occurrence = new Occurrence(
            1, [Steps.CardEntersPlay], Subject: minion.ObjectId, Player: 0);

        PendingAbility response = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);

        Assert.Equal(0, hawkeye.Tokens.GetValueOrDefault("c_arrow"));
        Assert.Equal(DeckType.AlliesArea, hawkeye.Area.Type);
        Assert.Equal(2, minion.Damage);
        Assert.Empty(runner.Waiting(
            world,
            new Occurrence(
                2, [Steps.CardEntersPlay], Subject: minion.ObjectId, Player: 0),
            WindowKind.Response));
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
