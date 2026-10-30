using Marvel.Protocol;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngineHostSaveRefusalTests : EngineHostTestBase
{
    [Theory]
    [InlineData("replay")]
    [InlineData("rng")]
    [InlineData("digest")]
    [InlineData("cards")]
    [InlineData("setup")]
    [InlineData("abilities")]
    public void ASaveUnderAnyOtherIdentityIsRefusedToItsOwnAuthorityOnly(string identity)
    {
        var store = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("refused-owner"), store: store);
        var open = EngineRequest.OpenGame("open", "refused-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73));
        Assert.Null(first.Exchange(open).Error);
        StoredSession saved = Assert.Single(store.Load());
        SessionCompatibility other = saved.Save.Compatibility;
        other = identity switch
        {
            "replay" => other with { ReplayContract = "engine-replay-v1" },
            "rng" => other with { RngContract = "other-rng" },
            "digest" => other with { StateDigest = "state-digest-v1" },
            "cards" => other with { CardsSha256 = new string('a', 64) },
            "setup" => other with { SetupSha256 = new string('a', 64) },
            _ => other with { AbilitiesSha256 = new string('a', 64) },
        };
        StoredSession older = saved with { Save = saved.Save with { Compatibility = other } };

        var restarted = new EngineHost(factory, store: new FixedSessionStore(older));

        EngineError synced = Assert.IsType<EngineError>(
            restarted.Exchange(EngineRequest.SyncGame("sync", "refused-table", "refused-owner")).Error);
        Assert.Equal("save_incompatible", synced.Code);
        Assert.Equal(EngineFailureDisposition.SessionUnavailable, synced.Disposition);
        Assert.Equal("save_incompatible", restarted.Exchange(open with { RequestId = "recover" }).Error?.Code);
        Assert.Equal("session_not_found",
            restarted.Exchange(EngineRequest.SyncGame("other", "refused-table", "someone-else")).Error?.Code);
        Assert.Equal("session_not_found",
            restarted.Exchange(EngineRequest.SyncGame("label", "another-table", "refused-owner")).Error?.Code);
    }

    [Fact]
    public void ARevokedAuthorityOfARefusedSaveLearnsNothing()
    {
        var store = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("revoked-owner"), store: store);
        Assert.Null(first.Exchange(EngineRequest.OpenGame("open", "revoked-table",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73))).Error);
        StoredSession saved = Assert.Single(store.Load());
        StoredSession older = new(
            saved.Save with { Compatibility = saved.Save.Compatibility with { ReplayContract = "engine-replay-v1" } },
            [.. saved.Authorities.Select(authority => authority with { Revoked = true })]);

        var restarted = new EngineHost(factory, store: new FixedSessionStore(older));

        Assert.Equal("session_not_found",
            restarted.Exchange(EngineRequest.SyncGame("sync", "revoked-table", "revoked-owner")).Error?.Code);
    }
}
