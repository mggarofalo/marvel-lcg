using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class FacedownMinionAdmissionTests
{
    [Theory]
    [InlineData("discardPile", "returnOwnedToHand")]
    [InlineData("engagedEnemies", "returnOwnedToHand")]
    [InlineData("discardPile", "addToHand")]
    [InlineData("engagedEnemies", "addToHand")]
    public void SingularQueriesAfterEngagementAreRejectedBeforeMutation(string area, string operation)
    {
        // Synthetic composition: no Core card combines these effects. The
        // engine must refuse an unprojected dependent lookup before committing.
        var (world, runner, source) = Board(area, operation);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        int areas = world.Areas.Count;

        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));

        Assert.Contains("singular area query", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(areas, world.Areas.Count);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Single(world.Seats[0].Deck.Cards);
    }

    [Theory]
    [InlineData("returnOwnedToHand")]
    [InlineData("addToHand")]
    public void AnUnchangedPublicAreaQueryStillRunsAfterEngagement(string operation)
    {
        var (world, runner, source) = Board("supports", operation);
        var held = Assert.Single(world.Seats[0].Deck.Cards);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(DeckType.EngagedEnemiesArea, held.Area.Type);
        Assert.Equal("effective-drone", held.InstanceState.Profile?.Id);
        Assert.Contains(source, world.Seats[0].Hand.Cards);
        Assert.Single(world.Seats[0].Deck.Cards);
    }

    [Fact]
    public void ReturningToAnOwnersHandRequiresAPhysicalPlayerOwner()
    {
        var (world, runner, source) = Board("engagedEnemies", "returnOwnedToHand", engage: false);
        var minion = Assert.Single(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Equal(World.Scenario, minion.Owner);
        string before = world.Digest().Canonical();

        Assert.DoesNotContain(runner.Actions(world, 0), option => option.Card == source.ObjectId);

        Assert.Equal(before, world.Digest().Canonical());
    }

    [Theory]
    [InlineData("returnOwnedToHand", "returnOwnedToHand")]
    [InlineData("returnOwnedToHand", "addToHand")]
    [InlineData("addToHand", "returnOwnedToHand")]
    [InlineData("addToHand", "addToHand")]
    [InlineData("returnOwnedToDiscard", "returnOwnedToHand")]
    [InlineData("returnOwnedToDiscard", "addToHand")]
    public void SingularQueriesAfterHandMovementAreRejectedBeforeMutation(string prior, string next)
    {
        var (world, runner, _) = Board("discardPile", next, priorMovement: prior);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        int areas = world.Areas.Count;

        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));

        Assert.Contains("singular area query", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(areas, world.Areas.Count);
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(
        string area, string operation, bool engage = true, string? priorMovement = null)
    {
        var json = JsonNode.Parse(AuthoredCards.Text)!;
        var sourceDefinition = json["cards"]!.AsArray().Single(
            card => card!["card"]!.GetValue<string>() == AuthoredCards.AuntMay)!;
        sourceDefinition["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"seq":[
                {"engageTopAsMinion":{"player":"you","count":1,"profile":"effective-drone"}},
                {"OPERATION":{"last":{"inPlayerArea":{"area":"AREA","player":"you"}}}}
              ]}}]
            """.Replace("AREA", area, StringComparison.Ordinal).Replace("OPERATION", operation, StringComparison.Ordinal));
        if (!engage) sourceDefinition["abilities"]![0]!["effect"]!["seq"]!.AsArray().RemoveAt(0);
        if (priorMovement is not null)
        {
            string priorArea = priorMovement == "returnOwnedToDiscard" ? "supports" : "discardPile";
            var selection = new JsonObject
            {
                ["last"] = new JsonObject
                {
                    ["inPlayerArea"] = new JsonObject { ["area"] = priorArea, ["player"] = "you" },
                },
            };
            sourceDefinition["abilities"]![0]!["effect"]!["seq"]![0] = new JsonObject { [priorMovement] = selection };
        }
        var runner = new AbilityRunner(AbilityLowering.Book(AbilityCatalog.Parse(json.ToJsonString())));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 7) { Abilities = runner };
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        for (int i = 0; i < 4; i++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard(AuthoredCards.AuntMay,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01089", seat.Deck);
        world.CreateCard("01008", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        return (world, runner, source);
    }
}
