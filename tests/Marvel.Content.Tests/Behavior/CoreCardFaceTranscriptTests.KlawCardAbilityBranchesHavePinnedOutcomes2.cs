using System.Text.Json;
using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreCardFaceTranscriptKlawCardAbilityBranchesHavePinnedOutcomesTests
{
    [Fact]
    public void KlawCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/klaw-card-abilities.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(33, results.Count);
        Assert.Equal("567fe0ce17c160f45fb88f771cb08906742f7df9ff0198d934e7e663f9a315d4", results["behavior:card:01114:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("4c43ee9d92bc96561912d68918ae03b6e7ea64e78e44d45ab393ec40d5bfea85", results["behavior:card:01114:when-klaw-attacks-give-him-1-additional"].Digest);
        Assert.Equal("789c7a01fdd70ef7296a980a5125acf1a970a03ca722d0d81c50f9d6a39d1187", results["behavior:card:01115:toughness"].Digest);
        Assert.Equal("69dd1be8c9665308ce1a715c8483afcbcdc081dd610622061b599eb65902d392", results["behavior:card:01115:when-klaw-attacks-give-him-1-additional"].Digest);
        Assert.Equal("8c3c17dbf19992140e4febb0a15b2200d90a581cf9f2d2d074361cb08b7a7a5a", results["behavior:card:01118:attach-klaw"].Digest);
        Assert.Equal("42043848794610be70ba9567ccf25bb592716c81240687ae997159e5dbb3e643", results["behavior:card:01119:attach-klaw"].Digest);
        Assert.Equal("d2b69f426e8cbdf3af1d5ebd10301c94012d8100ff59e23c089fa3f252ef64e2", results["behavior:card:01125:place-additional-1-per-hero-threat-here"].Digest);
        Assert.Equal("61aea5c7b026d99e1cda0ca59b86b4afc59673949df5cba06955e5d95a018ece", results["behavior:card:01126:place-additional-1-per-hero-threat-here"].Digest);
        Assert.Equal("fbd0903aba7c90fb861d58eb9aba1a4db75d78642068a962cfe1b08b826c7a1b", results["behavior:card:01127:klaw-gets-10-hit-points"].Digest);
        Assert.Equal("a09e56b384aa8bef396b164ab78c2c71a01a6d6decc6618bbf3cad860ff4d85f", results["behavior:card:01122:discard-1-card-at-random-from-your"].Digest);
        Assert.Equal("963abefbf0467e835382c71c0bc7e17fdef91506ce221324accb77f0b94072ae", results["behavior:card:01122:klaw-attacks-you"].Digest);
        Assert.Equal("d4f84e451b9cc7e6f1b3572cc66f91c465214b99f63984e9b64710c3d71d7a87", results["behavior:card:01122:if-attack-deals-damage-place-1-threat-condition-not-met"].Digest);
        Assert.Equal("c71e99bdb3a7b07d7adfa336cef5573088d27227f5c03604975a265313491b76", results["behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-1"].Digest);
        Assert.Equal("e3551bb1f0a0272db094495c274f2038a650384ef55550884b62220655bc8827", results["behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-2"].Digest);
        Assert.Equal("6994fbf4479552f4bd62ed62ceeed8fa093435ed34b840c4adbee1c8e58cb9ae", results["behavior:card:01124:klaw-heals-4-damage"].Digest);
        Assert.Equal("81c0c00a6b3ab32138277547beeed614ec15eec160f586c37d1d10ade956056d", results["behavior:card:01124:if-no-damage-was-healed-way-card-condition-met"].Digest);
        Assert.Equal("172fb9f546b7a599200fc3105b33ea7dab9f7c6186fd1ef785f38ebefe4011a6", results["behavior:card:01124:take-2-damage"].Digest);
        Assert.Equal("78a070c9004ddc6ec55d370f87bc1b339906da7b543abb9e08fe8900fa3309d1", results["behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-met"].Digest);
        Assert.Equal("09c20cd3bf56d73e247f44dd8c71a95e992aff041aa31c53ebdf654b25023f7f", results["behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-not-met"].Digest);
        Assert.Equal("1fd61cf28cba4cfa66dffb890d80a1444c3fa363f1c031dfe51225653c1d002c", results["behavior:card:01128:discard-cards-from-encounter-deck-until-masters"].Digest);
        Assert.Equal("bf945ac27dc3d0ef1a85b681ce8cd8efd42ec5966b10c3e0c7e3a5104d58619f", results["behavior:card:01129:after-radioactive-man-attacks-you-discard-1"].Digest);
        Assert.Equal("cdbbbed12a773be56b828696e837ed6b9aa9f07466299873f97725635d0ff6df", results["behavior:card:01129:discard-1-card-at-random-from-your"].Digest);
        Assert.Equal("c12a63b8ad7e956ee5a9c38f6703eaa49517c6e8a5387e79a3f7c19a007497c9", results["behavior:card:01130:when-whirlwind-attacks-you-also-resolve-his"].Digest);
        Assert.Equal("194404d95f8a2f29f034c76be4bcb9c42c3e2fc1d38383251b26c14b623435c5", results["behavior:card:01130:deal-1-damage-each-hero"].Digest);
        Assert.Equal("f86b464f7bfa2e1ea763d7f8284a4c62474c99db0c5850a6cbf135d7a7e0ac0f", results["behavior:card:01131:after-tiger-shark-attacks-give-him-tough"].Digest);
        Assert.Equal("d2051b0874c6a826a09aceb255b52db1a2d52ab87aedf8f7ca296b89ba1ca149", results["behavior:card:01131:give-villain-tough-status-card"].Digest);
        Assert.Equal("9c895feaddb3a7d942b9ee55de7c220015cf5473852b530e4926ea5f2655b711", results["behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-met"].Digest);
        Assert.Equal("b340e9a620e3b307fa78344478b855892237644cbd74961d578fc1b6d00dc018", results["behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-not-met"].Digest);
        Assert.Equal("59cded7bdd92b526ebe7ae7e813044048f046482300b08b392c0d548c49f1f4b", results["behavior:card:01132:exhaust-each-ally-you-control"].Digest);
        Assert.Equal("73c4eaee260a9705c760d037f18e7aeb557e0c266be9ba207556386ae129a564", results["behavior:card:01133:each-masters-evil-minion-attacks-hero-it"].Digest);
        Assert.Equal("1bd22e988f8d728a5bab881fcf4507bca73f59cab26094304197db0c84f9470b", results["behavior:card:01133:if-no-attacks-were-made-way-search-condition-met"].Digest);
    }

    [Fact]
    public void UltronAttachmentBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-attachments.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(8, results.Count);
        Assert.Equal("5d1761c21896a73e60dde08383e2cbfa4099e36d0285e829629518d020ff0d30", results["behavior:card:01141:attach-ultron"].Digest);
        Assert.Equal("59f065ada6d01be38638a0584865ad5f99f1ba91b2b76ea1850c50bae67ec3c2", results["behavior:card:01141:after-ultron-schemes-place-1-threat-on"].Digest);
        Assert.Equal("68f52c4218f3da9292a4b5a030482ef01d1e00778b723a90e4911e34d36e0cbf", results["behavior:card:01142:attach-ultron-drones-environment"].Digest);
        Assert.Equal("044d40243b4b95ebd34cd012869c544056c76f215749fcb5e0c838bd3caab8ea", results["behavior:card:01142:each-facedown-drone-minion-gets-1-atk"].Digest);
        Assert.Equal("b95aef5caea8966398ef2d14e52ce2e003d46f291362c35407aa4337110ff603", results["behavior:card:01152:attach-villain"].Digest);
        Assert.Equal("1f4807fd826972cdadc918611aa5a2209e8e3ebf5e9ec91a0ffaae47ae59c22d", results["behavior:card:01152:exhaust-your-hero-and-spend-physical-physical"].Digest);
        Assert.Equal("36e03e9bd73ccf292ce570b7e4737b579d1ddd956541c771d7ac0fd9d031cd14", results["behavior:card:01153:attach-villain"].Digest);
        Assert.Equal("0479be3b7eb90361c2c3fede649c6cb50ac366ba9be20a1a5604eb3cde0aafa0", results["behavior:card:01153:exhaust-your-hero-and-spend-energy-energy"].Digest);
    }

    [Fact]
    public void UltronMainSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-main-schemes.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(11, results.Count);
        Assert.Equal("a3117b3431002ddcf3972413b5f81db4ea59b11784176aa7d9b9ffcb3791c516", results["behavior:card:01137b:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("6a32e8bab7d212cf888500159a5a8c664769f5bcddf98504e5a69f35dd1180cf", results["behavior:card:01138a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("9ee50759c7d1d88debf0f63a7608d64d6f21ef9c32f0a94665cba326b711dd72", results["behavior:card:01138a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("ee96f3c44bd76457ef8a4317afa441bdd83589a1daa091a1cb3deba1c5cc4f9f", results["behavior:card:01138b:after-placing-threat-here-during-step-one-choice-1"].Digest);
        Assert.Equal("2a32f1144b2c0a80cd4b85f71c242af825091a94977308297661d7b2b8c0198c", results["behavior:card:01138b:after-placing-threat-here-during-step-one-choice-2"].Digest);
        Assert.Equal("a9c18cede3718d730fdaeb9f52f85ee68b4f833d7e167b30f0857d890870e542", results["behavior:card:01138b:after-placing-threat-here-during-step-one-multiple-players"].Digest);
        Assert.Equal("01411f0533834523e124aae58d27b58bd38553909e7c92d19159207f2d96e393", results["behavior:card:01139a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("8969527902bf66572680184e1bc886e26924e4522110e1d4eb3f701b29c95de3", results["behavior:card:01139a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("ddc99345e7d633206d0834ea7deb327f544719183c7301259f11618daf471b64", results["behavior:card:01139b:threat-cannot-be-removed-from-scheme"].Digest);
        Assert.Equal("87301419ca6e93f9aea22f9480ca713700eb6b0a35fe67b62f1f213489efed7f", results["behavior:card:01139b:if-stage-is-completed-players-lose-game-condition-met"].Digest);
    }

    [Fact]
    public void UltronSideSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-side-schemes.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(6, results.Count);
        Assert.Equal("227d06094be6685abf4470115a01011817cd7edb61719d5f4e03273435bd4493", results["behavior:card:01148:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("e3ca19f84f2e43adafc9cacab83594ac6c647fbabe420f483ce49650d93da5b7", results["behavior:card:01148:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("93b908e8cbeb7c291224596fcd218ffc031cdac2fcd7f36bab0b4544d02b670e", results["behavior:card:01150:first-player-puts-top-2-cards-their"].Digest);
        Assert.Equal("28deee7f72694e445bd08e4c5bb4b0f7c1dadfb6f65ef7f9dc614d84ffe8e49f", results["behavior:card:01151:each-player-chooses-either-place-2-threat-one-player"].Digest);
        Assert.Equal("49891bd6018bd92bad6cb2cb0f96e9adeb85bcdb1d150dc8ee68f89efa0f6c43", results["behavior:card:01151:each-player-chooses-either-place-2-threat-multiple-players"].Digest);
    }

    [Fact]
    public void UltronVillainAndDroneBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-villain-and-drones.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(9, results.Count);
        Assert.Equal("3c42283a9de357c7c13094ff45ddae7b3c4ebdd2e211e22677c2a1e008c151cf", results["behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-1"].Digest);
        Assert.Equal("572ffda1e456533543c4d76f30acbbe032db171f21997645c2db8bea6a6a2772", results["behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-2"].Digest);
        Assert.Equal("b86a928efaa6aac6feddb578aa6f6eec4bc32c6c9aa7ef121ca83a23bef1643d", results["behavior:card:01135:when-ultron-attacks-you-put-top-card"].Digest);
        Assert.Equal("5987730f37ba278d2f93e11cb948892f1e4c460abcc14a7d30e2430f4ab5cd31", results["behavior:card:01135:until-end-his-attack-ultron-gets-1-multiple"].Digest);
        Assert.Equal("b59623086575aafb18c39190668f3b88cec0c3b07ac07b398930f2762f19a1fe", results["behavior:card:01136:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("9e00816b9f9ddf04e122c857d43d9b164c7a38e7bbe7988c0e7a7c32af2a6979", results["behavior:card:01140:each-facedown-drone-minion-engaged-with-player"].Digest);
        Assert.Equal("3d9aaa27a4715bf5a9874d36f4e4f6edc299211e42ff01c5f4dff142bfb3043b", results["behavior:card:01143:guard"].Digest);
    }

    [Fact]
    public void UltronTreacheryBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-treacheries.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(16, results.Count);
        Assert.Equal("914f51b14c046ff01d9861a63cbef0842ade2e7cc917e3b3fdd630b98f4b62f6", results["behavior:card:01145:ultron-schemes"].Digest);
        Assert.Equal("13bb52686d6a9b7b345461271732c04e5431801a321ab8cd8059590f809071ff", results["behavior:card:01145:discard-top-card-your-deck-for-each-zero"].Digest);
        Assert.Equal("feda7a3cbca336d749d3c2537d6f129c3b2c77507c7c8856a925da27301c9fde", results["behavior:card:01145:discard-top-card-your-deck-for-each-multiple"].Digest);
        Assert.Equal("a689fcef7ef4d23910430d1909271ae8db04181f5f439c7d36ee71104ce7ba89", results["behavior:card:01145:ultron-attacks-you"].Digest);
        Assert.Equal("6f63a3605d61268734f43b424303cb557f8a3f6770e5709ace019c5d24cd8b2e", results["behavior:card:01145:discard-top-card-your-deck-for-each-one-2"].Digest);
        Assert.Equal("c65bee14f5b445ee52e6f6bd0c75c162e1badc86f701ecd7264594345f4d61f4", results["behavior:card:01145:discard-top-card-your-deck-for-each-multiple-2"].Digest);
        Assert.Equal("bf02a7089a56a3b2bf3d3fa0a0b06dea3a9cb946e4007ed0f36238bd96cbbb8b", results["behavior:card:01146:ultron-heals-2-damage-for-each-drone-zero"].Digest);
        Assert.Equal("0e494f4c0acab4880f7b76b7e571d1e145e7ed97853a82f708bc2fb25f6a7ada", results["behavior:card:01146:ultron-heals-2-damage-for-each-drone-one"].Digest);
        Assert.Equal("b9e11dced502aa33363450767a95ea13b5789c019996796bf29214cea15e0874", results["behavior:card:01146:ultron-heals-2-damage-for-each-drone-multiple"].Digest);
        Assert.Equal("0ec7759298f8ea52b3b90f63f5891d6262e1df49c044eefbe377695866c045cd", results["behavior:card:01146:ultron-heals-1-damage-for-each-drone-zero"].Digest);
        Assert.Equal("77a944bb1081d7daf2caac38080b705873f7bd8dff738685cf2ebc674730740e", results["behavior:card:01146:ultron-heals-1-damage-for-each-drone-one"].Digest);
        Assert.Equal("bda52293055150b3fd1f77b8952b68bd5cf7026efaa9a144e2377fb93b906234", results["behavior:card:01146:ultron-heals-1-damage-for-each-drone-multiple"].Digest);
        Assert.Equal("f2be67f1ca4f44d1621c7eeae253d3e65714fccce5c8c2a28d24ba2969c9060a", results["behavior:card:01147:each-drone-minion-engaged-with-your-hero"].Digest);
        Assert.Equal("1fcb7872775bcc15e87c831a4d5eedec51c2f5df162dc0a573e7ebff868f2411", results["behavior:card:01147:if-no-attack-was-made-way-put-condition-met"].Digest);
    }
}
