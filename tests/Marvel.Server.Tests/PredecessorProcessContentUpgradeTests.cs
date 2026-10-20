using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PredecessorProcessContentUpgradeTests
{
    [Fact]
    public void ActualPredecessorProcessSaveRestoresAndContinuesLikeItsIndependentOldRuntimeControl()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            string archive = Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests",
                "Fixtures", "core-session-91098022.zip");
            Assert.Equal("63BD6E38C7668F6789BB48300165026D34CA55432B59ADF08FF2B19793E87ABC",
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))));
            string fixture = Path.Combine(root, "process-fixture");
            ZipFile.ExtractToDirectory(archive, fixture);
            var opened = Read(fixture, "opened");
            var guest = Read(fixture, "guest");
            var store = new FileSessionStore(Path.Combine(fixture, "sessions"));
            var host = new EngineHost(DatasetGameFactory.Load(root),
                visibility: new RestrictedVisibilityPolicy(0), store: store);
            var restored = host.Exchange(EngineRequest.SyncGame("snapshot", opened.GameId, guest.Capability!));
            Equivalent(Read(fixture, "before"), restored);
            var keep = Assert.Single(restored.Prompt!.Affordances, action => action.Verb == Game.ResolveMulligans);
            var continued = host.Exchange(EngineRequest.ResolveGame("keep-second", opened.GameId, guest.Capability!,
                Decision.Take(keep.Id), restored.Revision));
            Equivalent(Read(fixture, "control-continued"), continued);
            Assert.Single(Directory.GetFiles(Path.Combine(fixture, "sessions"),
                ".upgrade-*.session.json", SearchOption.AllDirectories));
        });
    }

    private static EngineResponse Read(string root, string name) =>
        HistoricalProtocol35Response.Read(File.ReadAllBytes(Path.Combine(root, "responses", name + ".json")));

    private static void Equivalent(EngineResponse expected, EngineResponse actual)
    {
        Assert.Null(actual.Error);
        // Wire-version numbering belongs to the current transport; all gameplay
        // and authorized presentation fields must retain their recorded values.
        expected = expected with { Version = EngineProtocol.Version };
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(EngineJson.Write(expected)),
            JsonNode.Parse(EngineJson.Write(actual))), "the complete authorized response changed");
    }
}
