using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngineHostTests
{
    [Theory]
    [InlineData("unus", "spider_man", null, "no campaign named 'unus'")]
    [InlineData("rhino", "sp_dr", null, "no hero named 'sp_dr'")]
    [InlineData("rhino", "spider_man", "sinister_syndicate",
        "no encounter set named 'sinister_syndicate'")]
    public void ExpansionProductsAreRejectedAtTheDatasetBoundary(
        string scenario, string hero, string? modular, string message)
    {
        // Product names resolve before WorldSetup creates a board or seeds its
        // RNG. Keeping the complete printed card catalog therefore cannot make
        // an unsupported expansion game partly start.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(
            scenario, [hero], modular is null ? null : [modular], Seed: 7);

        var refused = Assert.Throws<KeyNotFoundException>(() => factory.Create(specification));

        Assert.Contains(message, refused.Message, StringComparison.Ordinal);
    }

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
            "request-2", "game-1", RequiredCapability(opened), EngineDecision.Decline));

        Assert.Null(resolved.Error);
        Assert.NotNull(resolved.Prompt);
        Assert.Equal("request-2", resolved.RequestId);
    }

    [Fact]
    public void SeparateCapabilitiesMayUseTheSameClientChosenGameId()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new SequenceCapabilities("first-capability", "second-capability"));
        var request = EngineRequest.OpenGame(
            "first",
            "chosen-id",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7));

        var first = host.Exchange(request);
        var duplicate = host.Exchange(request with { RequestId = "again" });

        Assert.Null(first.Error);
        Assert.Null(duplicate.Error);
        Assert.NotEqual(first.Capability, duplicate.Capability);
    }

    [Fact]
    public void InvalidCommandsFailBeforeTheyReachAFactory()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        var wrongVersion = host.Exchange(new EngineRequest(
            99, "version", EngineProtocol.Open, "game",
            Game: new GameSpecification("rhino", ["spider_man"], null, 1)));
        var unknownGame = host.Exchange(EngineRequest.ResolveGame(
            "resolve", "missing", "missing-capability", EngineDecision.Decline));

        Assert.Equal("unsupported_version", wrongVersion.Error?.Code);
        Assert.Equal("session_not_found", unknownGame.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void EngineFailuresDoNotReturnInternalDiagnostics()
    {
        var host = new EngineHost(new FailingFactory("secret-card-identity"));

        EngineResponse failed = host.Exchange(EngineRequest.OpenGame(
            "open",
            "game",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));

        Assert.Equal("engine_error", failed.Error?.Code);
        Assert.DoesNotContain(
            "secret-card-identity", failed.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionOneIsRejectedBeforeItCanOpenAGame()
    {
        // The current protocol adds play-area topology kinds to the event
        // union. A version 1 client cannot deserialize those responses, so it is
        // rejected before the factory can create mutable game state.
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        var rejected = host.Exchange(new EngineRequest(
            1, "old-client", EngineProtocol.Open, "game",
            Game: new GameSpecification("rhino", ["spider_man"], null, 1)));

        Assert.Equal(3, EngineProtocol.Version);
        Assert.Equal(EngineProtocol.Version, rejected.Version);
        Assert.Equal("unsupported_version", rejected.Error?.Code);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void ClosingAGameReleasesItsClientChosenId()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var specification = new GameSpecification(
            "rhino", ["spider_man"], ModularSets: [], Seed: 7);
        var opened = host.Exchange(
            EngineRequest.OpenGame("open", "reusable", specification));
        Assert.Null(opened.Error);

        Assert.Null(host.Exchange(
            EngineRequest.CloseGame(
                "close", "reusable", RequiredCapability(opened))).Error);
        Assert.Null(host.Exchange(
            EngineRequest.OpenGame("reopen", "reusable", specification)).Error);
    }

    [Fact]
    public void AFailedResolveCannotLeaveAPartialGameAvailable()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var opened = host.Exchange(EngineRequest.OpenGame(
            "open",
            "fail-closed",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        Assert.Null(opened.Error);
        string capability = RequiredCapability(opened);

        var rejected = host.Exchange(EngineRequest.ResolveGame(
            "bad", "fail-closed", capability, new EngineDecision(999, [])));
        var after = host.Exchange(EngineRequest.ResolveGame(
            "after", "fail-closed", capability, EngineDecision.Decline));

        Assert.Equal("game_aborted", rejected.Error?.Code);
        Assert.Equal("session_not_found", after.Error?.Code);
    }

    [Fact]
    public void RestrictedHostsDoNotTrustWatchOrHotSeatClaims()
    {
        var specification = new GameSpecification(
            "rhino", ["spider_man", "captain_marvel"], [], Seed: 7);
        var seatZeroHost = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            visibility: new RestrictedVisibilityPolicy(0));
        var seatOneHost = new EngineHost(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            visibility: new RestrictedVisibilityPolicy(1));

        EngineResponse zero = seatZeroHost.Exchange(EngineRequest.OpenGame(
            "zero", "game", specification, new ViewerClaim(Watch: true)));
        EngineResponse one = seatOneHost.Exchange(EngineRequest.OpenGame(
            "one", "game", specification, new ViewerClaim(HotSeat: true)));

        Assert.Null(zero.Error);
        Assert.Null(one.Error);
        Assert.All(Hand(zero, 0), card => Assert.NotNull(card.Face));
        Assert.All(Hand(zero, 1), card =>
        {
            Assert.Null(card.Face);
            Assert.Null(card.Id);
        });
        Assert.All(Hand(one, 0), card =>
        {
            Assert.Null(card.Face);
            Assert.Null(card.Id);
        });
        Assert.All(Hand(one, 1), card => Assert.NotNull(card.Face));
    }

    [Fact]
    public void FileReadingAndCheatOperationsHaveNoServedSurface()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);

        EngineResponse read = host.Exchange(new EngineRequest(
            EngineProtocol.Version, "read", "read_file", "game"));
        EngineResponse cheat = host.Exchange(new EngineRequest(
            EngineProtocol.Version, "cheat", "cheat", "game"));

        Assert.Equal("invalid_request", read.Error?.Code);
        Assert.Equal("invalid_request", cheat.Error?.Code);
        Assert.Equal(0, factory.Calls);
        Assert.DoesNotContain(
            typeof(EngineRequest).GetProperties(),
            property => property.Name.Contains("File", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Cheat", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<Marvel.View.CardDescriptor> Hand(
        EngineResponse response, int seat) =>
        Assert.Single(
            Assert.IsType<WorldDescriptor>(response.World).Areas,
            area => area.Zone == "HandsArea" && area.Owner == seat).Cards;

    private static string RequiredCapability(EngineResponse response) =>
        Assert.IsType<string>(response.Capability);

    private sealed class UnusedFactory : IGameFactory
    {
        public int Calls { get; private set; }

        public OpenedGame Create(GameSpecification specification)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }

    private sealed class SequenceCapabilities(params string[] capabilities)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);

        public string Issue() => capabilities.Dequeue();
    }

    private sealed class FailingFactory(string message) : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification) =>
            throw new InvalidOperationException(message);
    }
}
