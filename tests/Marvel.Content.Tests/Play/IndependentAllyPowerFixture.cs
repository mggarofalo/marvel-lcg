using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic powers and attachment response for independent timing review.</summary>
internal sealed class IndependentAllyPowerFixture
{
    internal World World { get; }
    internal AbilityRunner Runner { get; }
    internal Card Ally { get; }
    internal Card Gloves { get; }
    internal Card Scheme => World.TheCardIn(DeckType.MainSchemesArea)!;
    internal Card Villain => World.TheCardIn(DeckType.VillainArea)!;

    internal IndependentAllyPowerFixture(bool attack, bool nestedThwart = false)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = book["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01068"));
        string power = attack
            ? """{"attack":{"target":{"query":"villain"},"effect":{"dealAttackDamage":{"cards":"chosen","amount":1}}}}"""
            : """{"thwart":{"target":{"query":"mainScheme"},"effect":{"removeThreat":{"scheme":"chosen","amount":1}}}}""";
        // The follow-up choice makes the consequential/outer-continuation boundary observable.
        rows.Add(JsonNode.Parse($$$"""
            {"card":"01068","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "labels":["{{{(attack ? "attack" : "thwart")}}}"],
              "effect":{"seq":[{{{power}}},{"choose":{"options":[
                {"draw":{"player":"you","count":1}},
                {"draw":{"player":"you","count":2}}
              ]}}]}
            }]}
            """));
        // This attachment is synthetic: its host performs the nested thwart.
        string response = nestedThwart
            ? """{"thwart":{"target":{"query":"mainScheme"},"effect":{"removeThreat":{"scheme":"chosen","amount":1}}}}"""
            : """{"draw":{"player":"you","count":1}}""";
        rows.Add(JsonNode.Parse($$$"""
            {"card":"12017","abilities":[{
              "trigger":{"event":"{{{(attack ? "WhenAttackEnds" : "WhenThwartEnds")}}}","timing":"Response","actor":"attachedTo"},
              "effect":{{{response}}}
            }]}
            """));
        Runner = new(AbilityCatalog.Parse(book.ToJsonString()));
        World = ThorActualFixture.Board("06001a", 2);
        World.Abilities = Runner;
        Ally = World.CreateCard("01068", World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        Gloves = World.CreateCard("12017", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), Ally.ObjectId, 0));
        Scheme.PlaceTokens("k_threat", 5);
    }

    internal Prompt Begin()
    {
        var action = Assert.Single(Runner.Actions(World, 1), item => item.Card == Ally.ObjectId);
        Runner.ExecuteAction(World, new PlayerAction(action, [], []));
        return Assert.IsType<Prompt>(Work());
    }

    internal Prompt? Work() => Sequence.Work(World, World.Facts, Runner, []);
    internal void Answer(Prompt prompt, Decision decision) => Sequence.Answer(World, World.Facts, Runner, prompt, decision, []);
}
