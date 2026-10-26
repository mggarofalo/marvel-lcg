using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic attack-only faceup interpretation; no expansion admission.</summary>
public sealed class AttackBoostFaceupQualifierTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:triggering-condition.2")]
    public void VillainAttackSharesOneFaceupWindowWithTheGenericInterrupt()
    {
        // "A single interrupt window and a single response window" handles
        // multiple descriptions of the same physical boost card turning faceup.
        var (world, runner, source, observer) = Board();
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        var prompt = Faceup(world, runner, events);
        var occurrence = world.Agenda.Occurrence!;
        Assert.True(occurrence.Is(BoostFaceupOperation.Faceup));
        Assert.True(occurrence.Is(BoostFaceupOperation.AttackFaceup));
        Assert.False(occurrence.Is(Steps.AttackInitiated));
        Assert.Contains(BoostFaceupOperation.AttackFaceup, StepConditions.EveryCondition);
        Assert.Contains(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == observer.ObjectId).Id), events);
        prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Same(occurrence, world.Agenda.Occurrence);
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == observer.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Single(events.OfType<AttackCompleted>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VillainSchemeFaceupDoesNotOfferTheAttackOnlyInterrupt(bool nested)
    {
        // 05014: "while the villain attacks". The generic observer keeps the
        // exact scheme faceup window open, including under a suspended attack.
        var (world, runner, source, observer) = Board();
        var events = new List<GameEvent>();
        int villain = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        if (nested)
        {
            world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain, Seat: 0));
            _ = Faceup(world, runner, events);
            Assert.True(world.Agenda.Occurrence!.Is(BoostFaceupOperation.AttackFaceup));
            // Synthetic scheduler suspension isolates the occurrence binding;
            // printed Boost-requested activations ordinarily wait for completion.
            world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
            world.Agenda.Now(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: villain, Seat: 0));
        }
        else world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: villain, Seat: 0));
        var prompt = Faceup(world, runner, events);
        Assert.False(world.Activation!.Attacking);
        Assert.Equal(nested, world.Attack is not null);
        Assert.False(world.Agenda.Current!.Value.Require<BoostFaceupOperation>().Attacking);
        Assert.DoesNotContain("uncancelled boost", prompt.Description ?? "");
        Assert.Equal(2, world.Agenda.Occurrence!.BoostIcons!.Remaining);
        Assert.True(world.Agenda.Occurrence.Is(BoostFaceupOperation.Faceup));
        Assert.False(world.Agenda.Occurrence.Is(BoostFaceupOperation.AttackFaceup));
        Assert.Contains(prompt.Affordances, a => a.AnchorId == observer.ObjectId);
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
    }

    [Fact]
    public void RemainingObserverSeesZeroAfterTheCurrentCardIconsAreCancelled()
    {
        // Synthetic observer keeps the same physical card's interrupt window open.
        var (world, runner, source, observer) = Board();
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        var prompt = Faceup(world, runner, events);
        var occurrence = world.Agenda.Occurrence!;
        Assert.Contains("2 uncancelled boost icons", prompt.Description);
        Sequence.Answer(world, Cards, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id,
                [], [payment.ObjectId]), events);

        prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Same(occurrence, world.Agenda.Occurrence);
        Assert.Equal(0, occurrence.BoostIcons!.Remaining);
        Assert.Contains("0 uncancelled boost icons", prompt.Description);
        Assert.Contains(prompt.Affordances, a => a.AnchorId == observer.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(0, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Null(world.Activation);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void MinionAttackFaceupDoesNotOfferTheVillainOnlyInterrupt()
    {
        var (world, runner, source, observer) = Board();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        // A preassigned boost card resolves even for a non-villainous minion.
        world.CreateCard("01102", world.AreaOf(DeckType.BoostCardsDeck, minion.Area.PlayArea, host: minion.ObjectId));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: minion.ObjectId, Seat: 0));
        var prompt = Faceup(world, runner, []);
        Assert.True(world.Agenda.Occurrence!.Is(BoostFaceupOperation.AttackFaceup));
        Assert.Equal(minion.ObjectId, world.Agenda.Occurrence.Actor);
        Assert.Contains(prompt.Affordances, a => a.AnchorId == observer.ObjectId);
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == source.ObjectId);
    }

    private static (World World, AbilityRunner Runner, Card Source, Card Observer) Board()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05014","abilities":[{
              "labels":["Defense"],
              "trigger":{"event":"WhenAttackBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain","form":"hero"},
              "effect":{"then":{"effect":{"cancelBoostIcons":"trigger"},
                "then":{"dealDamage":{"cards":"trigger.actor","amount":{"result":"cancelledBoostIcons"}}}}}
            }]},{"card":"01092","abilities":[{
              "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","actor":"enemy"},
              "cost":{"exhaust":"this"},"effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """));
        var world = Hero("03001a");
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01089", world.Seats[0].Deck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        var observer = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        return (world, runner, source, observer);
    }

    private static Prompt Faceup(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            Assert.NotNull(prompt);
            if (world.Agenda.Current?.Operation is BoostFaceupOperation) return prompt;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        throw new InvalidOperationException("faceup interrupt was not reached");
    }
}



