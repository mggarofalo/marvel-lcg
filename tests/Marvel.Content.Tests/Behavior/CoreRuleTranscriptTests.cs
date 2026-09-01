using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreRuleTranscriptTests
{
    private static readonly Lazy<IReadOnlyList<TranscriptResult>> Corpus = new(
        () => new CoreTranscriptSuite(RepositoryPaths.Root).RunPassingCorpus());

    [Fact]
    public void AllyLimitHasPinnedOutcome()
    {
        TranscriptResult result = Assert.Single(Corpus.Value, result =>
            result.Scenario.StartsWith(
                "specs/behavior/core/ally-limit.feature::",
                StringComparison.Ordinal));

        Assert.Equal("behavior:rr:ally-limit:published-result", result.Obligation);
        Assert.Equal(
            "8c4cc2574dcaf3373fdee717b3fee9a6c283735f219cd4fe4365993d93506261",
            result.Digest);
    }

    [Fact]
    public void AttachmentBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/attachments.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(3, results.Count);
        Assert.Equal(
            "422b6819e211329aa2c280f68d7df90a67ae359196e4bd66a46fb775e961f6c5",
            results["behavior:rr:attach-to:published-result"].Digest);
        Assert.Equal(
            "88e5685b977a139b549f3fba7c3f8bde377c9100a3e652d29fe403f072affe83",
            results["behavior:rr:attach-to.1:published-result"].Digest);
        Assert.Equal(
            "2b2f1084f9e733267098a0d3479984e30d5f786edf0a42009c2c7836247b2c7f",
            results["behavior:rr:attach-to.3:published-result"].Digest);
    }

    [Fact]
    public void CharacteristicBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/characteristics.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(5, results.Count);
        Assert.Equal(
            "34c725e7bf00f8edee5d88a05622b4ea7331342e372910a124114a8527af9a28",
            results["behavior:card:01039:you-get-1-hit-point"].Digest);
        Assert.Equal(
            "70c83706d9a4071f8d1971e131006ad4631b5ffae646de407983e2640515e9d2",
            results["behavior:rr:attachment.1:published-result"].Digest);
        Assert.Equal(
            "c5a14e8a14152ebd20333940065ac44678643817379e43da317231a6c4a05571",
            results["behavior:card:01039:exhaust-rocket-boots-and-spend-mental-resource"].Digest);
        Assert.Equal(
            "622899a27dbed3f733ecfbbc58bd17f672d591acb2183f47ecc671025275ca06",
            results["behavior:rr:modifiers.6.1:published-result"].Digest);
        Assert.Equal(
            "eae2c4796f896ebabf4f786154582acdfa5494b31c4c42f8f4b83bcd0cbc64eb",
            results["behavior:rr:lasting-effects.4:published-result"].Digest);
    }

    [Fact]
    public void PlayAreaBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/play-areas.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Single(results);
        Assert.Equal(
            "129b052cf1d9337a6fa2b05540538dda462d1bba59751234c5a0bd8d806fb519",
            results["behavior:rr:play-area.1:published-result"].Digest);
    }

    [Fact]
    public void MainSchemeBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/main-scheme.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(3, results.Count);
        Assert.Equal(
            "a0c29f1591b4fc8d736a64e76d9109a4f899a21a278cc4d2760cdd5a4c1f8b70",
            results["behavior:rr:main-scheme-main-scheme-deck.2:published-result"].Digest);
        Assert.Equal(
            "2cab030ee937e52952f66bbb62a1380d8d71faf78ecf985a28b78b00322921c6",
            results["behavior:rr:main-scheme-main-scheme-deck.2.1:published-result"].Digest);
        Assert.Equal(
            "ba88421ca03e246a89956e9b1cdee1b9d9d7694cd0bb60256362aceb668c99d9",
            results["behavior:rr:main-scheme-main-scheme-deck.6:published-result"].Digest);
    }

    [Fact]
    public void EncounterIconBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/encounter-icons.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(5, results.Count);
        Assert.Equal(
            "addf2e1a30ee9bd6e217a4c98150418b109cae8869e2c5555629a4cb479b8174",
            results["behavior:rr:acceleration-icon.1:published-result"].Digest);
        Assert.Equal(
            "734689a82d46c7ee345f331008705e7da91491cca538f442c7216a1c92ef5034",
            results["behavior:rr:acceleration-icon.2:published-result"].Digest);
        Assert.Equal(
            "0c04a5a5f23ac3a5efc0428070ae935ed641fe58146507506e414a1273ccf19e",
            results["behavior:rr:acceleration-icon.3:published-result"].Digest);
        Assert.Equal(
            "2cfa2ec91af0e75e2bdd875aadbaf3d1b6e09a68f502df1e5b42676c48b8947f",
            results["behavior:rr:crisis-icon.1:published-result"].Digest);
        Assert.Equal(
            "2cb656c532aea3e361cc1f76be37137240acfb88675a613b9ff3f76d0be39ad0",
            results["behavior:rr:hazard-icon:published-result"].Digest);
    }

    [Fact]
    public void EncounterRevealBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/encounter-reveal.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(7, results.Count);
        Assert.Equal(
            "185218ad400e2a7f175baf262f564963e8681749cb53f6a78f7d34b58fd65b4b",
            results["behavior:rr:reveal.5:published-result"].Digest);
        Assert.Equal(
            "65656902d0f5ede582036eb10cae34c68f07ba12613b54ff1293fa39261cfce6",
            results["behavior:rr:treachery.1:published-result"].Digest);
        Assert.Equal(
            "c30f3fe43d32a554540aca1a4f0de0cc5916b311b588e8de8d549529996cc8be",
            results["behavior:card:01104:if-no-damage-was-healed-way-card-condition-met"].Digest);
        Assert.Equal(
            "7a639c9c59a9b1327add25af8cf4807d2aaadd09c65b20effeeb5466578b078f",
            results["behavior:rr:reveal.8:published-result"].Digest);
        Assert.Equal(
            "eae3014b5502cdd02cd7694ea2a6332b306a402acf3e089126d2a661a7df28e4",
            results["behavior:card:01106:rhino-attacks-you"].Digest);
        Assert.Equal(
            "ee37b7f3a89ce756dfd77db1489ed47e0705f1f1c318c1945b9f64358b42bc21",
            results[
                "behavior:card:01149:each-player-discards-top-3-cards-their-one-player"].Digest);
        Assert.Equal(
            "0e89a8c73a6db2f6ac37524a7ffed0a4d01f3d067a74c74c6a1c06490db98d16",
            results[
                "behavior:card:01149:each-player-discards-top-3-cards-their-multiple-players"].Digest);
    }

    [Fact]
    public void LegalWorkBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/legal-work.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(2, results.Count);
        Assert.Equal(
            "5555335fa80dde057285925b06d58104f77428c6568acc5d29938b4c81333bd6",
            results[
                "behavior:card:01160:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal(
            "d1c1ec43da722baed6be9a31f50e0a20cc3ebdcb04ba04680a0f19257b43115e",
            results[
                "behavior:card:01160:you-may-flip-alter-ego-form-accepted"].Digest);
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
            "36fcdb5d79e4a039ba08dea0218a26d8c2402ead146d666691f43f6995e63c34",
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

        Assert.Equal(4, results.Count);
        Assert.Equal(
            "87b20f195af0f89ca10e94890082e869d08366a336f3b3b566b1811b99744754",
            results["behavior:rr:minion.2:published-result"].Digest);
        Assert.Equal(
            "7e0a4fc3ed4a997d5e5afda08d0606395fad4f86251d6d937d97fd5e455a8da8",
            results["behavior:rr:side-scheme.2:published-result"].Digest);
        Assert.Equal(
            "be40938c050e970910307f3563ad59453381899280587bb12e904c8865af56c7",
            results["behavior:rr:villain-defeat:published-result"].Digest);
        Assert.Equal(
            "9cc5b86fe4515ed10962285b02a812c97110b09be6de8f27b013563fa1b898d5",
            results["behavior:rr:winning-the-game:published-result"].Digest);
    }

    [Fact]
    public void WhenDefeatedBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/when-defeated.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(4, results.Count);
        Assert.Equal(
            "50db94cca8ba3f53f5a0f8335d5129932351b358972e815e59dfc17879401a71",
            results["behavior:rr:when-defeated-abilities.2.1:published-result"].Digest);
        Assert.Equal(
            "cc9b0c5ed4114e87fa527bf10e21a0168a001c9ba37840dcb0d859c1737c7e11",
            results["behavior:rr:damage.step.6:published-result"].Digest);
        Assert.Equal(
            "14afde0ec017bb9bba8551df18711a2697311d93db3886c32c98fe21e200cc8e",
            results["behavior:rr:damage.step.7:published-result"].Digest);
        Assert.Equal(
            "315a8c560b9a88e6d0ca2ad9876e5374c45842428880cdbcaa104015b280ed4f",
            results["behavior:rr:damage.step.9:published-result"].Digest);
    }

    [Fact]
    public void VillainPhaseBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/villain-phase.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(18, results.Count);
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
            "885c3b3e7d2c026365646e9da2b7b9477f460466ef8af776f5a8528745d0b88d",
            results["behavior:rr:boost-boost-icon.1:published-result"].Digest);
        Assert.Equal(
            "68ede43f99981dfc77fb4e887a38158c28cc95f0453bee33a638677e52c0f0d7",
            results["behavior:rr:boost-boost-icon.4:published-result"].Digest);
        Assert.Equal(
            "320436d6f906243eea9bf7212d42055747e3c3494ed302dc910460131545ae6d",
            results["behavior:card:01178:if-villain-is-making-undefended-attack-place-condition-not-met"].Digest);
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
            "b2d9cce19ff830d1c199e194dcf16790864a98ea3df7d4dfafcea80983fd81ad",
            results["behavior:card:01099:when-rhino-attacks-attack-gains-overkill"].Digest);
        Assert.Equal(
            "19ae349185692d4d06243e8ad5b6cb272e9eae4a699a5d993d9f563aef081212",
            results["behavior:rr:defend-defense.3:published-result"].Digest);
        Assert.Equal(
            "f9e1834f6d3f5862521bd902e7d8622353cacd79fd02b503547368943b9470eb",
            results["behavior:rr:activation.2:minion-attacks-hero"].Digest);
        Assert.Equal(
            "373a28f2528b37a3b32362940138c590f6d46fd14585b5dad1f3302fcdcf39d8",
            results["behavior:rr:activation.2:minion-schemes-against-alter-ego"].Digest);
        Assert.Equal(
            "080a2c1a8dfdcd99c12aeafaa40777a8d3c064a11753e890d26105140ed413c2",
            results["behavior:card:01003:when-you-would-take-any-amount-damage"].Digest);
        Assert.Equal(
            "591130ae9ca5d2bfa29ba64d56660cb89e096ef30c24e2ed669589f3e17fa2dd",
            results["behavior:card:01082:after-your-hero-defends-discard-indomitable-ready"].Digest);
        Assert.Equal(
            "30faaeaee0de61082e94349c75e92cd1594664a966961e643556694a9703b288",
            results["behavior:card:01077:after-your-hero-defends-against-enemy-attack"].Digest);
        Assert.Equal(
            "dfa94764f0b3bcd3d3a6bfd48b16cb1e9d29b5543990980a9e37403069015280",
            results["behavior:card:01004:when-treachery-card-is-revealed-from-encounter"].Digest);
    }

    [Fact]
    public void StatusCardBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/status-cards.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(9, results.Count);
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
            "6fc1a413ed3f43e85a910bc31d784d81089286b2ee8dfb81baecf09b90a396e9",
            results["behavior:rr:confuse-confused.2:published-result"].Digest);
        Assert.Equal(
            "2101b8ad401a6a79db1f7aa993da745e667782a87ba63310ece0c412507281c2",
            results["behavior:rr:confuse-confused.6:published-result"].Digest);
        Assert.Equal(
            "5fa890a7f260b7839348e83cd891ec85e0b800b4a5f14ef66c2e19b53ec46e28",
            results["behavior:rr:stun-stunned.6:published-result"].Digest);
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
    public void SetupBranchesHavePinnedOutcomes()
    {
        var results = Corpus.Value
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/setup.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(7, results.Count);
        Assert.Equal(
            "e72ed15ec31a9a7a09fc27204c124b81b9721e2591a43d75bed51ecd6e2f49d5",
            results["behavior:rr:appendix-ii-setup.step.1:published-result"].Digest);
        Assert.Equal(
            "8bed7c3f7cf64b3ffd42e60c6f02748eacba87495d945cc3676585b1eecd8a11",
            results["behavior:rr:modes-of-play.2:published-result"].Digest);
        Assert.Equal(
            "d932db79c8e0e61e965876db8983433706c3eb48a5b47bb57d2ff7620683e4a6",
            results["behavior:rr:modular-encounter-set.1:published-result"].Digest);
        Assert.Equal(
            "9c79668a61e30ba2e01dc4bcd26fd1082dff9e538342676341e0d9a166f3a68f",
            results["behavior:rr:appendix-ii-setup.step.15:published-result"].Digest);
        Assert.Equal(
            "063ebf9a425ae67894205a48cd6ac1444920d7f60a45925bdd2a533f28fc0aa6",
            results["behavior:card:01040b:search-your-deck-for-black-panther-upgrade"].Digest);
        Assert.Equal(
            "12469af423d842a404f38f0fa679e05f1686d269e7f30d2b820c184a4381e1e0",
            results["behavior:card:01116a:search-encounter-deck-for-defense-network-side"].Digest);
        Assert.Equal(
            "bd60d3534d573e413df7d3efa055d1b24035584a2707f3cee4af4f4811fa89a8",
            results["behavior:card:01137a:put-ultron-drones-environment-into-play"].Digest);
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
        var results = Corpus.Value
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/form-change.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(6, results.Count);
        Assert.Equal(
            "8ac76760febf2740557277dc71b65fbc299b5cef08e0a510bbb53a2c06a33a26",
            results["behavior:rr:form-change-form.1:flip-identity"].Digest);
        Assert.Equal(
            "7cd5c816aad3d3414b2a168c18ef54b42152f75e6301f10b17bccde0f43a243a",
            results["behavior:rr:form-change-form.1:voluntary-window-and-limit"].Digest);
        Assert.Equal(
            "410e4fc088da5f7bc65834761ff21efefe7c57bf7489ce0c669cabd7c2b76afa",
            results["behavior:rr:form-change-form.3:published-result"].Digest);
        Assert.Equal(
            "02934a0a9019cfb3b71e148a7f881d388edef755b04a4d61316e198fde976380",
            results["behavior:rr:form-change-form.4:published-result"].Digest);
        Assert.Equal(
            "b390e662689f7681c012ecd22494895156d1edf3446440841689575a2dee2662",
            results["behavior:rr:form-change-form.5:published-result"].Digest);
        Assert.Equal(
            "ce0ee3d8abd53d15fe6a673e15d7e2376a0455f5ca4a37db142c51153557a92e",
            results["behavior:rr:form-change-form.7:published-result"].Digest);
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
            "6c82c1944e5b28e57e0ee86754575d4a25672c96571153d058d77d981623fe2e",
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
    public void PlayerPhaseTurnOrderHasPinnedOutcome()
    {
        TranscriptResult result = Assert.Single(Corpus.Value, result =>
            result.Scenario.StartsWith(
                "specs/behavior/core/player-phase.feature::",
                StringComparison.Ordinal));

        Assert.Equal("behavior:rr:player-phase:published-result", result.Obligation);
        Assert.Equal(
            "6494d174d4898115b16d7ec8c75144afb6e359c98048422f56642012507e2d20",
            result.Digest);
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
