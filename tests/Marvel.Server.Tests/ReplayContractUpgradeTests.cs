using System.IO.Compression;
using Marvel.Protocol;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ReplayContractUpgradeTests
{
    [Theory]
    [InlineData("engine-replay-v19")]
    [InlineData("engine-replay-v20")]
    [InlineData("engine-replay-v21")]
    public void CurrentNamedAdapterVerifiesTheOriginalHistory(string sourceReplay)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var original = previous.Save;
            if (sourceReplay != original.Compatibility.ReplayContract)
            {
                var intermediate = original.Compatibility with { ReplayContract = sourceReplay };
                var route = sourceReplay == "engine-replay-v20"
                    ? SessionReplayUpgrade.CoreRorsV19ToV20 : SessionReplayUpgrade.CoreRorsV19ToV21;
                // Derived intermediate evidence retains every fact from the authentic v19 trace.
                original = new SessionContentUpgrade(original.Compatibility, intermediate, route)
                    .Verify(original, setup => Open(factory, setup));
            }
            string bytes = SessionSaveJson.Write(original);
            var upgrade = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(original.Compatibility));
            var current = upgrade.Verify(original, setup => Open(factory, setup));
            Assert.Equal("engine-replay-v28", current.Compatibility.ReplayContract);
            Assert.Equal(bytes, SessionSaveJson.Write(current with { Compatibility = original.Compatibility }));
            Assert.Equal(bytes, SessionSaveJson.Write(original));
            Assert.Null(factory.ContentUpgrade(original.Compatibility with { ReplayContract = "engine-replay-v22" }));
        });
    }

    [Fact]
    public void NamedAdapterVerifiesActualV19SaveBeforeChangingOnlyCompatibility()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var target = factory.Compatibility with { ReplayContract = "engine-replay-v22" };
            var admission = Admission(root, target);
            var route = Assert.IsType<SessionContentUpgrade>(admission.For(previous.Save.Compatibility));
            string original = SessionSaveJson.Write(previous.Save);
            var candidate = route.Verify(previous.Save, setup => Open(factory, setup));
            Assert.Equal(target, candidate.Compatibility);
            Assert.Equal(original, SessionSaveJson.Write(candidate with { Compatibility = previous.Save.Compatibility }));
            Assert.Equal(original, SessionSaveJson.Write(previous.Save));
            SessionReplay.Verify(candidate, target, setup => Open(factory, setup));
            Assert.Throws<SessionCompatibilityException>(() => SessionReplay.Verify(previous.Save, target, setup => Open(factory, setup)));
        });
    }

    [Fact]
    public void NamedV20AdapterVerifiesEveryFactOfAnExplicitlyVerifiedIntermediateSave()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var sameContentV20 = previous.Save.Compatibility with { ReplayContract = "engine-replay-v20" };
            var v20 = new SessionContentUpgrade(previous.Save.Compatibility, sameContentV20,
                SessionReplayUpgrade.CoreRorsV19ToV20)
                .Verify(previous.Save, setup => Open(factory, setup));
            var target = factory.Compatibility with { ReplayContract = "engine-replay-v22" };
            Assert.NotEqual(sameContentV20.SetupSha256, target.SetupSha256);
            var candidate = Assert.IsType<SessionContentUpgrade>(Admission(root, target).For(sameContentV20))
                .Verify(v20, setup => Open(factory, setup));
            Assert.Equal(target, candidate.Compatibility);
            Assert.Equal(SessionSaveJson.Write(v20), SessionSaveJson.Write(candidate with { Compatibility = sameContentV20 }));
            Assert.Null(Admission(root, sameContentV20).For(sameContentV20));
            Assert.Null(Admission(root, target).For(sameContentV20 with { ReplayContract = "engine-replay-v23" }));
        });
    }

    [Fact]
    public void NamedV21AdapterRequiresAnExactlyVerifiedIntermediateHistory()
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var intermediate = previous.Save.Compatibility with { ReplayContract = "engine-replay-v21" };
            // This verified intermediate is derived evidence, not a historical v21 recording.
            var v21 = new SessionContentUpgrade(previous.Save.Compatibility, intermediate,
                SessionReplayUpgrade.CoreRorsV19ToV21).Verify(previous.Save, setup => Open(factory, setup));
            var route = Assert.IsType<SessionContentUpgrade>(Admission(root, factory.Compatibility).For(intermediate));
            var candidate = route.Verify(v21, setup => Open(factory, setup));
            Assert.Equal(factory.Compatibility, candidate.Compatibility);
            Assert.Equal(SessionSaveJson.Write(v21), SessionSaveJson.Write(candidate with { Compatibility = intermediate }));
            Assert.Throws<SessionCompatibilityException>(() => SessionReplay.Verify(v21,
                factory.Compatibility, setup => Open(factory, setup)));
        });
    }

    [Theory]
    [InlineData("replay")]
    [InlineData("rng")]
    [InlineData("digest")]
    [InlineData("content")]
    public void UnknownSourceIdentityHasNoNamedRoute(string category)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var source = previous.Save.Compatibility;
            source = category switch
            {
                "replay" => source with { ReplayContract = "engine-replay-v18" },
                "rng" => source with { RngContract = "unknown" },
                "digest" => source with { StateDigest = "unknown" },
                _ => source with { CardsSha256 = new string('a', 64) },
            };
            Assert.Null(Admission(root, factory.Compatibility with { ReplayContract = "engine-replay-v22" }).For(source));
        });
    }

    [Theory]
    [InlineData("destination")]
    [InlineData("downgrade")]
    [InlineData("rng")]
    [InlineData("digest")]
    public void NamedAdapterDoesNotBypassOtherRuntimeContracts(string category)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var source = previous.Save.Compatibility;
            var target = factory.Compatibility with { ReplayContract = "engine-replay-v22" };
            target = category switch
            {
                "destination" => target with { ReplayContract = "engine-replay-v23" },
                "rng" => target with { RngContract = "unknown" },
                "digest" => target with { StateDigest = "unknown" },
                _ => target with { Application = "0.0.0" },
            };
            var route = new SessionContentUpgrade(source, target, SessionReplayUpgrade.CoreRorsV19ToV22);
            bool opened = false;
            Assert.Throws<SessionCompatibilityException>(() => route.Verify(previous.Save, setup =>
            {
                opened = true;
                return Open(factory, setup);
            }));
            Assert.False(opened);
        });
    }

    [Theory]
    [InlineData("engine-replay-v19")]
    [InlineData("engine-replay-v20")]
    [InlineData("engine-replay-v21")]
    [InlineData("engine-replay-v29")]
    public void HostAdmissionRequiresThisDeliveryDestination(string destination)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var admission = Admission(root, factory.Compatibility with { ReplayContract = destination });
            Assert.Null(admission.For(previous.Save.Compatibility));
            Assert.Null(admission.For(previous.Save.Compatibility with { ReplayContract = "engine-replay-v20" }));
        });
    }

    [Theory]
    [InlineData("engine-replay-v22")]
    [InlineData("engine-replay-v24")]
    [InlineData("engine-replay-v25")]
    [InlineData("engine-replay-v26")]
    [InlineData("engine-replay-v27")]
    [InlineData("engine-replay-v28")]
    public void DivergentRecordedGameplayIsRejectedWithoutProducingAnUpgrade(string destination)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = Fixture(root);
            var original = previous.Save with { Initial = previous.Save.Initial with { StateDigest = "wrong historical outcome" } };
            string bytes = SessionSaveJson.Write(original);
            var route = Assert.IsType<SessionContentUpgrade>(Admission(root,
                factory.Compatibility with { ReplayContract = destination }).For(original.Compatibility));
            Assert.Throws<ReplayDivergenceException>(() => route.Verify(original, setup => Open(factory, setup)));
            Assert.Equal(bytes, SessionSaveJson.Write(original));
        });
    }

    internal static ContentUpgradeAdmission Admission(string root, SessionCompatibility target) => new(target,
        File.ReadAllBytes(Path.Combine(root, "datasets", "cards", "cards.json")),
        File.ReadAllBytes(Path.Combine(root, "datasets", "setup", "setup.json")),
        File.ReadAllBytes(Path.Combine(root, "datasets", "abilities", "abilities.json")));

    internal static (StoredSession Previous, DatasetGameFactory Factory) Fixture(string root)
    {
        string directory = Path.Combine(root, "real-v19");
        ZipFile.ExtractToDirectory(Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests",
            "Fixtures", "core-session-91098022.zip"), directory);
        var previous = Assert.Single(new FileSessionStore(Path.Combine(directory, "sessions")).Load());
        Assert.Equal("engine-replay-v19", previous.Save.Compatibility.ReplayContract);
        return (previous, DatasetGameFactory.Load(root));
    }

    internal static ReplayOpenedGame Open(DatasetGameFactory factory, SessionSetup setup)
    {
        var opened = factory.Create(new GameSpecification(setup.Scenario, setup.Heroes, setup.ModularSets,
            setup.Seed, setup.PlayerDecks) { Campaign = setup.Campaign });
        return new(opened.Game, opened.SetupEvents);
    }
}
