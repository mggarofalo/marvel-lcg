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

        Assert.Equal(19, results.Count);
        Assert.Equal(
            "560775a73450c5e08a03a8e7c97f7ca5e35754ab02a0d978febf300ff5d24298",
            results["behavior:card:01005:deal-8-damage-enemy"].Digest);
        Assert.Equal(
            "878187c1cdd7a76d1fbd35a131a6d21375e4ec2df509a0dea22e3c525bbcc73d",
            results["behavior:rr:cancel.3:published-result"].Digest);
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
            "d76a21c9f4daa35a8eee7f05cf63bcd6f86553b2555aa6bd40e557cfe5934966",
            results["behavior:card:01053:if-you-paid-for-card-using-physical-condition-met"].Digest);
        Assert.Equal(
            "7a4d27570e90aa7c98f1a5eca0e90786bff9981739b963eb97c0c0d2e1599cff",
            results["behavior:card:01053:if-you-paid-for-card-using-physical-condition-not-met"].Digest);
        Assert.Equal(
            "675d0037dc995595e2cbf1a70985c25b7431dc42db41136b88e2ebbc599bff44",
            results["behavior:card:01023:choose-and-discard-up-5-cards-from-minimum"].Digest);
        Assert.Equal(
            "b3bab57cebe8e142917b4c3244c2af15f151d98a2834eb6443af0194bfdb5e5e",
            results["behavior:card:01023:choose-and-discard-up-5-cards-from-intermediate"].Digest);
        Assert.Equal(
            "c477aa40e1b88e69389385c2831e11b1b5eb902420c07cede591f5ae156211e3",
            results["behavior:card:01023:choose-and-discard-up-5-cards-from-maximum"].Digest);
        Assert.Equal(
            "8f5386c90bf545e65e809666cb8de3c16f5b2690f54ae1e97034bcefab7194b8",
            results["behavior:card:01056:uses-3-attack-counters"].Digest);
        Assert.Equal(
            "8152b5e5b44e9f6f24b69baecf6569204df7f05f242fdb466c60407cd794f85d",
            results["behavior:card:01087:deal-3-damage-enemy"].Digest);
        Assert.Equal(
            "4cd816982b3424ca8197c8e9dd903c7bd6b9a28fb7f106464f33feb1dfc948cd",
            results["behavior:card:01030:exhaust-war-machine-and-deal-2-damage"].Digest);
        Assert.Equal(
            "d529bc71b74eed39a0129d11c8e4996eb0b9258ccc3809e0f5a318b5e272c84a",
            results["behavior:card:01027:exhaust-focused-rage-and-take-1-damage"].Digest);
        Assert.Equal(
            "bf43a65805536fb5cfdb8b08f950bf67225919f1a677dc0a1ce5c21237037edb",
            results["behavior:rr:cost.12:damage-prevented"].Digest);
        Assert.Equal(
            "5245ae7628c39ce5bdc503b0d3cf21782c24181cd0722442e0feb234c732c1d3",
            results["behavior:rr:ability.3:requires-valid-target"].Digest);
        Assert.Equal(
            "abb1be15457183b878ae9567a5b879c0f717f83c353e8fd6f2cf20e50e44d2ca",
            results["behavior:rr:ability.2:in-play-player-card-ability"].Digest);
        Assert.Equal(
            "950eedb06dc1e8f1260ddb00deac232e94da07ebc99018474f5bcb439810d3f6",
            results["behavior:rr:ability.13:hero-form-required"].Digest);
    }

    [Fact]
    public void TriggeredKeywordBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/core-keywords.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(5, results.Count);
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
        Assert.Equal(
            "86ca8e9cffb3f4bbff86a0538135743c6e32bfb46eee814cd01af51adc9c6181",
            results["behavior:card:01119:klaw-gains-retaliate-1"].Digest);
    }
}
