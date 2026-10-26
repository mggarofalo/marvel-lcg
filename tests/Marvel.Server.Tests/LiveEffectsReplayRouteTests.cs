using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class LiveEffectsReplayRouteTests
{
    [Theory]
    [InlineData("engine-replay-v19", SessionReplayUpgrade.CoreRorsV19ToV27)]
    [InlineData("engine-replay-v20", SessionReplayUpgrade.CoreRorsV20ToV27)]
    [InlineData("engine-replay-v21", SessionReplayUpgrade.CoreRorsV21ToV27)]
    public void V27RouteCannotPublishItsTraceAsV26(string sourceReplay, SessionReplayUpgrade route)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var original = Source(previous.Save, factory, sourceReplay);
            string bytes = SessionSaveJson.Write(original);
            var target = factory.Compatibility with { ReplayContract = "engine-replay-v26" };
            bool opened = false;
            var failure = Assert.Throws<SessionCompatibilityException>(() =>
                new SessionContentUpgrade(original.Compatibility, target, route).Verify(original, setup =>
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
    [InlineData("engine-replay-v19")]
    [InlineData("engine-replay-v20")]
    [InlineData("engine-replay-v21")]
    public void V27AdmissionNeverReplacesAnOriginalRngCount(string sourceReplay)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var verified = Source(previous.Save, factory, sourceReplay);
            var original = verified with { Initial = verified.Initial with { RngWords = verified.Initial.RngWords + 1 } };
            string bytes = SessionSaveJson.Write(original);
            var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Compatibility));
            var failure = Assert.Throws<ReplayDivergenceException>(() =>
                route.Verify(original, setup => ReplayContractUpgradeTests.Open(factory, setup)));
            Assert.Contains("rng", failure.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(bytes, SessionSaveJson.Write(original));
        });
    }

    [Theory]
    [InlineData("engine-replay-v22")]
    [InlineData("engine-replay-v23")]
    [InlineData("engine-replay-v24")]
    [InlineData("engine-replay-v25")]
    [InlineData("engine-replay-v26")]
    public void V27HostHasNoRouteFromAnUnadmittedReplayEvenWithFrozenContent(string sourceReplay)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            // Synthetic identity probes exercise admission, not historical gameplay evidence.
            var frozenSource = previous.Save.Compatibility with { ReplayContract = sourceReplay };
            var currentContentSource = factory.Compatibility with { ReplayContract = sourceReplay };
            Assert.Equal("engine-replay-v27", factory.Compatibility.ReplayContract);
            Assert.Null(factory.ContentUpgrade(frozenSource));
            Assert.Null(factory.ContentUpgrade(currentContentSource));
        });
    }

    private static SessionSave Source(SessionSave original, DatasetGameFactory factory, string sourceReplay)
    {
        if (sourceReplay == "engine-replay-v19") return original;
        // Derived intermediates retain all authentic v19 facts; they are not archived v20/v21 recordings.
        var target = original.Compatibility with { ReplayContract = sourceReplay };
        var route = sourceReplay == "engine-replay-v20"
            ? SessionReplayUpgrade.CoreRorsV19ToV20 : SessionReplayUpgrade.CoreRorsV19ToV21;
        return new SessionContentUpgrade(original.Compatibility, target, route)
            .Verify(original, setup => ReplayContractUpgradeTests.Open(factory, setup));
    }
}
