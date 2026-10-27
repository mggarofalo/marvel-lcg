using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local Power Gloves and a synthetic ally Action (thwart); no product admission.</summary>
internal sealed class AllyThwartCompletionFixture
{
    internal World World { get; }
    internal AbilityRunner Runner { get; }
    internal Card Ally { get; }
    internal Card Gloves { get; }
    internal Card Scheme { get; }
    internal Card Villain => World.TheCardIn(DeckType.VillainArea)!;

    internal AllyThwartCompletionFixture(bool followingEffect = false, bool nestedResponse = false, bool unlabeled = false)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = book["cards"]!.AsArray();
        // Printed 12017: "After attached ally attacks or thwarts, deal 1 damage to an enemy."
        // The trigger and selector spellings are engine choices.
        var response = """
            {"chooseCard":{"from":{"query":"enemies"},"effect":{
              "dealDamage":{"cards":"chosen","amount":1}
            }}}
            """;
        if (nestedResponse)
            response = """{"seq":[{"choose":{"options":[{"draw":{"player":"you","count":1}},{"draw":{"player":"you","count":2}}]}},""" + response + "]}";
        rows.Add(JsonNode.Parse($$$$"""
            {"card":"12017","attachTo":{"withoutAnotherCopyAttached":{"withTrait":{
              "cards":{"query":"allies"},"trait":"AVENGER"}}},"abilities":[
              {"trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"attachedTo"},"effect":{{{{response}}}}},
              {"trigger":{"event":"WhenThwartEnds","timing":"Response","actor":"attachedTo"},"effect":{{{{response}}}}}
            ]}
            """));
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01068"));
        const string thwart = """
            {"thwart":{"target":{"query":"mainScheme"},"effect":{
              "removeThreat":{"scheme":"chosen","amount":1}
            }}}
            """;
        var effect = followingEffect
            ? """{"seq":[""" + thwart + """,{"draw":{"player":"you","count":1}}]}"""
            : thwart;
        if (unlabeled) effect = """{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}""";
        var labels = unlabeled ? "" : "\"labels\":[\"thwart\"],";
        rows.Add(JsonNode.Parse($$$$"""
            {"card":"01068","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              {{{{labels}}}}"effect":{{{{effect}}}}
            }]}
            """));
        Runner = new(AbilityCatalog.Parse(book.ToJsonString()));
        World = ThorActualFixture.Board("06001a", 2);
        World.Abilities = Runner;
        Ally = World.CreateCard("01068", World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        // Owner 0, controller 1, performer the exact attached ally.
        Gloves = World.CreateCard("12017", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), Ally.ObjectId, 0));
        Scheme = World.TheCardIn(DeckType.MainSchemesArea)!;
        Scheme.PlaceTokens("k_threat", 3);
    }

    internal Prompt? Work() => Sequence.Work(World, World.Facts, Runner, []);
    internal void Answer(Prompt prompt, Decision decision) => Sequence.Answer(World, World.Facts, Runner, prompt, decision, []);

    internal Prompt BeginLabeledThwart()
    {
        Assert.Same(Ally, LabeledAbilities.Performer(World, World.Facts, 1, Ally));
        var action = Assert.Single(Runner.Actions(World, 1), item => item.Card == Ally.ObjectId);
        Runner.ExecuteAction(World, new PlayerAction(action, [], []));
        return Assert.IsType<Prompt>(Work());
    }
}
