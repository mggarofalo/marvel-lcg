using System.Text.Json;
using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreCardFaceTranscriptCardActionBranchesHavePinnedOutcomesTests
{
    [Fact]
    public void CardActionBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/card-actions.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(51, results.Count);
        Assert.Equal("a28c5a59b66c973e782638089dee02710747f76025a7cfa380584a6062562a26", results["behavior:card:01043b:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal("d5468b4ec7184a46336d38d3a9d478df505e3b029498f23d0f9abe37cbc0787c", results["behavior:card:01043c:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal("91be3dea7c12c3eacf49f433265c1e6e24202da06d8a6cf45109cc2c7d144ced", results["behavior:card:01043d:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal("d227e2da9f5490ce815bdc5ff48f817f28184c83a5c744369b9bed4fd6408dee", results["behavior:card:01057:play-under-any-player-s-control"].Digest);
        Assert.Equal("9f06da4d9651209b9697cc1d01bab772213f27a9c6936bcfd5dbb2e6a4749f4b", results["behavior:card:01005:deal-8-damage-enemy"].Digest);
        Assert.Equal("305649c664396e1d741f7d98aa96f833030caf7178026bdffb6c0059b362b00e", results["behavior:card:01049:move-1-damage-from-your-hero-enemy-condition-met"].Digest);
        Assert.Equal("3f18c9a5ebdbc44462092b8723cfd93e30df6b8c84e4a8fb1f92c2d04e013482", results["behavior:card:01046:deal-1-damage-villain-and-each-enemy-condition-met"].Digest);
        Assert.Equal("cd2cb2b2ddf46cc7a634a7700311fa4394875aaa6631201975f7a8bb356ab499", results["behavior:card:01046:deal-1-damage-villain-and-each-enemy-condition-not-met"].Digest);
        Assert.Equal("878187c1cdd7a76d1fbd35a131a6d21375e4ec2df509a0dea22e3c525bbcc73d", results["behavior:rr:cancel.3:published-result"].Digest);
        Assert.Equal("0ab18f084fbb35a22ec1b1a61328a1a80dfd6943e52647e0086002d2208d043b", results["behavior:card:01013:if-you-paid-for-card-using-energy-condition-met"].Digest);
        Assert.Equal("6e04ae4193f3ea1915a59534947894e2df0f360c0e54e6f8e92de1efa3e0b51d", results["behavior:card:01013:if-you-paid-for-card-using-energy-condition-not-met"].Digest);
        Assert.Equal("96a7acbd0a2292913eadd775c682b396c769e9e7008a472211c88de17cdbf45d", results["behavior:card:01022:deal-1-damage-each-enemy"].Digest);
        Assert.Equal("e63a34db35f24fa3c08f6d1b42b53c608465b06dfcaf0f664714d264ea9095e2", results["behavior:card:01054:deal-5-damage-enemy"].Digest);
        Assert.Equal("1bf2c010c2bd2606a260cb3d090bfea365155e9ee701597cb5e2f8dee59673a0", results["behavior:card:01053:if-you-paid-for-card-using-physical-condition-met"].Digest);
        Assert.Equal("7a4d27570e90aa7c98f1a5eca0e90786bff9981739b963eb97c0c0d2e1599cff", results["behavior:card:01053:if-you-paid-for-card-using-physical-condition-not-met"].Digest);
        Assert.Equal("675d0037dc995595e2cbf1a70985c25b7431dc42db41136b88e2ebbc599bff44", results["behavior:card:01023:choose-and-discard-up-5-cards-from-minimum"].Digest);
        Assert.Equal("75f294e1395ead05dc3819fd94ae60f260f188e0ec3b4948bcb3d3815af17026", results["behavior:card:01023:choose-and-discard-up-5-cards-from-intermediate"].Digest);
        Assert.Equal("c477aa40e1b88e69389385c2831e11b1b5eb902420c07cede591f5ae156211e3", results["behavior:card:01023:choose-and-discard-up-5-cards-from-maximum"].Digest);
        Assert.Equal("8f5386c90bf545e65e809666cb8de3c16f5b2690f54ae1e97034bcefab7194b8", results["behavior:card:01056:uses-3-attack-counters"].Digest);
        Assert.Equal("168219da76567443963a390444a9b98c32acb996fdb8ad944e5b2d3f9b181e77", results["behavior:card:01064:uses-3-snoop-counters"].Digest);
        Assert.Equal("052227c1d101fa1e811c4ed02ac3902c6159ec45dafa28a0a36224c7680c17c9", results["behavior:card:01080:uses-3-medical-counters"].Digest);
        Assert.Equal("8152b5e5b44e9f6f24b69baecf6569204df7f05f242fdb466c60407cd794f85d", results["behavior:card:01087:deal-3-damage-enemy"].Digest);
        Assert.Equal("4cd816982b3424ca8197c8e9dd903c7bd6b9a28fb7f106464f33feb1dfc948cd", results["behavior:card:01030:exhaust-war-machine-and-deal-2-damage"].Digest);
        Assert.Equal("d529bc71b74eed39a0129d11c8e4996eb0b9258ccc3809e0f5a318b5e272c84a", results["behavior:card:01027:exhaust-focused-rage-and-take-1-damage"].Digest);
        Assert.Equal("bf43a65805536fb5cfdb8b08f950bf67225919f1a677dc0a1ce5c21237037edb", results["behavior:rr:cost.12:damage-prevented"].Digest);
        Assert.Equal("5245ae7628c39ce5bdc503b0d3cf21782c24181cd0722442e0feb234c732c1d3", results["behavior:rr:ability.3:requires-valid-target"].Digest);
        Assert.Equal("abb1be15457183b878ae9567a5b879c0f717f83c353e8fd6f2cf20e50e44d2ca", results["behavior:rr:ability.2:in-play-player-card-ability"].Digest);
        Assert.Equal("950eedb06dc1e8f1260ddb00deac232e94da07ebc99018474f5bcb439810d3f6", results["behavior:rr:ability.13:hero-form-required"].Digest);
        Assert.Equal("ea364f0282c5ba559af800c34faca79dace2e903fd6a7575fc6be8da763f3fd5", results["behavior:card:01018:spend-x-energy-resources-put-x-energy"].Digest);
        Assert.Equal("c2e7c43beec3fc5fb3641766933b850a168e95a54ec83e44afb67629c75622e0", results["behavior:card:01018:below-damage-cap"].Digest);
        Assert.Equal("721f2535f7df38cce66290f8cfb8ec4bac3ef9b91994d0a18123a558fac4ef58", results["behavior:card:01018:at-damage-cap"].Digest);
        Assert.Equal("e77741d2a3f0e8064585270598f6b6d74a8d0d34c38a33fcb711d9c71b68144a", results["behavior:rr:max-maximum.3:published-result"].Digest);
        Assert.Equal("643ee63aa56eff09e853e2c13c3271abb35ed093b7f2020eae529bf5f8b14041", results["behavior:card:01010a:spend-energy-resource-and-heal-1-damage"].Digest);
        Assert.Equal("bbb7a908275cfadaf06774c9c7c27e2030c1d5d53bbcc512bb6cba12f38481f6", results["behavior:card:01071:pay-printed-cost-ally-in-any-player"].Digest);
        Assert.Equal("9ffe55d71d2ea3f55e6b329ce7d8b4114a77a4a737a88643cb45c2d06f183800", results["behavior:card:01008:exhaust-web-shooter-and-remove-1-web"].Digest);
        Assert.Equal("66553e7e1db03bf57e6056cefd7130d441700dbaecbd129f0ecea090d50f514c", results["behavior:faq:01071:power-of-aspect-pays-printed-ally-cost"].Digest);
        Assert.Equal("26b308860e317077e6ddb8d73e0611614edf9c8a9865847cbf459c2ec6b65f1e", results["behavior:card:01012:then-if-you-have-aerial-trait-remove-condition-not-met"].Digest);
    }

    [Fact]
    public void TriggeredKeywordBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/core-keywords.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("574912182853f6c1c397a3dbb9ab8b146b18cb81a38df3d58871340b52a38875", results["behavior:card:01121:surge"].Digest);
        Assert.Equal("60251abe33ca79f359ec800c88a2ea0eb07ef4b9c35d1ee3b15b3f2a7177b117", results["behavior:card:01121:put-weapons-runner-into-play-engaged-with"].Digest);
        Assert.Equal("e68bb9edec359b1f737d40d278fa7806d5e5d8a05c7dfd11ab999f8362e2ed8e", results["behavior:card:01167:quickstrike"].Digest);
        Assert.Equal("271e4ccdef895fcebee75a00392c0f65ffff163d5c899ecdda9e30446ac38fba", results["behavior:card:01040a:retaliate-1"].Digest);
        Assert.Equal("166300f29580506bc7167816dfd86c9edff61e770bd790462d8d420f747db556", results["behavior:card:01119:klaw-gains-retaliate-1"].Digest);
    }

    [Fact]
    public void UltronAndroidEfficiencyBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-android-efficiency.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(12, results.Count);
        Assert.Equal("bbe032304d13d8cfd8a7696556682cbecf9ba57b8275b5eb1362339bdf756199", results["behavior:card:01144a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("09e8e4ef168d9b167c525b5a60650ee9a53436185bab5f692ae358d67e1c366e", results["behavior:card:01144a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("89df8e965c3719a7fdc7bcdacb2fa9fe03ca1b1fe940099f15d82721aa712878", results["behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-1"].Digest);
        Assert.Equal("1e8a79eb942e3af0c1a6651d632d23b255ab614648654794fb3fe3c3e9b22fa1", results["behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-2"].Digest);
        Assert.Equal("7b889d7f730555af20449a95661d8400b78b72ba1f23bf94f63201e54ff25ed3", results["behavior:card:01144b:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("a719cf944daedbf3508a4435e4be20e37e2264f1aaa89cf57859bc0c33984b36", results["behavior:card:01144b:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("a7073c316c5b5585ba88b5b2939bdc8819e5041a71d521b417f479f47c958314", results["behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-1"].Digest);
        Assert.Equal("6ceacf96af5d753693d0f559cd05d0105018c9b09a1a48ca6e4e77d8e0fd2376", results["behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-2"].Digest);
        Assert.Equal("1e6ff1e05033f3c3a2a8e6a74371ff126e9b5227ee93f3ebfde557e55c6d4740", results["behavior:card:01144c:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("1acff8e0df5dc455f25574dc94334c94588d6d7e02ad1a399c58479f8936dd0a", results["behavior:card:01144c:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("81b8d05378e418d625600e7a76f4e60a27aa5481d5b08cf0376180e225db13f1", results["behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-1"].Digest);
        Assert.Equal("8a9b8c8817a841b1959cd228f62484ff460edb650c4041f1c610b369bcf768c6", results["behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-2"].Digest);
    }

    [Fact]
    public void StandardAndExpertEncounterCardBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/standard-expert-encounter-cards.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01186:villain-schemes"] = "5e29760acd07496358216d0ad2f49c53a387161aa0caf0a8e49604918eb9f0cb",
            ["behavior:card:01187:card-gains-surge"] = "4f9435f818a4bb99756b428a6c35581a8940f7a67b2e766264d73edfc487abca",
            ["behavior:card:01187:villain-attacks-you"] = "a7e3e2c73c0c0e0812726e528ebf73d96d8dee6affa2a50b0a54e830e2e91da5",
            ["behavior:card:01188:if-no-cards-were-discarded-way-card-condition-not-met"] = "c72ec9206076f32041820d8cf740c93da963c79f73fe311e4edf6eeb22c694ba",
            ["behavior:faq:01036:published-clarification-1"] = "5058b370270e2c06702e0b32f85146fa827c558ee086a1fd2dff5b17d752cd71",
            ["behavior:faq:01039:published-clarification-1"] = "4ec16f79115ef1c2bbdde87dba848aef87738417b8f5df019370535482b310dc",
            ["behavior:card:01189:card-gains-surge"] = "64b5b49bdb024e44a03d782965275c21208f4791047c4e849e508c5842fe6ca7",
            ["behavior:card:01189:villain-and-each-minion-engaged-with-you"] = "8cd208c3620ecec39b026e99f3b51e5f2a260a9c1f5530b00fb4f3d165d2ddf2",
            ["behavior:card:01190:reveal-your-set-aside-nemesis-minion-and"] = "f2c0629cd6cdc09553d94e67e069ad3219685ef47547417df69a4e7bd9f896b7",
            ["behavior:card:01190:if-your-nemesis-minion-does-not-enter-condition-met"] = "8a9b0e12d15e224392da4e26ae0d08947da198ab8cab9d3bdf0ca706410e16e9",
            ["behavior:card:01192:place-4-threat-on-each-side-scheme"] = "5a7f282eaf358cbd1e3036f1bb4a0065727ca28abc4c6708409aea888429a0c7",
            ["behavior:card:01192:if-there-are-no-side-schemes-in-condition-met"] = "d62650d2bf058ea41ff601fdd0be6ef0d30357f78d65776917d70db61a54edd4",
            ["behavior:card:01193:surge"] = "bf95a321792a592f4cb53dbffab168d525ca218aec1ebe5120a51a4da7d4f885",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest)in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void BlackPantherNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/black-panther-nemesis.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01157:killmonger-cannot-take-damage-from-black-panther"] = "ddd96d0af40e16d44bcae30734030b185f52e2ecc155dd5792a14060037816a0",
            ["behavior:card:01158:surge-after-card-resolves-reveal-1-additional"] = "82014a5fbb1e74f1388e52bd969db49fb47e455b1c4ca68fa37cbb4b30ca3caf",
            ["behavior:card:01158:give-villain-tough-status-card"] = "732cbda6f8216c8bd16ed9698edc97162ca682623906cd797a072b3f66c94ad9",
            ["behavior:card:01159:discard-top-card-encounter-deck"] = "29b75b614149ce84152c976478e7e1048e9eb500d13690d73939d2eb9948e44a",
            ["behavior:card:01159:then-choose-either-deal-x-damage-your-choice-2"] = "40f458007da12ab493323f0ae07a8546f5e2fa558a1faefbc2ef4fa3ed34682b",
            ["behavior:card:01159:x-is-1-more-than-number-boost"] = "032092d3de1b94ae9e1968d6befd9a0cbbb6493e72228530a07e6accfbad2ef8",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest)in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void SheHulkNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/she-hulk-nemesis.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01161:place-additional-1-per-hero-threat-here"] = "17a645bd1dfd9c465111c067f375882c187618174dfe1c6f6dab50c445b4e446",
            ["behavior:card:01162:x-is-equal-titania-s-remaining-hit"] = "6d173d028bb9bd26ab4e593df353939cbd4c2d0e54ee0ad642018e4c87740515",
            ["behavior:card:01164:titania-attacks-your-hero"] = "bda60851782214d974dd1b642ee9603d50f5c10cd9076379ae4b1c4046bad238",
            ["behavior:card:01164:if-titania-did-not-attack-heal-all-condition-met"] = "80304606fb01c765d902a1bc385f8ba80446b399179b523548be79a366387c3b",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest)in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void SpiderManNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/spider-man-nemesis.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01166:each-player-places-random-card-from-their-one-player"] = "126e4835d82bedd44329c0e33af5a9313e82b74e3c073dcc2f47c3c8ff585ce5",
            ["behavior:card:01168:stun-your-hero"] = "0ab7f3f86edbc4554861933a7cde61f26810c4ed358c354f9df15dc3cc6aa4e0",
            ["behavior:card:01168:if-vulture-is-in-play-card-gains-condition-met"] = "0a72d26b4d71392b45963a29a45c1f6a07a35c58f09150d319e142e924d0e8b7",
            ["behavior:card:01168:if-activation-deals-damage-friendly-character-stun-condition-met"] = "63fcfd51806781803a2c0b47909ab3ff68f7df757152e78979bf75d846e2daed",
            ["behavior:card:01168:if-activation-deals-damage-friendly-character-stun-condition-not-met"] = "6ddc049bd99e68737448a9b369273e12295bb26c3e1fea42914348a3bf1914ab",
            ["behavior:card:01169:discard-1-card-at-random-from-each-one-player"] = "8c78b43d243dc963aebfa639795c8f772934d005685979eb59aa4903661c1192",
            ["behavior:card:01169:discard-1-card-at-random-from-each-multiple-players"] = "0e6cbfb3605fe7924f53c68d780d22d8dac39e741f8d083859756a922fe4abe0",
            ["behavior:card:01169:place-1-threat-on-main-scheme-for-zero"] = "eaf0a0192ebed377b346ceb1b38a23e957d63af2efd667fa97211c453d80a903",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest)in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void IronManNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/iron-man-nemesis.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01171:place-additional-1-per-hero-threat-here"] = "416b539f69365d22159cbb0b1373728faa6702cd619df9ade26ef234871f9138",
            ["behavior:card:01172:retaliate-1-after-character-is-attacked-deal"] = "dc00e8380ff20ebeb7fe526bfa5f1252a0f9a0435e8704e5579020b5a58289df",
            ["behavior:card:01173:choose-either-deal-1-damage-your-hero-zero"] = "744dd9808acb612fb976a8cd62dc351fd1178aa4bc007d5e7ef7099baca6ab8b",
            ["behavior:card:01173:choose-either-deal-1-damage-your-hero-choice-1"] = "ae7a4dd5c1da4f900b433ba575d208c8f37d0370fd7065baaeb0822b1465f43a",
            ["behavior:card:01173:choose-either-deal-1-damage-your-hero-choice-2"] = "8508d46390159c061a3259322f1aeaa93acc3cfaae4fbd354fa7e8dd1cf7882e",
            ["behavior:card:01173:if-villain-is-making-undefended-attack-choose-condition-met"] = "1be98fca6e19c02025c514a4a79f3204d55e37156d45bb8750f584fa53ae9790",
            ["behavior:card:01173:if-villain-is-making-undefended-attack-choose-condition-not-met"] = "22a4ca1dff7cbae06cf317b02583a66f8c2a90c03d56ae2a75cd2e94e92ec048",
            ["behavior:card:01174:each-player-discards-top-5-cards-their-one-player"] = "e6bd52919a62892fec7f2b661bdf44cc7d47cf98bf4aa41b83b5c92d9b5a63c5",
            ["behavior:card:01174:for-each-printed-energy-resource-player-discards-one"] = "870a87bc026b382d56959336430a412e0c23536a556a85f5287d268678a1874d",
            ["behavior:card:01174:for-each-printed-energy-resource-player-discards-multiple"] = "255eeb22511b4370d221acc1fadcd62794f8efd07e5fe1c33e67051a4a65838a",
            ["behavior:card:01174:each-player-discards-top-5-cards-their-multiple-players"] = "18f5c4aa8a47162bb9871e24c08d4c217d1a848c44f6f5685250cec380ce44f9",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest)in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void CaptainMarvelNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/captain-marvel-nemesis.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01176:place-additional-1per-hero-threat-here"] = "a405a92b881813420d52ec1a41b0adccddc7a4dd6286627b5540130ef30e9b1b",
            ["behavior:card:01177:after-yon-rogg-attacks-place-1-threat"] = "397b4fb6abcc7a0bbbe4222896c5e07517bf6a4dfcd21c52775ca0dd79663288",
            ["behavior:card:01178:surge"] = "761ab7ef51a96cc51dd150c6da3dce21aa92972fcddf246728e796aba8dab9f4",
            ["behavior:card:01179:discard-each-energy-resource-from-your-hand"] = "6f065b11fd459bd9d39c5163880ec40cd8b73b5b654a473427ac5f8e76814ff0",
            ["behavior:card:01179:if-you-discarded-no-cards-way-card-condition-met"] = "bdf8f3d25f3040e23466b097be9c37a3d482066860ec0db87aa27676aa20c92c",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest)in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }
}
