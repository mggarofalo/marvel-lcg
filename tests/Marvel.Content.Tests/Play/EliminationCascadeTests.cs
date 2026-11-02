using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EliminationCascadeTests
{
    /// <summary>
    /// Endurance (05023) played by the first seat on the second seat's identity leaves
    /// with the first seat's elimination (rr:player-elimination.step.4: "Place each card
    /// owned by the eliminated player in the eliminated player's discard pile."). Losing
    /// its +3 hit points defeats the second identity, and with every player eliminated
    /// the game ends once (rr:player-elimination.4: "If all players are eliminated, the
    /// game ends and the players lose.").
    /// </summary>
    [Rule("rr:player-elimination.step.4")]
    [Rule("rr:player-elimination.4")]
    [Fact]
    public void AnUpgradeLeavingWithItsOwnerCanEliminateTheLastPlayerOnce()
    {
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        var runner = AuthoredCards.Runner();
        var partner = world.Seats[1].IdentityCard;
        var endurance = world.CreateCard("05023", world.Seats[0].Hand);
        var pay = world.CreateCard("01003", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], endurance, [pay.ObjectId], [], [partner.ObjectId]);
        long health = DamagePlacement.Health(world, world.Facts, partner);
        partner.TakeDamage(health - 1);

        Elimination.Eliminate(world, world.Facts, 0, "test", []);

        Assert.True(world.Seats[1].Eliminated);
        Assert.Equal(Outcome.PlayersLose, world.Result);
        Assert.Equal(PlayerLossReason.AllPlayersEliminated, world.LossReason);
    }

    /// <summary>
    /// A game already over for another reason does not also end by elimination
    /// (rr:player-elimination.4); the second ending raises rather than being absorbed.
    /// </summary>
    [Rule("rr:player-elimination.4")]
    [Fact]
    public void EliminatingTheLastPlayerOfAGameAlreadyLostAnotherWayRaises()
    {
        var (_, world) = Playing(_ => { }, heroes: ["spider_man"], abilities: AuthoredCards.Runner());
        world.Finish(Outcome.PlayersLose, PlayerLossReason.EncounterCardsExhausted);

        Assert.Throws<RulesNotImplementedException>(() => Elimination.Eliminate(world, world.Facts, 0, "test", []));
    }

    /// <summary>The same departure leaves a partner with hit points to spare in the game.</summary>
    [Rule("rr:player-elimination.step.4")]
    [Fact]
    public void AnUpgradeLeavingWithItsOwnerSparesAPartnerAboveItsPrintedHitPoints()
    {
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        var runner = AuthoredCards.Runner();
        var partner = world.Seats[1].IdentityCard;
        var endurance = world.CreateCard("05023", world.Seats[0].Hand);
        var pay = world.CreateCard("01003", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], endurance, [pay.ObjectId], [], [partner.ObjectId]);
        long health = DamagePlacement.Health(world, world.Facts, partner);
        partner.TakeDamage(health - 4);

        Elimination.Eliminate(world, world.Facts, 0, "test", []);

        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }
}
