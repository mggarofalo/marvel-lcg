using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class KangCentralEliminationReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(10, true)]
    [Rule("rr:quickstrike.1")]
    public void FirstNemesisQuickstrikeEliminationDoesNotSkipSurvivorsSearch(int damage, bool eliminated)
    {
        // "After a minion with the quickstrike keyword engages a player whose identity is in hero form, that minion attacks that player."
        // 11013b searches for each player's nemesis.
        var (world, events) = KangCentralTransitionTests.ReadyForLastBoundary(captain: true);
        var captain = world.Seats[0].IdentityCard;
        captain.TurnTo("03001a");
        captain.TakeDamage(damage);
        var zemo = Assert.Single(world.Seats[0].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        var survivorMinion = Assert.Single(world.Seats[1].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 3, 4));
        for (int decisions = 0; ; decisions++)
        {
            var prompt = Sequence.Work(world, Cards, world.Abilities, events);
            if (prompt is null) break;
            Assert.True(decisions < 10, prompt.Label);
            Assert.Equal(Question.Defender, prompt.Asking);
            Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Decline, events);
        }
        Assert.Equal(eliminated, world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal(DeckType.EngagedEnemiesArea, zemo.Area.Type);
        Assert.Equal(PlayArea.Of(eliminated ? 1 : 0), zemo.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, survivorMinion.Area.Type);
        Assert.Equal(PlayArea.Of(1), survivorMinion.Area.PlayArea);
        Assert.Single(events.OfType<AreaReordered>(), item => item.Verb == "Shuffle" && item.Trigger == "11013b");
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:player-elimination.6")]
    public void PendingEliminatedPlayerIsSkippedAfterFirstNemesisEntrySuspends()
    {
        // "Effects that refer to the players in the game ignore eliminated players".
        // This synthetic eliminated-seat flag isolates a future seat during the actual quickstrike prompt.
        var (world, events) = KangCentralTransitionTests.ReadyForLastBoundary(captain: true);
        world.Seats[0].IdentityCard.TurnTo("03001a");
        var futureNemesis = Assert.Single(world.Seats[1].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 3, 4));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Defender, prompt.Asking);
        world.Seats[1].Eliminated = true;
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Decline, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.True(world.Seats[1].Eliminated);
        Assert.Equal(DeckType.AsideDeck, futureNemesis.Area.Type);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), move => move.Verb == "Put_Into_Play"
            && move.Cards.Any(landing => landing.Card == futureNemesis.ObjectId));
        Assert.Single(events.OfType<AreaReordered>(), item => item.Verb == "Shuffle" && item.Trigger == "11013b");
        Assert.Empty(world.Agenda.Outstanding);
    }

}
