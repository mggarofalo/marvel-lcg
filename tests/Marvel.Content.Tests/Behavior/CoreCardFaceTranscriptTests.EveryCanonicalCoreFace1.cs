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
        Assert.Equal("f166b2a393eb1cbf8a63bec8ca05525a2406fee737e5db370c893bd39ffe0fe7", results["behavior:card:01001a:printed-name"].Digest);
        Assert.Equal("16883a0c4b1d4529fe7959615147f48dc5625d4c5d59d69a41c2742bfb0947ea", results["behavior:card:01149:printed-name"].Digest);
    }

    [Fact]
    public void IdentityCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/identity-card-abilities.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("d46de12a5223add3f2203e2e9b83ba7a0c008aa0af6230de63d490b369facb1a", results["behavior:card:01001b:generate-mental-resource"].Digest);
        Assert.Equal("26a035785a16fd0bdc8b1abbd1df7dd3ddca7b4cf99077f8c33782d7622d794d", results["behavior:card:01010b:choose-player-draw-1-card"].Digest);
        Assert.Equal("04a5fcb2bac9b14f3852d2bd68aad8c5ed6febbd4e0c999ea5dd2a273b8bacf3", results["behavior:card:01029a:you-get-1-hand-size-for-each-zero"].Digest);
        Assert.Equal("ca8f9d039bcb84eead345000521a8f946bb8b36e556f009168ea29f9099b19dd", results["behavior:card:01029b:look-at-top-3-cards-your-deck"].Digest);
    }

    [Fact]
    public void PlayerCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/player-card-abilities.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(70, results.Count);
        Assert.Equal("942c7c7769123feb2634068e0e4eeb3822bdea0c230abb9c9e8c0679d4de9a2a", results["behavior:card:01058:after-daredevil-thwarts-deal-1-damage-enemy"].Digest);
        Assert.Equal("879610d7908a2657b12d1be3ce8ca0350f46072882f489bc34cc84296bc29dce", results["behavior:card:01073:increase-your-ally-limit-by-1-limit-reached"].Digest);
        Assert.Equal("a3dd15a5b648deaafb8db120ca0507629ed3c848e25bbc38c74a6096e969615c", results["behavior:card:01075:when-card-is-revealed-from-encounter-deck"].Digest);
        Assert.Equal("015cee3721c8b799304dcd5269f6300a51e76233e904a4f0fc002cdc07f15c49", results["behavior:card:01078:when-treachery-card-is-revealed-from-encounter"].Digest);
        Assert.Equal("5b0c4c647c6eea14fd4c9c12ce6d56a94a0f882f949d85ff6b28bc53cba7de01", results["behavior:card:01050:physical-deal-2-damage-enemy"].Digest);
        Assert.Equal("3355816960f2c6b36c2fa2a50cb7d2953dc34a725cd9bc19d4fcddf38e7235be", results["behavior:card:01050:energy-deal-1-damage-each-character"].Digest);
        Assert.Equal("99e826d906fd5a8d5aeddf5c9a5c8b81e318499549514042a0809fff55cc8e17", results["behavior:card:01050:mental-discard-hulk"].Digest);
        Assert.Equal("ae379c36134161a6252902afb22055a719f8ee191c0ce6419ea552e75ab5fe25", results["behavior:card:01050:wild-all-above"].Digest);
        Assert.Equal("0363505d44f84ff8ce1c5ab5159f063ecab495c019fb2f7e52eab888ee552f41", results["behavior:card:01007:attach-minion"].Digest);
        Assert.Equal("6a3652d2e089624606ec80cf3feffe28e610f4db9d2eb1b2412272ff998bc685", results["behavior:card:01042:choose-up-3-different-cards-in-your-minimum"].Digest);
        Assert.Equal("e6cd7d491f891b13af5e7066bef432a8fed3a2a06d9280ea262c3b4f5b2d2ce5", results["behavior:card:01042:choose-up-3-different-cards-in-your-intermediate"].Digest);
        Assert.Equal("55990185045503392e5779297cb8073a1512586a29d9ff1d80f1986131eea4ed", results["behavior:card:01042:choose-up-3-different-cards-in-your-maximum"].Digest);
        Assert.Equal("67de81717f3b3c0dc3cca2713e200db998c3c910e61b75e2a1b49f29d50e98cb", results["behavior:card:01018:max-1-per-player"].Digest);
        Assert.Equal("5e38da71ec0b7101f5da52d471a3c1bf9b9ba5b5ad44cedbe8b425b22dd2312d", results["behavior:card:01055:double-number-resources-card-generates-while-paying"].Digest);
        Assert.Equal("ae21a5c4cabecc7e8df9f0857d5f23e460d435a3aa29cc05abca431ce6461b90", results["behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-not-met"].Digest);
        Assert.Equal("d7738653c03360999a609b97199b9cffd43e3d0f98c198bbb7e08ac7d498541e", results["behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-met"].Digest);
        Assert.Equal("b6c5ca4d268526f5a2f69e05e53d12f7324ea6eb90d549b924206b3543abc32c", results["behavior:card:01062:double-number-resources-card-generates-while-paying"].Digest);
        Assert.Equal("d30070c30994c5c873b52fc50a6f68acbff85a4805d25ae8faa16110c4277ab7", results["behavior:card:01063:max-1-per-player"].Digest);
        Assert.Equal("8c360cb1898bae31bcdc3b1dc65a378973c5d257e9b7a53762fd7de388df4a34", results["behavior:card:01063:after-you-defeat-minion-exhaust-interrogation-room"].Digest);
        Assert.Equal("c05378dd8cdd624516568c8a890a370c050f5158b19c801150de426dcbdf8b8b", results["behavior:card:01067:after-maria-hill-enters-play-each-player-multiple-players"].Digest);
        Assert.Equal("81db58abc2d31bd5dc478370430541c98463348c313865f6e12190f5bbdc611c", results["behavior:card:01076:toughness"].Digest);
        Assert.Equal("c4fe1e77b86f576ac6803e8f3ffb15e343def984f42a0b75a7a877ecba72a70a", results["behavior:card:01093:spend-physical-resource-and-discard-card-ready"].Digest);
        Assert.Equal("f8f3ecc459be52e526426c9233adcb55a0b3f6b8a4921e3adad4557835d14178", results["behavior:card:01028:she-hulk-gets-2-atk"].Digest);
        Assert.Equal("d62eb9bf8db245ce8215888eea115e68c60ff6ea5e06af335328cce73eb7cac6", results["behavior:card:01031:for-each-printed-energy-resource-discarded-way-zero"].Digest);
        Assert.Equal("f136c6402f9eab31c03b11e809f3744a11f567c4b25ed51550426507da5cdafd", results["behavior:card:01031:for-each-printed-energy-resource-discarded-way-one"].Digest);
        Assert.Equal("2ce687e2d3d128417371548fa3273fad1d74c4863d0d94644a081b2c879efdcd", results["behavior:card:01059:jessica-jones-gets-1-thw-for-each-zero"].Digest);
        Assert.Equal("83a69cd7c8aea514995e3544f892e0e816e9b076fb1ef070d2a1d62010e1c08a", results["behavior:card:01059:jessica-jones-gets-1-thw-for-each-one"].Digest);
        Assert.Equal("d964be28b0d360b0781e53fe5765566979771a5a72ad02210df8abecaa6a54f7", results["behavior:card:01059:jessica-jones-gets-1-thw-for-each-multiple"].Digest);
        Assert.Equal("43321a13acef3113a7e17fe31e4c2905370407286862f56bcb9f203de6a4d7f0", results["behavior:card:01065:play-under-any-player-s-control"].Digest);
        Assert.Equal("07439cae66151bd5005443426ebf6143b6f1d125067c5285612cdf6c0a42b407", results["behavior:card:01065:max-1-per-player"].Digest);
        Assert.Equal("ef1e8af4c22bfc004c75e70ea50a6b79cd5a1d12e410c0d8f1c0dce20698ebfd", results["behavior:card:01081:play-under-any-player-s-control"].Digest);
        Assert.Equal("aad10fb81e66300476a915c533e117ae35929b798630d4e1c16a936ce234df7e", results["behavior:card:01081:max-1-per-player"].Digest);
        Assert.Equal("026aca6d5424ac5b022a2f159ad65b5e78016f6d62578189c77ad28bd811d611", results["behavior:card:01002:after-you-play-black-cat-discard-top"].Digest);
        Assert.Equal("f7dcdc3c723dbe698865eee3c6eb3de0d4c589f31046029bbe6c290ab3231cbb", results["behavior:card:01011:after-spider-woman-enters-play-confuse-villain"].Digest);
        Assert.Equal("c2ece15d054edcc22adc92af08b8f455a10b11593eb3ef0b806d9901fddf66ae", results["behavior:card:01019a:after-you-change-form-deal-2-damage"].Digest);
        Assert.Equal("fb43fc49e75bb8b81eb0b08772f69cc8707bcfc306d828cb29ceb858e9a7ac69", results["behavior:card:01024:after-you-make-basic-attack-using-your"].Digest);
        Assert.Equal("d42e5505bd0e5fabc86cfc7328225f032d0ce36b8fc51ccc100a66c374cec003", results["behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-not-met"].Digest);
        Assert.Equal("3112d7a3543aa7d330edb6cb21dfe0b7726de8ef12caef3f448a5d807d4d884e", results["behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-met"].Digest);
        Assert.Equal("b6e23c351b2e616cee5aea0e3db909756e7c83bc807f0d090a012a0a2ccb59ce", results["behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-not-met"].Digest);
        Assert.Equal("27d5db84be7a4ac54ce8fa63f929645ee487591d643d5df64f5d9b2d426f235a", results["behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-met"].Digest);
        Assert.Equal("9ae31578c15b762555c5399bad6512c92454c17071136d7ff04461c902136379", results["behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-not-met"].Digest);
        Assert.Equal("0633f7f7f766f40f1b4bdd8e201bda42c70ef4f3619bb199484ed16dfaa674e5", results["behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-met"].Digest);
        Assert.Equal("03df40ae6ceac7d4b6c94fcaf97e185e3eca0c0948fd20d570df26d5823c6503", results["behavior:card:01084:after-entering-play-remove-two-threat"].Digest);
        Assert.Equal("ef5067a91fb1e64d04af508175e6c7e6b63e5fbcafae4f58d84adc57bc5589a8", results["behavior:card:01084:after-entering-play-deal-four-damage"].Digest);
        Assert.Equal("73429b06e06aac69e83c9747c047cf1263a76e6b23cb489de9202a616dc6764d", results["behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-not-met"].Digest);
        Assert.Equal("74b768ace6a3265777f0eec610ad2d205af799e3145ab2f5bd8ab3f0d9919560", results["behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-met"].Digest);
        Assert.Equal("bf14a182626b760d182d0a1e611be7c73e6605e06d61a6ba53abda50ab612e0b", results["behavior:card:01017:when-captain-marvel-would-take-damage-discard"].Digest);
        Assert.Equal("a7231e18de95fa513b562f8f1eeedc8db9be34f7005b493d1be05d6e53f5e6d0", results["behavior:card:01008:when-those-are-gone-discard-card"].Digest);
        Assert.Equal("3e1d13290b6ce29527eb0cae35ebbfb897ddc5d8c6e51a08c3fbcfb8b4d52fdc", results["behavior:card:01083:after-mockingbird-enters-play-stun-enemy"].Digest);
        Assert.Equal("972ad7da1753fcc4ad6a68a0bb480871c6a28424c83d996c19ebdab268cf73bb", results["behavior:card:01068:choose-thw-plus-two-until-end-phase"].Digest);
        Assert.Equal("f2acf656d1837dc30c688e59e129c288bad617303fda322bc4b76a3cef53d9a9", results["behavior:card:01068:choose-atk-plus-two-until-end-phase"].Digest);
        Assert.Equal("b17f9fbb3d730047490cb61c0f065daa8c730d29ac2d28f50c7681c2882c1ad3", results["behavior:card:01035:exhaust-arc-reactor-ready-iron-man"].Digest);
        Assert.Equal("352a3f743b5fd56afd7bbf73c950b94f48dadb56431412524de1cf368d477eb7", results["behavior:card:01036:you-get-6-hit-points"].Digest);
        Assert.Equal("baba20d06a4ae560afad0d5c873059ac2997644932c9bf9f4a0a5a62531dd5d0", results["behavior:card:01045:exhaust-golden-city-draw-2-cards"].Digest);
        Assert.Equal("e25f1313dc5ce5fbbebe4212c0750ffdc7fc55417330cbf05eaa0af0f1fa4930", results["behavior:card:01069:ready-ally"].Digest);
        Assert.Equal("5e753604530663b611d5028237c3dc51fd8e0584e09617ac78b835364a4aba1f", results["behavior:card:01086:heal-2-damage-from-any-character"].Digest);
        Assert.Equal("8ea0ffdad1bd63a5cb9a4baeda9d24867a335c3e0327ea7c8b8a72f90efd78b7", results["behavior:card:01020:return-hellcat-your-hand"].Digest);
        Assert.Equal("e6cf4f880e001bc03a7d5c05eca80611dfb536efe4f6c4d2aea885fdd757bd7f", results["behavior:card:01091:exhaust-avengers-mansion-choose-player"].Digest);
        Assert.Equal("fdc1fe9a404b8ee735bf36adef04640631fee32c1c5e0279aa7321fedfb61639", results["behavior:card:01015:exhaust-alpha-flight-station-choose-and-discard-condition-met"].Digest);
        Assert.Equal("f7d1e8e807c5da9102f16333e42c7242becadb4e86db18b3f3e0d293fd9ae529", results["behavior:card:01026:exhaust-superhuman-law-division-and-spend-mental"].Digest);
        Assert.Equal("510369bea032b2c09917c59fb16654c5baa6712fec98af0b6df23f8fab940573", results["behavior:card:01033:exhaust-pepper-potts-generate-resources-top-card"].Digest);
        Assert.Equal("220eb146ad58642d460bca21ee7526d6e03560381c9424bae02921f77f618709", results["behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-accepted"].Digest);
        Assert.Equal("5896375c93ec09a9309e1e3a75b3bc50628c8f3d896014673074f8e95eb0e566", results["behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-declined"].Digest);
        Assert.Equal("518c9e5a17da771d3951d6e9c5dede1d42261ccd51b55cbe344e0bc9d0db8034", results["behavior:card:01034:exhaust-stark-tower-choose-player"].Digest);
    }

    [Fact]
    public void RhinoCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/rhino-card-abilities.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(15, results.Count);
        Assert.Equal("52303ca82ba61eecfacfa708c390bf5d637fbbcf1cb51ede2ecf55abaeb1ec2c", results["behavior:card:01095:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("6cbf533d47f0a327837f9b9128327152208c21367b8ba8aacddc17a8fdd497f9", results["behavior:card:01097b:if-stage-is-completed-players-lose-game-condition-not-met"].Digest);
        Assert.Equal("e54175a23686cc32d77ce3fb94274c2456efa43bd574f6e233c5a116fc11a117", results["behavior:card:01098:attach-rhino"].Digest);
        Assert.Equal("77c80bc5e77c81b565196ddabac9a16572ac17415d61b13305a8af73dddaf27c", results["behavior:card:01098:then-if-there-is-at-least-5-condition-not-met"].Digest);
        Assert.Equal("b09ed1930a066f03bfa32407b31058d31593edebfdc5e819caf63e3cf3a1a792", results["behavior:card:01100:attach-rhino"].Digest);
        Assert.Equal("0e0c9dc3e9c962316f80d4ab116112a72351e5c330f36a2fb59579fc6cc01136", results["behavior:card:01103:deal-1-damage-each-hero"].Digest);
        Assert.Equal("8be2818aa61bf984ab2171c98f75217b311cc66f27d7de186207c756517b0136", results["behavior:card:01105:give-rhino-tough-status-card"].Digest);
        Assert.Equal("5feba949d3a19b37b7957f3ac8e21c1a5947c6a9df55fba80020058155b78d81", results["behavior:card:01105:if-rhino-already-has-tough-status-card-condition-met"].Digest);
        Assert.Equal("cb179aee011ab2ed5c888cb1b7d5bb1790612f661cdae15c74e2e1832e662e47", results["behavior:card:01106:card-gains-surge"].Digest);
        Assert.Equal("2380e4a8d77c313b973a965c59bc41fcd790e467a116f13e0018e7afc9af8561", results["behavior:card:01106:if-character-is-damaged-by-attack-that-condition-not-met"].Digest);
        Assert.Equal("85bebc79cdc7fb5b5330ecdf3ea0aa09bf63fcd5424e2dfba401a39053679d26", results["behavior:card:01110:when-revealed-take-two-damage"].Digest);
        Assert.Equal("641a116581cdf0b9fd2b9c43d378256d3d8a39c3b06097c3c36007396dfda7ce", results["behavior:card:01110:when-revealed-place-one-threat"].Digest);
        Assert.Equal("65c0a5e16448651c6dee7d9fa78eba6af77841d123aed0c6ecc843f5ebae73bf", results["behavior:card:01112:if-you-are-already-confused-card-gains-condition-met"].Digest);
        Assert.Equal("d15413648737eb718319bd1139e237fbb00c53a3c27676162b00210ca6376fe2", results["behavior:card:01111:if-bomb-scare-is-in-play-assign-condition-met"].Digest);
        Assert.Equal("6c7e81f838e9c924c72db1009795168070513585007f527f978f59d6c7004c03", results["behavior:card:01111:if-bomb-scare-is-not-in-play-condition-met"].Digest);
    }
}
