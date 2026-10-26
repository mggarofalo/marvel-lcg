using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ShrinkPaymentPreflightTests
{
    private const string Draw = """{"draw":{"player":"you","count":1}}""";
    private const string Removal = """{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":2}}""";
    private const string Uncertain = """
        {"seq":[{"draw":{"player":"you","count":1}},
          {"if":{"test":{"inForm":{"player":"you","form":"hero"}},
            "then":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":2}},
            "else":{"draw":{"player":"you","count":1}}}}]}
        """;

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05011","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "when":{"hasTrait":{"card":"trigger.subject","trait":"THWART"}},
          "cost":{"exhaust":"this"},
          "effect":{"modifyPlayedEvent":{"quantity":"threatRemoval","amount":2}}
        }]},{"card":"05004","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":BODY
        }]}]}
        """.Replace("BODY", effect, StringComparison.Ordinal)));

    private static string Conditional(string then, string otherwise) =>
        """{"if":{"test":{"paidWithResource":"Y"},"then":THEN,"else":ELSE}}"""
            .Replace("THEN", then, StringComparison.Ordinal)
            .Replace("ELSE", otherwise, StringComparison.Ordinal);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BothPaymentBranchesAreCheckedBeforeAnyCosts(bool uncertainThen)
    {
        // Synthetic event bodies exercise the engine's conservative preflight
        // policy; they do not admit Ms. Marvel's pack to public play.
        var runner = Runner(Conditional(uncertainThen ? Uncertain : Removal,
            uncertainThen ? Draw : Uncertain));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("05004", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();

        var error = Assert.Throws<RulesNotImplementedException>(() =>
            runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])));

        Assert.Contains("before costs", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Same(world.Seats[0].Hand, card.Area);
        Assert.Same(world.Seats[0].Hand, payment.Area);
        Assert.True(upgrade.Ready);
        Assert.Empty(world.Effects.Registered);
    }

    [Theory]
    [InlineData("01088", true)]
    [InlineData("01089", false)]
    public void SupportedPaymentBranchesUseTheSettledReceipt(string resource, bool removesThreat)
    {
        var runner = Runner(Conditional(Removal, Draw));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("05004", world.Seats[0].Hand);
        var payment = world.CreateCard(resource, world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 10);
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);

        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        if (removesThreat)
        {
            var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Assert.Equal(upgrade.ObjectId, Assert.Single(interrupt.Affordances).AnchorId);
            Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(interrupt.Affordances[0].Id), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.Equal(before - (removesThreat ? 4 : 0), scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(!removesThreat, upgrade.Ready);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventThreatRemoval");
    }
}
