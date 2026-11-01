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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.5")]
    public void ABoostThatEliminatesALoneStagePlayerFinishesBeforeTheStageCompletes(bool expert)
    {
        // 11042 Boost: "Take 1 damage. Give this enemy another boost card." rr:player-elimination.5:
        // "If a player is eliminated partway through the resolution of an ability, resolve the
        // entire ability." 11009b-11012b: "If all the players at this stage are defeated, this
        // stage is complete[d]", and its Forced Response stores a Dominion beneath 4A. The
        // entire boost ability resolves first, so the extra boost card is dealt before the
        // completion stores the Dominion.
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, expert, events);
        const int seat = 0;
        world.FirstPlayer = seat;
        var hero = world.Seats[seat].IdentityCard;
        hero.TurnTo("01001a");
        hero.TakeDamage(9);
        var scheme = KangStageFlowFixture.LocalScheme(world, seat);
        var raston = world.CreateCard("11042", world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(raston, world.AreaOf(DeckType.EncounterDeck));
        int from = events.Count;

        KangStageFlowFixture.VillainPhase(world, 1, events);

        Assert.True(world.Seats[seat].Eliminated);
        var played = events.Skip(from).ToList();
        int flipped = played.FindIndex(e => e is CardsFlipped { Verb: "Boost" } flip
            && flip.Cards.Contains(raston.ObjectId));
        int extra = played.FindIndex(flipped + 1, e => e is CardsMoved { Verb: "Boost" } moved
            && moved.From.Zone == nameof(DeckType.EncounterDeck));
        int stored = played.FindIndex(e => e is CardsMoved { Verb: "Store_Beneath" } moved
            && moved.Trigger == scheme.FaceId);
        Assert.True(flipped >= 0 && extra > flipped && stored > extra,
            $"flip {flipped}, extra boost {extra}, Dominion stored {stored}");
        Assert.Equal(Outcome.Unfinished, world.Result);
    }
}
