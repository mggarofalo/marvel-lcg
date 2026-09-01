using System.Text.Json;
using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreCardFaceTranscriptTests
{
    private static readonly Lazy<IReadOnlyList<TranscriptResult>> Corpus = new(
        () => new CoreTranscriptSuite(RepositoryPaths.Root).RunPassingCorpus());

    [Fact]
    public void EveryCanonicalCoreFaceHasAnExecutablePrintedFactTranscript()
    {
        using var cards = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("cards", "cards.json")));
        string[] expected =
        [
            .. cards.RootElement.GetProperty("cards").EnumerateArray()
                .Where(card => card.GetProperty("pack").GetString() == "core")
                .Select(card =>
                    $"behavior:card:{card.GetProperty("card_id").GetString()}:printed-name")
                .Order(StringComparer.Ordinal),
        ];
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/card-faces.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(209, results.Count);
        Assert.Equal(expected, results.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(
            "f166b2a393eb1cbf8a63bec8ca05525a2406fee737e5db370c893bd39ffe0fe7",
            results["behavior:card:01001a:printed-name"].Digest);
        Assert.Equal(
            "28e935913f4352fee6d06f2617a6d48d7725475a94b2cf8f092cf22278299beb",
            results["behavior:card:01149:printed-name"].Digest);
    }

    [Fact]
    public void CardActionBranchesHavePinnedOutcomes()
    {
        TranscriptResult result = Assert.Single(
            Corpus.Value,
            candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/card-actions.feature::", StringComparison.Ordinal));

        Assert.Equal("behavior:card:01005:deal-8-damage-enemy", result.Obligation);
        Assert.Equal(
            "560775a73450c5e08a03a8e7c97f7ca5e35754ab02a0d978febf300ff5d24298",
            result.Digest);
    }
}
