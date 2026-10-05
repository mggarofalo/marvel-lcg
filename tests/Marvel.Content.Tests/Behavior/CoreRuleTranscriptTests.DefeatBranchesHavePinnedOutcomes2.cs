using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreRuleTranscriptDefeatBranchesHavePinnedOutcomesTests
{
    [Fact]
    public void DefeatBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/defeat.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(6, results.Count);
        Assert.Equal("714ab9a88f3aed2bba1f189c4a1547ea6304421714fe210aee8d7f09afc9bea3", results["behavior:rr:minion.2:published-result"].Digest);
        Assert.Equal("a8622c2b607e932edc2b9c2cf0da619c26a2bd8cdf552cef095116112a81a7e2", results["behavior:rr:side-scheme.2:published-result"].Digest);
        Assert.Equal("17b6d1ed7963473a22f4dff2e1cf0883fa5e6e340e060868cde6efa38a461175", results["behavior:rr:villain-defeat:published-result"].Digest);
        Assert.Equal("9abcbfbb07699ca2eb3b58207acbc69f9291f7462e63feb74d14237a40d02bbf", results["behavior:rr:winning-the-game:published-result"].Digest);
    }

    [Fact]
    public void WhenDefeatedBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/when-defeated.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("1a8795dfcd4991da8f16b1a4ffa5b423858d18b6097c0689288bfae84feb55fa", results["behavior:rr:when-defeated-abilities.2.1:published-result"].Digest);
        Assert.Equal("01db892bd33eeadcfa93f37f285a48c76551ab1d6639bcc90af79ba68e6b2cec", results["behavior:rr:damage.step.6:published-result"].Digest);
        Assert.Equal("25c21c78f425e04fa16aea47dfddae4b457239a5dad69c90ac8f84aea4c35e4f", results["behavior:rr:damage.step.7:published-result"].Digest);
        Assert.Equal("728ae46a188452edf36cbe276561858b90003285802859519a087fd6d809c8b1", results["behavior:rr:damage.step.9:published-result"].Digest);
    }

    [Fact]
    public void VillainPhaseBranchesHavePinnedOutcomes()
    {
        var transcripts = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/villain-phase.feature::", StringComparison.Ordinal)).ToList();
        var results = transcripts.GroupBy(result => result.Obligation, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        Assert.Equal(["90785168acdbfe04069fc52329de5a53ae5228b5bcd9b1e2b12885d1d7768a4e", "d55193bf62e08ded4a9e322c585f7f75990508603a059402b2a5c8edcd1d8512", ], transcripts.Where(result => result.Obligation == "behavior:ruling:2ea7a5960d1275c8:published-clarification").Select(result => result.Digest).Order(StringComparer.Ordinal));
        Assert.Equal(32, results.Count);
        Assert.Equal("10e6ff94391b5978fdb5b175f86eed57919cc6f5728d09c824f7da7bbd35bd1d", results["behavior:rr:villain-phase:published-result"].Digest);
        Assert.Equal("b0ff2a7447a36803025223083bf02d64838a091294e678ffa8f1428cbff4dcc8", results["behavior:rr:villain-phase.step.5:published-result"].Digest);
        Assert.Equal("1cc04cfad1df57ffc4e4c2b2e6ca3191de6f2d615871d51329cbd1e5bf8c0dc8", results["behavior:rr:attack-enemy-activation:published-result"].Digest);
        Assert.Equal("b0c68f2a46a2e8c66317b76aac921bb2507a6ce5a67d51f4133493f5f61829d8", results["behavior:rr:boost-boost-icon.1:published-result"].Digest);
        Assert.Equal("abfbe4b174c105d8bf418c072b1cfea79b0917c3fea1f8261cc08382686e6521", results["behavior:rr:boost-boost-icon.4:published-result"].Digest);
        Assert.Equal("133a79973fea4acf7924ae51b863e298f70bdaa1e1a1131eda7eeb819d8e08d7", results["behavior:rr:scheme-enemy-activation.step.2.b:published-result"].Digest);
        Assert.Equal("b31c04122c4595d7d0925a9e81357adf6c2e1b26b5087b4126175617a90375e8", results["behavior:card:01178:if-villain-is-making-undefended-attack-place-condition-not-met"].Digest);
        Assert.Equal("3afed311809090907ecef08a62185d054961a82c50c64d1c1578d48279604075", results["behavior:rr:defend-defense.2:published-result"].Digest);
        Assert.Equal("f2490b69959814987e20e5944b68df094b91ce836db4307016cf37a4cfb87ba2", results["behavior:rr:attack-enemy-activation.2.2:published-result"].Digest);
        Assert.Equal("d552a161854747279b07d87ef652f78dbd349631a8137cf95c2da446b60bc5a0", results["behavior:rr:attack-enemy-activation.1.2:published-result"].Digest);
        Assert.Equal("3d5e0d079ed8729fda8355b2044773313e524c708c13710a8cc4c574f96b45fd", results["behavior:rr:defend-defense.5.2:published-result"].Digest);
        Assert.Equal("01d2a7cd1a08f28d4cdb3304e0e49dfea2cad7e104df1b3541f62732893cc859", results["behavior:card:01001a:when-villain-initiates-attack-against-you-draw"].Digest);
        Assert.Equal("9bb85953b0dfce95a2256151e5dae74a29805854227b35f15a648cf52fc40ac5", results["behavior:card:01099:when-rhino-attacks-attack-gains-overkill"].Digest);
        Assert.Equal("790be8003ca59bdeee19d09028fb6237e03cdec1754a8de7599d147482fce0ed", results["behavior:rr:defend-defense.3:published-result"].Digest);
        Assert.Equal("3b654ea3635c6ac21fb3936205e43d553259e89c5598c62ca9026c64871c37f0", results["behavior:rr:attack-enemy-activation.3.2:published-result"].Digest);
        Assert.Equal("289e54e1cfc0ce6d71992d6ce439d7f41a4d2a5217b35da861bab80f1fe38f66", results["behavior:rr:boost-boost-icon.3:published-result"].Digest);
        Assert.Equal("f1f5e2ca2eb7824329fbdb0b35e53a6208c852b6bf0c461e052239a54f999571", results["behavior:rr:activation.2:minion-attacks-hero"].Digest);
        Assert.Equal("c52dd398ac9ac69c087e013db02872a31442f61410b62b55481e36f64f0604f0", results["behavior:rr:activation.2:minion-schemes-against-alter-ego"].Digest);
        Assert.Equal("d6a079061f1e84cb9f3578acc9e576b769af90d6cfee6823e15895b6393fc73a", results["behavior:card:01003:when-you-would-take-any-amount-damage"].Digest);
        Assert.Equal("7f96f0d94fc1455b82b7ceb88fd4f729c34eec25c9e6485fb29e495a12c04633", results["behavior:card:01082:after-your-hero-defends-discard-indomitable-ready"].Digest);
        Assert.Equal("9c3745c9b471dcf720f4685d9bfed1aff5976eb2259e7f57b34bd9f165f6a5d3", results["behavior:card:01077:after-your-hero-defends-against-enemy-attack"].Digest);
        Assert.Equal("d002f1a999146884a16191abeebcecefa1a34c90c756a7ba5347ca5504422154", results["behavior:card:01004:when-treachery-card-is-revealed-from-encounter"].Digest);
    }

    [Fact]
    public void StatusCardBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/status-cards.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(10, results.Count);
        Assert.Equal("95422bc5dd7977120b841a634490f47aba4c0adcef0a3913cb29d5b9809cae48", results["behavior:rr:stun-stunned.5:published-result"].Digest);
        Assert.Equal("9e61447b7b126911a15c09b05e5d6c20449bbae9a01509ea424503430a6b3446", results["behavior:rr:confuse-confused.5:published-result"].Digest);
        Assert.Equal("195fbc1e543c474751df1b053bfc055e1603513496742924132dbe30862bb0c6", results["behavior:rr:stun-stunned.2:published-result"].Digest);
        Assert.Equal("b582040e3156d41882723bb7ce9482b6d714f176fcd18f69fc4d499d83145a1a", results["behavior:rr:confuse-confused.2:published-result"].Digest);
        Assert.Equal("01c6c3be11adc39aeb3c34666d19a3ac6663cec8504d31e2645d42457e71c599", results["behavior:rr:confuse-confused.6:published-result"].Digest);
        Assert.Equal("857554c05fa5fd06cfa4c217680c07428af50c1e4d593c94bbe06016a9537f26", results["behavior:rr:stun-stunned.6:published-result"].Digest);
        Assert.Equal("195fbc1e543c474751df1b053bfc055e1603513496742924132dbe30862bb0c6", results["behavior:rr:status-cards.1:published-result"].Digest);
        Assert.Equal("e128c30e647d113852a54caed45c133212cb750f679dc6f518f685a5b42c1911", results["behavior:rr:tough.2:published-result"].Digest);
        Assert.Equal("db08a2062631087a6053475ac7c1d50732333aa5324cc7703e38acd7edc0f5a2", results["behavior:rr:toughness:published-result"].Digest);
    }

    [Fact]
    public void SetupBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/setup.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(9, results.Count);
        Assert.Equal("2f9c6a8f3380beb27f4bd050577cfe041c3786528fe137487b0cf189127139ac", results["behavior:rr:appendix-ii-setup.step.1:published-result"].Digest);
        Assert.Equal("01f6ad5a98e93248e2590f80009847ce41cf28f94476845a7c6fb1d5ef9c7bd5", results["behavior:rr:modes-of-play.2:published-result"].Digest);
        Assert.Equal("c84cb8fa6d730f6c263103adecd6c8d30750a1553400b4256e4806af8175d412", results["behavior:rr:modular-encounter-set.1:published-result"].Digest);
        Assert.Equal("48cca5ee5a42796f2b00375df18e4c0232c69cde1f07e104516125720136e7dd", results["behavior:rr:appendix-ii-setup.step.15:published-result"].Digest);
        Assert.Equal("17e0cd6e760f3f0201bdfbadf27e22cd7202002092bc9c4f9d446aa5cc26a6b3", results["behavior:card:01040b:search-your-deck-for-black-panther-upgrade"].Digest);
        Assert.Equal("d053f1672b3b860fdc70b0a46dc08712a203d0c73810e5e0f241d57045139108", results["behavior:card:01116a:search-encounter-deck-for-defense-network-side"].Digest);
        Assert.Equal("66a27ed1b7d8f5692280431175aa43583ccf21dd52ffb8a899e2dcacd6adf346", results["behavior:card:01137a:put-ultron-drones-environment-into-play"].Digest);
        Assert.Equal("9ff6e11a5e9807275f68698c1d8e654f81af27cff479b8814e3179cbf15861f4", results["behavior:card:01116a:klaw-ii-and-klaw-iii-instead-for"].Digest);
        Assert.Equal("a588af4775e8d1c032e61e24d1218e421836dc01edf0dfef4fa573c6009374e7", results["behavior:card:01137a:ultron-ii-and-ultron-iii-instead-for"].Digest);
    }

    [Fact]
    public void DiscardBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/discard.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(3, results.Count);
        Assert.Equal("d30a7e62db3629bc4c81817304281f1eb185bacecfd1f8113d43828d2912252c", results["behavior:rr:discard.1:published-result"].Digest);
        Assert.Equal("3437531e41ea97cddfd470fec18fe0c1db7bc0b34d2d038f3f1d75917e2caa75", results["behavior:rr:discard.2:published-result"].Digest);
        Assert.Equal("0aa7a5c0ea184651f4d75675d90f67fd3c825ff7fcb6cb634ca19bdf7549e347", results["behavior:rr:discard.4:published-result"].Digest);
    }

    [Fact]
    public void FormChangeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/form-change.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(8, results.Count);
        Assert.Equal("0c919a534d397c889e9e98cbe71726154c51140e4bdb381bc6110cee048c5bc9", results["behavior:rr:form-change-form.1:flip-identity"].Digest);
        Assert.Equal("7f45980b7f1575a2aad014d0810c4d92ae29a03c3ab97db9fe4c067f0b79d239", results["behavior:rr:form-change-form.1:voluntary-window-and-limit"].Digest);
        Assert.Equal("31b2f56e8e79c1578b6f3288b0053cbd308fc4f07944508a06bc2465cffcbc2b", results["behavior:rr:form-change-form.3:published-result"].Digest);
        Assert.Equal("239aaaa8ef1d88f110f53f6dd2245c08c02285f3198e2a5733d73148c30a1619", results["behavior:card:01025:then-draw-up-your-printed-hand-size-intermediate"].Digest);
        Assert.Equal("423b5cf71b961907c1d63dc70ad46aa0248918f52857b685d584f2d2768c43f7", results["behavior:card:01025:then-draw-up-your-printed-hand-size-minimum"].Digest);
        Assert.Equal("159a70550d17f9aaab6d37da5311afa6741ecae3b4a5b5b89ef814a56dbc46ff", results["behavior:rr:form-change-form.4:published-result"].Digest);
        Assert.Equal("799962ad1150e2322df59a44732bece8c9875bd39e712e569165c2bfe726e320", results["behavior:rr:form-change-form.5:published-result"].Digest);
        Assert.Equal("604e45f501fc1af1026f2d840ed696927978c3f8e93fe3874982b37a2092ba73", results["behavior:rr:form-change-form.7:published-result"].Digest);
    }

    [Fact]
    public void PlayerEliminationBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/player-elimination.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("77275b950cc268c82067c48f060a2d070688ebe852fa8cf13c6f527e426d65a5", results["behavior:rr:player-elimination:published-result"].Digest);
        Assert.Equal("03661c7d3c891adb124d8a3e885c23d782c306d1e657b39662b48e317b80f646", results["behavior:rr:player-elimination.3:published-result"].Digest);
        Assert.Equal("3775e34eb9c35a349628d652550cd9114a0c7998733004662c82465a0fa5e906", results["behavior:rr:player-elimination.5:published-result"].Digest);
        Assert.Equal("c1e2215d7e40cc0f807a134a3d42ac4d3f8e8f55ec29e670e114056f90321ab8", results["behavior:rr:player-elimination.4:published-result"].Digest);
    }

    [Fact]
    public void SimultaneousResolutionHasAPinnedOutcome()
    {
        var result = Assert.Single(CoreTranscriptCorpus.All, result => result.Scenario.StartsWith("specs/behavior/core/simultaneous-resolution.feature::", StringComparison.Ordinal));
        Assert.Equal("behavior:rr:simultaneous-resolution:published-result", result.Obligation);
        Assert.Equal("257936164c7e0749dd7b2e7d1954996b72c1583650c97ca6ce4e6b83715b3e7a", result.Digest);
    }

    [Fact]
    public void EndOfPlayerPhaseBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/end-of-player-phase.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("f3eab52cfda18db04e2f2d5fdb213fd02efd550c790908ca5f2a6913f2fd8f2c", results["behavior:rr:end-of-player-phase.step.1:optional-at-or-below"].Digest);
        Assert.Equal("5c941bb555d6f458c769116c7fe8fa51fca83a115268598e79dd7b9a5aa2879d", results["behavior:rr:end-of-player-phase.step.1:mandatory-above-limit"].Digest);
        Assert.Equal("4aa49996a3dbb18961fca6ed5ee0e951edb0aa33ef4be61fbedc7753d1622ec9", results["behavior:rr:end-of-player-phase.step.2:below-limit"].Digest);
        Assert.Equal("4aa49996a3dbb18961fca6ed5ee0e951edb0aa33ef4be61fbedc7753d1622ec9", results["behavior:rr:end-of-player-phase.step.2:at-limit"].Digest);
        Assert.Equal("93201ed18853aaa84874971fca0e0624765c8829f4cdb808877cfad227e5060d", results["behavior:rr:end-of-player-phase.step.3:ready-all-in-play"].Digest);
    }

    [Fact]
    public void PlayerPhaseTurnOrderHasPinnedOutcome()
    {
        TranscriptResult result = Assert.Single(CoreTranscriptCorpus.All, result => result.Scenario.StartsWith("specs/behavior/core/player-phase.feature::", StringComparison.Ordinal));
        Assert.Equal("behavior:rr:player-phase:published-result", result.Obligation);
        Assert.Equal("906f9ea321af514b8fa7ea30215d84550d1fabc90627f21760cb35e1302f5252", result.Digest);
    }
}
