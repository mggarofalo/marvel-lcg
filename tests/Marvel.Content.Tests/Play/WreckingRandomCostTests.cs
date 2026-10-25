using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Core.Random;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingRandomCostTests
{
    [Theory]
    [InlineData("07006", 0, 0)]
    [InlineData("07006", 0, 1)]
    [InlineData("07020", 1, 0)]
    [InlineData("07020", 1, 1)]
    [InlineData("07049", 3, 0)]
    [InlineData("07049", 3, 1)]
    [Rule("rr:cost.5")]
    public void HeroExhaustionAndRandomDiscardPayBeforeAttachmentLeaves(string face, int owner, int player)
    {
        // Printed Hero Action: "Exhaust your hero and discard 1 card at random from your hand".
        // rr:cost.5 requires the multiple costs to be paid simultaneously.
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, owner, players: 2);
        world.CreateCard("01088", world.Seats[1].Deck);
        var runner = Runner(face, world.Facts.Title(groups[owner].Villain.FaceId));
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, player, []);
        Card[] hand = [world.CreateCard("01005", world.Seats[player].Hand),
            world.CreateCard("01003", world.Seats[player].Hand), world.CreateCard("01007", world.Seats[player].Hand)];
        var foreign = world.CreateCard("01005", world.Seats[1 - player].Hand);
        var expectedRandom = new EngineRandom(0);
        var expectedCard = expectedRandom.Choice(hand);
        long before = world.Random.Generator.WordsConsumed;
        var action = Assert.Single(runner.Actions(world, player), item => item.Card == source.ObjectId);
        _ = runner.Actions(world, player);
        Assert.Equal(before, world.Random.Generator.WordsConsumed);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(world.Seats[player].IdentityCard.Ready);
        Assert.True(world.Seats[1 - player].IdentityCard.Ready);
        Assert.Equal(2, world.Seats[player].Hand.Cards.Count);
        Assert.Same(expectedCard, Assert.Single(hand, card => card.Area.Type == DeckType.DiscardPile));
        Assert.Same(world.Seats[1 - player].Hand, foreign.Area);
        Assert.Same(groups[owner].Discard, source.Area);
        Assert.Equal(expectedRandom.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("exhausted")]
    [InlineData("alter-ego")]
    public void UnpayableRandomCostIsNotOfferedAndDoesNotMutate(string condition)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07006", 0,
            condition == "alter-ego" ? "01001b" : "01001a");
        var runner = Runner("07006", "Wrecker");
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        if (condition != "empty") world.CreateCard("01005", world.Seats[0].Hand);
        if (condition == "exhausted") world.Seats[0].IdentityCard.Exhaust();
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(groups[0].Villain.ObjectId, source.Area.Host);
    }

    [Fact]
    public void StaleEmptyHandRejectsBeforeExhaustionOrRandomConsumption()
    {
        var (world, _, source) = WreckingEncounterCardFixture.Create("07006", 0);
        var runner = Runner("07006", "Wrecker");
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        var handCard = world.CreateCard("01005", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        World.MoveToTop(handCard, world.AreaOf(DeckType.RemovedArea));
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    private static AbilityRunner Runner(string face, string title) => face != "07049"
        ? AuthoredCards.Runner() : new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"{{face}}","attachTo":{"titled":"{{title}}"},"abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "cost":{"seq":[{"exhaust":"you"},{"discardRandomFromHand":1}]},
          "effect":{"discard":"this"}
        }]}]}
        """));
}
