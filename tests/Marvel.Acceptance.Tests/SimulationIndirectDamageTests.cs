using Marvel.Sim;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SimulationIndirectDamageTests : SimulationHarnessTestBase
{
    [Fact]
    [Rule("rr:indirect-damage.1")]
    public void FullAutoIndirectDamageOnOneCharacterCompletesAndReplays()
    {
        // Crossbones seed 9 reveals Full Auto (04067) with three indirect damage
        // and two characters, one of which has a single point of health left.
        // rr:indirect-damage.1: "Indirect damage dealt to a player can be divided
        // as that player chooses among characters under their control."
        var record = RoundTrip(new SimulationConfig(
            "crossbones", "standard", ["captain_marvel"], null, 1, [9], null, null,
            275, 600, null, RepositoryRoot()));

        var assignments = record
            .Where(item => item.GetProperty("type").GetString() == "step")
            .Where(item => item.GetProperty("prompt").GetProperty("label").GetString() == "04067: assign 3 damage")
            .Select(item => item.GetProperty("targets").EnumerateArray().Select(id => id.GetInt32()).ToList())
            .ToList();

        Assert.NotEmpty(assignments);
        Assert.All(assignments, targets =>
        {
            Assert.Equal(3, targets.Count);
            Assert.True(targets.Distinct().Count() < targets.Count);
        });
    }
}
