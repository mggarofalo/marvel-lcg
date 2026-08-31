using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreRuleTranscriptTests
{
    [Fact]
    public void EncounterDeckBranchesHavePinnedOutcomes()
    {
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);
        var results = suite.RunPassingCorpus()
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/encounter-deck-empty.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(4, results.Count);
        Assert.Equal(
            "4e166a739c6896f41af4c7102dbdab83e8e5614312cac4347bab11706243ee4a",
            results["behavior:rr:encounter-deck.1:empty-with-discard"].Digest);
        Assert.Equal(
            "4e166a739c6896f41af4c7102dbdab83e8e5614312cac4347bab11706243ee4a",
            results["behavior:rr:encounter-deck.2:published-result"].Digest);
        Assert.Equal(
            "1386625b57ce40c8d47968c346bba5d6689bd5276c5892c670661db3239f45fe",
            results["behavior:rr:encounter-deck.3:published-result"].Digest);
        Assert.Equal(
            "133186472d2488c900098c9f16d67f0872c8e3e0929803d0532e083731689738",
            results["behavior:rr:encounter-deck.4:published-result"].Digest);
    }

    [Fact]
    public void PlayerDeckBranchesHavePinnedOutcomes()
    {
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);
        var results = suite.RunPassingCorpus()
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/player-deck-empty.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(4, results.Count);
        Assert.Equal(
            "c56eb62acf59a2595edbd5f1ea68d5f4943c831fd006affbe219b7f2244eb4fb",
            results["behavior:rr:player-deck.1:empty-with-discard"].Digest);
        Assert.Equal(
            "630f931c433098646b8aaeb96e9baa0f7df8b9a95db6786153607231f57fca45",
            results["behavior:rr:player-deck.2:published-result"].Digest);
        Assert.Equal(
            "632b3814faa3f565357047d8e210d63e95c9496deb89e0521cf8b45cccd6a0be",
            results["behavior:rr:player-deck.3:published-result"].Digest);
        Assert.Equal(
            "f2537289d2410f47121e99baaf5974340b605db0add098daa33cc93b63decbb9",
            results["behavior:rr:player-deck.4:published-result"].Digest);
    }
}
