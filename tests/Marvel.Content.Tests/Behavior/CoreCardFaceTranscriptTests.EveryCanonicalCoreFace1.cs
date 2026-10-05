using System.Text.Json;
using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreCardFaceTranscriptEveryCanonicalCoreFaceTests
{
    [Fact]
    public void EveryCanonicalCoreFaceHasAnExecutablePrintedFactTranscript()
    {
        using var cards = JsonDocument.Parse(File.ReadAllBytes(RepositoryPaths.Dataset("cards", "cards.json")));
        string[] expected = [..cards.RootElement.GetProperty("cards").EnumerateArray().Where(card => card.GetProperty("pack").GetString() == "core").Select(card => $"behavior:card:{card.GetProperty("card_id").GetString()}:printed-name").Order(StringComparer.Ordinal), ];
        var results = CoreTranscriptCorpus.All.Where(result => result.Scenario.StartsWith("specs/behavior/core/card-faces.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(209, results.Count);
        Assert.Equal(expected, results.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("e90636404d04a2481ae95576f43503632cf10ab20cde1ed49642d1f864f1c17c", results["behavior:card:01001a:printed-name"].Digest);
        Assert.Equal("6ec1b3ae36d19568d243e3ef6c762758ddde07cec0aa91515adfcb21ce30472d", results["behavior:card:01149:printed-name"].Digest);
    }

    [Fact]
    public void IdentityCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/identity-card-abilities.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("dca70c9dc9a7a16b07266603770e0fd52f259b6ae9a4eb5c339fd91cc127397e", results["behavior:card:01001b:generate-mental-resource"].Digest);
        Assert.Equal("d54db9d0830d17a0b91fc6907d7e1e95daf08498e899055a2a2e59e8b0875b8d", results["behavior:card:01010b:choose-player-draw-1-card"].Digest);
        Assert.Equal("bfa62095f3c85549b325bb4fca21b72d7c81388d76cf5f401c279cfca608048c", results["behavior:card:01029a:you-get-1-hand-size-for-each-zero"].Digest);
        Assert.Equal("96a6d4eeeff9326db04594d3270c1634b1dd40f704a69d7ace1832f5545e95e5", results["behavior:card:01029b:look-at-top-3-cards-your-deck"].Digest);
    }

    [Fact]
    public void PlayerCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/player-card-abilities.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(70, results.Count);
        Assert.Equal("b950f49f2e17185f00b7cd3673c90ee86dfe272d63d7229d3677b634c9adbc02", results["behavior:card:01058:after-daredevil-thwarts-deal-1-damage-enemy"].Digest);
        Assert.Equal("e3e7ed520ac9f23cce57154a02a8d9b4f68d4e160015d2f0d8906c19e0ba355d", results["behavior:card:01073:increase-your-ally-limit-by-1-limit-reached"].Digest);
        // Engine choice: the revealing area registers the treachery's zero threat
        // pool, which digest v2 retains after the cancelled card is discarded.
        Assert.Equal("c76f479f81c8dce99b2c726bb2367f86f8ef60d8b0131d0f932dfebcd3d36b94", results["behavior:card:01075:when-card-is-revealed-from-encounter-deck"].Digest);
        Assert.Equal("43f69fc41d1775e17e11f22385401ed23dc01bb3303210438866af75da6ae291", results["behavior:card:01078:when-treachery-card-is-revealed-from-encounter"].Digest);
        Assert.Equal("debf460d029990a36066323af246bda74c3e55700ea52964d0eb4816ffc5c471", results["behavior:card:01050:physical-deal-2-damage-enemy"].Digest);
        Assert.Equal("8ca916fee9fb648f3ff7fdea3ba686d6cf815d2393f91f6d4d6ce37a30371df1", results["behavior:card:01050:energy-deal-1-damage-each-character"].Digest);
        Assert.Equal("8227772ae4dbefca273c3003fbd50e2d30bfa9f901b1e47c8c031b9da497e9de", results["behavior:card:01050:mental-discard-hulk"].Digest);
        Assert.Equal("bef7a24132bf2a9dd1e0012ba42d4f32b88e23c5f555b15184e32682a08dd328", results["behavior:card:01050:wild-all-above"].Digest);
        Assert.Equal("ad1f73a6e2c43076d4fedd1f969d0d83aede10a76d09cfdaafceb3e8d62a073b", results["behavior:card:01007:attach-minion"].Digest);
        Assert.Equal("4e00fd3f4e10deb085e962c30fdf0ab087d584f17ceddea4fb0c94125e0921b7", results["behavior:card:01042:choose-up-3-different-cards-in-your-minimum"].Digest);
        Assert.Equal("b35e00aea1e1a54c07f329e5333bc86bcf0d829d4ed91b5deb6c0725def34c0e", results["behavior:card:01042:choose-up-3-different-cards-in-your-intermediate"].Digest);
        Assert.Equal("00d2820ab32f24c6279fb91681831a3bc9f4c49143e2be022e3609bbadd2f5aa", results["behavior:card:01042:choose-up-3-different-cards-in-your-maximum"].Digest);
        Assert.Equal("a4a6afa317a8746f7a819290f36279ae04bf720679e255cab22efd7874eff18a", results["behavior:card:01018:max-1-per-player"].Digest);
        Assert.Equal("9d743b51f45fe9801075368e6c8eb8223b6854807154174b30a290333e9e4b51", results["behavior:card:01055:double-number-resources-card-generates-while-paying"].Digest);
        Assert.Equal("f35610fcd958139c07271ab40e6c3342e8c37454ca38c88ee91fa3e229f9e0df", results["behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-not-met"].Digest);
        Assert.Equal("c415abf39c1d583f40816a279fd70ebe4cf60fbcd870446ac0732b7e1b4676fa", results["behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-met"].Digest);
        Assert.Equal("a591cd2dac6277f3b43ba8668ca687c865abcabe2df973e0a58ce7ffb7bb4d0d", results["behavior:card:01062:double-number-resources-card-generates-while-paying"].Digest);
        Assert.Equal("228abab4892111250ba5973967aa58504064adbeef77e21426364a89380ebe83", results["behavior:card:01063:max-1-per-player"].Digest);
        Assert.Equal("3feb974a36bc7fe15b2148c0ccc638932f23f3ba695560d8902c7107bbaca45f", results["behavior:card:01063:after-you-defeat-minion-exhaust-interrogation-room"].Digest);
        Assert.Equal("ecf6fd227c5cfedbbeb9bf05007f7b6f65f8b531fc5ee08c7271e7b36dbf1fbc", results["behavior:card:01067:after-maria-hill-enters-play-each-player-multiple-players"].Digest);
        Assert.Equal("db1ba1e142e1959a497a5a99a19fad6f9ae6fc6b3e0d8238cafe6a876580876c", results["behavior:card:01076:toughness"].Digest);
        Assert.Equal("7ead61dfa24e665211123cfc27dd8dfcf56851c0b4c84ab295afb80ff984feb8", results["behavior:card:01093:spend-physical-resource-and-discard-card-ready"].Digest);
        Assert.Equal("0f7ee742f3254d6e0020507fafc48d13df376a55148ef5dfa7fdd6fa04781fb7", results["behavior:card:01028:she-hulk-gets-2-atk"].Digest);
        Assert.Equal("c408ab6da44c777c7d7a0a0fc5ed8045f623c7906ed18fda5a80debac25a353b", results["behavior:card:01031:for-each-printed-energy-resource-discarded-way-zero"].Digest);
        Assert.Equal("4c02775f75e988598a740c9b56986e4b926215d1239aaf3374a9740cc5b54002", results["behavior:card:01031:for-each-printed-energy-resource-discarded-way-one"].Digest);
        Assert.Equal("21f562aa55e4cd25a77b196574526e8113c939a6c2003b49bc58a4febdc45cf7", results["behavior:card:01059:jessica-jones-gets-1-thw-for-each-zero"].Digest);
        Assert.Equal("1528e7ec155189c79fb03adf9cb685b9ec38f190578ecd895026040bbfcae586", results["behavior:card:01059:jessica-jones-gets-1-thw-for-each-one"].Digest);
        Assert.Equal("7cbc96bfef763aca6b978121312a942671af188807525975d32b153bd9eee6c3", results["behavior:card:01059:jessica-jones-gets-1-thw-for-each-multiple"].Digest);
        Assert.Equal("9b2a06d0749ef4fb8021dba2f184274beb7b3d58912528f37c006dbbcdef6eed", results["behavior:card:01065:play-under-any-player-s-control"].Digest);
        Assert.Equal("7d04a90a6491e87e9008b3dcb650cff46299d879d6a1c31348e5d1e5ab274a94", results["behavior:card:01065:max-1-per-player"].Digest);
        Assert.Equal("b34883bde44469c78a7e0cd80b433d5938b31ed8893c08b10a8959f45258f8f4", results["behavior:card:01081:play-under-any-player-s-control"].Digest);
        Assert.Equal("6540f6e5bdd9588d7a7203567cb822048a674fa6e0cea4c79ad3a3ccebc40bcb", results["behavior:card:01081:max-1-per-player"].Digest);
        Assert.Equal("b05f4cb672ca8a08a12b52725667f7063dc262b75fa140e965b5c20b705e0aef", results["behavior:card:01002:after-you-play-black-cat-discard-top"].Digest);
        Assert.Equal("ae04ebf4c964d0a5f3d036d018cc57a0d2d7205a0219c1d070b10770433acce1", results["behavior:card:01011:after-spider-woman-enters-play-confuse-villain"].Digest);
        Assert.Equal("03f95538e20a43b3c8ee8cff0f44744d6b912a2b6b127333d227065557b465c5", results["behavior:card:01019a:after-you-change-form-deal-2-damage"].Digest);
        Assert.Equal("0192d55f44ee0deb7bb2f3fbf7134e03208137abb79a032e41fef53ac5d1a3ca", results["behavior:card:01024:after-you-make-basic-attack-using-your"].Digest);
        Assert.Equal("ec677a02eaf5fdba4f1e97f11f061895fb0cce3e7c9a680c9dad3f4680ecbcb6", results["behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-not-met"].Digest);
        Assert.Equal("cf066f1d94682af042941dfbd8b874a237d77174aea000aa6df96813a701de09", results["behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-met"].Digest);
        Assert.Equal("27ce648d6d6b1b3fefd1020966c35a68933f902416ab8d4029f60b442dbc69f7", results["behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-not-met"].Digest);
        Assert.Equal("bb8ae1de9d5f44e683257b501be501517ade4657661841e99ccef10d620ec7f3", results["behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-met"].Digest);
        Assert.Equal("97549fa3c0ce6085647aeffdfbec34abbe3d9ffbf7dd8c885bbfde999f9da52d", results["behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-not-met"].Digest);
        Assert.Equal("8b204bd385ac59959ef4bdb71165092c0f66232be39c73c9a9af8b4ca7a78f17", results["behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-met"].Digest);
        Assert.Equal("e0d2ab2ffeb8edd62883a3637e420d9b8e8dbb4a895a0d3b436a32704e8c6c05", results["behavior:card:01084:after-entering-play-remove-two-threat"].Digest);
        Assert.Equal("00cfccd16622e7fd2b6ba154f10849ced92d04d0014549dd7150f72b84504e33", results["behavior:card:01084:after-entering-play-deal-four-damage"].Digest);
        Assert.Equal("4fcbe50728850dcab2263806c0fd2914f4938d00a8ffb125b94aadb2c5d70fb4", results["behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-not-met"].Digest);
        Assert.Equal("32921e75d9c57580862d95d0883d11ff3b7d399392621521ff600fcd840fe3ad", results["behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-met"].Digest);
        Assert.Equal("920cd851713e1cd2ea93d37844b0e1405c4587c69f01e4a3d7242f58b7cca211", results["behavior:card:01017:when-captain-marvel-would-take-damage-discard"].Digest);
        Assert.Equal("162f1a24c59a1eb8dbb0193e4f427000ff3c46f7c128da2eac48198cae6bd02a", results["behavior:card:01008:when-those-are-gone-discard-card"].Digest);
        Assert.Equal("05b16e69566597e2bc78959c71e70df538f93b58d5a76e4ff27b52c8be5b47ad", results["behavior:card:01083:after-mockingbird-enters-play-stun-enemy"].Digest);
        Assert.Equal("e6076b44a2df71a9a91e32d5553f942dda3e73a08946e7444bbc2dd5ac8e08c1", results["behavior:card:01068:choose-thw-plus-two-until-end-phase"].Digest);
        Assert.Equal("6597b8f2c5272568e3da6621434bec1d8b5baaac64f8f819db29e388594b3a8f", results["behavior:card:01068:choose-atk-plus-two-until-end-phase"].Digest);
        Assert.Equal("1e527885bcf4f89eda34ac349a599d44282ae2ef0f1d43a907329a19cc9a1335", results["behavior:card:01035:exhaust-arc-reactor-ready-iron-man"].Digest);
        Assert.Equal("c8771caae4cdd4c4e05481fd92718846f08f097634cf2cdc29221fadc0f45e16", results["behavior:card:01036:you-get-6-hit-points"].Digest);
        Assert.Equal("457bae58ebbb787226588ec0aafcde70e9d5563da513456c4e1ed184ad22b243", results["behavior:card:01045:exhaust-golden-city-draw-2-cards"].Digest);
        Assert.Equal("6c4d62c5fdedbb3f0d037b378e8b9bcdc5fd27e6b07480b1fb47d579218817e8", results["behavior:card:01069:ready-ally"].Digest);
        Assert.Equal("d70f62033bbd7bedda3fd0b30f519a7e2cf5b123b23b4fc92d9d290bd1e40f70", results["behavior:card:01086:heal-2-damage-from-any-character"].Digest);
        Assert.Equal("f9497b00f43f0fc0e4fc12ef06bfa56fb1ee55e377314267a99f575fddd7a233", results["behavior:card:01020:return-hellcat-your-hand"].Digest);
        Assert.Equal("07515a10451a337accf45c014c40267bad4641251aafaf8d139dda162a23cff4", results["behavior:card:01091:exhaust-avengers-mansion-choose-player"].Digest);
        Assert.Equal("40a776d0ca36cd8350b0141cc6211b240896099b53d22de2c94d06802f4053f1", results["behavior:card:01015:exhaust-alpha-flight-station-choose-and-discard-condition-met"].Digest);
        Assert.Equal("ae58dce148ad9dfb8c2dbc232822e409210a3f89d7eebc592210fbea4c666368", results["behavior:card:01026:exhaust-superhuman-law-division-and-spend-mental"].Digest);
        Assert.Equal("d03c4194a1aceee53f7b8d41128da4cd8402d84f16825247599e119bfa114b6d", results["behavior:card:01033:exhaust-pepper-potts-generate-resources-top-card"].Digest);
        Assert.Equal("7cdc6d04ea65a0794d96ea9e5458dd59f000c6f138ea08ef917b203f2b78808e", results["behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-accepted"].Digest);
        Assert.Equal("eacd501dfd0e07f86476fe6b23ec744d04a4fdbf0f0e1b5f5164975eec148e65", results["behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-declined"].Digest);
        Assert.Equal("4a5e33b7ef6fbf0d24c676e36fedf31697cae2f485a9593c063e33db9df879b2", results["behavior:card:01034:exhaust-stark-tower-choose-player"].Digest);
    }

    [Fact]
    public void RhinoCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/rhino-card-abilities.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(15, results.Count);
        Assert.Equal("c11bead744e33d0850108ef57da67c932a3197b333ad958b1c9da3589ffe05e4", results["behavior:card:01095:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("ca831e242d96039570f76ec65ab7e3970df6bddc2423c9ea7c5c06e8db4090ab", results["behavior:card:01097b:if-stage-is-completed-players-lose-game-condition-not-met"].Digest);
        Assert.Equal("d6f863ac2cdfa366216b7c7ac83aa84414018cead35246abdd7e67b0307f429b", results["behavior:card:01098:attach-rhino"].Digest);
        Assert.Equal("0ae1e75029b798a0c1d041e127174275457bc602e8c0ef346f345e071abb06ac", results["behavior:card:01098:then-if-there-is-at-least-5-condition-not-met"].Digest);
        Assert.Equal("6c6af2e19904248d36152af2d475495cdab15ebc12d768b82d16b19f9131878a", results["behavior:card:01100:attach-rhino"].Digest);
        Assert.Equal("4c57fa0a7b4834b9987a1211ae12effb5178c3a574d1bf21fc3813aa02a34e21", results["behavior:card:01103:deal-1-damage-each-hero"].Digest);
        Assert.Equal("1e89036f96fba2f39ee71163ff71f918b90e5046f8a7f85942792d6a108e88a7", results["behavior:card:01105:give-rhino-tough-status-card"].Digest);
        Assert.Equal("01eb380d4d79ea7430c178bb83f51bb133c434ca89a725707df1670e92b74364", results["behavior:card:01105:if-rhino-already-has-tough-status-card-condition-met"].Digest);
        Assert.Equal("4d4ca56196d60b5bba27831af9ab8fe8e5376b0450196d0ec4ec232ca61a7b57", results["behavior:card:01106:card-gains-surge"].Digest);
        Assert.Equal("30fcaba821aeb9de1fb554f794f96e137cf63b9309cb2334b063d98df5f804ba", results["behavior:card:01106:if-character-is-damaged-by-attack-that-condition-not-met"].Digest);
        Assert.Equal("84f0e89ab619a1525d1fe70e3be4887f2c02147738d78201a5a583bd5b7a2908", results["behavior:card:01110:when-revealed-take-two-damage"].Digest);
        Assert.Equal("bdd445b2890dff548a0d1d2b73a182f3b3feee9dcc8dc31dfc8753606f5a7e75", results["behavior:card:01110:when-revealed-place-one-threat"].Digest);
        Assert.Equal("f3a455bc6e122b8cfc7008ee3705399d9cc54233351a9b4f6e5f29fa29bf12bd", results["behavior:card:01112:if-you-are-already-confused-card-gains-condition-met"].Digest);
        Assert.Equal("740c2a93d131483bd5b0a976ad0fa04f0d99e72ccb22a7e0bfe55cbae4d011a9", results["behavior:card:01111:if-bomb-scare-is-in-play-assign-condition-met"].Digest);
        Assert.Equal("d46848c7e77fe7a61a8e5aaacf745e6857a3adeab67af2d38a2b8b13b52c9284", results["behavior:card:01111:if-bomb-scare-is-not-in-play-condition-met"].Digest);
    }
}
