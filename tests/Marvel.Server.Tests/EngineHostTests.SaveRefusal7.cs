using System.Text.Json.Nodes;
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

    [Theory]
    [InlineData(6, "Start a new game")]
    [InlineData(8, "newer version")]
    public void ASaveUnderAnotherSchemaIsRefusedToItsHolderAndLeftUntouched(int schema, string advice)
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-schema-refusal-" + Guid.NewGuid().ToString("N"));
        try
        {
            IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
            var first = new EngineHost(factory, new SequenceCapabilities("schema-owner"), store: new FileSessionStore(root));
            Assert.Null(first.Exchange(EngineRequest.OpenGame("open", "schema-table",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 73))).Error);
            string file = Assert.Single(Directory.GetFiles(root, "*.session.json", SearchOption.AllDirectories));
            var document = JsonNode.Parse(File.ReadAllText(file))!;
            document["schema"] = schema;
            File.WriteAllText(file, document.ToJsonString());
            string written = File.ReadAllText(file);

            var restarted = new EngineHost(factory, store: new FileSessionStore(root));

            EngineError refused = Assert.IsType<EngineError>(restarted.Exchange(
                EngineRequest.SyncGame("sync", "schema-table", "schema-owner")).Error);
            Assert.Equal("save_incompatible", refused.Code);
            Assert.Contains(advice, refused.Message, StringComparison.Ordinal);
            Assert.Equal("session_not_found", restarted.Exchange(
                EngineRequest.SyncGame("stranger", "schema-table", "someone-else")).Error?.Code);
            Assert.Equal(written, File.ReadAllText(file));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void AnUnreadableSaveUnderTheCurrentSchemaIsNotCalledIncompatible()
    {
        Assert.Null(SessionSchemaProbe.Refusal("{\"schema\":7,\"session\":{\"label\":\"t\"}}", "[]"));
        Assert.Null(SessionSchemaProbe.Refusal("{\"schema\":6}", "[]"));
        Assert.Null(SessionSchemaProbe.Refusal("{\"schema\":6,\"session\":{\"label\":\"\"}}", "[]"));
        Assert.Null(SessionSchemaProbe.Refusal("[]", "[]"));
        Assert.Null(SessionSchemaProbe.Refusal("{\"schema\":\"6\",\"session\":{\"label\":\"t\"}}", "[]"));
        Assert.Null(SessionSchemaProbe.Refusal("{\"schema\":6,\"session\":{\"label\":\"t\"}}", "not-json"));
    }
}
