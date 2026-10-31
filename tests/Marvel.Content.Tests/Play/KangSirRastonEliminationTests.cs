using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual-card nonpublic board; Kang's public admission stays closed.
public sealed class KangSirRastonEliminationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:boost-boost-icon.6")]
    public void ABoostThatEliminatesItsPlayerStillGivesItsEnemyTheNextBoostCard(bool expert)
    {
        // 11042 Boost: "Take 1 damage. Give this enemy another boost card." rr:player-elimination.5:
        // "If a player is eliminated partway through the resolution of an ability, resolve the
        // entire ability"; .5.1: "If a player is eliminated in the middle of an attack, that
        // attack ends." rr:boost-boost-icon.6: a boost card dealt to an enemy outside its own
        // activation "remains facedown on that enemy until that enemy activates".
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Board(2, expert, events: events);
        world.FirstPlayer = 0;
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo("01001a");
        hero.TakeDamage(9);
        var raston = world.CreateCard("11042", world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(raston, world.AreaOf(DeckType.EncounterDeck));
        int from = events.Count;

        KangStageFlowFixture.VillainPhase(world, 1, events);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal(Outcome.Unfinished, world.Result);
        // p0's activation boost, the one Sir Raston gives, then p1's activation boost.
        Assert.Equal(3, events.Skip(from).OfType<CardsMoved>().Count(moved => moved.Verb == "Boost"
            && moved.From.Zone == nameof(DeckType.EncounterDeck)));
    }
}
