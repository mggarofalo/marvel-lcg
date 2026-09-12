using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreRuleTranscriptOwnershipTests
{
    [Fact]
    public void OwnershipAndControlBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ownership-control.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(4, results.Count);
        Assert.Equal("fc0969e044e19641e706f861ab461224eedae2eb03560f926571c20a3d6c2ab2", results["behavior:rr:ownership-and-control.2.1:published-result"].Digest);
        Assert.Equal("270b3cab1ef837a77419721764d66dbfdc9bbfe1a898cdb0b7b40993d0f3b923", results["behavior:rr:cost.7:published-result"].Digest);
        Assert.Equal("9e5c6be4c13d55abbf6c15d1a39f6d2554daaf95d78fafbecca39b6d5928cb58", results["behavior:rr:ownership-and-control.8:published-result"].Digest);
    }

    [Fact]
    public void ThreatPreventionBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/threat-prevention.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(2, results.Count);
        Assert.Equal("50f656d54db4bc2bdd8bb146cde7374a2ed3a46ab8e6ab80c88bad6b73f7f463", results["behavior:rr:prevent.2:published-result"].Digest);
        Assert.Equal("a1d7bc3b1777635d2d7533d9c826672de58b97619123449f1b47920b6c772295", results["behavior:rr:you-your.2:published-result"].Digest);
    }

    [Fact]
    public void TimingWindowBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/timing-windows.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(7, results.Count);
        Assert.Equal("cf0c14b5a10b7cc87760c3ba462daa2a657242c520db5ef0a3334d6d0f8e74be", results["behavior:rr:interrupt.2:published-result"].Digest);
        Assert.Equal("6d908134dac70fe57bba1892870238d0ea86dda556786e7bab66407d00b2d3eb", results["behavior:rr:interrupt.4:published-result"].Digest);
        Assert.Equal("28223233dad071844323e9f5c9ad02dfd4598264e1dab3e3e0ac54bde8293016", results["behavior:rr:interrupt.5:published-result"].Digest);
        Assert.Equal("f126d0407e6845603f387368def162da7d4f6451ea60044b69eadc8fe2a108d9", results["behavior:rr:response.2:published-result"].Digest);
        Assert.Equal("6f0f7dc71af08c51e682ca353a505e934e51aa0ece2ceec45cb55d2e523a6cf6", results["behavior:rr:triggering-condition.1:published-result"].Digest);
        Assert.Equal("910925405dbe99abeeb5ca5a7aaa969b2be078c8aadfa68165ffdfea8cc48339", results["behavior:rr:response.4:published-result"].Digest);
    }

    [Fact]
    public void AllyLimitHasPinnedOutcome()
    {
        TranscriptResult result = Assert.Single(CoreTranscriptCorpus.All, result => result.Scenario.StartsWith("specs/behavior/core/ally-limit.feature::", StringComparison.Ordinal));
        Assert.Equal("behavior:rr:ally-limit:published-result", result.Obligation);
        Assert.Equal("8c4cc2574dcaf3373fdee717b3fee9a6c283735f219cd4fe4365993d93506261", result.Digest);
    }

    [Fact]
    public void AttachmentBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/attachments.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(7, results.Count);
        Assert.Equal("f00098f9a7b58eb4ead41d6950018a674febe272eef54560f5cdc6978dbdd4f3", results["behavior:card:01009:attach-enemy"].Digest);
        Assert.Equal("422b6819e211329aa2c280f68d7df90a67ae359196e4bd66a46fb775e961f6c5", results["behavior:rr:attach-to:published-result"].Digest);
        Assert.Equal("88e5685b977a139b549f3fba7c3f8bde377c9100a3e652d29fe403f072affe83", results["behavior:rr:attach-to.1:published-result"].Digest);
        Assert.Equal("2b2f1084f9e733267098a0d3479984e30d5f786edf0a42009c2c7836247b2c7f", results["behavior:rr:attach-to.3:published-result"].Digest);
        Assert.Equal("b0519e50cb3b5fa1e265795eaa4bbe18deaa19fb300c515a6a36b7ec6d67361f", results["behavior:rr:max-maximum.4:published-result"].Digest);
        Assert.Equal("e9fd28b2b2c2364def7f1b172f27d077d2f48bc318cee4867a06681937a8c7aa", results["behavior:card:01163:attach-minion-with-highest-printed-hit-points"].Digest);
        Assert.Equal("0f4332812192a3edf68718b79c17b0cdc49349bd67c91fc37bd23931b4dbcfb3", results["behavior:card:01163:if-there-are-no-minions-in-play-condition-met"].Digest);
    }

    [Fact]
    public void CharacteristicBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/characteristics.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("34c725e7bf00f8edee5d88a05622b4ea7331342e372910a124114a8527af9a28", results["behavior:card:01039:you-get-1-hit-point"].Digest);
        Assert.Equal("5f922258b17be5385f9204dbebd9a27de7765b60d786dacdbae421e0abeb5038", results["behavior:rr:attachment.1:published-result"].Digest);
        Assert.Equal("c5a14e8a14152ebd20333940065ac44678643817379e43da317231a6c4a05571", results["behavior:card:01039:exhaust-rocket-boots-and-spend-mental-resource"].Digest);
        Assert.Equal("622899a27dbed3f733ecfbbc58bd17f672d591acb2183f47ecc671025275ca06", results["behavior:rr:modifiers.6.1:published-result"].Digest);
        Assert.Equal("eae2c4796f896ebabf4f786154582acdfa5494b31c4c42f8f4b83bcd0cbc64eb", results["behavior:rr:lasting-effects.4:published-result"].Digest);
    }

    [Fact]
    public void PlayAreaBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/play-areas.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Single(results);
        Assert.Equal("3d1a9d514dfecae1def8df553d07f182622b997475dfb65643d3156817885ec4", results["behavior:rr:play-area.1:published-result"].Digest);
    }

    [Fact]
    public void MainSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/main-scheme.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(4, results.Count);
        Assert.Equal("f7e6aa3540fa3483267f71004045e833c9a48d2f6c1d4077bb34dabf48c876d0", results["behavior:rr:main-scheme-main-scheme-deck.2:published-result"].Digest);
        Assert.Equal("2cab030ee937e52952f66bbb62a1380d8d71faf78ecf985a28b78b00322921c6", results["behavior:rr:main-scheme-main-scheme-deck.2.1:published-result"].Digest);
        Assert.Equal("ba88421ca03e246a89956e9b1cdee1b9d9d7694cd0bb60256362aceb668c99d9", results["behavior:rr:main-scheme-main-scheme-deck.6:published-result"].Digest);
        Assert.Equal("dd354d457fd349e13e8c04eb46e5e4a7eab48adedce309584e706f5181e375b3", results["behavior:card:01117b:if-stage-is-completed-players-lose-game-condition-met"].Digest);
    }

    [Fact]
    public void EncounterIconBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/encounter-icons.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("addf2e1a30ee9bd6e217a4c98150418b109cae8869e2c5555629a4cb479b8174", results["behavior:rr:acceleration-icon.1:published-result"].Digest);
        Assert.Equal("734689a82d46c7ee345f331008705e7da91491cca538f442c7216a1c92ef5034", results["behavior:rr:acceleration-icon.2:published-result"].Digest);
        Assert.Equal("0c04a5a5f23ac3a5efc0428070ae935ed641fe58146507506e414a1273ccf19e", results["behavior:rr:acceleration-icon.3:published-result"].Digest);
        Assert.Equal("2cfa2ec91af0e75e2bdd875aadbaf3d1b6e09a68f502df1e5b42676c48b8947f", results["behavior:rr:crisis-icon.1:published-result"].Digest);
        Assert.Equal("2cb656c532aea3e361cc1f76be37137240acfb88675a613b9ff3f76d0be39ad0", results["behavior:rr:hazard-icon:published-result"].Digest);
    }

    [Fact]
    public void EncounterRevealBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/encounter-reveal.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(10, results.Count);
        Assert.Equal("185218ad400e2a7f175baf262f564963e8681749cb53f6a78f7d34b58fd65b4b", results["behavior:rr:reveal.5:published-result"].Digest);
        Assert.Equal("65656902d0f5ede582036eb10cae34c68f07ba12613b54ff1293fa39261cfce6", results["behavior:rr:treachery.1:published-result"].Digest);
        Assert.Equal("c30f3fe43d32a554540aca1a4f0de0cc5916b311b588e8de8d549529996cc8be", results["behavior:card:01104:if-no-damage-was-healed-way-card-condition-met"].Digest);
        Assert.Equal("7a639c9c59a9b1327add25af8cf4807d2aaadd09c65b20effeeb5466578b078f", results["behavior:rr:reveal.8:published-result"].Digest);
        Assert.Equal("eae3014b5502cdd02cd7694ea2a6332b306a402acf3e089126d2a661a7df28e4", results["behavior:card:01106:rhino-attacks-you"].Digest);
        Assert.Equal("864e7268a6c38b29c45295e15c084b720cd0fe603b0fc79009a34514eeec9f5d", results["behavior:card:01149:each-player-discards-top-3-cards-their-one-player"].Digest);
        Assert.Equal("72f55a96a9339d9f95c5ada89dce89c3bc02717abfbf1b857bc5f4438edd2d70", results["behavior:card:01149:each-player-discards-top-3-cards-their-multiple-players"].Digest);
    }

    [Fact]
    public void CoreIdentityObligationBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/legal-work.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(10, results.Count);
        Assert.Equal("5555335fa80dde057285925b06d58104f77428c6568acc5d29938b4c81333bd6", results["behavior:card:01160:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("d1c1ec43da722baed6be9a31f50e0a20cc3ebdcb04ba04680a0f19257b43115e", results["behavior:card:01160:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("2b80db251223ef118e229b6718136a3a90f92b63b5269693eea9804e81efbc77", results["behavior:card:01155:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("73bc9831566b5df35541b94aea505ac774eacbcb5060b84eed21742f3f094380", results["behavior:card:01155:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("5738df4b38e857dddb9196398196c0d0ecf10173f2506205fa47f9b869d383fb", results["behavior:card:01165:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("0b838c25ca6bdeac12496e6d3f0835f9c4b6e9721ad0ee412b5fd6b6fd8256d9", results["behavior:card:01165:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("4a0606febb0c70ed09b2b4d07154a6040ce2b4d4abeffe380f188302b47d8ac7", results["behavior:card:01170:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("65c41c4fb25749752526fc259d37b9a956ad3c0e515abdd3b5522e5295640e75", results["behavior:card:01170:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("c28e6854a17bbf90af64b40536b734d4f734ae21dd518d2a4180a3e62c196b0e", results["behavior:card:01175:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("c176b18fe710c0d21550b038a31b33659020a86e611779f9efaf0053bf9a6421", results["behavior:card:01175:you-may-flip-alter-ego-form-declined"].Digest);
    }

    [Fact]
    public void BasicPowerBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/basic-powers.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("18fc35ed0b6fc714a853ec3bedb7814696bc741496c82e5af4e8932f1f48ad18", results["behavior:rr:attack-player-ability-type.1:published-result"].Digest);
        Assert.Equal("af7b74e4760c3e5d358eb6ac4f3b9e174cad0686a8cd11aeebe4ff9fa2edb3aa", results["behavior:rr:ally.2:published-result"].Digest);
        Assert.Equal("735e397ab8fbdc6d3ccc89d0b1d452da40d6ad8080a9c8d0cdd01186dc5c6c26", results["behavior:rr:thwart.1:published-result"].Digest);
        Assert.Equal("5544ca405e8d6c6682d491e3215491fdd98c8f8f5e9e2740743f3d810bb0b489", results["behavior:rr:consequential-damage.1:published-result"].Digest);
        Assert.Equal("990abfbca51dc3c52846309365d832c3755c69853501253b8e55fdf2ab4382da", results["behavior:rr:recover-recovery:published-result"].Digest);
    }

    [Fact]
    public void BasicPowerRestrictionBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/basic-power-restrictions.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(3, results.Count);
        Assert.Equal("36fcdb5d79e4a039ba08dea0218a26d8c2402ead146d666691f43f6995e63c34", results["behavior:rr:guard:published-result"].Digest);
        Assert.Equal("0852412184728a9360eb6fa1be001946ec862cd5afa5f9d68f60cfba82c90fd0", results["behavior:rr:thwart.1.1:published-result"].Digest);
        Assert.Equal("089db7f223cf896d245e49103ba2f1588a560189d7f7abaf79c517597f1c5c9f", results["behavior:rr:recover-recovery.1:published-result"].Digest);
    }
}
