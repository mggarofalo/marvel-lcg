using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ReplayV29RouteTests
{
    [Theory]
    [InlineData(SessionReplayUpgrade.CoreRorsV19ToV29, "engine-replay-v19")]
    [InlineData(SessionReplayUpgrade.CoreRorsV20ToV29, "engine-replay-v20")]
    [InlineData(SessionReplayUpgrade.CoreRorsV21ToV29, "engine-replay-v21")]
    public void RouteRequiresItsExactSourceAndDestination(SessionReplayUpgrade route, string sourceReplay)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var source = previous.Save.Compatibility with { ReplayContract = sourceReplay };
            var original = previous.Save with { Compatibility = source };
            foreach (var pair in new[] { (source, "engine-replay-v28"),
                (source with { ReplayContract = "engine-replay-v28" }, "engine-replay-v29") })
            {
                bool opened = false;
                var failure = Assert.Throws<SessionCompatibilityException>(() =>
                    new SessionContentUpgrade(pair.Item1, factory.Compatibility with { ReplayContract = pair.Item2 }, route)
                        .Verify(original with { Compatibility = pair.Item1 }, setup =>
                        {
                            opened = true;
                            return ReplayContractUpgradeTests.Open(factory, setup);
                        }));
                Assert.Equal("replay_identity_mismatch", failure.Category);
                Assert.False(opened);
            }
        });
    }

    [Theory]
    [InlineData("downgrade", "unsupported_downgrade")]
    [InlineData("rng", "rng_identity_mismatch")]
    [InlineData("digest", "digest_identity_mismatch")]
    public void V29RouteKeepsIndependentRuntimeGuards(string contract, string category)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var target = contract switch
            {
                "downgrade" => factory.Compatibility with { Application = "0.0.0" },
                "rng" => factory.Compatibility with { RngContract = "unknown" },
                _ => factory.Compatibility with { StateDigest = "unknown" },
            };
            bool opened = false;
            var failure = Assert.Throws<SessionCompatibilityException>(() =>
                new SessionContentUpgrade(previous.Save.Compatibility, target,
                    SessionReplayUpgrade.CoreRorsV19ToV29).Verify(previous.Save, setup =>
                {
                    opened = true;
                    return ReplayContractUpgradeTests.Open(factory, setup);
                }));
            Assert.Equal(category, failure.Category);
            Assert.False(opened);
        });
    }

    [Theory]
    [InlineData("engine-replay-v18")]
    [InlineData("engine-replay-v22")]
    [InlineData("engine-replay-v23")]
    [InlineData("engine-replay-v24")]
    [InlineData("engine-replay-v25")]
    [InlineData("engine-replay-v26")]
    [InlineData("engine-replay-v27")]
    [InlineData("engine-replay-v28")]
    [InlineData("engine-replay-v30")]
    public void V29AdmissionRejectsOtherReplaySources(string replay)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            // Synthetic identities exercise admission, not historical gameplay.
            Assert.Equal("engine-replay-v29", factory.Compatibility.ReplayContract);
            Assert.NotNull(factory.ContentUpgrade(previous.Save.Compatibility));
            var source = previous.Save.Compatibility with { ReplayContract = replay };
            Assert.Null(factory.ContentUpgrade(source));
        });
    }

    [Theory]
    [InlineData("rng")]
    [InlineData("digest")]
    [InlineData("cards")]
    [InlineData("setup")]
    [InlineData("abilities")]
    public void V29AdmissionRejectsUnknownSourceIdentity(string part)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var source = previous.Save.Compatibility;
            Assert.Equal("engine-replay-v29", factory.Compatibility.ReplayContract);
            Assert.NotNull(factory.ContentUpgrade(source));
            var unknown = new string('a', 64);
            source = part switch
            {
                "rng" => source with { RngContract = "unknown" },
                "digest" => source with { StateDigest = "unknown" },
                "cards" => source with { CardsSha256 = unknown },
                "setup" => source with { SetupSha256 = unknown },
                _ => source with { AbilitiesSha256 = unknown },
            };
            Assert.Null(factory.ContentUpgrade(source));
        });
    }
}
