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
            ["specs/behavior/core/core-modular-nemesis.feature::The first player breaks an encounter attachment target tie"] = "65900b85a34453a4992c5cdb7d78d67166cb12cb5c7b6d40b98010f5e39b2edc",
            ["specs/behavior/core/core-modular-nemesis.feature::Legions of Hydra finds Madame Hydra before counting Hydra enemies"] = "f17df5aa6684d20eff1dd1011dd7d0b369488d6025ef4782dbc919ee2e5da5b0",
            ["specs/behavior/core/core-modular-nemesis.feature::Legions of Hydra counts every Hydra enemy already in play"] = "3be9b030dd0e432991d31969ff3619c08cb2e4e09b4efea211b0c3e4a92e8ffe",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra places threat after attacking"] = "87e5113dbdfc9b2721c270620eea1a6a89bfbd16eddd712e78892879ee783208",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra chooses one of two Legions schemes for her threat"] = "5c77130191f777c6f2395c94b499b538e0483d4afc5cb635df13bbb18134b76d",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra places threat after scheming"] = "19df77dfb688c0994eecdaab6cba191bb29d3a38144cb721b755e69476b22cd6",
            ["specs/behavior/core/core-modular-nemesis.feature::The Doomsday Chair finds M.O.D.O.K. outside play"] = "6f9cacfb78e8140a95d019ba1c8e11b0d782bc2a48a0f10b10fc7812aa5bfee2",
            ["specs/behavior/core/core-modular-nemesis.feature::The Doomsday Chair does not search when M.O.D.O.K. is in play"] = "646837941474844b69d5de2a3a96a050f38580ddc430edf07c44bea92c1e6bb6",
            ["specs/behavior/core/core-modular-nemesis.feature::M.O.D.O.K. retaliates against an attacking hero"] = "66bd9dc80319e667ff05b617c2f4e7ca8724392141294818958527befff01336",
            ["specs/behavior/core/core-modular-nemesis.feature::Biomechanical Upgrades attaches to the highest printed hit points and surges"] = "c475ca3a6a440e1eb2644e8fa651fdaed063a30029abb7d389107d53b93b4f9f",
            ["specs/behavior/core/core-modular-nemesis.feature::Biomechanical Upgrades can attach to a facedown Drone"] = "04e04e14e9f06e21bf07f2cd575a7d266beb0aa484d5a1b5ec2fda750035af4b",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string scenario, string digest)in expected)
        {
            Assert.Equal(digest, results[scenario].Digest);
        }
    }
}
