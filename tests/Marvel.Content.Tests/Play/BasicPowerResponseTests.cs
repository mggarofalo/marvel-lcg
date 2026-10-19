using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BasicPowerResponseTests
{
    [Fact]
    [Rule("rr:recover-recovery.1")]
    [Rule("rr:after")]
    public void RecoveryCompletesBeforeItsResponseAndRepeatedUsesAreDistinct()
    {
        // "heal damage ... equal to their REC value" before an after-use response.
        var runner = Runner("01006", "Response", """{"draw":{"player":"you","count":1}}""",
            eventName: BasicPowerTiming.RecoveryUsed, player: "you");
        var (_, world) = Playing(board => InPlay(board, "01006"), abilities: runner);
        world.Seats[0].IdentityCard.TakeDamage(8);
        int hand = world.Seats[0].Hand.Cards.Count;
        for (int use = 0; use < 2; use++)
        {
            AllyBasicPowers.BasicRecovery(world, world.Facts, 0, []);
            Assert.Equal(8 - (use + 1) * 3, world.Seats[0].IdentityCard.Damage);
            Assert.False(world.Seats[0].IdentityCard.Ready);
            var prompt = Sequence.Work(world, world.Facts, runner, [])!;
            var response = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(hand + use + 1, world.Seats[0].Hand.Cards.Count);
            world.Seats[0].IdentityCard.Refresh();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:confuse-confused.1")]
    public void CancelledThwartDoesNotProduceCompletedBasicPowerResponse(bool confused)
    {
        var runner = Runner("01006", "Response", """{"draw":{"player":"you","count":1}}""",
            eventName: BasicPowerTiming.ThwartEnds);
        var (_, world) = Playing(board => InPlay(board, "01006"), hero: true, abilities: runner);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        if (confused) Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        int hand = world.Seats[0].Hand.Cards.Count;
        BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        if (!confused)
        {
            var response = Assert.Single(prompt!.Affordances);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
        }
        else Assert.Null(prompt);
        Assert.Equal(hand + (confused ? 0 : 1), world.Seats[0].Hand.Cards.Count);
    }
}
