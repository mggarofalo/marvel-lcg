using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class RonanPublicRestartTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("rhino", false, 1u, 10, "90001", DeckType.EngagedEnemiesArea, 0)]
    [InlineData("rhino_expert", false, 2u, 18, "90003", DeckType.UpgradesArea, 0)]
    [InlineData("rhino_expert", true, 4u, 22, "90001", DeckType.EngagedEnemiesArea, 1)]
    [InlineData("rhino_expert", true, 4u, 31, "90001", DeckType.EngagedEnemiesArea, 0)]
    public void NaturalModularEncounterContinuesExactlyAcrossTwoFreshHosts(
        string scenario, bool multiplayer, uint seed, int checkpoint, string face,
        DeckType expectedArea, int expectedSeat)
    {
        string[] heroes = multiplayer ? ["captain_marvel", "spider_man"] : ["spider_man"];
        var original = new HulkRestartJourney(new GameSpecification(scenario, heroes, ["kree_fanatic"], seed));
        var policy = new CoreGamePolicy(original.Factory.Game.State.Facts);
        for (int index = 0; index < checkpoint; index++)
            original.Answer(policy.Answer(original.Factory.Game));

        // These checkpoints are natural encounters in the fixed public games, not arranged states.
        var source = Assert.Single(original.Factory.Game.State.Cards, card => card.FaceId == face);
        Assert.Equal(expectedArea, source.Area.Type);
        Assert.Equal(PlayArea.Of(expectedSeat), source.Area.PlayArea);
        // Printed 90003: "Attach to your identity."
        if (face == "90003")
            Assert.Equal(original.Factory.Game.State.Seats[expectedSeat].IdentityCard.ObjectId, source.Area.Host);
        Assert.NotNull(original.Current.Prompt);
        var fresh = original.Restart();
        var answer = policy.Answer(original.Factory.Game);
        fresh.Reject(answer, stale: true);
        original.Continue(fresh, answer);
        var second = original.Restart();
        original.Equivalent(second);
        while (original.Factory.Game.Pending is not null)
        {
            Assert.True(policy.Answered < 2000, "Fixed public game must terminate within the decision bound.");
            original.Continue(second, policy.Answer(original.Factory.Game));
        }
        Assert.NotEqual(Outcome.Unfinished, original.Factory.Game.State.Result);
        Assert.Null(second.Current.Prompt);
        original.Equivalent(second);
        output.WriteLine($"{scenario}/{seed} checkpoint {checkpoint}: {face} in {expectedArea}, seat {expectedSeat}; "
            + $"two serialized fresh hosts, stale rejection and terminal continuation matched after {policy.Answered} decisions.");
    }
}
