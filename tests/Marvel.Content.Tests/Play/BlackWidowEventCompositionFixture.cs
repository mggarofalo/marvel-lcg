using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Content.Tests.Play;

// Full printed event interpretations are test-local; this fixture does not admit the pack.
internal static class BlackWidowEventCompositionFixture
{
    internal static AbilityRunner Runner(bool conditional = false)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        foreach (var row in JsonNode.Parse(Rows)!["cards"]!.AsArray())
            book["cards"]!.AsArray().Add(row!.DeepClone());
        if (conditional)
        {
            var row = book["cards"]!.AsArray().Single(card => card!["card"]!.GetValue<string>() == "08003")!;
            var effects = row["abilities"]![0]!["effect"]!["seq"]!.AsArray();
            var choice = effects[0]!.DeepClone();
            effects[0] = new JsonObject { ["if"] = new JsonObject
            {
                ["test"] = JsonNode.Parse("{\"exists\":{\"query\":\"thwartableSchemes\"}}"),
                ["then"] = choice,
            } };
        }
        return new(AbilityCatalog.Parse(book.ToJsonString()));
    }

    internal static World Board(bool hero = true)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("08001a,08001b", seat.Hero);
        if (!hero) seat.IdentityCard.TurnTo("08001b");
        world.Abilities = Runner();
        return world;
    }

    internal static Card PlayEvent(World world, string face)
    {
        var seat = world.Seats[1];
        var card = world.CreateCard(face, seat.Hand);
        int[] payment = Enumerable.Range(0, 3)
            .Select(_ => world.CreateCard("01093", seat.Hand).ObjectId).ToArray();
        ((AbilityRunner)world.Abilities!).ExecuteAction(world,
            new PlayerAction(new PendingAbility(card.ObjectId, AbilityType.Action, 1), payment, []));
        return card;
    }

    internal const string Rows = """
        { "cards": [
          { "card": "08001a", "abilities": [] },
          { "card": "08003", "abilities": [{
            "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "this" },
            "labels": ["thwart"],
            "effect": { "seq": [
              { "chooseCard": { "from": { "query": "thwartableSchemes" }, "effect": {
                "thwart": { "target": "chosen", "effect": { "seq": [
                  { "removeThreat": { "scheme": "chosen", "amount": 4 } },
                  { "giveStatus": { "card": { "query": "villain" }, "status": "confused" } }
                ] } }
              } } }
            ] }
          }] },
          { "card": "08004", "abilities": [{
            "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "this", "form": "hero" },
            "effect": { "seq": [
              { "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": {
                "attack": { "target": "chosen", "effect": {
                  "dealAttackDamage": { "cards": "chosen", "amount": 1 }
                } }
              } } },
              { "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": {
                "attack": { "target": "chosen", "effect": {
                  "dealAttackDamage": { "cards": "chosen", "amount": 2 }
                } }
              } } },
              { "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": {
                "attack": { "target": "chosen", "effect": {
                  "dealAttackDamage": { "cards": "chosen", "amount": 3 }
                } }
              } } }
            ] }
          }] }
        ] }
        """;
}









