using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// A test-local row on 13006 proves the receipt's simultaneous path; no product reads it.
public sealed class WaspExcessDealtReceiptTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:excess-damage")]
    public void ADividedSimultaneousAttackRecordsEachRecipientsExcessDealt()
    {
        // rr:excess-damage: "Excess damage is any amount of damage that is dealt to a character beyond that character’s remaining hit points."
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = root["cards"]!.AsArray();
        cards.Remove(cards.First(row => (string?)row!["card"] == "13006"));
        cards.Add(JsonNode.Parse("""
            {"card":"13006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "labels":["attack"],"effect":{"seq":[
                {"attack":{"target":{"query":"attackableEnemies"},"total":4,
                  "effect":{"dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}}}},
                {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":{"result":"excessDamageDealt"}}}]}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        var world = WaspBoards.Board("13001c");
        world.Abilities = runner;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var minion = WaspBoards.Engaged(world, "01110");
        var card = world.CreateCard("13006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(offer.Id) with
            { Targets = [minion.ObjectId, minion.ObjectId, minion.ObjectId, minion.ObjectId] }, []);
        while (Sequence.Work(world, Cards, runner, []) is { } response)
            Sequence.Answer(world, Cards, runner, response, Decision.Decline, []);
        // Hydra Bomber has 2 hit points; 4 dealt leaves 2 excess.
        Assert.Equal(3, main.Tokens["k_threat"]);
    }
}
