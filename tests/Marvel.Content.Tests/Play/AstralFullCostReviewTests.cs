using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Production Astral on nonpublic boards; altered bodies are explicit synthetic controls.
public sealed class AstralFullCostReviewTests
{
    private const string Body = """
        {"seq":[{"and":[
          {"removeThreat":{"scheme":"chosen","amount":3}},
          {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}}
        ]},{"removeThreat":{"scheme":"chosen","amount":{"result":"boostIconsLookedAt"}}}]}
        """;

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [Rule("rr:and.1")]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:leaves-play.1")]
    public void PrintedBodyRetainsItsCopyThroughSchemeDefeatAndPrivateLook(
        bool reverseAuthoredPair, bool inspectFirst, bool reenterDuringLook)
    {
        // AND effects "are not dependent on each other". A side scheme with
        // no threat is "defeated and discarded"; reentry is "a new copy".
        var body = JsonNode.Parse(Body)!;
        if (reverseAuthoredPair)
        {
            var pair = body["seq"]![0]!["and"]!.AsArray();
            var removal = pair[0]!.DeepClone();
            pair[0] = pair[1]!.DeepClone();
            pair[1] = removal;
        }
        var runner = Runner(body);
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var chosen = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var originalArea = chosen.Area;
        int initialThreat = reenterDuringLook ? 8 : 3;
        chosen.PlaceTokens("k_threat", initialThreat);
        var other = world.CreateCard("01107", originalArea);
        other.PlaceTokens("k_threat", 9);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long mainThreat = main.Tokens.GetValueOrDefault("k_threat");
        var source = world.CreateCard("09003", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var top = world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck));
        int[] deckOrder = top.Area.Cards.Select(card => card.ObjectId).ToArray();
        var action = Assert.Single(runner.Actions(world, 0), entry => entry.Card == source.ObjectId);
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [chosen.ObjectId])).Events.ToList();
        var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        int inspection = reverseAuthoredPair ? 0 : 1;
        Sequence.Answer(world, world.Facts, runner, order,
            Decision.Take(order.Affordances[0].Id,
                inspectFirst ? [inspection, 1 - inspection] : [1 - inspection, inspection], []), events);
        var look = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(PublicDecisionKind.CardLook, look.PublicKind);
        Assert.Equal(new[] { top.ObjectId }, look.ContextCardIds);
        Assert.False(top.FaceUp);
        Assert.Equal(inspectFirst ? initialThreat : initialThreat - 3,
            chosen.Tokens.GetValueOrDefault("k_threat"));
        if (reenterDuringLook)
        {
            int incarnation = chosen.Incarnation;
            World.MoveToTop(chosen, world.AreaOf(DeckType.EncounterDiscardPile));
            World.MoveToTop(chosen, originalArea);
            chosen.PlaceTokens("k_threat", 8);
            Assert.NotEqual(incarnation, chosen.Incarnation);
        }
        Sequence.Answer(world, world.Facts, runner, look,
            Decision.Take(Assert.Single(look.Affordances).Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(reenterDuringLook ? 8 : 0, chosen.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(reenterDuringLook ? DeckType.SideSchemesArea : DeckType.EncounterDiscardPile,
            chosen.Area.Type);
        Assert.Equal(9, other.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(mainThreat, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(deckOrder, top.Area.Cards.Select(card => card.ObjectId));
        Assert.False(top.FaceUp);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.DoesNotContain(events, entry => entry.Verb == "Choose");
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("scheme")]
    [InlineData("tailScheme")]
    [InlineData("tailResult")]
    public void InspectionAdmissionRefusesNearbyUnprovedShapes(string change)
    {
        // Engine choice: only the complete printed 09003 inspection shape is admitted.
        var body = JsonNode.Parse(Body)!;
        var removal = body["seq"]![0]!["and"]![0]!["removeThreat"]!;
        var tail = body["seq"]![1]!["removeThreat"]!;
        if (change == "amount") removal["amount"] = 4;
        else if (change == "scheme") removal["scheme"] = JsonNode.Parse("""{"query":"mainScheme"}""");
        else if (change == "tailScheme") tail["scheme"] = JsonNode.Parse("""{"query":"mainScheme"}""");
        else tail["amount"] = JsonNode.Parse("""{"result":"treacheriesLookedAt"}""");
        Assert.Contains("chooseScheme", Assert.Throws<AbilityException>(() => Runner(body)).Message);
    }

    private static AbilityRunner Runner(JsonNode body) =>
        JsonNode.DeepEquals(body, JsonNode.Parse(Body))
            ? Marvel.Content.Tests.Cards.AuthoredCards.Runner() :
        DoctorStrangeAbilityFixture.WithReplacements(JsonNode.Parse($$$$"""
        [{"card":"09003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "labels":["thwart"],"cost":{"chooseScheme":"chosen"},
          "effect":{"thwart":{"target":"chosen","effect":{{{{body.ToJsonString()}}}}}}
        }]}]
        """)!.AsArray());
}
