using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngineHostTests
{
    [Fact]
    public void CanonicalContentOpensAndResolvesThroughTheHost()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var opened = host.Exchange(EngineRequest.OpenGame(
            "request-1",
            "game-1",
            new GameSpecification("rhino", ["spider_man"], ModularSets: null, Seed: 7)));

        Assert.Null(opened.Error);
        Assert.NotNull(opened.Prompt);
        Assert.Equal("request-1", opened.RequestId);
        Assert.Equal("game-1", opened.GameId);

        var resolved = host.Exchange(EngineRequest.ResolveGame(
            "request-2", "game-1", EngineDecision.Decline));

        Assert.Null(resolved.Error);
        Assert.NotNull(resolved.Prompt);
        Assert.Equal("request-2", resolved.RequestId);
    }

    [Fact]
    public void AClientOwnsTheGameIdAndCannotReplaceAnOpenGame()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var request = EngineRequest.OpenGame(
            "first",
            "chosen-id",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7));

        Assert.Null(host.Exchange(request).Error);
        var duplicate = host.Exchange(request with { RequestId = "again" });

        Assert.Equal("game_exists", duplicate.Error?.Code);
        Assert.Equal("again", duplicate.RequestId);
    }

    [Fact]
    public void InvalidCommandsFailBeforeTheyReachAFactory()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        var wrongVersion = host.Exchange(new EngineRequest(
            99, "version", EngineProtocol.Open, "game",
            new GameSpecification("rhino", ["spider_man"], null, 1)));
        var unknownGame = host.Exchange(EngineRequest.ResolveGame(
            "resolve", "missing", EngineDecision.Decline));

        Assert.Equal("unsupported_version", wrongVersion.Error?.Code);
        Assert.Equal("game_not_found", unknownGame.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void ClosingAGameReleasesItsClientChosenId()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var specification = new GameSpecification(
            "rhino", ["spider_man"], ModularSets: [], Seed: 7);
        Assert.Null(host.Exchange(
            EngineRequest.OpenGame("open", "reusable", specification)).Error);

        Assert.Null(host.Exchange(
            EngineRequest.CloseGame("close", "reusable")).Error);
        Assert.Null(host.Exchange(
            EngineRequest.OpenGame("reopen", "reusable", specification)).Error);
    }

    [Fact]
    public void AFailedResolveCannotLeaveAPartialGameAvailable()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        Assert.Null(host.Exchange(EngineRequest.OpenGame(
            "open",
            "fail-closed",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7))).Error);

        var rejected = host.Exchange(EngineRequest.ResolveGame(
            "bad", "fail-closed", new EngineDecision(999, [])));
        var after = host.Exchange(EngineRequest.ResolveGame(
            "after", "fail-closed", EngineDecision.Decline));

        Assert.Equal("game_aborted", rejected.Error?.Code);
        Assert.Equal("game_not_found", after.Error?.Code);
    }

    private sealed class UnusedFactory : IGameFactory
    {
        public int Calls { get; private set; }

        public OpenedGame Create(GameSpecification specification)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }
}
