using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreSelectionResourceTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void FuturistOffersTheTopThreeInTopToBottomOrderAndDiscardsTheOtherTwo()
    {
        var world = Board("01029b");
        var bottom = world.CreateCard("01002", world.Seats[0].Deck);
        var third = world.CreateCard("01003", world.Seats[0].Deck);
        var second = world.CreateCard("01004", world.Seats[0].Deck);
        var top = world.CreateCard("01005", world.Seats[0].Deck);
        var tony = world.Seats[0].IdentityCard;
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(tony.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, tony, 0, choice.Index, choice.Tier)!;
        Assert.Equal(
            [top.ObjectId, second.ObjectId, third.ObjectId],
            prompt.Affordances.Select(option => option.Id));
        Assert.True(prompt.ExposesConcealedCandidates);

        runner.Chose(
            world, tony, 0, choice.Index, Decision.Take(second.ObjectId), choice.Tier);

        Assert.Contains(second, world.Seats[0].Hand.Cards);
        Assert.Single(world.Seats[0].Deck.Cards);
        Assert.Contains(bottom, world.Seats[0].Deck.Cards);
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        Assert.Equal([top.ObjectId, third.ObjectId], discard.Cards.Select(card => card.ObjectId));
    }

    [Rule("rr:printed")]
    [Fact]
    public void PepperPottsAdvertisesAndGeneratesTheCurrentTopDiscardsPrintedResources()
    {
        var world = Board("01029b");
        var pepper = world.CreateCard(
            "01033", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01043a", discard);
        world.CreateCard("01044", discard);
        var runner = AuthoredCards.Runner();

        var source = Assert.Single(
            runner.ResourceAbilities(world, 0), option => option.Effect == pepper.ObjectId);
        Assert.Equal("GG", source.Generates);
        Assert.Equal("GG", runner.UseResource(world, 0, pepper.ObjectId, []));
        Assert.False(pepper.Ready);
    }

    [Fact]
    public void StarkTowerReturnsTheChosenPlayersTopmostTechUpgrade()
    {
        var world = new World(Cards, players: 2);
        var first = world.CreateSeat("p0");
        first.IdentityCard = world.CreateCard("01029b", first.Hero);
        var second = world.CreateSeat("p1");
        second.IdentityCard = world.CreateCard("01001a", second.Hero);
        var tower = world.CreateCard(
            "01034", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        var lowerTech = world.CreateCard("01035", discard);
        var topTech = world.CreateCard("01036", discard);
        var nonTech = world.CreateCard("01044", discard);
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(tower.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, tower, 0, choice.Index,
            Decision.Take(second.IdentityCard.ObjectId), choice.Tier);

        Assert.Contains(topTech, second.Hand.Cards);
        Assert.Contains(lowerTech, discard.Cards);
        Assert.Contains(nonTech, discard.Cards);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void ResourceDiscoveryPaymentAndLimitsUseTheCompiledBook(bool mutateBeforeOffering, bool anotherPlayer)
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01033","abilities":[
              {"trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"game"},
               "effect":{"draw":{"player":"you","count":1}}},
              {"name":"Stored energy","trigger":{"timing":"Resource","event":"WhenActionTriggered","form":"alter-ego"},
               "limitPerRound":1,"anyPlayer":true,"cost":{"exhaust":"this"},"printedResources":"GG",
               "effect":{"generate":"GG"}}
            ]}]}
            """);
        var abilities = parsed.Abilities.ToList();
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook(abilities, parsed.Authored));
        var world = new World(Cards, players: 2);
        var first = world.CreateSeat("p0");
        first.IdentityCard = world.CreateCard("01029b", first.Hero);
        var second = world.CreateSeat("p1");
        second.IdentityCard = world.CreateCard("01001b", second.Hero);
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("01033",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        int player = anotherPlayer ? 1 : 0;

        // Engine-choice fixture: the compiler snapshots the entire book,
        // including the non-resource ability preceding this resource ability.
        // Removing caller-owned rows cannot change discovery, naming, payment,
        // or the face-wide ordinal used to account for this ability's limit.
        if (mutateBeforeOffering)
        {
            abilities.Clear();
        }
        Assert.Equal("GG", Assert.Single(runner.ResourceAbilities(world, player)).Generates);
        abilities.Clear();
        Assert.Equal("Stored energy", runner.ResourceGeneratorName(world, player, source.ObjectId));
        Assert.Equal(source.ObjectId, Assert.Single(runner.PrintedResourceAbilities(world, player)).Effect);
        Assert.Equal("GG", runner.UseResource(world, player, source.ObjectId, []));
        Assert.False(source.Ready);
        Assert.Contains(world.Effects.Active(), effect =>
            effect.Kind == $"spent:{source.Incarnation}:01033:1");

        source.Refresh();
        Assert.Empty(runner.ResourceAbilities(world, player));
        Assert.Empty(runner.PrintedResourceAbilities(world, player));
        Assert.Throws<RulesNotImplementedException>(() => runner.UseResource(world, player, source.ObjectId, []));
        Assert.True(source.Ready);
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
