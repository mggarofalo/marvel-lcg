using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ShrinkThreatLimitTests
{
    private const string Shrink = """
        {"card":"05011","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "when":{"hasTrait":{"card":"trigger.subject","trait":"THWART"}},
          "cost":{"exhaust":"this"},"effect":{"modifyPlayedEvent":{"quantity":"threatRemoval","amount":2}}
        }]}
        """;

    private static AbilityRunner Runner(string card, string effect, string trigger) => new(AbilityCatalog.Parse(
        "{\"cards\":[" + Shrink + ",{\"card\":\"" + card + "\",\"abilities\":[{\"trigger\":" + trigger
        + ",\"effect\":" + effect + "}]}]}"));

    [Theory]
    [InlineData(2, 20, 4)]
    [InlineData(4, 20, 5)]
    [InlineData(5, 20, 5)]
    [InlineData(7, 20, 5)]
    [InlineData(4, 3, 3)]
    [Rule("rr:event.5")]
    public void PrintedMaximumAppliesAfterShrinksBonus(int damage, int threat, int removed)
    {
        // 10016: "for each damage you took from the attack (to a maximum of 5)".
        // FAQ 10016 / 88369b11361157e4: "the maximum of 5 applies" to Shrink.
        // The test completes a villain attack and binds the sole scheme;
        // this does not admit Hulk or Ms. Marvel to the production catalog.
        var runner = Runner("10016", """
            {"removeThreat":{"scheme":{"query":"mainScheme"},
              "amount":{"completedAttackDamageDealt":"game"},"maximum":5}}
            """, """{"event":"WhenAttackEnds","timing":"Response","subject":"game","form":"hero"}""");
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

    [Fact]
    [Rule("rr:prevent.2")]
    public void EmergencyPreventsOnlyOneThreatAndDoesNotOfferShrink()
    {
        // FAQ 05011 / ea46606ca85f46ab: "Emergency only prevents threat and does not remove any".
        // rr:prevent.2 reduces an effect's amount "before it is applied".
        // The synthetic placement isolates prevention from the villain activation.
        var runner = Runner("01085", """{"preventThreat":1}""",
            """{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"}""");
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
}
