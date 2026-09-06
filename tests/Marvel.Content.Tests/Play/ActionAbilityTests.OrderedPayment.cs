using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Fact]
    public void PaymentCommitsTheSelectedCardsCapturedBeforeResourceGeneration()
    {
        // Engine choice: a caller-owned answer is snapshotted before commitment.
        // A resource callback cannot rewrite which ally the player selected.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """
                {"seq":[{"spend":"Y"},{"exhaustChosen":{
                  "from":{"query":"alliesYouControl"},"count":1}}]}
                """);
        var (world, source) = OrderedPaymentBoard(runner);
        var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var selected = world.CreateCard(AuthoredCards.BlackCat, allies);
        var other = world.CreateCard("01066", allies);
        List<int> chosen = [selected.ObjectId];
        var generator = InPlay(world, "01091");
        var probe = new PaymentResourceProbe(generator, "Y", () => chosen[0] = other.ObjectId);
        world.Abilities = probe;
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);

        var events = runner.Act(world, action, [generator.ObjectId], chosen);

        Assert.Equal(other.ObjectId, Assert.Single(chosen));
        Assert.False(selected.Ready);
        Assert.True(other.Ready);
        Assert.Equal(1, probe.Uses);
        Assert.Single(events.OfType<FieldSet>(), change =>
            change.Card == selected.ObjectId && change.Field == "is_exhaust");
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
    }

    [Rule("rr:leaves-play.1")]
    [Fact]
    public void PaymentDoesNotBindThisToASourceThatReenteredDuringResourceGeneration()
    {
        // A returning card is "a new copy of the card." The prepared exhaust
        // still names the incarnation that initiated, not the returning copy.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """{"seq":[{"spend":"Y"},{"exhaust":"this"}]}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var generator = InPlay(world, "01091");
        var area = source.Area;
        int incarnation = source.Incarnation;
        world.Abilities = new PaymentResourceProbe(generator, "Y", () =>
        {
            Discard.Card(world, source, CardPlay.Verb, []);
            World.MoveToTop(source, area);
        });
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);

        var events = runner.Act(world, action, [generator.ObjectId], []);

        Assert.Equal(incarnation + 1, source.Incarnation);
        Assert.True(source.Ready);
        Assert.DoesNotContain(events.OfType<FieldSet>(), change =>
            change.Card == source.ObjectId && change.Field == "is_exhaust");
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
    }

    [Rule("rr:uses-x-type.1")]
    [Theory]
    [InlineData("Action", true)]
    [InlineData("Action", false)]
    [InlineData("Response", true)]
    [InlineData("Resource", true)]
    public void CounterPaymentUsesTheExecutingInterpretersPool(string timing, bool uses)
    {
        // Uses says "If there are no all-purpose counters on this card,
        // discard this card." Ordinary starting counters have no such rule.
        // Engine choice: a component caller may supply a different interpreter
        // from the world's rules-only default; its authored pool still owns this cost.
        string effect = timing == "Resource" ? """{"generate":"Y"}"""
            : """{"placeCounters":{"card":"you","counter":"test","count":1}}""";
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"01006",
              "startingCounters":{"type":"test","count":1,"uses":{{{(uses ? "true" : "false")}}}},
              "abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"{{{timing}}}","subject":"game"},
                "cost":{"removeCounters":{"card":"this","counter":"test","count":1}},
                "effect":{{{effect}}}
              }]}]}
            """));
        var (world, source) = OrderedPaymentBoard(runner);
        source.PlaceTokens("c_test", 1);
        world.Abilities = new NoCardAbilities();
        var events = new List<GameEvent>();

        if (timing == "Resource")
            Assert.Equal("Y", runner.UseResource(world, 0, source.ObjectId, events));
        else if (timing == "Response")
            events.AddRange(runner.Resolve(world, new Occurrence(0, ["WhenActionTriggered"], Player: 0),
                new PendingAbility(source.ObjectId, AbilityType.Response, 0), [], []));
        else
            events.AddRange(runner.Act(world,
                Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId), [], []));

        Assert.Equal(uses ? DeckType.DiscardPile : DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Single(events.OfType<FieldSet>(), change => change.Field == "c_test" && change.Card == source.ObjectId);
        Assert.Equal(timing == "Resource" ? 0 : 1,
            world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.3")]
    [Fact]
    public void OrderedPaymentCombinesResourceComponentsIntoOneGeneratorUse()
    {
        // Multiple costs "must be paid simultaneously"; a Resource ability
        // producing two icons is used once for the two components.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """{"seq":[{"exhaust":"this"},{"spend":"Y"},{"spend":"Y"}]}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var generator = InPlay(world, "01091");
        var probe = new PaymentResourceProbe(generator, "YY", () => Assert.True(source.Ready));
        world.Abilities = probe;

        runner.Act(world, Assert.Single(runner.Actions(world, 0)), [generator.ObjectId], []);

        Assert.Equal(1, probe.Uses);
        Assert.False(source.Ready);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void OrderedPaymentPreservesHealingResultsAndAuthoredPrimitiveOrder()
    {
        // "Pay the cost(s)" before resolving the effect. The engine orders
        // these non-resource components as authored and returns actual healing.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"placeCounters":{"card":"this","counter":"test","count":{"result":"healed"}}}""",
            cost: """
                {"seq":[{"dealDamage":{"cards":"you","amount":2}},
                  {"heal":{"card":"you","amount":2}}]}
                """);
        var (world, source) = OrderedPaymentBoard(runner);
        world.Seats[0].IdentityCard.TakeDamage(1);

        runner.Act(world, Assert.Single(runner.Actions(world, 0)), [], []);

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, source.Tokens.GetValueOrDefault("c_test"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EventDamageCostsReadLiveModifiersButAreNotAttacks(bool removeModifier)
    {
        // Engine payment context carries no basic-power frame. Event modifiers
        // apply at commitment, after resource abilities have run; attack-only
        // modifiers cannot turn paying a damage cost into an attack.
        var runner = Runner(AuthoredCards.Backflip, "Action",
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """{"seq":[{"spend":"Y"},{"dealDamage":{"cards":"you","amount":1}}]}""");
        var (world, _) = OrderedPaymentBoard(runner);
        var source = world.CreateCard(AuthoredCards.Backflip, world.Seats[0].Hand);
        var generator = InPlay(world, "01091");
        var modifier = new ContinuousEffect(EffectSource.LastingEffect, Kind: "eventDamage", Amount: 2,
            Card: generator.ObjectId, Affects: source.ObjectId, Lasts: new Duration(Uses: 1));
        world.Effects.Register(modifier);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "attackDamage", Amount: 100,
            Card: generator.ObjectId, Affects: source.ObjectId, Lasts: new Duration(Uses: 1)));
        world.Abilities = new PaymentResourceProbe(generator, "Y", () =>
        {
            if (removeModifier) world.Effects.Use(modifier);
        });
        var occurrence = new Occurrence(0, [Steps.TurnAction], Subject: source.ObjectId, Player: 0);

        runner.Act(world, Assert.Single(runner.Actions(world, 0)), [generator.ObjectId], [], occurrence,
            allocations: [new ResourceAllocation(generator.ObjectId, Cost: 1, PaidAs: "Y")]);

        Assert.Equal(removeModifier ? 1 : 3, world.Seats[0].IdentityCard.Damage);
        Assert.False(occurrence.Is(Steps.DamageDealt));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
    }

    [Rule("rr:cost.4.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VariablePaymentReturnsTheDeclaredEnergyNotItsOverpayment(int declared)
    {
        // Excess resources "were not paid for that cost." An unpayable X
        // must "abort this process without paying any costs."
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"placeCounters":{"card":"this","counter":"test","count":{"result":"energy"}}}""",
            cost: """{"spendEnergyX":"Y"}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var generator = InPlay(world, "01091");
        var values = new Dictionary<string, long>(StringComparer.Ordinal) { ["X"] = declared };
        var probe = new PaymentResourceProbe(generator, "YY", () => values["X"] = 99);
        world.Abilities = probe;
        var action = Assert.Single(runner.Actions(world, 0));
        string before = world.Digest().Canonical();

        if (declared == 3)
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                runner.Act(world, action, [generator.ObjectId], [], values: values));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(0, probe.Uses);
            return;
        }

        runner.Act(world, action, [generator.ObjectId], [], values: values);

        Assert.Equal(99, values["X"]);
        Assert.Equal(declared, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(1, probe.Uses);
    }

    private static (World World, Card Source) OrderedPaymentBoard(AbilityRunner runner)
    {
        var (world, source) = FixedCountBoard(runner);
        // Keep discard observations separate from the empty-player-deck procedure.
        world.CreateCard("01088", world.Seats[0].Deck);
        return (world, source);
    }

    private sealed class PaymentResourceProbe(Card generator, string resources, Action commit) : NoCardAbilities
    {
        internal int Uses { get; private set; }

        public override IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) =>
            [new(generator.ObjectId, resources)];

        public override string UseResource(World world, int player, int card, List<GameEvent> events)
        {
            Assert.Equal(generator.ObjectId, card);
            Uses++;
            commit();
            return resources;
        }
    }
}
