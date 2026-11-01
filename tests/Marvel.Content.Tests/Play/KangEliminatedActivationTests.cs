using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual-card nonpublic board; Kang's public admission stays closed.
public sealed class KangEliminatedActivationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.6")]
    [Rule("rr:villain-phase.step.2")]
    public void APlayerEliminatedByTheirLocalKangEndsTheirActivationsAndTheOtherAreaStillActivates(bool expert)
    {
        // rr:villain-phase.step.2: "in player order, each player resolves" activations.
        // rr:player-elimination.6: "Effects that refer to the players in the game ignore
        // eliminated players". An eliminated player has left every game area
        // (pack:mc11:game-areas), so no Kang is "theirs" to look up; the remaining player's
        // Kang still activates in their own area.
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, expert, events);
        int first = world.PlayerOrder.First();
        int other = 1 - first;
        var hero = world.Seats[first].IdentityCard;
        hero.TurnTo("01001a");
        hero.TakeDamage(9);
        var otherScheme = KangStageFlowFixture.LocalScheme(world, other);
        long threat = otherScheme.Tokens.GetValueOrDefault("k_threat");

        KangStageFlowFixture.VillainPhase(world, 1, events);

        Assert.True(world.Seats[first].Eliminated);
        Assert.False(world.Seats[other].Eliminated);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.True(otherScheme.Tokens.GetValueOrDefault("k_threat") > threat);
    }
}
