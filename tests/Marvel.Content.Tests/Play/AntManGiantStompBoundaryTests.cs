using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic unsupported composition, explicitly replacing the authored row.
public sealed class AntManGiantStompBoundaryTests
{
    [Fact]
    public void GiantStompNestedTargetAfterAreaAttackRaisesBeforePayment()
    {
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = root["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "12003"));
        root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"12003","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "when":{"hasTrait":{"card":"you","trait":"GIANT"}},
              "effect":{"attack":{"target":{"query":"minions"},"effect":{"seq":[
                {"dealAttackDamage":{"cards":{"query":"minions"},"amount":1}},
                {"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
                  "dealAttackDamage":{"cards":"chosen","amount":8}}}}
              ]}}}
            }]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        var printed = CardCatalog.Parse(File.ReadAllText(Marvel.Tests.RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(printed, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.Seats[0].IdentityCard.TurnTo("12001c");
        world.Abilities = runner;
        world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.CreateCard("12003", world.Seats[0].Hand);
        world.CreateCard("12021", world.Seats[0].Hand);
        world.CreateCard("12023", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        var failure = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("selector whose single-card resolution", failure.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(world.Seats[0].Hand, source.Area);
        Discard.Card(world, source, "test", []);
        var valid = world.CreateCard("01005", world.Seats[0].Hand);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == valid.ObjectId);
        Assert.Contains("12003", AuthoredCards.Book.Authored);
    }
}
