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
    public void IdentityCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/identity-card-abilities.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(4, results.Count);
        Assert.Equal(
            "d46de12a5223add3f2203e2e9b83ba7a0c008aa0af6230de63d490b369facb1a",
            results["behavior:card:01001b:generate-mental-resource"].Digest);
        Assert.Equal(
            "26a035785a16fd0bdc8b1abbd1df7dd3ddca7b4cf99077f8c33782d7622d794d",
            results["behavior:card:01010b:choose-player-draw-1-card"].Digest);
        Assert.Equal(
            "04a5fcb2bac9b14f3852d2bd68aad8c5ed6febbd4e0c999ea5dd2a273b8bacf3",
            results["behavior:card:01029a:you-get-1-hand-size-for-each-zero"].Digest);
        Assert.Equal(
            "ca8f9d039bcb84eead345000521a8f946bb8b36e556f009168ea29f9099b19dd",
            results["behavior:card:01029b:look-at-top-3-cards-your-deck"].Digest);
    }

    [Fact]
    public void PlayerCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/player-card-abilities.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(22, results.Count);
        Assert.Equal(
            "03df40ae6ceac7d4b6c94fcaf97e185e3eca0c0948fd20d570df26d5823c6503",
            results[
                "behavior:card:01084:after-entering-play-remove-two-threat"].Digest);
        Assert.Equal(
            "ef5067a91fb1e64d04af508175e6c7e6b63e5fbcafae4f58d84adc57bc5589a8",
            results[
                "behavior:card:01084:after-entering-play-deal-four-damage"].Digest);
        Assert.Equal(
            "73429b06e06aac69e83c9747c047cf1263a76e6b23cb489de9202a616dc6764d",
            results[
                "behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-not-met"].Digest);
        Assert.Equal(
            "74b768ace6a3265777f0eec610ad2d205af799e3145ab2f5bd8ab3f0d9919560",
            results[
                "behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-met"].Digest);
        Assert.Equal(
            "bf14a182626b760d182d0a1e611be7c73e6605e06d61a6ba53abda50ab612e0b",
            results[
                "behavior:card:01017:when-captain-marvel-would-take-damage-discard"].Digest);
        Assert.Equal(
            "a7231e18de95fa513b562f8f1eeedc8db9be34f7005b493d1be05d6e53f5e6d0",
            results[
                "behavior:card:01008:when-those-are-gone-discard-card"].Digest);
        Assert.Equal(
            "3e1d13290b6ce29527eb0cae35ebbfb897ddc5d8c6e51a08c3fbcfb8b4d52fdc",
            results[
                "behavior:card:01083:after-mockingbird-enters-play-stun-enemy"].Digest);
        Assert.Equal(
            "972ad7da1753fcc4ad6a68a0bb480871c6a28424c83d996c19ebdab268cf73bb",
            results[
                "behavior:card:01068:choose-thw-plus-two-until-end-phase"].Digest);
        Assert.Equal(
            "f2acf656d1837dc30c688e59e129c288bad617303fda322bc4b76a3cef53d9a9",
            results[
                "behavior:card:01068:choose-atk-plus-two-until-end-phase"].Digest);
        Assert.Equal(
            "b17f9fbb3d730047490cb61c0f065daa8c730d29ac2d28f50c7681c2882c1ad3",
            results["behavior:card:01035:exhaust-arc-reactor-ready-iron-man"].Digest);
        Assert.Equal(
            "352a3f743b5fd56afd7bbf73c950b94f48dadb56431412524de1cf368d477eb7",
            results["behavior:card:01036:you-get-6-hit-points"].Digest);
        Assert.Equal(
            "baba20d06a4ae560afad0d5c873059ac2997644932c9bf9f4a0a5a62531dd5d0",
            results["behavior:card:01045:exhaust-golden-city-draw-2-cards"].Digest);
        Assert.Equal(
            "e25f1313dc5ce5fbbebe4212c0750ffdc7fc55417330cbf05eaa0af0f1fa4930",
            results["behavior:card:01069:ready-ally"].Digest);
        Assert.Equal(
            "5e753604530663b611d5028237c3dc51fd8e0584e09617ac78b835364a4aba1f",
            results["behavior:card:01086:heal-2-damage-from-any-character"].Digest);
        Assert.Equal(
            "8ea0ffdad1bd63a5cb9a4baeda9d24867a335c3e0327ea7c8b8a72f90efd78b7",
            results["behavior:card:01020:return-hellcat-your-hand"].Digest);
        Assert.Equal(
            "e6cf4f880e001bc03a7d5c05eca80611dfb536efe4f6c4d2aea885fdd757bd7f",
            results[
                "behavior:card:01091:exhaust-avengers-mansion-choose-player"].Digest);
        Assert.Equal(
            "fdc1fe9a404b8ee735bf36adef04640631fee32c1c5e0279aa7321fedfb61639",
            results[
                "behavior:card:01015:exhaust-alpha-flight-station-choose-and-discard-condition-met"].Digest);
        Assert.Equal(
            "f7d1e8e807c5da9102f16333e42c7242becadb4e86db18b3f3e0d293fd9ae529",
            results[
                "behavior:card:01026:exhaust-superhuman-law-division-and-spend-mental"].Digest);
        Assert.Equal(
            "510369bea032b2c09917c59fb16654c5baa6712fec98af0b6df23f8fab940573",
            results[
                "behavior:card:01033:exhaust-pepper-potts-generate-resources-top-card"].Digest);
        Assert.Equal(
            "220eb146ad58642d460bca21ee7526d6e03560381c9424bae02921f77f618709",
            results[
                "behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-accepted"].Digest);
        Assert.Equal(
            "5896375c93ec09a9309e1e3a75b3bc50628c8f3d896014673074f8e95eb0e566",
            results[
                "behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-declined"].Digest);
        Assert.Equal(
            "518c9e5a17da771d3951d6e9c5dede1d42261ccd51b55cbe344e0bc9d0db8034",
            results[
                "behavior:card:01034:exhaust-stark-tower-choose-player"].Digest);
    }

    [Fact]
    public void CardActionBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/card-actions.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(44, results.Count);
        Assert.Equal(
            "d227e2da9f5490ce815bdc5ff48f817f28184c83a5c744369b9bed4fd6408dee",
            results[
                "behavior:card:01057:play-under-any-player-s-control"].Digest);
        Assert.Equal(
            "5a3c519fb8e602123a5186bc67ab95ff553eb67080b527c327b8f3974ae6f2a4",
            results["behavior:card:01005:deal-8-damage-enemy"].Digest);
        Assert.Equal(
            "305649c664396e1d741f7d98aa96f833030caf7178026bdffb6c0059b362b00e",
            results["behavior:card:01049:move-1-damage-from-your-hero-enemy-condition-met"].Digest);
        Assert.Equal(
            "3f18c9a5ebdbc44462092b8723cfd93e30df6b8c84e4a8fb1f92c2d04e013482",
            results[
                "behavior:card:01046:deal-1-damage-villain-and-each-enemy-condition-met"].Digest);
        Assert.Equal(
            "cd2cb2b2ddf46cc7a634a7700311fa4394875aaa6631201975f7a8bb356ab499",
            results[
                "behavior:card:01046:deal-1-damage-villain-and-each-enemy-condition-not-met"].Digest);
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
            "1bf2c010c2bd2606a260cb3d090bfea365155e9ee701597cb5e2f8dee59673a0",
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
            "168219da76567443963a390444a9b98c32acb996fdb8ad944e5b2d3f9b181e77",
            results["behavior:card:01064:uses-3-snoop-counters"].Digest);
        Assert.Equal(
            "052227c1d101fa1e811c4ed02ac3902c6159ec45dafa28a0a36224c7680c17c9",
            results["behavior:card:01080:uses-3-medical-counters"].Digest);
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
        Assert.Equal(
            "6a4fab8891cf347c0cd6a2139abd2368717352443f795de7ad3cf3230d97cbb5",
            results["behavior:card:01018:spend-x-energy-resources-put-x-energy"].Digest);
        Assert.Equal(
            "b6f72af5698b600a7aea17b5f938be35ec0ba4b1453a5b3136aef409edddae9b",
            results["behavior:card:01018:below-damage-cap"].Digest);
        Assert.Equal(
            "721f2535f7df38cce66290f8cfb8ec4bac3ef9b91994d0a18123a558fac4ef58",
            results["behavior:card:01018:at-damage-cap"].Digest);
        Assert.Equal(
            "e77741d2a3f0e8064585270598f6b6d74a8d0d34c38a33fcb711d9c71b68144a",
            results["behavior:rr:max-maximum.3:published-result"].Digest);
        Assert.Equal(
            "643ee63aa56eff09e853e2c13c3271abb35ed093b7f2020eae529bf5f8b14041",
            results[
                "behavior:card:01010a:spend-energy-resource-and-heal-1-damage"].Digest);
        Assert.Equal(
            "bbb7a908275cfadaf06774c9c7c27e2030c1d5d53bbcc512bb6cba12f38481f6",
            results[
                "behavior:card:01071:pay-printed-cost-ally-in-any-player"].Digest);
        Assert.Equal(
            "9ffe55d71d2ea3f55e6b329ce7d8b4114a77a4a737a88643cb45c2d06f183800",
            results[
                "behavior:card:01008:exhaust-web-shooter-and-remove-1-web"].Digest);
        Assert.Equal(
            "66553e7e1db03bf57e6056cefd7130d441700dbaecbd129f0ecea090d50f514c",
            results[
                "behavior:faq:01071:power-of-aspect-pays-printed-ally-cost"].Digest);
        Assert.Equal(
            "26b308860e317077e6ddb8d73e0611614edf9c8a9865847cbf459c2ec6b65f1e",
            results[
                "behavior:card:01012:then-if-you-have-aerial-trait-remove-condition-not-met"].Digest);
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
            "6012ca33e5deae43c6c2e5e1e710e56308796bfc1671ba2c0f1229956f67de6d",
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
