using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EspionageSurgeTests
{
    [Fact]
    [Rule("rr:surge.1")]
    [Rule("rr:surge.2")]
    public void PrintedEspionageInterruptsActualSurgeBeforeDealingAndThenCompletesPreparation()
    {
        // Surge: "When Revealed: Deal yourself 1 facedown encounter card."
        // 08033: "discard Espionage → draw 2 cards."
        var f = new EspionageSurgeFixture(suit: true);
        var interrupt = f.Work();
        Assert.Equal("WhenSurgeWouldResolve", interrupt.Trigger);
        Assert.Equal(1, interrupt.Player);
        Assert.True(interrupt.Cancellable);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        f.Answer(interrupt, f.Espionage[0].ObjectId);
        var response = f.Work();
        Assert.Equal(2, f.World.Seats[1].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, f.Espionage[0].Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        var suitOffer = Assert.Single(response.Affordances, offer => offer.AnchorId == f.Suit!.ObjectId);
        Assert.Equal("Exhaust Synth-Suit", suitOffer.CostDescription);
        Assert.Equal("Ready Black Widow.", suitOffer.Description);
        Assert.Equal(1, suitOffer.AnchorPlayer);
        Assert.False(f.Hero.Ready);
        Assert.True(f.Suit!.Ready);
        f.Answer(response, f.Suit!.ObjectId);
        f.Finish();
        Assert.True(f.Hero.Ready);
        Assert.False(f.Suit.Ready);
        Assert.False(Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards).FaceUp);
        Assert.Equal(DeckType.EngagedEnemiesArea, f.Encounter.Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [Rule("rr:triggering-condition.1.1")]
    public void EachControlledCopyMayInterruptTheSameSurge(int resolver, int controller)
    {
        // "Multiple copies of the same ability may be triggered once each."
        var f = new EspionageSurgeFixture(resolver, controller, copies: 3);
        for (int i = 0; i < 3; i++)
        {
            var prompt = f.Work();
            Assert.Equal(controller, prompt.Player);
            Assert.Equal("WhenSurgeWouldResolve", prompt.Trigger);
            Assert.Equal(3 - i, prompt.Affordances.Count);
            f.Answer(prompt, f.Espionage[i].ObjectId);
            Assert.Equal((i + 1) * 2, f.World.Seats[controller].Hand.Cards.Count);
        }
        f.Finish();
        Assert.Empty(f.World.Seats[1 - controller].Hand.Cards);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(resolver)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1 - resolver)).Cards);
    }

    [Fact]
    [Rule("rr:surge.1")]
    public void DecliningSurgeInterruptPreservesTheCopyAndResetsTheEncounterDeck()
    {
        // "Deal yourself 1 facedown encounter card."
        var f = new EspionageSurgeFixture();
        foreach (var card in f.World.AreaOf(DeckType.EncounterDeck).Cards.ToArray())
            World.MoveToTop(card, f.World.AreaOf(DeckType.EncounterDiscardPile));
        f.Decline(f.Work()); f.Finish();
        Assert.Equal(DeckType.UpgradesArea, f.Espionage[0].Area.Type);
        Assert.Empty(f.World.Seats[1].Hand.Cards);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal(11, f.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.False(f.World.IsOver);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrintedAbilityAndSurgeRespectTheChosenOrder(bool surgeFirst)
    {
        // Synthetic draw distinguishes the keyword from the initial reveal.
        var f = new EspionageSurgeFixture(printed: true);
        var order = f.Work();
        Assert.Equal(Question.Order, order.Asking);
        Assert.DoesNotContain(order.Affordances, a => a.AnchorId == f.Espionage[0].ObjectId);
        var surge = Assert.Single(order.Affordances, a => a.Label == "Surge");
        var printed = Assert.Single(order.Affordances, a => a.Id != surge.Id);
        Sequence.Answer(f.World, f.Cards, f.Runner, order, Decision.Take(surgeFirst ? surge.Id : printed.Id), f.Events);
        var interrupt = f.Work();
        Assert.Equal("WhenSurgeWouldResolve", interrupt.Trigger);
        Assert.Equal(surgeFirst ? 0 : 1, f.World.Seats[1].Hand.Cards.Count);
        f.Answer(interrupt, f.Espionage[0].ObjectId); f.Finish();
        Assert.Equal(3, f.World.Seats[1].Hand.Cards.Count);
    }

    [Fact]
    public void CanceledRevealEffectsNeverOfferEspionage()
    {
        // Ruling 84569f1a5fcccb6f: canceled surge leaves nothing to interrupt.
        // Synthetic cancel control isolates canceled Surge from Spycraft's replacement reveal.
        var f = new EspionageSurgeFixture(cancel: true);
        var reveal = f.Work();
        Assert.Equal(Steps.CardRevealed, reveal.Trigger);
        Assert.DoesNotContain(reveal.Affordances, a => a.AnchorId == f.Espionage[0].ObjectId);
        f.Answer(reveal, Assert.Single(reveal.Affordances).AnchorId);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.Empty(f.World.Seats[1].Hand.Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal(DeckType.UpgradesArea, f.Espionage[0].Area.Type);
    }

    [Fact]
    public void QueriesInvalidAnswersAndStaleHandlesPreserveThePendingSurge()
    {
        var f = new EspionageSurgeFixture(); var control = new EspionageSurgeFixture();
        var prompt = f.Work(); var controlPrompt = control.Work();
        string before = Snapshot(f);
        var pending = f.World.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(prompt.Affordances, f.Work().Affordances);
            Assert.Equal(before, Snapshot(f));
        }
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            prompt, Decision.Take(int.MaxValue), f.Events));
        Assert.Equal(before, Snapshot(f));
        Assert.Equal(pending, f.World.Agenda.Outstanding.ToArray());
        f.Answer(prompt, f.Espionage[0].ObjectId); control.Answer(controlPrompt, control.Espionage[0].ObjectId);
        f.Finish(); control.Finish();
        Assert.Equal(Snapshot(control), Snapshot(f));
        string completed = Snapshot(f);
        Assert.Throws<ArgumentException>(() => f.Answer(prompt, f.Espionage[0].ObjectId));
        Assert.Equal(completed, Snapshot(f));
    }

    [Fact]
    public void GainedSurgeHasAnInterruptBeforeItsDeal()
    {
        var f = new EspionageSurgeFixture(gained: true);
        var prompt = f.Work();
        Assert.Equal("WhenSurgeWouldResolve", prompt.Trigger);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        f.Answer(prompt, f.Espionage[0].ObjectId); f.Finish();
        Assert.Equal(2, f.World.Seats[1].Hand.Cards.Count);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }


    [Fact]
    [Rule("rr:surge.2")]
    public void VillainPhaseQueueWaitsForOriginalTreacheryCleanupBeforeRevealingSurgeCard()
    {
        // "Complete the process of resolving the original card ... before revealing the additional card."
        var f = new EspionageSurgeFixture();
        f.World.Agenda.Abandon();
        World.MoveToTop(f.Encounter, f.World.AreaOf(DeckType.EncounterDiscardPile));
        var exhaustion = f.World.CreateCard("01191", f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        var extra = f.World.CreateCard("01122", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4));
        var order = f.Work();
        Assert.Equal(Question.Order, order.Asking);
        Sequence.Answer(f.World, f.Cards, f.Runner, order,
            Decision.Take(Assert.Single(order.Affordances, a => a.Label == "Surge").Id), f.Events);
        var interrupt = f.Work();
        Assert.Equal(DeckType.RevealingArea, exhaustion.Area.Type);
        Assert.False(extra.FaceUp);
        f.Answer(interrupt, f.Espionage[0].ObjectId);
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, exhaustion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, extra.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.RevealingArea).Cards);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    private static string Snapshot(EspionageSurgeFixture f) => System.Text.Json.JsonSerializer.Serialize(new
    {
        Digest = f.World.Digest().Canonical(), Random = f.World.Random.Generator.GetState(),
        f.World.Random.Generator.WordsConsumed, f.World.Agenda.Stage, Effects = f.World.Effects.Registered,
        Pending = f.World.Agenda.Outstanding.Select(s => new { s.What, s.Subject, s.Seat, s.Index }),
    });
}

