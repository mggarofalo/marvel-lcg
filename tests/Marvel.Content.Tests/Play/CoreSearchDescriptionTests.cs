using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreSearchDescriptionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ShuriExplainsTheSearchBeforeRevealingWhetherAnUpgradeExists(int upgrades)
    {
        var world = Board();
        Card shuri = world.CreateCard("01041", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        for (int index = 0; index < upgrades; index++) world.CreateCard("01046", world.Seats[0].Deck);
        world.CreateCard("01044", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(1, [Steps.CardEntersPlay], Subject: shuri.ObjectId, Player: 0);
        var response = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        var before = world.Digest().Fingerprint();

        Affordance offered = runner.Describe(world, response);

        // The wording is an engine presentation choice, independent of hidden matches.
        Assert.Equal("Search your deck for an upgrade and add it to your hand", offered.DisplayLabel);
        Assert.Equal(offered.DisplayLabel, offered.CommitLabel);
        Assert.Contains("Choose a matching card if available, then shuffle your deck", offered.Description);
        Assert.Null(offered.Targets);
        Assert.Equal(before, world.Digest().Fingerprint());
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:search.1")]
    [Rule("rr:search.3")]
    [Fact]
    public void AcceptedSearchRequiresOneMatchingUpgradeAndThenShuffles()
    {
        // rr:search.1: "If a player finds multiple cards that satisfy the criteria
        // of a search, the player chooses among those options."
        // rr:search.3: "If any portion of a deck is searched, upon completion of
        // that game step, game function, or card ability, shuffle that entire deck."
        var world = Board();
        Card shuri = world.CreateCard("01041", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Card claws = world.CreateCard("01047", world.Seats[0].Deck);
        Card suit = world.CreateCard("01049", world.Seats[0].Deck);
        world.CreateCard("01044", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(1, [Steps.CardEntersPlay], Subject: shuri.ObjectId, Player: 0);
        var response = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);

        Prompt prompt = runner.Choosing(world, shuri, 0, choice.Index, choice.Tier)!;

        Assert.True(prompt.ExposesConcealedCandidates);
        Assert.Equal(PublicDecisionKind.CardSearch, prompt.PublicKind);
        Assert.False(prompt.Cancellable);
        Assert.Equal("Shuri: choose an upgrade to add to your hand", prompt.DisplayQuestion);
        Assert.Equal(new[] { claws.ObjectId, suit.ObjectId }, prompt.Affordances.Select(option => option.Id));
        Assert.All(prompt.Affordances, option => Assert.StartsWith("Add ", option.CommitLabel));
        Assert.Contains("shuffle your deck", prompt.Description);
        long before = world.Random.Generator.WordsConsumed;
        runner.Chose(world, shuri, 0, choice.Index, Decision.Take(claws.ObjectId), choice.Tier);

        Assert.Contains(claws, world.Seats[0].Hand.Cards);
        Assert.Contains(suit, world.Seats[0].Deck.Cards);
        Assert.True(world.Random.Generator.WordsConsumed > before);
    }

    private static World Board()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        return world;
    }
}
