using System.Text.Json;
using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreCardFaceTranscriptCoreModularNemesisBranchesHavePinnedOutcomesTests
{
    [Fact]
    public void CoreModularNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/core-modular-nemesis.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Scenario, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["specs/behavior/core/core-modular-nemesis.feature::The first player breaks an encounter attachment target tie"] = "db4bd0dec49fc8a970487dde68c0f3793673649679b7827a35db3f913226beed",
            ["specs/behavior/core/core-modular-nemesis.feature::Legions of Hydra finds Madame Hydra before counting Hydra enemies"] = "0da0c87d211528ec7e3085f92d5af9e16cc6b49fb6f08d56f8d6b11339778abd",
            ["specs/behavior/core/core-modular-nemesis.feature::Legions of Hydra counts every Hydra enemy already in play"] = "f898d922151d4aa3e329f81071bfe78d02b79d4fb54bf0983b8767c656b60660",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra places threat after attacking"] = "545637f393c8fdb5046e152ad803402e8ae64682fcfc75a389a8932c99eee702",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra chooses one of two Legions schemes for her threat"] = "fae40c75e08c5affea609535391e01ffb4d54be09ffd4b97e466ea43ee667d95",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra places threat after scheming"] = "6c74ea5a78fba678fc5cc49ea6245c173cc3619d5fa36bda0a85b245d7da312a",
            ["specs/behavior/core/core-modular-nemesis.feature::The Doomsday Chair finds M.O.D.O.K. outside play"] = "24ee06e2b821671842862d24a5218cd13f2c7b098d32c1cdaaeda093a80271ab",
            ["specs/behavior/core/core-modular-nemesis.feature::The Doomsday Chair does not search when M.O.D.O.K. is in play"] = "847ae5ffc504c0b4ff438390c144f71321ebdcd6c9ecb972ced76812f378fbb9",
            ["specs/behavior/core/core-modular-nemesis.feature::M.O.D.O.K. retaliates against an attacking hero"] = "c2610cedeceb17e0f005ef15fdedf91a9476a4f624c07f87b558a1d26e28df1c",
            ["specs/behavior/core/core-modular-nemesis.feature::Biomechanical Upgrades attaches to the highest printed hit points and surges"] = "6227bf73fcd3641464fd87156b386427f62849f9a4ba077df0b0a1fb0cac4e73",
            ["specs/behavior/core/core-modular-nemesis.feature::Biomechanical Upgrades can attach to a facedown Drone"] = "d9480802bdc2c9bee1dabab82fb1ac87a2982cfe98bec17ffbc27c61a53efd09",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string scenario, string digest)in expected)
        {
            Assert.Equal(digest, results[scenario].Digest);
        }
    }
}
