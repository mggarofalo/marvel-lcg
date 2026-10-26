using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic faceup-interrupt controls; no expansion content admission.</summary>
public sealed class BoostCancellationIndependentReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1, 3)]
    [InlineData(-1, 1)]
    [Rule("rr:attack-enemy-activation.step.3")]
    public void EarlierFaceupInterruptChangesTheIconsCancelledAndDamage(int modifier, long expected)
    {
        // Ruling 937d6ff4f970a1da: Crest can change the counted icons before
        // Preemptive Strike, which deals additional damage for the added icon.
        // Synthetic modifier uses the faceup window to isolate this prerequisite.
        var runner = Runner("""
            ,{"card":"01092","abilities":[{
              "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain"},
              "cost":{"exhaust":"this"},
              "effect":{"grantUntil":{"card":"trigger.subject","keyword":"boost_const",
                "amount":MODIFIER,"until":"EndOfActivation"}}
            }]}
            """.Replace("MODIFIER", modifier.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        var world = Board(runner);
        var modifierSource = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var boost = world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, source, events);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == modifierSource.ObjectId).Id), events);
        prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.False(modifierSource.Ready);
        Assert.Equal(expected, Modified(world, boost, "boost_const"));
        Assert.Equal(expected, world.Agenda.Occurrence!.BoostIcons!.Remaining);
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(expected, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Null(world.Activation);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-3)]
    [Rule("rr:modifiers.4")]
    public void EarlierInterruptRemovingEveryIconWithdrawsCancellationWithoutSpending(int modifier)
    {
        // "A card cannot have 'negative' icons". Both zero and an over-reduced
        // total leave nothing to cancel; synthetic faceup effects isolate admission.
        var runner = Runner("""
            ,{"card":"01092","abilities":[{
              "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain"},
              "cost":{"exhaust":"this"},
              "effect":{"grantUntil":{"card":"trigger.subject","keyword":"boost_const",
                "amount":MODIFIER,"until":"EndOfActivation"}}
            }]}
            """.Replace("MODIFIER", modifier.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        var world = Board(runner);
        var modifierSource = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        // A second optional modifier keeps the faceup window open for inspection.
        world.CreateCard("01092", modifierSource.Area);
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, source, events);
        var receipt = world.Agenda.Occurrence!.BoostIcons!;
        Assert.Equal(2, receipt.Remaining);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == modifierSource.ObjectId).Id), events);
        var next = Sequence.Work(world, Cards, runner, events);
        Assert.NotNull(next);
        Assert.False(modifierSource.Ready);
        Assert.Equal(0, receipt.Remaining);
        Assert.DoesNotContain(next?.Affordances ?? [], a => a.AnchorId == source.ObjectId);
        Sequence.Answer(world, Cards, runner, next!, Decision.Decline, events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.Equal(DeckType.HandsArea, payment.Area.Type);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Null(world.Activation);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void SecondEventCannotCancelTheSameIconsAfterTheFirstEventCompletes()
    {
        // Ruling 33d8c914aa86d19f: "You can only resolve one of these cards per boost card."
        var runner = Runner();
        var world = Board(runner);
        var first = world.CreateCard("05014", world.Seats[0].Hand);
        var second = world.CreateCard("05014", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var spare = world.CreateCard("01089", world.Seats[0].Hand);
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, first, events);
        Assert.Contains(prompt.Affordances, a => a.AnchorId == second.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances, a => a.AnchorId == first.ObjectId).Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Equal(DeckType.HandsArea, second.Area.Type);
        Assert.Equal(DeckType.HandsArea, spare.Area.Type);
        Assert.Single(events.OfType<AttackCompleted>());
    }

    [Fact]
    [Rule("rr:activation.8")]
    public void BoostRequestedAttackRetainsItsOwnUncancelledCard()
    {
        // "The new activation does not resolve until the current activation is complete."
        var runner = Runner("""
            ,{"card":"02030","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}
            }]}
            """);
        var world = Board(runner);
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var later = world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var first = world.CreateCard("02030", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, source, events);
        Sequence.Answer(world, Cards, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id, [], [payment.ObjectId]), events);
        var defender = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Defender, defender.Asking);
        Sequence.Answer(world, Cards, runner, defender, Decision.Decline, events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(new[] { first.ObjectId, later.ObjectId }, events.OfType<BoostResolved>().Select(e => e.Card));
        Assert.Equal(new long[] { 0, 2 }, events.OfType<BoostResolved>().Select(e => e.Icons));
        Assert.Equal(new long?[] { 2, 4 }, events.OfType<AttackCompleted>().Select(e => e.DamageDealt));
        Assert.Null(world.Activation);
        Assert.Null(world.Attack);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void PaymentThatLeftHandIsRejectedWithoutConsumingTheFaceupWindow()
    {
        var control = ResolveAfterStalePayment(false);
        var probed = ResolveAfterStalePayment(true);
        Assert.Equal(control, probed);
    }

    private static string ResolveAfterStalePayment(bool reject)
    {
        var runner = Runner();
        var world = Board(runner);
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        var stale = world.CreateCard("01089", world.Seats[0].Hand);
        var valid = world.CreateCard("01089", world.Seats[0].Hand);
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, source, events);
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        World.MoveToTop(stale, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        string before = Snapshot();
        var occurrence = world.Agenda.Occurrence;
        var pending = world.Agenda.Outstanding.ToArray();
        if (reject)
        {
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner, prompt,
                new Decision(offer.Id, [], [stale.ObjectId]), events));
            Assert.Equal(before, Snapshot());
            Assert.Same(occurrence, world.Agenda.Occurrence);
            Assert.Equal(pending, world.Agenda.Outstanding);
            Assert.Equal(2, occurrence!.BoostIcons!.Remaining);
        }
        Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [valid.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.False(world.Agenda.IsBusy);
        return Snapshot();

        string Snapshot() => System.Text.Json.JsonSerializer.Serialize(new
        {
            Digest = world.Digest().Canonical(),
            Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
            world.Agenda.Stage, world.Activation, world.Attack,
            Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Subject, step.OccurrenceId, step.ActivationId }),
            Events = events.Select(e => System.Text.Json.JsonSerializer.Serialize(e, e.GetType())),
        });
    }
    [Fact]
    [Rule("rr:attack-enemy-activation.step.3.b")]
    public void CancelledBoostStillPutsItselfIntoPlayAndIsNotDiscarded()
    {
        // "Resolve any 'Boost' abilities". Synthetic Goblin Soldier-shaped Boost
        // moves its physical source into play after its numeric icons are cancelled.
        var runner = Runner("""
            ,{"card":"02023","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"putIntoPlay":{"card":"this","where":"engagedWithYou"}}
            }]}
            """);
        var world = Board(runner);
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var boost = world.CreateCard("02023", world.AreaOf(DeckType.EncounterDeck));
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(
            Marvel.Rules.Timing.EffectSource.LastingEffect, "boost_const", 1, Affects: boost.ObjectId,
            Lasts: Marvel.Rules.Timing.Duration.UntilEndOf(Marvel.Rules.Timing.TimingPoints.EndOfActivation)));
        long expected = Modified(world, boost, "boost_const");
        Assert.True(expected > 0);
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, source, events);
        Sequence.Answer(world, Cards, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(expected, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, boost.Area.Type);
        Assert.Equal(0, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.False(world.Agenda.IsBusy);
    }
    private static AbilityRunner Runner(string extra = "") => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05014","abilities":[{
          "labels":["Defense"],
          "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain","form":"hero"},
          "effect":{"then":{"effect":{"cancelBoostIcons":"trigger"},
            "then":{"dealDamage":{"cards":"trigger.actor","amount":{"result":"cancelledBoostIcons"}}}}}
        }]}
        """ + extra + "]}"));

    private static World Board(AbilityRunner runner)
    {
        var world = Hero("03001a");
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01089", world.Seats[0].Deck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static Prompt Faceup(World world, AbilityRunner runner, Card source, List<GameEvent> events)
    {
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            Assert.NotNull(prompt);
            if (prompt.Affordances.Any(a => a.AnchorId == source.ObjectId)) return prompt;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        throw new InvalidOperationException("faceup interrupt was not offered");
    }
}
