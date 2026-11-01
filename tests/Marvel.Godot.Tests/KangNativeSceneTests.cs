using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

// The host side of the Kang native journeys, without Godot: each checkpoint the client will
// observe is reached through the same host, and the join is the client's own answer.
public sealed class KangNativeSceneTests
{
    [Theory]
    [InlineData("kang-3", 3)]
    [InlineData("kang-4", 4)]
    public void TheGameOpensWithOneGameAreaPerPlayer(string contract, int players)
    {
        var factory = new NativeSceneGameFactory(contract);
        Game game = factory.Create(Specification(players)).Game;
        Assert.Equal(players, GameAreaFacts.Of(game.State).Count(area => !area.Central));
        Assert.Equal(2, KangTable.Of(game.State)!.Stage);
    }

    [Fact]
    public void TheTwoPlayerJourneyReachesActingDepartureJoinAndReunion()
    {
        var factory = new NativeSceneGameFactory("kang-2");
        var checkpoints = new KangNativeCheckpoints("kang-2", factory.Kang);
        var host = new EngineHost(factory);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "kang", Specification(2)));
        string capability = opened.Capability!;
        EngineResponse current = checkpoints.Advance(host, opened, capability);
        Assert.Equal(PublicDecisionKind.PlayerAction, current.Prompt!.PublicKind);
        for (int sync = 0; sync < 3; sync++) current = checkpoints.Advance(host, current, capability);
        Assert.Equal(KangJoinOperation.Join, current.Prompt!.Trigger);
        current = host.Exchange(EngineRequest.ResolveGame("join", "kang", capability,
            Decision.Take(current.Prompt.Affordances[0].Id), current.Revision));
        Assert.Null(current.Error);
        Assert.Single(current.World!.GameAreas, area => !area.Central);
        current = checkpoints.Advance(host, current, capability);
        Assert.Equal(3, current.World!.Kang!.Stage);
        Assert.Equal(["split", "acting", "departure", "join", "reunion"], checkpoints.Reached);
    }

    private static GameSpecification Specification(int players) =>
        new("rhino", [.. KangNativeScene.Heroes.Take(players)], null, Seed: 1);
}
