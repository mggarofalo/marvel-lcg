using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

// Kang stays outside public admission; KangRecipeFactory deals the declared recipe
// through the production dealer behind the host's durable factory seam.
public sealed class KangRecipeRestartTests
{
    [Fact]
    [Rule("rr:first-player.3")]
    public void ASplitGameSurvivesFreshHostRestartsThroughItsJoinAndReunion()
    {
        // pack:mc11:areas: each player "create[s] your own game area"; pack:mc11:game-areas:
        // "When you defeat Kang (II) in your game area, you are instructed to join another
        // game area at the end of the phase." The session restarts once on the separated
        // table and again while the join destination is pending, then plays through the reunion
        // with every decision's state, RNG and prompt compared against the uninterrupted host.
        var journey = new PublicRestartJourney(new GameSpecification(Scenario, KangRecipeGames.Heroes.Take(Players).ToArray(),
            [Modular], Seed), "kang-restart", () => new KangRecipeFactory());
        var policy = new CoreGamePolicy(journey.Factory.Game.State.Facts);
        while (!Places.Separated(journey.Factory.Game.State) || !journey.Factory.Game.IsRootPrompt)
        {
            Assert.True(policy.Answered < 3000);
            journey.Answer(KangInsertAdvice.Answer(policy, journey.Factory.Game));
        }
        var fresh = journey.Restart();
        journey.Continue(fresh, KangInsertAdvice.Answer(policy, journey.Factory.Game));
        while (journey.Factory.Game.Pending!.Trigger != KangJoinOperation.Join)
            Both(journey, fresh, KangInsertAdvice.Answer(policy, journey.Factory.Game), policy);
        journey.Equivalent(fresh);
        Assert.True(Places.Separated(fresh.Factory.Game.State));
        var second = fresh.Restart();
        journey.Continue(second, KangInsertAdvice.Answer(policy, journey.Factory.Game));
        while (Places.Separated(journey.Factory.Game.State))
            Both(journey, second, KangInsertAdvice.Answer(policy, journey.Factory.Game), policy);
        journey.Equivalent(second);
        var world = second.Factory.Game.State;
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Contains(world.AreaOf(DeckType.VillainArea).Cards, card => card.FaceId == "11006");
        Assert.Contains(world.Cards, card => card.FaceId is "11013a" or "11013b"
            && card.Area.Type == DeckType.MainSchemesArea);
    }

    // Every answer goes to both hosts; the full save, world and history comparison runs
    // after the first answer and at each checkpoint, and the state digest after every answer.
    private static void Both(PublicRestartJourney journey, PublicRestartJourney other, Decision answer, CoreGamePolicy policy)
    {
        Assert.True(policy.Answered < 3000);
        journey.Answer(answer);
        other.Answer(answer);
        journey.EquivalentState(other);
    }

    private const string Scenario = "kang";
    private const string Modular = "temporal";
    private const int Players = 2;
    private const uint Seed = 1;
}
