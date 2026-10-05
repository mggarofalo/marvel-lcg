using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreRuleTranscriptEncounterDeckBranchesHavePinnedOutcomesTests
{
    [Fact]
    public void EncounterDeckBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/encounter-deck-empty.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(4, results.Count);
        Assert.Equal("554326721b1ef2635a2250f0d98340f9be0c91b4e8e9080368ca3669194c7eeb", results["behavior:rr:encounter-deck.1:empty-with-discard"].Digest);
        Assert.Equal("554326721b1ef2635a2250f0d98340f9be0c91b4e8e9080368ca3669194c7eeb", results["behavior:rr:encounter-deck.2:published-result"].Digest);
        Assert.Equal("ce7a1f5331bff75ee3ff31d19dc13ae8ca76bf7993a5be6de67c231373098b1d", results["behavior:rr:encounter-deck.3:published-result"].Digest);
        Assert.Equal("eaf88e081696bc058340bb54cfccd2be5765a15eba80ca7f2caf947916cbb777", results["behavior:rr:encounter-deck.4:published-result"].Digest);
    }

    [Fact]
    public void PlayerDeckBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/player-deck-empty.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(4, results.Count);
        Assert.Equal("1b2294b21eb65392d01b23c1f807bdf3ac68bed745a720e7fee2f2a44a8528a6", results["behavior:rr:player-deck.1:empty-with-discard"].Digest);
        Assert.Equal("05325a4af79242636921e854b4d3339c806c146d7eace3efde8ac4496e3f4697", results["behavior:rr:player-deck.2:published-result"].Digest);
        Assert.Equal("ee95624b513e86513ef7f7df82edfaf5c29ff2bd74522d1178121c6a54feb634", results["behavior:rr:player-deck.3:published-result"].Digest);
        Assert.Equal("de4be50276700c3bf7e198d2f667f4e1585d3778681646cf1cc7cec7b858e831", results["behavior:rr:player-deck.4:published-result"].Digest);
    }
}
