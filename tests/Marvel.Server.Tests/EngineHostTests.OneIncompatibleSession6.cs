using Marvel.Decisions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public sealed class EngineHostOneIncompatibleSessionTests : EngineHostTestBase
{
    [Fact]
    public void OneIncompatibleSessionIsQuarantinedWithoutHidingAHealthySession()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("bad-owner", "healthy-owner"), store: store);
        _ = first.Exchange(EngineRequest.OpenGame("bad-open", "bad-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        _ = first.Exchange(EngineRequest.OpenGame("healthy-open", "healthy-table", new GameSpecification("rhino", ["captain_marvel"], [], Seed: 74)));
        StoredSession[] saved = [..store.Load()];
        StoredSession bad = saved.Single(session => session.Save.Session.Label == "bad-table");
        StoredSession healthy = saved.Single(session => session.Save.Session.Label == "healthy-table");
        bad = bad with
        {
            Save = bad.Save with
            {
                Compatibility = bad.Save.Compatibility with
                {
                    ReplayContract = "future-contract",
                },
            },
        };
        var restarted = new EngineHost(factory, store: new FixedSessionStore(bad, healthy));
        Assert.Equal("session_not_found", restarted.Exchange(EngineRequest.SyncGame("bad", "bad-table", "bad-owner")).Error?.Code);
        Assert.Null(restarted.Exchange(EngineRequest.SyncGame("healthy", "healthy-table", "healthy-owner")).Error);
    }

    [Fact]
    public void APartlyReadAuthorityListPublishesNoneOfTheQuarantinedSession()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("partial-owner"), store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame("open", "partial-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        StoredSession stored = Assert.Single(store.Load());
        stored = stored with
        {
            Authorities = [..stored.Authorities, new StoredAuthority(new string ('f', 64), [1], Owner: false, Invitation: false), ],
        };
        var restarted = new EngineHost(factory, store: new FixedSessionStore(stored));
        Assert.Equal("session_not_found", restarted.Exchange(EngineRequest.SyncGame("sync", "partial-table", RequiredCapability(opened))).Error?.Code);
    }

    [Fact]
    public void EveryGameplayCommitStampsTheCurrentVersionAndBlocksADowngrade()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory inner = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(new VersionedFactory(inner, "0.1.0"), new SequenceCapabilities("version-owner"), store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame("open", "version-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        var upgraded = new EngineHost(new VersionedFactory(inner, "0.1.1-preview.2"), store: store);
        EngineResponse current = upgraded.Exchange(EngineRequest.SyncGame("sync", "version-table", "version-owner"));
        EngineResponse resolved = upgraded.Exchange(EngineRequest.ResolveGame("resolve", "version-table", "version-owner", TakeOnly(current), current.Revision));
        Assert.Null(resolved.Error);
        Assert.Equal("0.1.1-preview.2", Assert.Single(store.Load()).Save.Compatibility.Application);
        var downgraded = new EngineHost(new VersionedFactory(inner, "0.1.1-preview.1"), store: store);
        Assert.Equal("session_not_found", downgraded.Exchange(EngineRequest.SyncGame("downgrade", "version-table", "version-owner")).Error?.Code);
    }

    [Fact]
    public void FileReadingAndCheatOperationsHaveNoServedSurface()
    {
        var factory = new UnusedFactory();
        var host = new EngineHost(factory);
        EngineResponse read = host.Exchange(new EngineRequest(EngineProtocol.Version, "read", "read_file", "game"));
        EngineResponse cheat = host.Exchange(new EngineRequest(EngineProtocol.Version, "cheat", "cheat", "game"));
        Assert.Equal("invalid_request", read.Error?.Code);
        Assert.Equal("invalid_request", cheat.Error?.Code);
        Assert.Equal(0, factory.Calls);
        Assert.DoesNotContain(typeof(EngineRequest).GetProperties(), property => property.Name.Contains("File", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Cheat", StringComparison.OrdinalIgnoreCase));
    }
}
