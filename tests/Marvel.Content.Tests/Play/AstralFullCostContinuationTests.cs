using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual 09003 printed body in a nonpublic fixture; resource/interrupt rows are synthetic.
public sealed class AstralFullCostContinuationTests
{
    private const string Body = """
        {"seq":[{"and":[
          {"removeThreat":{"scheme":"chosen","amount":3}},
          {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}}
        ]},{"removeThreat":{"scheme":"chosen","amount":{"result":"boostIconsLookedAt"}}}]}
        """;

    [Theory]
    [InlineData("unchanged", false, false)]
    [InlineData("unchanged", true, false)]
    [InlineData("departed", false, false)]
    [InlineData("departed", true, false)]
    [InlineData("reentered", false, false)]
    [InlineData("reentered", true, false)]
    [InlineData("unchanged", false, true)]
    [InlineData("departed", false, true)]
    [InlineData("reentered", false, true)]
    [Rule("rr:and.1")]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:confuse-confused.5")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:initiating-abilities.step.5")]
    public void FullPrintedBodyRetainsTheCostChoiceThroughPaymentSuspension(
        string movement, bool inspectFirst, bool confused)
    {
        // Effects connected by "and" "are not dependent on each other".
        // A departed card is "a new copy". Confusion still requires costs to
        // "be paid" and cancels "all effects" of the labeled ability.
        var runner = Runner();
        Card source = null!, generator = null!, chosen = null!, other = null!, top = null!;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("09003", board.Seats[0].Hand);
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea,
                PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            chosen = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            other = board.CreateCard("01107", chosen.Area);
            chosen.PlaceTokens("k_threat", 8);
            other.PlaceTokens("k_threat", 9);
            top = board.CreateCard("01107", board.AreaOf(DeckType.EncounterDeck));
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        var deck = top.Area;
        int[] deckOrder = deck.Cards.Select(card => card.ObjectId).ToArray();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long mainThreat = main.Tokens.GetValueOrDefault("k_threat");
        if (confused) Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var action = Assert.Single(runner.Actions(world, 0), entry => entry.Card == source.ObjectId);
        var result = runner.ExecuteAction(world,
            new PlayerAction(action, [generator.ObjectId], [chosen.ObjectId]));
        var events = result.Events.ToList();
        var payment = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Opportunity, payment.Asking);
        Assert.False(result.IsComplete);
        Assert.Equal(8, chosen.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        var schemeArea = chosen.Area;
        int incarnation = chosen.Incarnation;
        if (movement != "unchanged")
        {
            World.MoveToTop(chosen, world.AreaOf(DeckType.EncounterDiscardPile));
            if (movement == "reentered")
            {
                World.MoveToTop(chosen, schemeArea);
                chosen.PlaceTokens("k_threat", 8);
                Assert.NotEqual(incarnation, chosen.Incarnation);
            }
        }
        Sequence.Answer(world, world.Facts, runner, payment, Decision.Decline, events);
        if (!confused)
        {
            var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
            Assert.Equal(Question.Order, order.Asking);
            Sequence.Answer(world, world.Facts, runner, order,
                Decision.Take(order.Affordances[0].Id, inspectFirst ? [1, 0] : [0, 1], []), events);
            var look = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
            Assert.Equal(PublicDecisionKind.CardLook, look.PublicKind);
            Assert.Equal(0, look.Player);
            Assert.True(look.ExposesConcealedCandidates);
            Assert.Equal(new[] { top.ObjectId }, look.ContextCardIds);
            Assert.False(top.FaceUp);
            Assert.Equal(RemainingThreat(movement, inspectFirst ? 8 : 5),
                chosen.Tokens.GetValueOrDefault("k_threat"));
            Sequence.Answer(world, world.Facts, runner, look,
                Decision.Take(Assert.Single(look.Affordances).Id), events);
        }
        else Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(RemainingThreat(movement, confused ? 8 : 3),
            chosen.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(9, other.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(mainThreat, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(deckOrder, deck.Cards.Select(card => card.ObjectId));
        Assert.False(top.FaceUp);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.DoesNotContain(events, entry => entry.Verb == "Choose");
    }

    private static int RemainingThreat(string movement, int unchanged) => movement switch
    {
        "unchanged" => unchanged,
        "reentered" => 8,
        _ => 0,
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ImmediatePaymentCompletesThePrintedThwartInEitherOrder(bool inspectFirst)
    {
        // Printed 09003: remove 3, look at one encounter card, then remove its
        // numeric boost icons from the chosen scheme. 01107 has two such icons.
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 8);
        var source = world.CreateCard("09003", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var top = world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck));
        var action = Assert.Single(runner.Actions(world, 0), entry => entry.Card == source.ObjectId);
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [scheme.ObjectId])).Events.ToList();
        var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Sequence.Answer(world, world.Facts, runner, order,
            Decision.Take(order.Affordances[0].Id, inspectFirst ? [1, 0] : [0, 1], []), events);
        var look = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(PublicDecisionKind.CardLook, look.PublicKind);
        Assert.Equal(inspectFirst ? 8 : 5, scheme.Tokens.GetValueOrDefault("k_threat"));
        Sequence.Answer(world, world.Facts, runner, look,
            Decision.Take(Assert.Single(look.Affordances).Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, top.Area.Type);
        Assert.False(top.FaceUp);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("count", "2")]
    [InlineData("result", "treacheriesLookedAt")]
    [InlineData("tail", "discard")]
    public void NearbyUnsupportedBodiesAreRefusedBeforePayment(string change, string value)
    {
        var body = JsonNode.Parse(Body)!;
        if (change == "tail") body["seq"]![1] = JsonNode.Parse("""{"discard":"chosen"}""");
        else body["seq"]![0]!["and"]![1]!["inspectEncounterTop"]![change] =
            change == "count" ? JsonValue.Create(int.Parse(value)) : JsonValue.Create(value);
        Assert.Contains("chooseScheme", Assert.Throws<AbilityException>(() => Runner(body.ToJsonString())).Message);
    }

    private static AbilityRunner Runner(string body = Body) =>
        DoctorStrangeAbilityFixture.WithReplacements(JsonNode.Parse($$$$"""
        [
          {"card":"09003","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
            "labels":["thwart"],"cost":{"chooseScheme":"chosen"},
            "effect":{"thwart":{"target":"chosen","effect":{{{{body}}}}}}
          }]},
          {"card":"01030","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
            "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"GG"}
          }]},
          {"card":"01092","abilities":[{
            "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
            "effect":{"draw":{"player":"you","count":1}}
          }]}
        ]
        """)!.AsArray());
}
