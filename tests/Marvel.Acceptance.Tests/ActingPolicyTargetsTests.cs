using Marvel.Core.Random;
using Marvel.Rules.Prompts;
using Marvel.Sim;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class ActingPolicyTargetsTests
{
    [Fact]
    [Rule("rr:indirect-damage.1")]
    public void ResearchPolicyPlacesSeveralIndirectPointsOnOneCharacter()
    {
        // rr:indirect-damage.1: "Indirect damage dealt to a player can be
        // divided as that player chooses among characters under their control."
        // Three points among two characters must repeat at least one of them.
        var wanted = Indirect(3, new Dictionary<int, int> { [10] = 5, [20] = 5 });

        for (uint seed = 0; seed < 32; seed++)
        {
            IReadOnlyList<int> chosen = ActingPolicyTargets.Select(wanted, -1, new EngineRandom(seed));

            Assert.Equal(3, chosen.Count);
            Assert.True(wanted.Allows(chosen));
        }
    }

    [Fact]
    [Rule("rr:indirect-damage.1")]
    public void ResearchPolicyKeepsEachCharacterWithinItsOfferedCapacity()
    {
        // The engine offers each character only its remaining health.
        var wanted = Indirect(3, new Dictionary<int, int> { [10] = 1, [20] = 2 });

        for (uint seed = 0; seed < 32; seed++)
        {
            IReadOnlyList<int> chosen = ActingPolicyTargets.Select(wanted, -1, new EngineRandom(seed));

            Assert.Equal([10, 20, 20], chosen.Order());
        }
    }

    [Fact]
    public void ResearchPolicyAllocationFollowsItsSeededStream()
    {
        var wanted = Indirect(6, new Dictionary<int, int> { [10] = 6, [20] = 6, [30] = 6 });

        var first = ActingPolicyTargets.Select(wanted, -1, new EngineRandom(7));
        var again = ActingPolicyTargets.Select(wanted, -1, new EngineRandom(7));
        var spread = Enumerable.Range(0, 32)
            .Select(seed => string.Join(',', ActingPolicyTargets.Select(wanted, -1, new EngineRandom((uint)seed))))
            .Distinct()
            .Count();

        Assert.Equal(first, again);
        Assert.True(spread > 1);
    }

    [Fact]
    public void AnAdmittedDistinctAllocationKeepsTheVersionThreeAnswerAndDrawsNothing()
    {
        // Policy version 3 is kept because every answer it had accepted is unchanged.
        var wanted = Indirect(2, new Dictionary<int, int> { [10] = 4, [20] = 4, [30] = 4 });
        var random = new EngineRandom(11);

        IReadOnlyList<int> chosen = ActingPolicyTargets.Select(wanted, -1, random);

        Assert.Equal([10, 20], chosen);
        Assert.Equal(new EngineRandom(11).Generator.WordsConsumed, random.Generator.WordsConsumed);
    }

    [Fact]
    public void ResearchPolicyStillPrefersTheVillainForASingleDistinctTarget()
    {
        var wanted = new TargetRequest([10, 20, 30], 1, 1);

        Assert.Equal([20], ActingPolicyTargets.Select(wanted, 20, new EngineRandom(0)));
    }

    [Fact]
    public void AnAllocationNoOfferedTargetCanCompleteFailsInThePolicy()
    {
        var wanted = Indirect(3, new Dictionary<int, int> { [10] = 1, [20] = 1 });

        Assert.Throws<InvalidOperationException>(
            () => ActingPolicyTargets.Select(wanted, -1, new EngineRandom(0)));
    }

    private static TargetRequest Indirect(int points, IReadOnlyDictionary<int, int> capacity) =>
        new([.. capacity.Keys], points, points,
            Rule: "rr:indirect-damage.1", AllowRepeated: true, MaximumOccurrences: capacity);
}
