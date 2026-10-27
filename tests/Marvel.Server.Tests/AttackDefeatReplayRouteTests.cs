using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class AttackDefeatReplayRouteTests
{
    private static readonly string[] OtherDestinations = ["engine-replay-v27", "engine-replay-v29"];
    [Theory]
    [InlineData(SessionReplayUpgrade.CoreRorsV19ToV28, "engine-replay-v19")]
    [InlineData(SessionReplayUpgrade.CoreRorsV20ToV28, "engine-replay-v20")]
    [InlineData(SessionReplayUpgrade.CoreRorsV21ToV28, "engine-replay-v21")]
    public void V28RouteRequiresItsExactSourceAndDestinationBeforeOpening(
        SessionReplayUpgrade route, string sourceReplay)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            // Identity probes test admission only; verified intermediate history is covered separately.
            var source = previous.Save.Compatibility with { ReplayContract = sourceReplay };
            var original = previous.Save with { Compatibility = source };
            string bytes = SessionSaveJson.Write(original);
            foreach (string destination in OtherDestinations)
            {
                bool opened = false;
                var failure = Assert.Throws<SessionCompatibilityException>(() =>
                    new SessionContentUpgrade(source, factory.Compatibility with
                        { ReplayContract = destination }, route).Verify(original, setup =>
                    {
                        opened = true;
                        return ReplayContractUpgradeTests.Open(factory, setup);
                    }));
                Assert.Equal("replay_identity_mismatch", failure.Category);
                Assert.False(opened);
            }
            bool wrongSourceOpened = false;
            var wrongSource = source with { ReplayContract = "engine-replay-v27" };
            var wrong = original with { Compatibility = wrongSource };
            Assert.Throws<SessionCompatibilityException>(() =>
                new SessionContentUpgrade(wrongSource, factory.Compatibility, route).Verify(wrong, setup =>
                {
                    wrongSourceOpened = true;
                    return ReplayContractUpgradeTests.Open(factory, setup);
                }));
            Assert.False(wrongSourceOpened);
            Assert.Equal(bytes, SessionSaveJson.Write(original));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void V27HasNoMigrationAndStrictReplayRejectsBeforeOpening(bool currentContent)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            // These synthetic identity probes are not archived v27 gameplay evidence.
            var source = (currentContent ? factory.Compatibility : previous.Save.Compatibility) with
                { ReplayContract = "engine-replay-v27" };
            var original = previous.Save with { Compatibility = source };
            string bytes = SessionSaveJson.Write(original);
            Assert.Equal("engine-replay-v28", factory.Compatibility.ReplayContract);
            Assert.Null(factory.ContentUpgrade(source));
            bool opened = false;
            var failure = Assert.Throws<SessionCompatibilityException>(() =>
                SessionReplay.Verify(original, factory.Compatibility, setup =>
                {
                    opened = true;
                    return ReplayContractUpgradeTests.Open(factory, setup);
                }));
            Assert.Equal("replay_identity_mismatch", failure.Category);
            Assert.False(opened);
            Assert.Equal(bytes, SessionSaveJson.Write(original));
        });
    }

    [Theory]
    [InlineData("downgrade", "unsupported_downgrade")]
    [InlineData("rng", "rng_identity_mismatch")]
    [InlineData("digest", "digest_identity_mismatch")]
    public void V28RouteKeepsIndependentRuntimeGuards(string contract, string category)
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
                    SessionReplayUpgrade.CoreRorsV19ToV28).Verify(previous.Save, setup =>
                {
                    opened = true;
                    return ReplayContractUpgradeTests.Open(factory, setup);
                }));
            Assert.Equal(category, failure.Category);
            Assert.False(opened);
        });
    }
}
