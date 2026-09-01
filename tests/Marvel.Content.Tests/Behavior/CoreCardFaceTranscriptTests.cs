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
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/card-actions.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(7, results.Count);
        Assert.Equal(
            "560775a73450c5e08a03a8e7c97f7ca5e35754ab02a0d978febf300ff5d24298",
            results["behavior:card:01005:deal-8-damage-enemy"].Digest);
        Assert.Equal(
            "0ab18f084fbb35a22ec1b1a61328a1a80dfd6943e52647e0086002d2208d043b",
            results["behavior:card:01013:if-you-paid-for-card-using-energy-condition-met"].Digest);
        Assert.Equal(
            "6e04ae4193f3ea1915a59534947894e2df0f360c0e54e6f8e92de1efa3e0b51d",
            results["behavior:card:01013:if-you-paid-for-card-using-energy-condition-not-met"].Digest);
        Assert.Equal(
            "96a7acbd0a2292913eadd775c682b396c769e9e7008a472211c88de17cdbf45d",
            results["behavior:card:01022:deal-1-damage-each-enemy"].Digest);
        Assert.Equal(
            "82643ed799db2373ad67cdb5a400147de38a073b631527496f160214cc232528",
            results["behavior:card:01054:deal-5-damage-enemy"].Digest);
        Assert.Equal(
            "8f5386c90bf545e65e809666cb8de3c16f5b2690f54ae1e97034bcefab7194b8",
            results["behavior:card:01056:uses-3-attack-counters"].Digest);
        Assert.Equal(
            "8152b5e5b44e9f6f24b69baecf6569204df7f05f242fdb466c60407cd794f85d",
            results["behavior:card:01087:deal-3-damage-enemy"].Digest);
    }

    [Fact]
    public void TriggeredKeywordBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/core-keywords.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(4, results.Count);
        Assert.Equal(
            "44539cd9a74dc6025015263af5b8693e180b4a25c4a3c3373862bb6de3a4c10f",
            results["behavior:card:01121:surge"].Digest);
        Assert.Equal(
            "7de1ad47d61f5cf884d1e489f5317867b18bbc2111ad68945a0f1e2ec572ca17",
            results["behavior:card:01121:put-weapons-runner-into-play-engaged-with"].Digest);
        Assert.Equal(
            "e68bb9edec359b1f737d40d278fa7806d5e5d8a05c7dfd11ab999f8362e2ed8e",
            results["behavior:card:01167:quickstrike"].Digest);
        Assert.Equal(
            "271e4ccdef895fcebee75a00392c0f65ffff163d5c899ecdda9e30446ac38fba",
            results["behavior:card:01040a:retaliate-1"].Digest);
    }
}
