using System.Text.Json.Nodes;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelShrinkReviewTests
{
    // The upgrade is the candidate's actual authored row; event fixtures below
    // isolate printed caps, prevention and active-body selection.
    private static string Shrink => JsonNode.Parse(AuthoredCards.Text)!["cards"]!.AsArray()
        .Single(row => row!["card"]!.GetValue<string>() == "05011")!.ToJsonString();

    private static AbilityRunner Runner(string card, string effect, string trigger) => new(AbilityCatalog.Parse(
        "{\"cards\":[" + Shrink + ",{\"card\":\"" + card + "\",\"abilities\":[{\"trigger\":" + trigger
        + ",\"effect\":" + effect + "}]}]}"));

    [Theory]
    [InlineData(2, 20, 4, false)]
    [InlineData(2, 20, 4, true)]
    [InlineData(4, 20, 5, false)]
    [InlineData(5, 20, 5, false)]
    [InlineData(7, 20, 5, false)]
    [InlineData(4, 3, 3, false)]
    [Rule("rr:event.5")]
    public void PrintedMaximumAppliesAfterShrinksBonus(int damage, int threat, int removed, bool conditional)
    {
        // 10016: "for each damage you took from the attack (to a maximum of 5)".
        // FAQ 10016 / 88369b11361157e4: "the maximum of 5 applies" to Shrink.
        // The test completes a villain attack and binds the sole scheme;
        // this does not admit Hulk or Ms. Marvel to the production catalog.
        string effect = """
            {"removeThreat":{"scheme":{"query":"mainScheme"},
              "amount":{"completedAttackDamageDealt":"game"},"maximum":5}}
            """;
        if (conditional) effect = """{"if":{"test":{"inForm":{"player":"you","form":"hero"}},"then":BODY,"else":{"preventThreat":1}}}""".Replace("BODY", effect, StringComparison.Ordinal);
        var runner = Runner("10016", effect, """{"event":"WhenAttackEnds","timing":"Response","subject":"game","form":"hero"}""");
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("10016", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", threat - scheme.Tokens.GetValueOrDefault("k_threat"));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack",
            Amount: damage - StateFields.Modified(world, villain, "attack", world.Facts, 1),
            Card: villain.ObjectId, Affects: villain.ObjectId));
        // A zero-icon boost fixture isolates the requested attack damage.
        world.CreateCard("01088", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        Prompt? response = null;
        for (int count = 0; count < 8; count++)
        {
            var next = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            if (next.Affordances.Any(offer => offer.AnchorId == card.ObjectId)) { response = next; break; }
            Sequence.Answer(world, world.Facts, runner, next, Decision.Decline, []);
        }
        Assert.NotNull(response);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == card.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, PaidDecision.Take(offer, [payment.ObjectId]), []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(upgrade.ObjectId, Assert.Single(interrupt.Affordances).AnchorId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(interrupt.Affordances[0].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(threat - removed, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(upgrade.Ready);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventThreatRemoval");
    }

    [Fact]
    public void NegativeMaximumIsRejectedBeforeExecution()
    {
        var error = Assert.Throws<AbilityException>(() => Runner("10016",
            """{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":2,"maximum":-1}}""",
            """{"event":"WhenActionTriggered","timing":"Action","subject":"this"}"""));
        Assert.Contains("maximum", error.Message);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Rule("rr:prevent.2")]
    public void EmergencyPreventsOnlyOneThreatAndDoesNotOfferShrink(bool conditional, bool otherAbility)
    {
        // FAQ 05011 / ea46606ca85f46ab: "Emergency only prevents threat and does not remove any".
        // rr:prevent.2 reduces an effect's amount "before it is applied".
        // The synthetic placement isolates prevention from the villain activation.
        string effect = conditional
            ? """{"if":{"test":{"inForm":{"player":"you","form":"hero"}},"then":{"preventThreat":1},"else":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":2}}}}"""
            : """{"preventThreat":1}""";
        var runner = Runner("01085", effect,
            """{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"}""");
        if (otherAbility)
        {
            string body = "{\"cards\":[" + Shrink + """
                ,{"card":"01085","abilities":[
                  {"trigger":{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"},"effect":{"preventThreat":1}},
                  {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
                   "effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":2}}}]}]}
                """;
            runner = new AbilityRunner(AbilityCatalog.Parse(body));
        }
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var emergency = world.CreateCard("01085", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        Threat.Schedule(world, new([scheme], null, 3, ThreatCause.CardAbility, "synthetic", 0));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offered = Assert.Single(prompt.Affordances, offer => offer.AnchorId == emergency.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offered.Id), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(before + 2, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(upgrade.Ready);
        Assert.Equal(DeckType.DiscardPile, emergency.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventThreatRemoval");
    }

    [Fact]
    public void UncertainLaterConditionalIsRejectedBeforeEventCosts()
    {
        var runner = Runner("05004", """
            {"seq":[{"draw":{"player":"you","count":1}},
              {"if":{"test":{"inForm":{"player":"you","form":"hero"}},
                "then":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":2}},"else":{"preventThreat":1}}}]}
            """, """{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"}""");
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("05004", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])));
        Assert.Contains("before costs", error.Message);
        Assert.Same(world.Seats[0].Hand, card.Area);
        Assert.Same(world.Seats[0].Hand, payment.Area);
        Assert.True(upgrade.Ready);
    }

    [Fact]
    public void ExhaustedShrinkDoesNotRefuseAnOtherwiseValidLaterConditional()
    {
        var runner = Runner("05004", """
            {"seq":[{"draw":{"player":"you","count":1}},
              {"if":{"test":{"inForm":{"player":"you","form":"hero"}},
                "then":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":2}},"else":{"preventThreat":1}}}]}
            """, """{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"}""");
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        upgrade.Exhaust();
        var card = world.CreateCard("05004", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 10);
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before - 2, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(upgrade.Ready);
    }
}
