using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreRuleTranscriptTests
{
    private static readonly Lazy<IReadOnlyList<TranscriptResult>> Corpus = new(
        () => new CoreTranscriptSuite(RepositoryPaths.Root).RunPassingCorpus());

    [Fact]
    public void MainSchemeBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/main-scheme.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(2, results.Count);
        Assert.Equal(
            "a0c29f1591b4fc8d736a64e76d9109a4f899a21a278cc4d2760cdd5a4c1f8b70",
            results["behavior:rr:main-scheme-main-scheme-deck.2:published-result"].Digest);
        Assert.Equal(
            "2cab030ee937e52952f66bbb62a1380d8d71faf78ecf985a28b78b00322921c6",
            results["behavior:rr:main-scheme-main-scheme-deck.2.1:published-result"].Digest);
    }

    [Fact]
    public void EncounterIconBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/encounter-icons.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(4, results.Count);
        Assert.Equal(
            "addf2e1a30ee9bd6e217a4c98150418b109cae8869e2c5555629a4cb479b8174",
            results["behavior:rr:acceleration-icon.1:published-result"].Digest);
        Assert.Equal(
            "734689a82d46c7ee345f331008705e7da91491cca538f442c7216a1c92ef5034",
            results["behavior:rr:acceleration-icon.2:published-result"].Digest);
        Assert.Equal(
            "2cfa2ec91af0e75e2bdd875aadbaf3d1b6e09a68f502df1e5b42676c48b8947f",
            results["behavior:rr:crisis-icon.1:published-result"].Digest);
        Assert.Equal(
            "2cb656c532aea3e361cc1f76be37137240acfb88675a613b9ff3f76d0be39ad0",
            results["behavior:rr:hazard-icon:published-result"].Digest);
    }

    [Fact]
    public void BasicPowerBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/basic-powers.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(5, results.Count);
        Assert.Equal(
            "18fc35ed0b6fc714a853ec3bedb7814696bc741496c82e5af4e8932f1f48ad18",
            results["behavior:rr:attack-player-ability-type.1:published-result"].Digest);
        Assert.Equal(
            "af7b74e4760c3e5d358eb6ac4f3b9e174cad0686a8cd11aeebe4ff9fa2edb3aa",
            results["behavior:rr:ally.2:published-result"].Digest);
        Assert.Equal(
            "735e397ab8fbdc6d3ccc89d0b1d452da40d6ad8080a9c8d0cdd01186dc5c6c26",
            results["behavior:rr:thwart.1:published-result"].Digest);
        Assert.Equal(
            "5544ca405e8d6c6682d491e3215491fdd98c8f8f5e9e2740743f3d810bb0b489",
            results["behavior:rr:consequential-damage.1:published-result"].Digest);
        Assert.Equal(
            "990abfbca51dc3c52846309365d832c3755c69853501253b8e55fdf2ab4382da",
            results["behavior:rr:recover-recovery:published-result"].Digest);
    }

    [Fact]
    public void BasicPowerRestrictionBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/basic-power-restrictions.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(3, results.Count);
        Assert.Equal(
            "f3ac2273508e74e547cbbcbb0ea49b5340052b1d2ddfc9d6a5fa1f6961443c83",
            results["behavior:rr:guard:published-result"].Digest);
        Assert.Equal(
            "0852412184728a9360eb6fa1be001946ec862cd5afa5f9d68f60cfba82c90fd0",
            results["behavior:rr:thwart.1.1:published-result"].Digest);
        Assert.Equal(
            "089db7f223cf896d245e49103ba2f1588a560189d7f7abaf79c517597f1c5c9f",
            results["behavior:rr:recover-recovery.1:published-result"].Digest);
    }

    [Fact]
    public void DefeatBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/defeat.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(3, results.Count);
        Assert.Equal(
            "87b20f195af0f89ca10e94890082e869d08366a336f3b3b566b1811b99744754",
            results["behavior:rr:minion.2:published-result"].Digest);
        Assert.Equal(
            "071e9a2731b848811a847e30d8d452ecfc3195ce515e9ed2afd9397f8bbb71bd",
            results["behavior:rr:side-scheme.2:published-result"].Digest);
        Assert.Equal(
            "107af270de710690ab92afe8c1bd3c073593b6c171aea3c6dfd921c08b822976",
            results["behavior:rr:villain-defeat:published-result"].Digest);
    }

    [Fact]
    public void VillainPhaseBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/villain-phase.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(10, results.Count);
        Assert.Equal(
            "aadbff792b4caca6a6ecbb13041865133c3e9ce1d588d1a9c6a59834d8a97fc8",
            results["behavior:rr:villain-phase:published-result"].Digest);
        Assert.Equal(
            "056458e0067d1547ec97b141112469a9bbf885320236cab8539b54d32902c3c9",
            results["behavior:rr:villain-phase.step.5:published-result"].Digest);
        Assert.Equal(
            "d4864dcf86f264cb0b7cb1a6df0bf3832cfa3c3c388a761716b4cb8787c23d29",
            results["behavior:rr:attack-enemy-activation:published-result"].Digest);
        Assert.Equal(
            "56d25f9a5c00d4e9955cea06c73edf7c41e1c0f38021d9b539684b97f36519fd",
            results["behavior:rr:defend-defense.2:published-result"].Digest);
        Assert.Equal(
            "aeef1195f4475b5e1b1c0fc0c2bd7a7b045e8529404e1c365c502ba181af1d93",
            results["behavior:rr:attack-enemy-activation.2.2:published-result"].Digest);
        Assert.Equal(
            "a259366a144343476f52089e383c34b94d2d5f89adac81865a523c6d276fc35d",
            results["behavior:rr:attack-enemy-activation.1.2:published-result"].Digest);
        Assert.Equal(
            "47f4cb61477103656512e5b7a18f1dc3e90a0d3dcc3f525035a04a234e458f2c",
            results["behavior:card:01001a:when-villain-initiates-attack-against-you-draw"].Digest);
        Assert.Equal(
            "19ae349185692d4d06243e8ad5b6cb272e9eae4a699a5d993d9f563aef081212",
            results["behavior:rr:defend-defense.3:published-result"].Digest);
        Assert.Equal(
            "f9e1834f6d3f5862521bd902e7d8622353cacd79fd02b503547368943b9470eb",
            results["behavior:rr:activation.2:minion-attacks-hero"].Digest);
        Assert.Equal(
            "373a28f2528b37a3b32362940138c590f6d46fd14585b5dad1f3302fcdcf39d8",
            results["behavior:rr:activation.2:minion-schemes-against-alter-ego"].Digest);
    }

    [Fact]
    public void StatusCardBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/status-cards.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(6, results.Count);
        Assert.Equal(
            "dab07315ec82d4dffa5852140b360299a47d7cc396c78e54b34be5b4c22cfe53",
            results["behavior:rr:stun-stunned.5:published-result"].Digest);
        Assert.Equal(
            "2e19591503121238703885aa7b629ade053670e7e220727ce82bcf5e7b12857e",
            results["behavior:rr:confuse-confused.5:published-result"].Digest);
        Assert.Equal(
            "7d7ced5d78722f6dd1c69d9a07e291dad214bd3037a647c5ceeab7a6933e2906",
            results["behavior:rr:stun-stunned.2:published-result"].Digest);
        Assert.Equal(
            "7d7ced5d78722f6dd1c69d9a07e291dad214bd3037a647c5ceeab7a6933e2906",
            results["behavior:rr:status-cards.1:published-result"].Digest);
        Assert.Equal(
            "ec6c7fd5a9ebfe1aff876848a982ec1ff60371028415b7ab983b9919b13b0eb2",
            results["behavior:rr:tough.2:published-result"].Digest);
        Assert.Equal(
            "99aabf9e027f556c91314e0f769edab28f3021124ddba46a06fb489e2ad2957d",
            results["behavior:rr:toughness:published-result"].Digest);
    }

    [Fact]
    public void DiscardBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/discard.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(3, results.Count);
        Assert.Equal(
            "2ba5b4286eab4a1bb821b5d3c4774ee37feab34cd36737774db64c09f513b710",
            results["behavior:rr:discard.1:published-result"].Digest);
        Assert.Equal(
            "a7a02fd9e996eb1681de52ad46f6b97822f4e7ba11c0047f9ce198f87b8eec09",
            results["behavior:rr:discard.2:published-result"].Digest);
        Assert.Equal(
            "2c42f35da3197479de24e5e865d7e98f105812cb26a5ceb96c4dbd525719b56a",
            results["behavior:rr:discard.4:published-result"].Digest);
    }

    [Fact]
    public void FormChangeBranchesHavePinnedOutcomes()
    {
        TranscriptResult result = Assert.Single(
            Corpus.Value,
            candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/form-change.feature::",
                StringComparison.Ordinal));

        Assert.Equal("behavior:rr:form-change-form.1:flip-identity", result.Obligation);
        Assert.Equal(
            "8ac76760febf2740557277dc71b65fbc299b5cef08e0a510bbb53a2c06a33a26",
            result.Digest);
    }

    [Fact]
    public void PlayerEliminationBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/player-elimination.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(2, results.Count);
        Assert.Equal(
            "738068fc245332178451bfe360292ac3d058c7b1cede5e38abfa090541f59e0f",
            results["behavior:rr:player-elimination:published-result"].Digest);
        Assert.Equal(
            "c838d45bba9a3b039bc03b7084664ed0875d7d19ac9b5c8febff67f0a187569f",
            results["behavior:rr:player-elimination.4:published-result"].Digest);
    }

    [Fact]
    public void EndOfPlayerPhaseBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/end-of-player-phase.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(5, results.Count);
        Assert.Equal(
            "c81566a2f566d19ac039532532df0d728f6d5583b7028e1798eba3d555e42e23",
            results["behavior:rr:end-of-player-phase.step.1:optional-at-or-below"].Digest);
        Assert.Equal(
            "228ad5267ebe07ec00349ff20b304812ecdf680ab264bfbf98f77c49dde400d3",
            results["behavior:rr:end-of-player-phase.step.1:mandatory-above-limit"].Digest);
        Assert.Equal(
            "ba21ebd4d8f01244d40f9dbe5d30c095ea72fdd748ad123d726cdb1596ce45d6",
            results["behavior:rr:end-of-player-phase.step.2:below-limit"].Digest);
        Assert.Equal(
            "ba21ebd4d8f01244d40f9dbe5d30c095ea72fdd748ad123d726cdb1596ce45d6",
            results["behavior:rr:end-of-player-phase.step.2:at-limit"].Digest);
        Assert.Equal(
            "4159ea0cb84196706b181c689f9c9dba4b4cc6e3cf7a5775b4342eafbc166e44",
            results["behavior:rr:end-of-player-phase.step.3:ready-all-in-play"].Digest);
    }

    [Fact]
    public void EncounterDeckBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
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
        var results = Corpus.Value
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
