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
        Assert.Equal("365abc298353875d0c3b56a0d5b5b293b25857c1099e62891371c006d05425b0", results["behavior:rr:ownership-and-control.2.1:published-result"].Digest);
        Assert.Equal("9db026a4974fbc372c2df9a7b39ad84022f24d0dfddd27cd27d4434fc1a633f4", results["behavior:rr:cost.7:published-result"].Digest);
        Assert.Equal("64946656fe17bdd3fb7e73be1838f726625e680462c9597f154bddb3c0d2078d", results["behavior:rr:ownership-and-control.8:published-result"].Digest);
    }

    [Fact]
    public void ThreatPreventionBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/threat-prevention.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(2, results.Count);
        Assert.Equal("016db2b0d30fe965200ad077bc5a532526c111ae6496d3ba479ee316f4060bab", results["behavior:rr:prevent.2:published-result"].Digest);
        Assert.Equal("67390115e61df9115000131c5b0677e9215364489b86f98f19cc5fcbf91dbcd0", results["behavior:rr:you-your.2:published-result"].Digest);
    }

    [Fact]
    public void TimingWindowBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/timing-windows.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(7, results.Count);
        Assert.Equal("c649cda7de52fb9cba1ac5036abe45864731382d9fca4047f03e909e39de3768", results["behavior:rr:interrupt.2:published-result"].Digest);
        Assert.Equal("84faf577417af9b2c1b19359df806f8050896d3765dae35bf04ee958988c63d4", results["behavior:rr:interrupt.4:published-result"].Digest);
        Assert.Equal("47fb07147bad845b21c5e927b6f242071e01c03f0d884476dda93b8f137b3638", results["behavior:rr:interrupt.5:published-result"].Digest);
        Assert.Equal("d4013c1aad25506eecfb65150a96f3444d5db61687203275503b7a774e59c89a", results["behavior:rr:response.2:published-result"].Digest);
        Assert.Equal("2c7cea3ec7e5d4411017d4661740115bdcb98bf2b7e494597d53ca759da347a5", results["behavior:rr:triggering-condition.1:published-result"].Digest);
        Assert.Equal("36ae322edd9b3c117cbe111e860d2d90fd6a9eb6c278e182fa047e96d66faa90", results["behavior:rr:response.4:published-result"].Digest);
    }

    [Fact]
    public void AllyLimitHasPinnedOutcome()
    {
        TranscriptResult result = Assert.Single(CoreTranscriptCorpus.All, result => result.Scenario.StartsWith("specs/behavior/core/ally-limit.feature::", StringComparison.Ordinal));
        Assert.Equal("behavior:rr:ally-limit:published-result", result.Obligation);
        Assert.Equal("34d063e64ebcd10287afc9d407a5885b1aa074f69e035b28c9aae686bb293200", result.Digest);
    }

    [Fact]
    public void AttachmentBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/attachments.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(7, results.Count);
        Assert.Equal("16f19eb6412255b3f080249a29d074caaf470eca0c3931fb63c19524bf49734a", results["behavior:card:01009:attach-enemy"].Digest);
        Assert.Equal("90d904d2bf8c03efffedc03a7525b57c0eb6752bc68e0b78649e38a170a0f351", results["behavior:rr:attach-to:published-result"].Digest);
        Assert.Equal("c093c5e23a444420dbc47e47d62aab7c299bc0247495b09f55dd376fa874f43a", results["behavior:rr:attach-to.1:published-result"].Digest);
        Assert.Equal("ab17e8994d2eb202c5ab4495c6651c416f6c8c6af6d91c543ff4f25d942d5c6e", results["behavior:rr:attach-to.3:published-result"].Digest);
        Assert.Equal("46dbcd2dc686c04c86cf2f6dd12d352bafdde0c883effd14173c1ef91592bd7c", results["behavior:rr:max-maximum.4:published-result"].Digest);
        Assert.Equal("c4ca0e1e381324de0982cdb0d3d2f328ea98377296cf5e1e76ed9596972e0f6a", results["behavior:card:01163:attach-minion-with-highest-printed-hit-points"].Digest);
        Assert.Equal("cd1015d62f809cc177b110254c935b5e23d05173ea35e4540d9946d42e38f279", results["behavior:card:01163:if-there-are-no-minions-in-play-condition-met"].Digest);
    }

    [Fact]
    public void CharacteristicBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/characteristics.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("308d17f2a535691da7126d9e7c72e984ec4793f6e659573cd6abc2428b262a32", results["behavior:card:01039:you-get-1-hit-point"].Digest);
        Assert.Equal("08aef96e7e99fd4c214571156223ccc875d9779eccfec2a4eafa2112cb9cf768", results["behavior:rr:attachment.1:published-result"].Digest);
        Assert.Equal("4bb4f07e0806b6358d253c219514631d353a998967f90976cfca5e077b9ee1ef", results["behavior:card:01039:exhaust-rocket-boots-and-spend-mental-resource"].Digest);
        Assert.Equal("514e567a1dd67c346dfce4e27a1317ab5d1798dedf7cc7721a55b1d86b47a8ce", results["behavior:rr:modifiers.6.1:published-result"].Digest);
        Assert.Equal("9e9541217fe9b41f37bcf80e567e5c3388a4073b2892c12f2b3742f69ce596e2", results["behavior:rr:lasting-effects.4:published-result"].Digest);
    }

    [Fact]
    public void PlayAreaBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/play-areas.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Single(results);
        Assert.Equal("2b79d2c7d2d97f20866869d511700f5aaa330213855770906beaf50987029112", results["behavior:rr:play-area.1:published-result"].Digest);
    }

    [Fact]
    public void MainSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/main-scheme.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(4, results.Count);
        Assert.Equal("fe3d784d3024af1bebe9876cdda2967a919734c13c6c294b282413ee40cf0f2b", results["behavior:rr:main-scheme-main-scheme-deck.2:published-result"].Digest);
        Assert.Equal("4d333e280e1b614830f90de65c020694b9675c17538bdc1f5fc0379c6f495f9e", results["behavior:rr:main-scheme-main-scheme-deck.2.1:published-result"].Digest);
        Assert.Equal("1caaaaaadb8e72303b96593a07706e009fb2aa39938276ee3b89f675887672a4", results["behavior:rr:main-scheme-main-scheme-deck.6:published-result"].Digest);
        Assert.Equal("f309a320c0e58a30b73848712b5f473a7716c4e6bce0d66a679c54e9c38e9700", results["behavior:card:01117b:if-stage-is-completed-players-lose-game-condition-met"].Digest);
    }

    [Fact]
    public void EncounterIconBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/encounter-icons.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("a3a7a0d153367ad52d090c8ce025100c33e0a5a42794856f299c8a81e2938da0", results["behavior:rr:acceleration-icon.1:published-result"].Digest);
        Assert.Equal("15c6fe967a3619ec88303efa7a464c8dc668d158f6cc6d479a4d81df93f6ce73", results["behavior:rr:acceleration-icon.2:published-result"].Digest);
        Assert.Equal("6b1c2be8795323f55ffbb02f05a1c66d275b99990a50174be5f688b8de57c89e", results["behavior:rr:acceleration-icon.3:published-result"].Digest);
        Assert.Equal("15c2d2d29f9f5643470c5a9a4dcfe0eb63247bdad6ad6dba337ce595d57e2809", results["behavior:rr:crisis-icon.1:published-result"].Digest);
        Assert.Equal("147ecd1adcad4f238cdfe9b086bfd7d95e3597290be7bf4c67c8b01fed705161", results["behavior:rr:hazard-icon:published-result"].Digest);
    }

    [Fact]
    public void EncounterRevealBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/encounter-reveal.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(10, results.Count);
        Assert.Equal("883aa122f220523b8294d95e287dab6b187cc8186cd41902cecf7f6c1c13fc7b", results["behavior:rr:reveal.5:published-result"].Digest);
        Assert.Equal("889b83f88c9b0143c5e62f681399a090c3819db580a1ccfa90003a0acbdd1312", results["behavior:rr:treachery.1:published-result"].Digest);
        Assert.Equal("83443b174fbf060b1ce070bd646bfe548eb88f6ceb053f3a52420809017d7122", results["behavior:card:01104:if-no-damage-was-healed-way-card-condition-met"].Digest);
        Assert.Equal("efa5bdc07f11feff3abf0bd6acb6d1f24fbd8c1eb1479415fab744f53dfc8358", results["behavior:rr:reveal.8:published-result"].Digest);
        Assert.Equal("5c074df691681fa4851efd854fe108fbd54aec9eeff281c58ceffadf81df7243", results["behavior:card:01106:rhino-attacks-you"].Digest);
        Assert.Equal("3828314ccfd329dbd7431370814f1ef16eb8b3e7ca5e9d2e787cfd78f71157ef", results["behavior:card:01149:each-player-discards-top-3-cards-their-one-player"].Digest);
        Assert.Equal("a5d3026622726f66a3bd95e9f9c5c96e5284ab0040e3a0f97dd38fb04152c3d5", results["behavior:card:01149:each-player-discards-top-3-cards-their-multiple-players"].Digest);
    }

    [Fact]
    public void CoreIdentityObligationBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/legal-work.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(10, results.Count);
        Assert.Equal("20f17ea176ff30fd1bac1644cd31e246c3ff7d09a9cb34fe3e8d82c5a986daa2", results["behavior:card:01160:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("d168ecb21fca45038e9c73c7ea777ea8883f4fd255a955c8f3305e523b922b99", results["behavior:card:01160:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("c9126b38bb9762a6d6acdea13961c851346d2698f6ac5b1e06d56414ad211682", results["behavior:card:01155:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("d5dae81ffd752d365fd4281c116a095a240697f1b5ebdd746fb743d8b3ead8ef", results["behavior:card:01155:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("4766115432026227a3fa5100cad84622b75e14b11f01231530b4d14eb615a2ee", results["behavior:card:01165:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("7885624e321129a854660c5524454d79fdd357a3e2cb3617f4d2c18e3fc17ada", results["behavior:card:01165:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("89eedfabefb2aafa9af3da0a8d287fba7e280fc5f640e8eab806e818250da01b", results["behavior:card:01170:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("e75b5ed978350b79667477a198b39e3a3d6e18a64df358d2c964c9e90b799d53", results["behavior:card:01170:you-may-flip-alter-ego-form-declined"].Digest);
        Assert.Equal("86a15ac782e65e09ad511fe956cc74c84d3c7d11d0f805fa20ae9e0f217df7ef", results["behavior:card:01175:you-may-flip-alter-ego-form-accepted"].Digest);
        Assert.Equal("113f564d5f3a5d78d6f5ab130a18bf6c6c92c3e0af72ef8452f9d6111638731e", results["behavior:card:01175:you-may-flip-alter-ego-form-declined"].Digest);
    }

    [Fact]
    public void BasicPowerBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/basic-powers.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("ff5230e199b961effa3d45a3b905fbfc36e29a026fdc1c844fea7a038f26958b", results["behavior:rr:attack-player-ability-type.1:published-result"].Digest);
        Assert.Equal("9042256275c6f4c9a08bd0fbb8cba7484dc4c0ed40d6326a4b99a914465547ec", results["behavior:rr:ally.2:published-result"].Digest);
        Assert.Equal("407fa224fa4bd79e3ce0d3dbff74880e51961e9d9c47af2ae263b3547f071889", results["behavior:rr:thwart.1:published-result"].Digest);
        Assert.Equal("619c60b1873d7f1e4f61a2ba2690a88d55ea45527a88959daa969c76d62a399e", results["behavior:rr:consequential-damage.1:published-result"].Digest);
        Assert.Equal("46fadd442327fc0f1a273505b125701dd96ea832415af00b13160d11c08cc59b", results["behavior:rr:recover-recovery:published-result"].Digest);
    }

    [Fact]
    public void BasicPowerRestrictionBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/basic-power-restrictions.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(3, results.Count);
        Assert.Equal("5b25215b781743f30815c5193c97d34f6a7cf1150d8edd430f8e2bda3bd21136", results["behavior:rr:guard:published-result"].Digest);
        Assert.Equal("cd78fb68764a9ba34753f5dc0c4f27b3378c651964e480928ee8edbf370cd4b1", results["behavior:rr:thwart.1.1:published-result"].Digest);
        Assert.Equal("ae73057b8328df46a5b843b4e2ecf52deffb301125b4a46247d5f5add328dfa1", results["behavior:rr:recover-recovery.1:published-result"].Digest);
    }
}
