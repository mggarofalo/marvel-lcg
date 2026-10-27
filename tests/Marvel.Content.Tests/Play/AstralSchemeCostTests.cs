using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic 09003 cost probes. The complete simultaneous inspection body has
// separate power-continuation prerequisites; no production row is admitted here.
public sealed class AstralSchemeCostTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("nonscheme")]
    [InlineData("departed")]
    [Rule("rr:cost.1")]
    [Rule("rr:initiating-abilities.step.5")]
    public void InvalidSchemeCommitmentsPreserveStateBeforeConfusedEventPayment(string invalid)
    {
        // "Pay the cost(s)" precedes initiation. The arrow "distinguishes a cost
        // from an effect"; confusion cannot waive the printed scheme choice.
        var (world, runner, source, payment) = Board();
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var side = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 4);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        if (invalid == "departed") World.MoveToTop(side, world.AreaOf(DeckType.EncounterDiscardPile));
        int[] chosen = invalid switch
        {
            "missing" => [],
            "duplicate" => [scheme.ObjectId, scheme.ObjectId],
            "nonscheme" => [payment.ObjectId],
            _ => [side.ObjectId],
        };
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var current = world.Agenda.Current;
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], chosen)));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(current, world.Agenda.Current);
        Assert.Same(world.Seats[0].Hand, source.Area);
        Assert.Same(world.Seats[0].Hand, payment.Area);
        // The same printed price succeeds when its arrow choice is complete.
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [scheme.ObjectId])).Events.ToList();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(8, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:confuse-confused.5")]
    public void PrintedPriceAndExactSchemeChoicePrecedeThwartOrConfusion(bool confused)
    {
        // "Costs associated with the thwart attempt ... must still be paid."
        var (world, runner, source, payment) = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        var side = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 6);
        if (confused) Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        for (int repeat = 0; repeat < 3; repeat++)
        {
            var offer = runner.Describe(world, action);
            Assert.Equal(1, offer.Targets!.Min);
            Assert.Equal(1, offer.Targets.Max);
            Assert.Equal(new[] { main.ObjectId, side.ObjectId }, offer.Targets.Legal);
            Assert.Contains("Choose one scheme", offer.CostDescription);
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [side.ObjectId])).Events.ToList();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(confused ? 6 : 3, side.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(8, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("{\"seq\":[{\"chooseScheme\":\"chosen\"},{\"exhaust\":\"you\"}]}", "{\"thwart\":{\"target\":\"chosen\",\"effect\":{\"removeThreat\":{\"scheme\":\"chosen\",\"amount\":3}}}}")]
    [InlineData("{\"chooseScheme\":\"chosen\"}", "{\"draw\":{\"player\":\"you\",\"count\":1}}")]
    [InlineData("{\"chooseScheme\":\"chosen\"}", "{\"thwart\":{\"target\":\"chosen\",\"effect\":{\"discard\":\"chosen\"}}}")]
    public void UnsupportedCostCompositionsAreRefusedDuringLowering(string cost, string effect)
    {
        Assert.Contains("chooseScheme", Assert.Throws<AbilityException>(() => Runner(cost, effect)).Message);
    }

    internal static AbilityRunner Runner(string cost = "{\"chooseScheme\":\"chosen\"}",
        string effect = "{\"thwart\":{\"target\":\"chosen\",\"effect\":{\"removeThreat\":{\"scheme\":\"chosen\",\"amount\":3}}}}") =>
        DoctorStrangeAbilityFixture.WithReplacements(JsonNode.Parse($$"""
            [{"card":"09003","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "labels":["thwart"],"cost":{{cost}},"effect":{{effect}}
            }]}]
            """)!.AsArray());

    private static (World World, AbilityRunner Runner, Card Source, Card Payment) Board()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner();
        world.Abilities = runner;
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 8);
        var source = world.CreateCard("09003", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        return (world, runner, source, payment);
    }
}
