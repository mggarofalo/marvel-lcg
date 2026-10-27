using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic observers challenge phase-boundary lifetime on the real scheduler.</summary>
public sealed class VillainPhaseBeginningLifetimeTests : CoreCardTestBase
{
    [Theory]
    [InlineData("ForcedInterrupt")]
    [InlineData("ForcedResponse")]
    public void CompletionObservationWaitsForSuspendedStartSourceToLeavePlay(string timing)
    {
        var world = Board();
        var observer = world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01103","abilities":[
                {"trigger":{"event":"WhenVillainPhaseBegins","timing":"__TIMING__","subject":"game"},
                 "effect":{"seq":[
                   {"chooseCard":{"chooser":"firstPlayer","from":{"query":"heroes"},
                     "effect":{"dealDamage":{"cards":"chosen","amount":1}}}},
                   {"discard":"this"}]}},
                {"trigger":{"event":"WhenVillainPhaseStepOneCompleted","timing":"ForcedResponse","subject":"game"},
                 "effect":{"dealDamage":{"cards":{"query":"villain"},"amount":1}}}]},
              {"card":"90001","abilities":[
                {"trigger":{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"},
                 "effect":{"preventThreat":1}}]}
            ]}
            """.Replace("__TIMING__", timing, StringComparison.Ordinal)));
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        var startChoice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.True(runner.ObservesVillainStepOneCompletion(world));
        Assert.Equal(0, Threat(world));
        Sequence.Answer(world, Cards, runner, startChoice,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), events);
        var threatInterrupt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Steps.ThreatWouldBePlaced, threatInterrupt.Trigger);
        Assert.Equal(DeckType.EncounterDiscardPile, observer.Area.Type);
        Assert.False(runner.ObservesVillainStepOneCompletion(world));
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == VillainStepOneTiming.Completed);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, Threat(world));
        Assert.Single(events.OfType<PhaseStarted>());
    }

    [Fact]
    [Rule("rr:triggering-condition.1")]
    public void SeparatePhaseOccurrencesEachResolveTheBeginningOnce()
    {
        // Each ability "can only be triggered once per occurrence"; later phases are new occurrences.
        // Only step one is queued here to isolate the lifetime from activation and encounter decisions.
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01094","abilities":[
              {"trigger":{"event":"WhenVillainPhaseBegins","timing":"ForcedInterrupt","subject":"game"},
               "effect":{"dealDamage":{"cards":"this","amount":1}}},
              {"trigger":{"event":"WhenVillainPhaseStepOneCompleted","timing":"ForcedResponse","subject":"game"},
               "effect":{"dealDamage":{"cards":"this","amount":1}}}
            ]}]}
            """));
        var events = new List<GameEvent>();
        for (int round = 1; round <= 2; round++)
        {
            world.Agenda.Add(new PhaseStep(new ThreatStageOperation(ThreatStageKind.PlaceThreat), round, 1));
            Assert.Null(Sequence.Work(world, Cards, runner, events));
            Assert.False(world.Agenda.IsBusy);
            Assert.Equal(round * 2, villain.Damage);
            Assert.Equal(round * 2, Threat(world));
            Assert.Equal(round, events.OfType<PhaseStarted>().Count());
        }
        Assert.Equal(new int?[] { 1, 2 }, events.OfType<PhaseStarted>().Select(start => start.Round));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    public void SelectedForcedSourceStartsFirstAndFinishesBeforeTheOtherSource(int selectedPlayer)
    {
        // "the first player determines the order"; "Each forced ability must resolve
        // as completely as possible before the next forced ability ... may initiate."
        var world = Board();
        world.FirstPlayer = 1;
        var sources = Enumerable.Range(0, 2).Select(player => world.CreateCard("90001",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)))).ToArray();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"90001","abilities":[{
              "trigger":{"event":"WhenVillainPhaseBegins","timing":"ForcedInterrupt","subject":"game"},
              "effect":{"seq":[{"dealDamage":{"cards":"this","amount":1}},
                {"chooseCard":{"chooser":"firstPlayer","from":{"query":"heroes"},
                  "effect":{"dealDamage":{"cards":"chosen","amount":1}}}}]}
            }]}]}
            """));
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(1, order.Player);
        var selected = Assert.Single(order.Affordances, offer => offer.AnchorId == sources[selectedPlayer].ObjectId);
        Sequence.Answer(world, Cards, runner, order, Decision.Take(selected.Id), events);
        var firstChoice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(1, sources[selectedPlayer].Damage);
        Assert.Equal(0, sources[1 - selectedPlayer].Damage);
        Assert.Equal(0, Threat(world));
        Sequence.Answer(world, Cards, runner, firstChoice,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), events);
        var secondChoice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.All(sources, source => Assert.Equal(1, source.Damage));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, Threat(world));
        Sequence.Answer(world, Cards, runner, secondChoice,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), events);
        Assert.Equal(Question.Defender, Sequence.Work(world, Cards, runner, events)!.Asking);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Single(events.OfType<PhaseStarted>());
    }

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
