using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngagementReplayRouteTests
{
    private static readonly string[] OtherDestinations = ["engine-replay-v25", "engine-replay-v27"];

    [Theory]
    [InlineData(SessionReplayUpgrade.CoreRorsV19ToV26)]
    [InlineData(SessionReplayUpgrade.CoreRorsV20ToV26)]
    [InlineData(SessionReplayUpgrade.CoreRorsV21ToV26)]
    public void V26RouteRejectsAnotherDestinationBeforeOpeningGameplay(SessionReplayUpgrade route)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var original = Source(previous.Save, factory, route);
            string bytes = SessionSaveJson.Write(original);
            foreach (string destination in OtherDestinations)
            {
                var target = factory.Compatibility with { ReplayContract = destination };
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
            }
        });
    }

    [Theory]
    [InlineData("engine-replay-v22", SessionReplayUpgrade.CoreRorsV19ToV22)]
    [InlineData("engine-replay-v23", SessionReplayUpgrade.CoreRorsV19ToV23)]
    [InlineData("engine-replay-v24", SessionReplayUpgrade.CoreRorsV19ToV24)]
    [InlineData("engine-replay-v25", SessionReplayUpgrade.CoreRorsV19ToV25)]
    [InlineData("engine-replay-v26", SessionReplayUpgrade.CoreRorsV19ToV26)]
    [InlineData("engine-replay-v27", SessionReplayUpgrade.CoreRorsV19ToV27)]
    public void FrozenDestinationsRetainTheirNamedRoutesAndOriginalFacts(
        string destination, SessionReplayUpgrade route)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var target = factory.Compatibility with { ReplayContract = destination };
            var admitted = Assert.IsType<SessionContentUpgrade>(
                ReplayContractUpgradeTests.Admission(root, target).For(previous.Save.Compatibility));
            var candidate = admitted.Verify(previous.Save, setup => ReplayContractUpgradeTests.Open(factory, setup));
            var explicitCandidate = new SessionContentUpgrade(previous.Save.Compatibility, target, route)
                .Verify(previous.Save, setup => ReplayContractUpgradeTests.Open(factory, setup));
            Assert.Equal(target, candidate.Compatibility);
            Assert.Equal(SessionSaveJson.Write(explicitCandidate), SessionSaveJson.Write(candidate));
            Assert.Equal(SessionSaveJson.Write(previous.Save),
                SessionSaveJson.Write(candidate with { Compatibility = previous.Save.Compatibility }));
        });
    }

    private static SessionSave Source(SessionSave original, DatasetGameFactory factory, SessionReplayUpgrade route)
    {
        if (route == SessionReplayUpgrade.CoreRorsV19ToV26) return original;
        bool v20 = route == SessionReplayUpgrade.CoreRorsV20ToV26;
        var target = original.Compatibility with { ReplayContract = v20 ? "engine-replay-v20" : "engine-replay-v21" };
        // These intermediates are derived evidence retaining all authentic v19 facts, not historical recordings.
        return new SessionContentUpgrade(original.Compatibility, target,
            v20 ? SessionReplayUpgrade.CoreRorsV19ToV20 : SessionReplayUpgrade.CoreRorsV19ToV21)
            .Verify(original, setup => ReplayContractUpgradeTests.Open(factory, setup));
    }
}
