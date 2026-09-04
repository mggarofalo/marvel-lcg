using System.Text.Json;
using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreCardFaceTranscriptTests
{
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
        var results = CoreTranscriptCorpus.All
            .Where(result => result.Scenario.StartsWith(
                "specs/behavior/core/card-faces.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(209, results.Count);
        Assert.Equal(expected, results.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(
            "f166b2a393eb1cbf8a63bec8ca05525a2406fee737e5db370c893bd39ffe0fe7",
            results["behavior:card:01001a:printed-name"].Digest);
        Assert.Equal(
            "9c0d8ada5ab4241a49f9ced9fa27fdb0e067765eae1d436ced7e9a018a05245b",
            results["behavior:card:01149:printed-name"].Digest);
    }

    [Fact]
    public void IdentityCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/identity-card-abilities.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(5, results.Count);
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
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/player-card-abilities.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(70, results.Count);
        Assert.Equal(
            "942c7c7769123feb2634068e0e4eeb3822bdea0c230abb9c9e8c0679d4de9a2a",
            results[
                "behavior:card:01058:after-daredevil-thwarts-deal-1-damage-enemy"].Digest);
        Assert.Equal(
            "879610d7908a2657b12d1be3ce8ca0350f46072882f489bc34cc84296bc29dce",
            results[
                "behavior:card:01073:increase-your-ally-limit-by-1-limit-reached"].Digest);
        Assert.Equal(
            "a3dd15a5b648deaafb8db120ca0507629ed3c848e25bbc38c74a6096e969615c",
            results[
                "behavior:card:01075:when-card-is-revealed-from-encounter-deck"].Digest);
        Assert.Equal(
            "015cee3721c8b799304dcd5269f6300a51e76233e904a4f0fc002cdc07f15c49",
            results[
                "behavior:card:01078:when-treachery-card-is-revealed-from-encounter"].Digest);
        Assert.Equal(
            "5b0c4c647c6eea14fd4c9c12ce6d56a94a0f882f949d85ff6b28bc53cba7de01",
            results["behavior:card:01050:physical-deal-2-damage-enemy"].Digest);
        Assert.Equal(
            "3355816960f2c6b36c2fa2a50cb7d2953dc34a725cd9bc19d4fcddf38e7235be",
            results["behavior:card:01050:energy-deal-1-damage-each-character"].Digest);
        Assert.Equal(
            "99e826d906fd5a8d5aeddf5c9a5c8b81e318499549514042a0809fff55cc8e17",
            results["behavior:card:01050:mental-discard-hulk"].Digest);
        Assert.Equal(
            "ae379c36134161a6252902afb22055a719f8ee191c0ce6419ea552e75ab5fe25",
            results["behavior:card:01050:wild-all-above"].Digest);
        Assert.Equal(
            "0363505d44f84ff8ce1c5ab5159f063ecab495c019fb2f7e52eab888ee552f41",
            results["behavior:card:01007:attach-minion"].Digest);
        Assert.Equal(
            "6a3652d2e089624606ec80cf3feffe28e610f4db9d2eb1b2412272ff998bc685",
            results[
                "behavior:card:01042:choose-up-3-different-cards-in-your-minimum"].Digest);
        Assert.Equal(
            "e6cd7d491f891b13af5e7066bef432a8fed3a2a06d9280ea262c3b4f5b2d2ce5",
            results[
                "behavior:card:01042:choose-up-3-different-cards-in-your-intermediate"].Digest);
        Assert.Equal(
            "55990185045503392e5779297cb8073a1512586a29d9ff1d80f1986131eea4ed",
            results[
                "behavior:card:01042:choose-up-3-different-cards-in-your-maximum"].Digest);
        Assert.Equal(
            "67de81717f3b3c0dc3cca2713e200db998c3c910e61b75e2a1b49f29d50e98cb",
            results["behavior:card:01018:max-1-per-player"].Digest);
        Assert.Equal(
            "5e38da71ec0b7101f5da52d471a3c1bf9b9ba5b5ad44cedbe8b425b22dd2312d",
            results[
                "behavior:card:01055:double-number-resources-card-generates-while-paying"].Digest);
        Assert.Equal(
            "ae21a5c4cabecc7e8df9f0857d5f23e460d435a3aa29cc05abca431ce6461b90",
            results[
                "behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-not-met"].Digest);
        Assert.Equal(
            "d7738653c03360999a609b97199b9cffd43e3d0f98c198bbb7e08ac7d498541e",
            results[
                "behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-met"].Digest);
        Assert.Equal(
            "b6c5ca4d268526f5a2f69e05e53d12f7324ea6eb90d549b924206b3543abc32c",
            results[
                "behavior:card:01062:double-number-resources-card-generates-while-paying"].Digest);
        Assert.Equal(
            "d30070c30994c5c873b52fc50a6f68acbff85a4805d25ae8faa16110c4277ab7",
            results["behavior:card:01063:max-1-per-player"].Digest);
        Assert.Equal(
            "8c360cb1898bae31bcdc3b1dc65a378973c5d257e9b7a53762fd7de388df4a34",
            results[
                "behavior:card:01063:after-you-defeat-minion-exhaust-interrogation-room"].Digest);
        Assert.Equal(
            "c05378dd8cdd624516568c8a890a370c050f5158b19c801150de426dcbdf8b8b",
            results[
                "behavior:card:01067:after-maria-hill-enters-play-each-player-multiple-players"].Digest);
        Assert.Equal(
            "81db58abc2d31bd5dc478370430541c98463348c313865f6e12190f5bbdc611c",
            results["behavior:card:01076:toughness"].Digest);
        Assert.Equal(
            "c4fe1e77b86f576ac6803e8f3ffb15e343def984f42a0b75a7a877ecba72a70a",
            results[
                "behavior:card:01093:spend-physical-resource-and-discard-card-ready"].Digest);
        Assert.Equal(
            "f8f3ecc459be52e526426c9233adcb55a0b3f6b8a4921e3adad4557835d14178",
            results["behavior:card:01028:she-hulk-gets-2-atk"].Digest);
        Assert.Equal(
            "d62eb9bf8db245ce8215888eea115e68c60ff6ea5e06af335328cce73eb7cac6",
            results[
                "behavior:card:01031:for-each-printed-energy-resource-discarded-way-zero"].Digest);
        Assert.Equal(
            "f136c6402f9eab31c03b11e809f3744a11f567c4b25ed51550426507da5cdafd",
            results[
                "behavior:card:01031:for-each-printed-energy-resource-discarded-way-one"].Digest);
        Assert.Equal(
            "2ce687e2d3d128417371548fa3273fad1d74c4863d0d94644a081b2c879efdcd",
            results[
                "behavior:card:01059:jessica-jones-gets-1-thw-for-each-zero"].Digest);
        Assert.Equal(
            "83a69cd7c8aea514995e3544f892e0e816e9b076fb1ef070d2a1d62010e1c08a",
            results[
                "behavior:card:01059:jessica-jones-gets-1-thw-for-each-one"].Digest);
        Assert.Equal(
            "d964be28b0d360b0781e53fe5765566979771a5a72ad02210df8abecaa6a54f7",
            results[
                "behavior:card:01059:jessica-jones-gets-1-thw-for-each-multiple"].Digest);
        Assert.Equal(
            "43321a13acef3113a7e17fe31e4c2905370407286862f56bcb9f203de6a4d7f0",
            results[
                "behavior:card:01065:play-under-any-player-s-control"].Digest);
        Assert.Equal(
            "07439cae66151bd5005443426ebf6143b6f1d125067c5285612cdf6c0a42b407",
            results["behavior:card:01065:max-1-per-player"].Digest);
        Assert.Equal(
            "ef1e8af4c22bfc004c75e70ea50a6b79cd5a1d12e410c0d8f1c0dce20698ebfd",
            results[
                "behavior:card:01081:play-under-any-player-s-control"].Digest);
        Assert.Equal(
            "aad10fb81e66300476a915c533e117ae35929b798630d4e1c16a936ce234df7e",
            results["behavior:card:01081:max-1-per-player"].Digest);
        Assert.Equal(
            "026aca6d5424ac5b022a2f159ad65b5e78016f6d62578189c77ad28bd811d611",
            results[
                "behavior:card:01002:after-you-play-black-cat-discard-top"].Digest);
        Assert.Equal(
            "f7dcdc3c723dbe698865eee3c6eb3de0d4c589f31046029bbe6c290ab3231cbb",
            results[
                "behavior:card:01011:after-spider-woman-enters-play-confuse-villain"].Digest);
        Assert.Equal(
            "c2ece15d054edcc22adc92af08b8f455a10b11593eb3ef0b806d9901fddf66ae",
            results[
                "behavior:card:01019a:after-you-change-form-deal-2-damage"].Digest);
        Assert.Equal(
            "fb43fc49e75bb8b81eb0b08772f69cc8707bcfc306d828cb29ceb858e9a7ac69",
            results[
                "behavior:card:01024:after-you-make-basic-attack-using-your"].Digest);
        Assert.Equal(
            "d42e5505bd0e5fabc86cfc7328225f032d0ce36b8fc51ccc100a66c374cec003",
            results[
                "behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-not-met"].Digest);
        Assert.Equal(
            "3112d7a3543aa7d330edb6cb21dfe0b7726de8ef12caef3f448a5d807d4d884e",
            results[
                "behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-met"].Digest);
        Assert.Equal(
            "b6e23c351b2e616cee5aea0e3db909756e7c83bc807f0d090a012a0a2ccb59ce",
            results[
                "behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-not-met"].Digest);
        Assert.Equal(
            "27d5db84be7a4ac54ce8fa63f929645ee487591d643d5df64f5d9b2d426f235a",
            results[
                "behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-met"].Digest);
        Assert.Equal(
            "9ae31578c15b762555c5399bad6512c92454c17071136d7ff04461c902136379",
            results[
                "behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-not-met"].Digest);
        Assert.Equal(
            "0633f7f7f766f40f1b4bdd8e201bda42c70ef4f3619bb199484ed16dfaa674e5",
            results[
                "behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-met"].Digest);
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
    public void RhinoCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/rhino-card-abilities.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(15, results.Count);
        Assert.Equal("52303ca82ba61eecfacfa708c390bf5d637fbbcf1cb51ede2ecf55abaeb1ec2c",
            results["behavior:card:01095:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("6cbf533d47f0a327837f9b9128327152208c21367b8ba8aacddc17a8fdd497f9",
            results["behavior:card:01097b:if-stage-is-completed-players-lose-game-condition-not-met"].Digest);
        Assert.Equal("e54175a23686cc32d77ce3fb94274c2456efa43bd574f6e233c5a116fc11a117",
            results["behavior:card:01098:attach-rhino"].Digest);
        Assert.Equal("77c80bc5e77c81b565196ddabac9a16572ac17415d61b13305a8af73dddaf27c",
            results["behavior:card:01098:then-if-there-is-at-least-5-condition-not-met"].Digest);
        Assert.Equal("b09ed1930a066f03bfa32407b31058d31593edebfdc5e819caf63e3cf3a1a792",
            results["behavior:card:01100:attach-rhino"].Digest);
        Assert.Equal("0e0c9dc3e9c962316f80d4ab116112a72351e5c330f36a2fb59579fc6cc01136",
            results["behavior:card:01103:deal-1-damage-each-hero"].Digest);
        Assert.Equal("8be2818aa61bf984ab2171c98f75217b311cc66f27d7de186207c756517b0136",
            results["behavior:card:01105:give-rhino-tough-status-card"].Digest);
        Assert.Equal("5feba949d3a19b37b7957f3ac8e21c1a5947c6a9df55fba80020058155b78d81",
            results["behavior:card:01105:if-rhino-already-has-tough-status-card-condition-met"].Digest);
        Assert.Equal("cb179aee011ab2ed5c888cb1b7d5bb1790612f661cdae15c74e2e1832e662e47",
            results["behavior:card:01106:card-gains-surge"].Digest);
        Assert.Equal("2380e4a8d77c313b973a965c59bc41fcd790e467a116f13e0018e7afc9af8561",
            results["behavior:card:01106:if-character-is-damaged-by-attack-that-condition-not-met"].Digest);
        Assert.Equal("85bebc79cdc7fb5b5330ecdf3ea0aa09bf63fcd5424e2dfba401a39053679d26",
            results["behavior:card:01110:when-revealed-take-two-damage"].Digest);
        Assert.Equal("641a116581cdf0b9fd2b9c43d378256d3d8a39c3b06097c3c36007396dfda7ce",
            results["behavior:card:01110:when-revealed-place-one-threat"].Digest);
        Assert.Equal("65c0a5e16448651c6dee7d9fa78eba6af77841d123aed0c6ecc843f5ebae73bf",
            results["behavior:card:01112:if-you-are-already-confused-card-gains-condition-met"].Digest);
        Assert.Equal("d15413648737eb718319bd1139e237fbb00c53a3c27676162b00210ca6376fe2",
            results["behavior:card:01111:if-bomb-scare-is-in-play-assign-condition-met"].Digest);
        Assert.Equal("6c7e81f838e9c924c72db1009795168070513585007f527f978f59d6c7004c03",
            results["behavior:card:01111:if-bomb-scare-is-not-in-play-condition-met"].Digest);
    }

    [Fact]
    public void KlawCardAbilityBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/klaw-card-abilities.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(33, results.Count);
        Assert.Equal("74977959d3ac2dfd74920cc8f0d69710a890f57f931e64629d02c1c656c095f2",
            results["behavior:card:01114:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("6f534db787710e0ca4552d994de3ee0590186ffd2ea1498dff8359f6bece2985",
            results["behavior:card:01114:when-klaw-attacks-give-him-1-additional"].Digest);
        Assert.Equal("58199dccd3d5857dfd05ccbd3aa39266b1440de0c8f2f296d7fff086933beff1",
            results["behavior:card:01115:toughness"].Digest);
        Assert.Equal("4f2a1589b6867a0c09fbe1c5719694dfb037fd47d40b98b878a5d9a4309afe1f",
            results["behavior:card:01115:when-klaw-attacks-give-him-1-additional"].Digest);
        Assert.Equal("27ad3c1592156f96a9eb02848083668269d338f2fef86e221fe0b25ae879db9f",
            results["behavior:card:01118:attach-klaw"].Digest);
        Assert.Equal("6bfe917e81e62df564f9bf54cf28b810000943520caf18e002ca92d2cde8273e",
            results["behavior:card:01119:attach-klaw"].Digest);
        Assert.Equal("6b1c4cb8cf587e8da881736c299cef41a13fc6f81eaa5d194c3b081d5a482201",
            results["behavior:card:01125:place-additional-1-per-hero-threat-here"].Digest);
        Assert.Equal("a664274835e5a11a5319eb39ebf6edb4b8126f2e953378ea735867a86fc45752",
            results["behavior:card:01126:place-additional-1-per-hero-threat-here"].Digest);
        Assert.Equal("a3c8a74ed92a8f8467df642172275878abe4b18b98e0a51c3a1fe7440c6d5006",
            results["behavior:card:01127:klaw-gets-10-hit-points"].Digest);
        Assert.Equal("cfce6c3697ebaa86705398932303530c9faf120ec77f2b71fbb5d68797a52bd1",
            results["behavior:card:01122:discard-1-card-at-random-from-your"].Digest);
        Assert.Equal("56cefa785c7f43c551641107ebc7427fac5889ef2748a0107f9290d482cf72a5",
            results["behavior:card:01122:klaw-attacks-you"].Digest);
        Assert.Equal("7008941842fd2393a2b0e7dc7638af1f5d5f80750755dd8b50c7b1f96b07a080",
            results[
                "behavior:card:01122:if-attack-deals-damage-place-1-threat-condition-not-met"].Digest);
        Assert.Equal("dadf0d6e7f57439ccc2b63c9bfefa1e21375b7c612a75b68a18381c3e3a25d4c",
            results[
                "behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-1"].Digest);
        Assert.Equal("1e9b2b1eb0795172f46bde4447a9a96cc092a018d4f6cf171ae6c3f5bb76bc85",
            results[
                "behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-2"].Digest);
        Assert.Equal("1622f7fb206461f3762d75b0cc452f96a35061ac7f158227102f714bf7135bbf",
            results["behavior:card:01124:klaw-heals-4-damage"].Digest);
        Assert.Equal("cf9a3a1cde38db8b1d36e2a732b1ad69ce529c8bf024ce3c01e19abb8201e78a",
            results[
                "behavior:card:01124:if-no-damage-was-healed-way-card-condition-met"].Digest);
        Assert.Equal("d7796479e9a5aac10f1b4bba5df7c755b04e856257be3b73be1425a757702fb6",
            results["behavior:card:01124:take-2-damage"].Digest);
        Assert.Equal("31bb61ff7897ab1d8a38b6905c27d5d780c8dcaf51f045b47aa6fe13dcb8db93",
            results[
                "behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-met"].Digest);
        Assert.Equal("e08e3c635f378cd4fd0f5d6e265607566d58c4416823daf07f5cd72c7696c5b2",
            results[
                "behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-not-met"].Digest);
        Assert.Equal("bb9d773d691c38e0ff9211c388686935de36ee7341c3e725a5534f7774e5215b",
            results[
                "behavior:card:01128:discard-cards-from-encounter-deck-until-masters"].Digest);
        Assert.Equal("c35146a5397b4034c493e78e15c94dd49a8884b8b1a559461c07c2b86facf4a2",
            results[
                "behavior:card:01129:after-radioactive-man-attacks-you-discard-1"].Digest);
        Assert.Equal("9e1ed1fc3f66d7b1227715ff0e289c886647d8a9f562a78f4ed4e8b04c872dc4",
            results["behavior:card:01129:discard-1-card-at-random-from-your"].Digest);
        Assert.Equal("ea8fedaa373dcf704c6249d7181a6799b0f40518e4639019a75a782a2c444982",
            results[
                "behavior:card:01130:when-whirlwind-attacks-you-also-resolve-his"].Digest);
        Assert.Equal("c8e2c63433a71edfabda4e55193c91ddd462c43fc696edbd64380893adfc3195",
            results["behavior:card:01130:deal-1-damage-each-hero"].Digest);
        Assert.Equal("15a6c845cc19cfce07bcfd677ece17366490f1100c0a9e4b5a79f90ea74e2878",
            results[
                "behavior:card:01131:after-tiger-shark-attacks-give-him-tough"].Digest);
        Assert.Equal("c3854c9132ba950b259a65bab78916c149350a3a62f11163fa3460ea3ce1f9df",
            results["behavior:card:01131:give-villain-tough-status-card"].Digest);
        Assert.Equal("365f216dc1433c3063a922d5f04ab0b11ce16f8274184b89ade052e3ff490734",
            results[
                "behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-met"].Digest);
        Assert.Equal("dd4db31fdb006158f9b683e0777f91b715738b6521bbe2937101acab4ad3fec7",
            results[
                "behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-not-met"].Digest);
        Assert.Equal("87408c8683fbd60fa254b2831f55f390962a495af27d1f9850ea0a42f8e07035",
            results["behavior:card:01132:exhaust-each-ally-you-control"].Digest);
        Assert.Equal("3b8713204120580617e6203f3714b23556716066848663f3579f0e78b6873963",
            results[
                "behavior:card:01133:each-masters-evil-minion-attacks-hero-it"].Digest);
        Assert.Equal("5dd63a1b5b262081b712eedc4f64ba0ff70a52bd34716cf86a6f56d174bf3fb9",
            results[
                "behavior:card:01133:if-no-attacks-were-made-way-search-condition-met"].Digest);
    }

    [Fact]
    public void UltronAttachmentBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/ultron-attachments.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(8, results.Count);
        Assert.Equal("696ffec6d3bb2318e9e579fc9c641b1e644901d0575ad7fabf2c98aa23cce1c7",
            results["behavior:card:01141:attach-ultron"].Digest);
        Assert.Equal("30e13fd0a5f990c2226821e4608adfa2c16a7bdd9e835e58d59688b0fe34828b",
            results[
                "behavior:card:01141:after-ultron-schemes-place-1-threat-on"].Digest);
        Assert.Equal("58469c039eac98cf8d206316fd966bfe9510ffa4dec69d06ac0d8c8494eae7b8",
            results["behavior:card:01142:attach-ultron-drones-environment"].Digest);
        Assert.Equal("e363a3261dcf9430f5ba72925925e24fdc8bb026f8fbae2cb599c7b6a3a2c608",
            results[
                "behavior:card:01142:each-facedown-drone-minion-gets-1-atk"].Digest);
        Assert.Equal("0e5f834f33625cb9711cbd0cc12778cc48cc690d6c3a0efce0f1c97ea6e5dc45",
            results["behavior:card:01152:attach-villain"].Digest);
        Assert.Equal("5d1cb132912f2871d45cc0cf72228c7b02a4d57562da59fde9813631dfae039a",
            results[
                "behavior:card:01152:exhaust-your-hero-and-spend-physical-physical"].Digest);
        Assert.Equal("8ea3d59c89c882d6f09e11418e50d15515e4dcad31a2556e65b010aa5aff211b",
            results["behavior:card:01153:attach-villain"].Digest);
        Assert.Equal("1666fd4af0d6e26a278040f0cefab41e79fae25ef5a7c8a78162584c5a3b8477",
            results[
                "behavior:card:01153:exhaust-your-hero-and-spend-energy-energy"].Digest);
    }

    [Fact]
    public void UltronMainSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/ultron-main-schemes.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(11, results.Count);
        Assert.Equal("870ef71d126c3054f1cab459a87cd77c57574103989b59e5c5ad8edc1de47cb2",
            results[
                "behavior:card:01137b:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("752f0c5a0c37a57ac883ad303132756d01dfe2fbef6c3ebb096e103631b44229",
            results[
                "behavior:card:01138a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("bcd21d2b1f3234cbb96c205c4fef652855549a66586d78f03bdfe54d237b31d3",
            results[
                "behavior:card:01138a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("b7c8df04402215deca6dced9aa21d321a6caf2c2c834e66ed84ede797c8b00ca",
            results[
                "behavior:card:01138b:after-placing-threat-here-during-step-one-choice-1"].Digest);
        Assert.Equal("f984963f28550ede2397f1ab3dfc9507a8c4d3a4d9ae322afbf10d719d943a64",
            results[
                "behavior:card:01138b:after-placing-threat-here-during-step-one-choice-2"].Digest);
        Assert.Equal("54c6e0a39a203a0628704f0b042b7c425f6547794a15b95f57c3a7e8ef5dd528",
            results[
                "behavior:card:01138b:after-placing-threat-here-during-step-one-multiple-players"].Digest);
        Assert.Equal("b29b9adc130f46013c2a9ffde4cf409f842954139bfa8ca05643e7efa6905376",
            results[
                "behavior:card:01139a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("18f3ee0620c9b832469f495abd3b1641d0893fd13da693ea9773ea52ca2108b1",
            results[
                "behavior:card:01139a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("4c66d517451ee97afd628431786f97ed368a3735f3fd77f698260913f382a7cb",
            results[
                "behavior:card:01139b:threat-cannot-be-removed-from-scheme"].Digest);
        Assert.Equal("2291b4105b519ef1c2299ee8a8c48cb160873eb704241d3654db2163515656cd",
            results[
                "behavior:card:01139b:if-stage-is-completed-players-lose-game-condition-met"].Digest);
    }

    [Fact]
    public void UltronSideSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/ultron-side-schemes.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(6, results.Count);
        Assert.Equal("4f27ab7cd4c7b20239b62f64c7c2a125c0d89dc4ba17a47650a38b6174804c03",
            results[
                "behavior:card:01148:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("a1c11bb5a7edcc4481714ed279151acf925c990907adad40d62161cd5e87699d",
            results[
                "behavior:card:01148:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("72c93ed5545f7f2804293890329445abd3deb4d66f712f793df636fdb781769b",
            results["behavior:card:01150:first-player-puts-top-2-cards-their"].Digest);
        Assert.Equal("2e41788ec0dfc9e7afc9e32723b6e982a077db3e59f3699beefbd90adea442d6",
            results[
                "behavior:card:01151:each-player-chooses-either-place-2-threat-one-player"].Digest);
        Assert.Equal("a2d48433c8b39881085acfe0ad754f08cfcbb056ef10ae8295620194224d2e74",
            results[
                "behavior:card:01151:each-player-chooses-either-place-2-threat-multiple-players"].Digest);
    }

    [Fact]
    public void UltronVillainAndDroneBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/ultron-villain-and-drones.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(9, results.Count);
        Assert.Equal("62e1050af2ffdf32d28bd699913ac85c574dc84925b9f00f12489b224ab00470",
            results[
                "behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-1"].Digest);
        Assert.Equal("e04919326d0ec2813485e5713081af2781c4aacf909e0248c09e79108abbfa32",
            results[
                "behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-2"].Digest);
        Assert.Equal("5e4b05ed76aa73828f89d2c973f4c4636d2c0425edc15bbee6526c06815b2795",
            results["behavior:card:01135:when-ultron-attacks-you-put-top-card"].Digest);
        Assert.Equal("549db6712688b546a0d194f7888eca5f410af51b59a1876873115aefff542035",
            results[
                "behavior:card:01135:until-end-his-attack-ultron-gets-1-multiple"].Digest);
        Assert.Equal("c3ecbd73663ad43c562303b1d865b94b15997b2f292501e47ff0a10ad488a7d4",
            results[
                "behavior:card:01136:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("063becf8f5e8de57fbb8428c234ce4daa4e9465913bad9d6018d7b7c4a70809a",
            results[
                "behavior:card:01140:each-facedown-drone-minion-engaged-with-player"].Digest);
        Assert.Equal("7d03e747b7fbaf6bb091c9c373aad404bc940e7789ada4869ec76e646fd10e7d",
            results["behavior:card:01143:guard"].Digest);
    }

    [Fact]
    public void UltronTreacheryBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/ultron-treacheries.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(16, results.Count);
        Assert.Equal("5f832f8f13b1ee16b00e3fc8a1fffcc83efaea50eae119b97433aa43f8085ec9",
            results["behavior:card:01145:ultron-schemes"].Digest);
        Assert.Equal("0328de1a7b9d93b8fd925cc2ea430d80ad685fddf34f20e82e3082b27d0927a9",
            results[
                "behavior:card:01145:discard-top-card-your-deck-for-each-zero"].Digest);
        Assert.Equal("4f161f028d4914d2a1a6f18be176121fb0e78bf8454a522bc0a04e23b7b21ae5",
            results[
                "behavior:card:01145:discard-top-card-your-deck-for-each-multiple"].Digest);
        Assert.Equal("1584bf1a9b5c84d91cc2a9cb66352d75238e50274ce06e6b027f7e9f9737f6fc",
            results["behavior:card:01145:ultron-attacks-you"].Digest);
        Assert.Equal("47d858c343bdee8e8bd36840591df5e53d5b8761069f5182cb8079b6fd63be83",
            results[
                "behavior:card:01145:discard-top-card-your-deck-for-each-one-2"].Digest);
        Assert.Equal("9dd62faf7bdc21b8cf7339243e944016a85ec1d05ae6ce7ebbeaef4aeed1826a",
            results[
                "behavior:card:01145:discard-top-card-your-deck-for-each-multiple-2"].Digest);
        Assert.Equal("2a7e50bdcf1ead787b8dd6b2db5b386171f7b2e59b2a62d16d77b80a638da6e9",
            results[
                "behavior:card:01146:ultron-heals-2-damage-for-each-drone-zero"].Digest);
        Assert.Equal("f5a42d105f821e37818e28841cf0f8df473d328585a2bc2030d19e31a79820a6",
            results[
                "behavior:card:01146:ultron-heals-2-damage-for-each-drone-one"].Digest);
        Assert.Equal("79f1ead61fee95706c46d02bb9a1733537816dfe17f766e1590ae2a8dc0ba89a",
            results[
                "behavior:card:01146:ultron-heals-2-damage-for-each-drone-multiple"].Digest);
        Assert.Equal("526cfdf1ee73f3d75a279e9496e61d51f6b6707ee282b3e2acbf5818035a5358",
            results[
                "behavior:card:01146:ultron-heals-1-damage-for-each-drone-zero"].Digest);
        Assert.Equal("b2d12aeecaa69d5db0fa76ddf7bf02d5ec071ee6d73deb0f8369c467196bffdf",
            results[
                "behavior:card:01146:ultron-heals-1-damage-for-each-drone-one"].Digest);
        Assert.Equal("cefe505d1fdf9fc59f312cda5162fbd118b235cc5ab8bebcb1ea01cd519d56f9",
            results[
                "behavior:card:01146:ultron-heals-1-damage-for-each-drone-multiple"].Digest);
        Assert.Equal("fa0dcb4232851887fd6482ed5c3953fd916eca40de292e010ee32f039d7d5627",
            results[
                "behavior:card:01147:each-drone-minion-engaged-with-your-hero"].Digest);
        Assert.Equal("5f714ff8b81a3a0dd8f613eb45285f2a24eb583218dd49fe065f3441dc688112",
            results[
                "behavior:card:01147:if-no-attack-was-made-way-put-condition-met"].Digest);
    }

    [Fact]
    public void CardActionBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/card-actions.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(51, results.Count);
        Assert.Equal(
            "a28c5a59b66c973e782638089dee02710747f76025a7cfa380584a6062562a26",
            results[
                "behavior:card:01043b:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal(
            "d5468b4ec7184a46336d38d3a9d478df505e3b029498f23d0f9abe37cbc0787c",
            results[
                "behavior:card:01043c:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal(
            "91be3dea7c12c3eacf49f433265c1e6e24202da06d8a6cf45109cc2c7d144ced",
            results[
                "behavior:card:01043d:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal(
            "d227e2da9f5490ce815bdc5ff48f817f28184c83a5c744369b9bed4fd6408dee",
            results[
                "behavior:card:01057:play-under-any-player-s-control"].Digest);
        Assert.Equal(
            "9f06da4d9651209b9697cc1d01bab772213f27a9c6936bcfd5dbb2e6a4749f4b",
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
            "e63a34db35f24fa3c08f6d1b42b53c608465b06dfcaf0f664714d264ea9095e2",
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
            "75f294e1395ead05dc3819fd94ae60f260f188e0ec3b4948bcb3d3815af17026",
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
            "55f512a54aefb56447e6c96eddd9f9d0d5103281bba5ee8a354129c5d79d1397",
            results["behavior:card:01018:spend-x-energy-resources-put-x-energy"].Digest);
        Assert.Equal(
            "c2e7c43beec3fc5fb3641766933b850a168e95a54ec83e44afb67629c75622e0",
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
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/core-keywords.feature::", StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(5, results.Count);
        Assert.Equal(
            "574912182853f6c1c397a3dbb9ab8b146b18cb81a38df3d58871340b52a38875",
            results["behavior:card:01121:surge"].Digest);
        Assert.Equal(
            "60251abe33ca79f359ec800c88a2ea0eb07ef4b9c35d1ee3b15b3f2a7177b117",
            results["behavior:card:01121:put-weapons-runner-into-play-engaged-with"].Digest);
        Assert.Equal(
            "e68bb9edec359b1f737d40d278fa7806d5e5d8a05c7dfd11ab999f8362e2ed8e",
            results["behavior:card:01167:quickstrike"].Digest);
        Assert.Equal(
            "271e4ccdef895fcebee75a00392c0f65ffff163d5c899ecdda9e30446ac38fba",
            results["behavior:card:01040a:retaliate-1"].Digest);
        Assert.Equal(
            "166300f29580506bc7167816dfd86c9edff61e770bd790462d8d420f747db556",
            results["behavior:card:01119:klaw-gains-retaliate-1"].Digest);
    }

    [Fact]
    public void UltronAndroidEfficiencyBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/ultron-android-efficiency.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);

        Assert.Equal(12, results.Count);
        Assert.Equal("14bf07d3dc98972cde907cce6b21dacde5af2a6c3c1e346bb051569cc72ce8ce", results["behavior:card:01144a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("9c036fd1843d99c807d64bd14c5639bee76916422d54d56a91a859f075a5b8c4", results["behavior:card:01144a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("39b85d4384ea45b98d2fa462c889c3a59cb7092e285863dbb77fd6544ff818e0", results["behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-1"].Digest);
        Assert.Equal("8e8c6197dba3df98061895a3633fbdf8ab11a1313f90b926ba0cdce2c3b800c1", results["behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-2"].Digest);
        Assert.Equal("096d4613217bc5929309c1fb0723b20217e267becf36493d4a8f27bd0a71a0ad", results["behavior:card:01144b:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("58ff8401bcddc55bdf6a0e62a6f44203e3421bf7044bd7c80dc77345024a84d6", results["behavior:card:01144b:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("29f1095eef367e71c9aab57fbb4f23bb198387a3b61c6ce5462b37299cd1b451", results["behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-1"].Digest);
        Assert.Equal("f3ba42b9c596c5651f8a72ab967d313af3d7e5a72dbfb371c38b6981cf8df8d5", results["behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-2"].Digest);
        Assert.Equal("e36585247f82927f4f9d333f76d39a6a7d7761f566ca9c09b9fe8c9e333bab07", results["behavior:card:01144c:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("47f6d9099f8c21b363ac820052eb125d9a7db63885254739af431be5c94f99d3", results["behavior:card:01144c:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("85eca6d9d215f3d432566425f737a8679d718aa0dcecef376b35cd8426be1abb", results["behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-1"].Digest);
        Assert.Equal("f27de204583d55c36d530d8dd157fe6a30b01bea3257329085428ca9402b4e20", results["behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-2"].Digest);
    }

    [Fact]
    public void StandardAndExpertEncounterCardBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/standard-expert-encounter-cards.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);
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
        foreach ((string obligation, string digest) in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void BlackPantherNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/black-panther-nemesis.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);
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
        foreach ((string obligation, string digest) in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void SheHulkNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/she-hulk-nemesis.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01161:place-additional-1-per-hero-threat-here"] = "17a645bd1dfd9c465111c067f375882c187618174dfe1c6f6dab50c445b4e446",
            ["behavior:card:01162:x-is-equal-titania-s-remaining-hit"] = "6d173d028bb9bd26ab4e593df353939cbd4c2d0e54ee0ad642018e4c87740515",
            ["behavior:card:01164:titania-attacks-your-hero"] = "bda60851782214d974dd1b642ee9603d50f5c10cd9076379ae4b1c4046bad238",
            ["behavior:card:01164:if-titania-did-not-attack-heal-all-condition-met"] = "80304606fb01c765d902a1bc385f8ba80446b399179b523548be79a366387c3b",
        };

        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest) in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void SpiderManNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/spider-man-nemesis.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);
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
        foreach ((string obligation, string digest) in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void IronManNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/iron-man-nemesis.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);
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
        foreach ((string obligation, string digest) in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void CaptainMarvelNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/captain-marvel-nemesis.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01176:place-additional-1per-hero-threat-here"] = "a405a92b881813420d52ec1a41b0adccddc7a4dd6286627b5540130ef30e9b1b",
            ["behavior:card:01177:after-yon-rogg-attacks-place-1-threat"] = "397b4fb6abcc7a0bbbe4222896c5e07517bf6a4dfcd21c52775ca0dd79663288",
            ["behavior:card:01178:surge"] = "761ab7ef51a96cc51dd150c6da3dce21aa92972fcddf246728e796aba8dab9f4",
            ["behavior:card:01179:discard-each-energy-resource-from-your-hand"] = "6f065b11fd459bd9d39c5163880ec40cd8b73b5b654a473427ac5f8e76814ff0",
            ["behavior:card:01179:if-you-discarded-no-cards-way-card-condition-met"] = "bdf8f3d25f3040e23466b097be9c37a3d482066860ec0db87aa27676aa20c92c",
        };

        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest) in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }

    [Fact]
    public void CoreModularNemesisBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All
            .Where(candidate => candidate.Scenario.StartsWith(
                "specs/behavior/core/core-modular-nemesis.feature::",
                StringComparison.Ordinal))
            .ToDictionary(result => result.Scenario, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["specs/behavior/core/core-modular-nemesis.feature::The first player breaks an encounter attachment target tie"] = "65900b85a34453a4992c5cdb7d78d67166cb12cb5c7b6d40b98010f5e39b2edc",
            ["specs/behavior/core/core-modular-nemesis.feature::Legions of Hydra finds Madame Hydra before counting Hydra enemies"] = "f17df5aa6684d20eff1dd1011dd7d0b369488d6025ef4782dbc919ee2e5da5b0",
            ["specs/behavior/core/core-modular-nemesis.feature::Legions of Hydra counts every Hydra enemy already in play"] = "3be9b030dd0e432991d31969ff3619c08cb2e4e09b4efea211b0c3e4a92e8ffe",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra places threat after attacking"] = "87e5113dbdfc9b2721c270620eea1a6a89bfbd16eddd712e78892879ee783208",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra chooses one of two Legions schemes for her threat"] = "5c77130191f777c6f2395c94b499b538e0483d4afc5cb635df13bbb18134b76d",
            ["specs/behavior/core/core-modular-nemesis.feature::Madame Hydra places threat after scheming"] = "19df77dfb688c0994eecdaab6cba191bb29d3a38144cb721b755e69476b22cd6",
            ["specs/behavior/core/core-modular-nemesis.feature::The Doomsday Chair finds M.O.D.O.K. outside play"] = "6f9cacfb78e8140a95d019ba1c8e11b0d782bc2a48a0f10b10fc7812aa5bfee2",
            ["specs/behavior/core/core-modular-nemesis.feature::The Doomsday Chair does not search when M.O.D.O.K. is in play"] = "646837941474844b69d5de2a3a96a050f38580ddc430edf07c44bea92c1e6bb6",
            ["specs/behavior/core/core-modular-nemesis.feature::M.O.D.O.K. retaliates against an attacking hero"] = "66bd9dc80319e667ff05b617c2f4e7ca8724392141294818958527befff01336",
            ["specs/behavior/core/core-modular-nemesis.feature::Biomechanical Upgrades attaches to the highest printed hit points and surges"] = "c475ca3a6a440e1eb2644e8fa651fdaed063a30029abb7d389107d53b93b4f9f",
            ["specs/behavior/core/core-modular-nemesis.feature::Biomechanical Upgrades can attach to a facedown Drone"] = "2e5b4236db564a60d1f739b1665ac1e6dbd34312525cd2f94db08fc18294f45e",
        };

        Assert.Equal(expected.Count, results.Count);
        foreach ((string scenario, string digest) in expected)
        {
            Assert.Equal(digest, results[scenario].Digest);
        }
    }
}
