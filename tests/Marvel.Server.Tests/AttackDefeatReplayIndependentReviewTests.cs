using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class AttackDefeatReplayIndependentReviewTests
{
    [Theory]
    [InlineData("cards")]
    [InlineData("setup")]
    [InlineData("abilities")]
    [InlineData("rng")]
    [InlineData("digest")]
    [InlineData("engine-replay-v22")]
    [InlineData("engine-replay-v23")]
    [InlineData("engine-replay-v24")]
    [InlineData("engine-replay-v25")]
    [InlineData("engine-replay-v26")]
    [InlineData("engine-replay-v27")]
    [InlineData("engine-replay-v29")]
    [InlineData("unknown")]
    public void V28AdmissionRequiresEveryFrozenSourceIdentity(string changed)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, baseline) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var source = previous.Save.Compatibility;
            // Synthetic identity changes exercise admission, not historical gameplay.
            var rejected = changed switch
            {
                "cards" => source with { CardsSha256 = new string('a', 64) },
                "setup" => source with { SetupSha256 = new string('a', 64) },
                "abilities" => source with { AbilitiesSha256 = new string('a', 64) },
                "rng" => source with { RngContract = "unknown" },
                "digest" => source with { StateDigest = "unknown" },
                _ => source with { ReplayContract = changed },
            };
            Assert.Equal("engine-replay-v28", factory.Compatibility.ReplayContract);
            Assert.NotNull(factory.ContentUpgrade(source));
            Assert.Null(factory.ContentUpgrade(rejected));
        });
    }

    [Fact]
    public void AuthenticV19DiskUpgradeRetainsOriginalSessionAndAuthorityBytes()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, baseline) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            string storage = Path.Combine(root, "real-v19", "sessions");
            string directory = Path.Combine(storage, previous.Save.Session.StorageId);
            string generation = File.ReadAllText(Path.Combine(directory, "current")).Trim();
            byte[] session = File.ReadAllBytes(Path.Combine(directory, generation + ".session.json"));
            byte[] authority = File.ReadAllBytes(Path.Combine(directory, generation + ".authority.json"));
            Assert.NotEmpty(previous.Save.Units);
            Assert.NotEmpty(previous.Save.Units.SelectMany(unit => unit.Decisions));
            string original = StoredSessionJson.Write(previous);
            var store = new FileSessionStore(storage);

            _ = new EngineHost(factory, store: store);

            var current = Assert.Single(store.Load());
            Assert.Equal(factory.Compatibility, current.Save.Compatibility);
            Assert.Equal("engine-replay-v28", current.Save.Compatibility.ReplayContract);
            Assert.Equal(original, StoredSessionJson.Write(current with
                { Save = current.Save with { Compatibility = previous.Save.Compatibility } }));
            Assert.Equal(session, File.ReadAllBytes(Path.Combine(directory,
                ".upgrade-" + generation + ".session.json")));
            Assert.Equal(authority, File.ReadAllBytes(Path.Combine(directory,
                ".upgrade-" + generation + ".authority.json")));
            string published = File.ReadAllText(Path.Combine(directory, "current"));
            Assert.NotEqual(generation, published.Trim());

            _ = new EngineHost(factory, store: store);

            Assert.Equal(published, File.ReadAllText(Path.Combine(directory, "current")));
            Assert.Equal(session, File.ReadAllBytes(Path.Combine(directory,
                ".upgrade-" + generation + ".session.json")));
            Assert.Equal(authority, File.ReadAllBytes(Path.Combine(directory,
                ".upgrade-" + generation + ".authority.json")));
            Assert.Single(Directory.GetFiles(directory, ".upgrade-*.session.json"));
            Assert.Single(Directory.GetFiles(directory, ".upgrade-*.authority.json"));
        });
    }
}
