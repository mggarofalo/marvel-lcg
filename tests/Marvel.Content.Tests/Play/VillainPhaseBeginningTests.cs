using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic phase-start abilities exercise the real phase scheduler.</summary>
public sealed class VillainPhaseBeginningTests : CoreCardTestBase
{
    private const string Begins = "WhenVillainPhaseBegins";

    [Fact]
    [Rule("rr:interrupt.3")]
    [Rule("rr:triggering-condition.1")]
    public void ForcedStartChoiceSuspendsBeforeThreatAndActivationsAndResumesOnce()
    {
        // An interrupt resolves "before that triggering condition resolves";
        // each ability triggers once per occurrence, including across a choice.
        var world = Board();
        world.FirstPlayer = 0;
        var source = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var runner = Runner("ForcedInterrupt");
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.NotEqual(Question.Defender, choice.Asking);
        Assert.Equal(0, choice.Player);
        Assert.Equal(0, Threat(world));
        Assert.DoesNotContain(events, e => e is AttackCompleted);
        Assert.Single(events.OfType<PhaseStarted>());
        Assert.Equal(choice.Player, Sequence.Work(world, Cards, runner, events)!.Player);
        Sequence.Answer(world, Cards, runner, choice,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), events);
        var defense = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Defender, defense.Asking);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(Threat(world) > 0);
        Assert.Single(events.OfType<PhaseStarted>());
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
    }

    [Fact]
    public void UnobservedStartPreservesOrdinaryPhaseEventsAndOccurrenceIdentity()
    {
        var world = Board();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""{"cards":[]}"""));
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        var defense = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Defender, defense.Asking);
        Assert.Single(events.OfType<PhaseStarted>());
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Begins);
        Assert.Equal(Moment.Id(1, 2, 0), world.Agenda.Occurrence!.Id);
        string transcript = string.Join("\n", events.Select(EventJson.Write)) + "\n"
            + world.Agenda.Occurrence.Id + "\n"
            + string.Join("\n", world.Agenda.Outstanding.Select(step =>
                $"{step.What}:{step.Round}:{step.Number}:{step.Index}:{step.Subject}:{step.Seat}:{step.ActivationId}"));
        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(transcript)));
        // Pinned from genuine b753c463 Sequence: complete events and remaining step coordinates.
        Assert.Equal("3EA267491A3FFBCE51FCC0E2D30E4874E002D1FCA80F3AE88AA7B4E8D563F732", hash);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    public void FirstPlayerOrdersSimultaneousStartAbilitiesAcrossSuspension(int selected)
    {
        // "the first player determines the order ... regardless of who controls"
        // and each forced ability resolves completely before the next initiates.
        var world = Board();
        world.FirstPlayer = 1;
        var first = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var runner = Runner("ForcedInterrupt");
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(1, order.Player);
        Assert.False(order.Cancellable);
        Assert.Equal(2, order.Affordances.Count);
        Sequence.Answer(world, Cards, runner, order, Decision.Take(order.Affordances[selected].Id), events);
        for (int count = 0; count < 2; count++)
        {
            var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            Assert.NotEqual(Question.Order, choice.Asking);
            Assert.NotEqual(Question.Defender, choice.Asking);
            Assert.Equal(1, choice.Player);
            Assert.Equal(0, Threat(world));
            Sequence.Answer(world, Cards, runner, choice,
                Decision.Take(world.Seats[count].IdentityCard.ObjectId), events);
        }
        Assert.Equal(Question.Defender, Sequence.Work(world, Cards, runner, events)!.Asking);
        Assert.All(world.Seats, seat => Assert.Equal(1, seat.IdentityCard.Damage));
        Assert.Single(events.OfType<PhaseStarted>());
    }

    [Theory]
    [InlineData("Interrupt")]
    [InlineData("Response")]
    public void OptionalStartAbilitiesKeepTheirTimingTierAndCanBeDeclined(string timing)
    {
        var world = Board();
        world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var runner = Runner(timing);
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        for (int i = 0; i < 2; i++)
        {
            var opportunity = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            Assert.Equal(Begins, opportunity.Trigger);
            Assert.True(opportunity.Cancellable);
            Assert.Equal(timing == "Interrupt" ? Marvel.Rules.Timing.TimingPriority.Interrupt
                : Marvel.Rules.Timing.TimingPriority.Response, opportunity.When);
            Assert.Equal(0, Threat(world));
            Sequence.Answer(world, Cards, runner, opportunity, Decision.Decline, events);
        }
        Assert.Equal(Question.Defender, Sequence.Work(world, Cards, runner, events)!.Asking);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Single(events.OfType<PhaseStarted>());
    }

    [Fact]
    [Rule("rr:forced.4")]
    public void ForcedStartInterruptCompletesBeforeOptionalStartInterruptIsOffered()
    {
        // "forced interrupts take priority and initiate before non-forced interrupts".
        var world = Board();
        var source = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"90001","abilities":[
              {"trigger":{"event":"WhenVillainPhaseBegins","timing":"ForcedInterrupt","subject":"game"},
               "effect":{"dealDamage":{"cards":"this","amount":1}}},
              {"trigger":{"event":"WhenVillainPhaseBegins","timing":"Interrupt","subject":"game"},
               "effect":{"dealDamage":{"cards":"this","amount":1}}}
            ]}]}
            """));
        VillainPhase.Schedule(world.Agenda, 1);
        var optional = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.True(optional.Cancellable);
        Assert.Equal(Marvel.Rules.Timing.TimingPriority.Interrupt, optional.When);
        Assert.Equal(1, source.Damage);
        Assert.Equal(0, Threat(world));
    }

    [Theory]
    [InlineData("ForcedInterrupt")]
    [InlineData("ForcedResponse")]
    [Rule("rr:interrupt.3")]
    [Rule("rr:response.1")]
    public void StartWindowCanIntroduceAnObserverOfStepOneCompletion(string timing)
    {
        // Interrupts resolve "before that triggering condition resolves"; a response
        // resolves "after its triggering condition has been resolved".
        // These synthetic bodies put a completion observer into play at phase start.
        var world = Hero("01001a,01001b");
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var observer = world.CreateCard("01103", world.AreaOf(DeckType.EncounterDiscardPile));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"90001","abilities":[{
                "trigger":{"event":"WhenVillainPhaseBegins","timing":"__TIMING__","subject":"game"},
                "effect":{"eachPlayer":{"order":"playerOrder","effect":{
                  "putIntoPlay":{"card":{"cardsIn":{"area":"encounterDiscardPile","kind":"Minion"}},
                    "where":"engagedWithYou"}}}}}]},
              {"card":"01103","abilities":[{
                "trigger":{"event":"WhenVillainPhaseStepOneCompleted","timing":"ForcedResponse","subject":"game"},
                "effect":{"dealDamage":{"cards":"this","amount":1}}}]}
            ]}
            """.Replace("__TIMING__", timing, StringComparison.Ordinal)));
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        Assert.Equal(Question.Defender, Sequence.Work(world, Cards, runner, events)!.Asking);
        Assert.Equal(DeckType.EngagedEnemiesArea, observer.Area.Type);
        Assert.True(Threat(world) > 0);
        Assert.Equal(1, observer.Damage);
        Assert.Single(events.OfType<PhaseStarted>());
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == VillainStepOneTiming.Completed);
    }

    private static AbilityRunner Runner(string timing) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"90001","abilities":[{
          "trigger":{"event":"WhenVillainPhaseBegins","timing":"__TIMING__","subject":"game"},
          "effect":{"chooseCard":{"chooser":"firstPlayer","from":{"query":"heroes"},
            "effect":{"dealDamage":{"cards":"chosen","amount":1}}}}
        }]}]}
        """.Replace("__TIMING__", timing, StringComparison.Ordinal)));

    private static long Threat(World world) => world.TheCardIn(DeckType.MainSchemesArea)!
        .Tokens.GetValueOrDefault("k_threat");

    private static World Board()
    {
        var world = Hero("01001a,01001b", 2);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }
}


