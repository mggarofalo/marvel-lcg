using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Kang II faces and the full authored book on a prepared two-area split.
public sealed class KangGameAreaDefenseTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [Rule("rr:defend-defense.5")]
    public void OnlyPlayersInTheAttackersGameAreaAreOfferedTheDefence(bool joined, int otherOffers)
    {
        // pack:mc11:game-areas: "Players cannot attack or defend enemies in other
        // game areas." rr:defend-defense.5 lets a player defend another player
        // once they share the attacker's game area.
        var (world, kang, _, _) = KangStageTwoVillainTests.Board("11003", "11010b", otherVillain: !joined);
        if (joined) world.Join(PlayArea.Of(1), world.GameAreaOf(PlayArea.Of(0))!, "prepared join", []);
        var other = world.Seats[1].IdentityCard;
        KangTemporalFixture.Attack(world, kang, 0);
        var askedSeats = new List<int>();
        int offersOfOther = 0;

        KangTemporalFixture.Drive(world, (Marvel.Cards.Run.AbilityRunner)world.Abilities, prompt =>
        {
            if (prompt.Asking != Question.Defender) return null;
            askedSeats.Add(prompt.Player);
            offersOfOther += prompt.Affordances.Count(option => option.Id == other.ObjectId);
            return Decision.Decline;
        });

        Assert.Equal(otherOffers, offersOfOther);
        Assert.Equal(joined, askedSeats.Contains(1));
        Assert.Contains(0, askedSeats);
        Assert.True(other.Ready);
    }
}
