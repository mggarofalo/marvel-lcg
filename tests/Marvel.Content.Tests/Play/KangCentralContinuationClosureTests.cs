using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class KangCentralContinuationClosureTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:quickstrike.1")]
    public void SearchResumesAfterQuickstrikeAndReadsNextNemesisCurrentLocation()
    {
        // "After a minion with the quickstrike keyword engages a player whose identity is in hero form, that minion attacks that player."
        // Synthetic movement during the defense prompt exercises a live location change.
        var (world, events) = KangCentralTransitionTests.ReadyForLastBoundary(captain: true);
        world.Seats[0].IdentityCard.TurnTo("03001a");
        var first = Assert.Single(world.Seats[0].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        var second = Assert.Single(world.Seats[1].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        int start = events.Count;
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 3, 4));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Defender, prompt.Asking);
        Assert.Equal(DeckType.AsideDeck, second.Area.Type);
        Assert.DoesNotContain(events.OfType<AreaReordered>(), e => e.Verb == "Shuffle" && e.Trigger == "11013b");
        World.MoveToTop(second, world.AreaOf(DeckType.EncounterDiscardPile));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Decline, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
        Assert.Equal(PlayArea.Of(0), first.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, second.Area.Type);
        Assert.Equal(PlayArea.Of(1), second.Area.PlayArea);
        int firstEntry = events.FindIndex(start, e => e is CardsMoved m && m.Verb == "Put_Into_Play" && m.Cards.Any(c => c.Card == first.ObjectId));
        int secondEntry = events.FindIndex(start, e => e is CardsMoved m && m.Verb == "Put_Into_Play" && m.Cards.Any(c => c.Card == second.ObjectId));
        int shuffle = events.FindIndex(start, e => e is AreaReordered { Verb: "Shuffle", Trigger: "11013b" });
        Assert.True(firstEntry >= start && secondEntry > firstEntry && shuffle > secondEntry);
        Assert.Single(events.OfType<AreaReordered>(), e => e.Verb == "Shuffle" && e.Trigger == "11013b");
        Assert.Empty(world.Agenda.Outstanding);
    }
    [Fact]
    [Rule("rr:in-player-order")]
    public void SearchStartsWithCurrentFirstPlayerAndFinishesBeforeShuffle()
    {
        // "If players are instructed to perform a sequence “in player order,” the first player performs their part of the sequence first, followed by the other players in clockwise order."
        var (world, events) = KangCentralTransitionTests.ReadyForLastBoundary();
        world.FirstPlayer = 1;
        var first = Assert.Single(world.Seats[1].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        var second = Assert.Single(world.Seats[0].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        int start = events.Count;
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 3, 4));
        Sequence.Finish(world, Cards, world.Abilities, events);
        int firstEntry = events.FindIndex(start, e => e is CardsMoved m && m.Verb == "Put_Into_Play" && m.Cards.Any(c => c.Card == first.ObjectId));
        int secondEntry = events.FindIndex(start, e => e is CardsMoved m && m.Verb == "Put_Into_Play" && m.Cards.Any(c => c.Card == second.ObjectId));
        int shuffle = events.FindIndex(start, e => e is AreaReordered { Verb: "Shuffle", Trigger: "11013b" });
        Assert.True(firstEntry >= start && secondEntry > firstEntry && shuffle > secondEntry);
        Assert.Equal(PlayArea.Of(1), first.Area.PlayArea);
        Assert.Equal(PlayArea.Of(0), second.Area.PlayArea);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
