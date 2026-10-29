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
}
