using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class AllyEntryReplayRouteTests
{
    private static readonly string[] OtherDestinations = ["engine-replay-v24", "engine-replay-v26"];

    [Theory]
    [InlineData("engine-replay-v19", SessionReplayUpgrade.CoreRorsV19ToV25)]
    [InlineData("engine-replay-v20", SessionReplayUpgrade.CoreRorsV20ToV25)]
    [InlineData("engine-replay-v21", SessionReplayUpgrade.CoreRorsV21ToV25)]
    public void V25RouteCannotPublishToAnOlderOrFutureDestination(
        string sourceReplay, SessionReplayUpgrade route)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var original = previous.Save;
            if (sourceReplay != original.Compatibility.ReplayContract)
            {
                // Derived intermediates retain every fact of the authentic v19 trace.
                var intermediate = original.Compatibility with { ReplayContract = sourceReplay };
                var historical = sourceReplay == "engine-replay-v20"
                    ? SessionReplayUpgrade.CoreRorsV19ToV20 : SessionReplayUpgrade.CoreRorsV19ToV21;
                original = new SessionContentUpgrade(original.Compatibility, intermediate, historical)
                    .Verify(original, setup => ReplayContractUpgradeTests.Open(factory, setup));
            }
            string bytes = SessionSaveJson.Write(original);
            foreach (string destination in OtherDestinations)
            {
                var target = factory.Compatibility with { ReplayContract = destination };
                var upgrade = new SessionContentUpgrade(original.Compatibility, target, route);
                bool opened = false;
                var failure = Assert.Throws<SessionCompatibilityException>(() => upgrade.Verify(original, setup =>
                {
                    opened = true;
                    return ReplayContractUpgradeTests.Open(factory, setup);
                }));
                Assert.Equal("replay_identity_mismatch", failure.Category);
                Assert.False(opened);
                Assert.Equal(bytes, SessionSaveJson.Write(original));
            }
        });
    }
}
