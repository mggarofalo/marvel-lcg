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
    public void ThreatRemovalUsesTheInitiatingRunnersThreatAbilities()
    {
        // Admission and resolution both belong to the runner passed to Act.
        // The world's aggregate can describe another game and must not decide
        // whether this card removes threat.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}
            }]}]}
            """));
        var (world, source) = OrderedPaymentBoard(runner);
        var scheme = world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        world.Abilities = new UnrelatedPaymentAbilities();

        runner.Act(world, Assert.Single(
            runner.Actions(world, 0), option => option.Card == source.ObjectId), [], []);

        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void PaymentUsesTheInitiatingRunnersResourceAbilities()
    {
        // Composed-path counterpart for the component probes below: action
        // admission, payment preparation, commitment, and the effect all use
        // the initiating runner even when World exposes an unrelated port.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"spend":"Y"},
                "effect":{"placeCounters":{"card":"you","counter":"test","count":1}}
              }]},
              {"card":"01091","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"this"},
                "effect":{"generate":"Y"}
              }]}
            ]}
            """));
        var (world, source) = OrderedPaymentBoard(runner);
        var generator = InPlay(world, "01091");
        world.Abilities = new UnrelatedPaymentAbilities();

        var action = Assert.Single(
            runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.Act(world, action, [generator.ObjectId], []);

        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
        Assert.True(generator.Ready);
    }

    [Fact]
    public void DamageCostEligibilityUsesTheInitiatingRunnersProgram()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01006","abilities":[
              {"trigger":{"event":"Probe","timing":"Response","subject":"game"},
               "cost":{"takeDamage":{"cards":"this","amount":1}},
               "effect":{"placeCounters":{"card":"you","counter":"test","count":1}}},
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"preventDamageWhile":{"card":"this","condition":{
                 "atLeast":{"value":{"damageOn":"this"},"count":1}}}}}
            ]}]}
            """));
        var (world, source) = OrderedPaymentBoard(runner);
        source.TakeDamage(1);
        world.Abilities = new UnrelatedPaymentAbilities();
        var occurrence = new Occurrence(0, ["Probe"], Subject: source.ObjectId, Player: 0);

        Assert.Throws<RulesNotImplementedException>(() => runner.Resolve(
            world, occurrence,
            new PendingAbility(source.ObjectId, AbilityType.Response, 0), [], []));

        Assert.Equal(1, source.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
    }

    [Fact]
    public void PaymentCommitsTheSelectedCardsCapturedBeforeResourceGeneration()
    {
        // Engine choice: a caller-owned answer is snapshotted before commitment.
        // A resource callback cannot rewrite which ally the player selected.
        // The compiled action is the initiation input, AbilityCostPayment.Prepare
        // captures it, and its explicit resource port runs during Commit before
        // the captured selection is exhausted.
        var (program, runner) = PaymentRunner(AuthoredCards.AuntMay,
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
        var ability = Assert.Single(program.On(source.FaceId));
        var payment = AbilityCostPayment.Prepare(
            world, source, 0, ability.Cost, [generator.ObjectId], chosen, program, probe);
        var events = new List<GameEvent>();

        payment.Commit(runner, Steps.TurnAction, events);

        Assert.Equal(other.ObjectId, Assert.Single(chosen));
        Assert.False(selected.Ready);
        Assert.True(other.Ready);
        Assert.Equal(1, probe.Uses);
        Assert.Single(events.OfType<FieldSet>(), change =>
            change.Card == selected.ObjectId && change.Field == "is_exhaust");
    }

    [Rule("rr:leaves-play.1")]
    [Fact]
    public void PaymentDoesNotBindThisToASourceThatReenteredDuringResourceGeneration()
    {
        // A returning card is "a new copy of the card." The prepared exhaust
        // still names the incarnation that initiated, not the returning copy.
        // AbilityCostPayment is the explicit preparation/commit boundary: the
        // resource callback reenters the source during Commit, after its original
        // incarnation was captured at initiation.
        var (program, runner) = PaymentRunner(AuthoredCards.AuntMay,
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """{"seq":[{"spend":"Y"},{"exhaust":"this"}]}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var generator = InPlay(world, "01091");
        var area = source.Area;
        int incarnation = source.Incarnation;
        var probe = new PaymentResourceProbe(generator, "Y", () =>
        {
            Discard.Card(world, source, CardPlay.Verb, []);
            World.MoveToTop(source, area);
        });
        var ability = Assert.Single(program.On(source.FaceId));
        var payment = AbilityCostPayment.Prepare(
            world, source, 0, ability.Cost, [generator.ObjectId], [], program, probe);
        var events = new List<GameEvent>();

        payment.Commit(runner, Steps.TurnAction, events);

        Assert.Equal(incarnation + 1, source.Incarnation);
        Assert.True(source.Ready);
        Assert.DoesNotContain(events.OfType<FieldSet>(), change =>
            change.Card == source.ObjectId && change.Field == "is_exhaust");
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
        // AbilityCostPayment.Prepare combines the initiated sequence; Commit
        // invokes the explicit resource port once before the prepared exhaust.
        var (program, runner) = PaymentRunner(AuthoredCards.AuntMay,
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """{"seq":[{"exhaust":"this"},{"spend":"Y"},{"spend":"Y"}]}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var generator = InPlay(world, "01091");
        var probe = new PaymentResourceProbe(generator, "YY", () => Assert.True(source.Ready));
        var ability = Assert.Single(program.On(source.FaceId));
        var payment = AbilityCostPayment.Prepare(
            world, source, 0, ability.Cost, [generator.ObjectId], [], program, probe);
        var events = new List<GameEvent>();

        payment.Commit(runner, Steps.TurnAction, events);

        Assert.Equal(1, probe.Uses);
        Assert.False(source.Ready);
        Assert.Single(events.OfType<FieldSet>(), change =>
            change.Card == source.ObjectId && change.Field == "is_exhaust");
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
        // The initiated event is prepared at both explicit boundaries. Its
        // AbilityEventPayment commits the resource callback first, then the
        // AbilityCostPayment commits the live damage component.
        var (program, runner) = PaymentRunner(AuthoredCards.Backflip,
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
        var probe = new PaymentResourceProbe(generator, "Y", () =>
        {
            if (removeModifier) world.Effects.Use(modifier);
        });
        var occurrence = new Occurrence(0, [Steps.TurnAction], Subject: source.ObjectId, Player: 0);
        var ability = Assert.Single(program.On(source.FaceId));
        var arrowPayment = AbilityCostPayment.Prepare(
            world, source, 0, ability.Cost, [generator.ObjectId], [], program, probe,
            resourcesPaidByEvent: true);
        var eventPayment = Assert.IsType<AbilityEventPayment>(AbilityEventPayment.Prepare(
            world, source, 0, [generator.ObjectId], ability.Effect, probe,
            allocations: [new ResourceAllocation(generator.ObjectId, Cost: 1, PaidAs: "Y")],
            additionalCost: ability.Cost));
        var events = new List<GameEvent>();

        eventPayment.Commit(occurrence, events);
        arrowPayment.Commit(runner, Steps.TurnAction, events);

        Assert.Equal(removeModifier ? 1 : 3, world.Seats[0].IdentityCard.Damage);
        Assert.False(occurrence.Is(Steps.DamageDealt));
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        Assert.Equal(1, probe.Uses);
        int moved = events.FindIndex(happened => happened is CardsMoved);
        int damaged = events.FindIndex(happened => happened is FieldSet change
            && change.Card == world.Seats[0].IdentityCard.ObjectId
            && change.Field == "health");
        Assert.True(moved >= 0);
        Assert.True(damaged > moved);
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
        // AbilityCostPayment.Prepare captures the initiated X declaration;
        // its explicit resource callback mutates the caller dictionary during
        // Commit, which must still return the prepared paid energy.
        var (program, runner) = PaymentRunner(AuthoredCards.AuntMay,
            """{"placeCounters":{"card":"this","counter":"test","count":{"result":"energy"}}}""",
            cost: """{"spendEnergyX":"Y"}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var generator = InPlay(world, "01091");
        var values = new Dictionary<string, long>(StringComparer.Ordinal) { ["X"] = declared };
        var probe = new PaymentResourceProbe(generator, "YY", () => values["X"] = 99);
        var ability = Assert.Single(program.On(source.FaceId));
        string before = world.Digest().Canonical();

        if (declared == 3)
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                AbilityCostPayment.Prepare(
                    world, source, 0, ability.Cost, [generator.ObjectId], [], program, probe,
                    values));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(0, probe.Uses);
            return;
        }

        var payment = AbilityCostPayment.Prepare(
            world, source, 0, ability.Cost, [generator.ObjectId], [], program, probe, values);
        var result = payment.Commit(runner, Steps.TurnAction, []);

        Assert.Equal(99, values["X"]);
        Assert.Equal(declared, result.Energy);
        Assert.Equal(1, probe.Uses);
    }

    private static (AbilityProgram Program, AbilityRunner Runner) PaymentRunner(
        string card, string effect, string cost)
    {
        var book = AbilityCatalog.Parse(
            $$"""
            {"cards":[{"card":"{{card}}","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{{cost}},
              "effect":{{effect}}
            }]}]}
            """);
        var program = AbilityLowering.Book(book);
        return (program, new AbilityRunner(program));
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

    private sealed class UnrelatedPaymentAbilities : NoCardAbilities
    {
        public override bool CanTakeDamage(World world, Card target, Card source) =>
            throw new InvalidOperationException("the unrelated World damage port was used");

        public override bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1) =>
            throw new InvalidOperationException("the unrelated World threat port was used");

        public override IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) =>
            throw new InvalidOperationException("the unrelated World resource port was used");

        public override string ResourcesGeneratedBy(World world, Card source, Card? payingFor) =>
            throw new InvalidOperationException("the unrelated World resource port was used");

        public override string UseResource(
            World world, int player, int card, List<GameEvent> events) =>
            throw new InvalidOperationException("the unrelated World resource port was used");
    }
}
