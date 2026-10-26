using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic cost-free observers distinguish timing windows from exhausted costs.</summary>
public sealed class AttackBoostFaceupIndependentReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:triggering-condition.1")]
    [Rule("rr:triggering-condition.2")]
    public void BothFaceupDescriptionsOfferEachAbilityOnceWithoutRepeatingAttackInitiation()
    {
        // "Each Interrupt and Response ability can only be triggered once per occurrence."
        // Multiple descriptions share "a single interrupt window and a single response window."
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenAttackInitiated","timing":"ForcedInterrupt","actor":"villain"},
              "effect":{"draw":{"player":"you","count":1}}
            },{
              "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain"},
              "effect":{"draw":{"player":"you","count":1}}
            },{
              "trigger":{"event":"WhenAttackBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """));
        var world = Hero("03001a");
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var observer = world.CreateCard("01092",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        for (int i = 0; i < 10; i++) world.CreateCard("01089", world.Seats[0].Deck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));

        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Defender, prompt.Asking);
        Assert.Single(world.Seats[0].Hand.Cards);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.IsType<BoostFaceupOperation>(world.Agenda.Current!.Value.Operation);
        var occurrence = world.Agenda.Occurrence!;
        var offers = prompt.Affordances.Where(a => a.AnchorId == observer.ObjectId).ToArray();
        Assert.Equal(2, offers.Length);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(offers[0].Id), events);

        prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Same(occurrence, world.Agenda.Occurrence);
        var remaining = Assert.Single(prompt.Affordances, a => a.AnchorId == observer.ObjectId);
        Assert.Equal(offers[1].Id, remaining.Id);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(remaining.Id), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(3, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(7, world.Seats[0].Deck.Cards.Count);
        Assert.Single(events.OfType<BoostResolved>());
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.True(observer.Ready);
        Assert.Null(world.Activation);
        Assert.False(world.Agenda.IsBusy);
    }
}
