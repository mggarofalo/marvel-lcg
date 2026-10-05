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
        Assert.Equal("2d3d7d820594937451e87e0def14cbe82106a9fc4af39e85b977816a3c4c1026", results["behavior:card:01043b:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal("8575c8306b6f3bb291b8911b27abaa96e7467642ed3d0ea30523586fc2ded0f1", results["behavior:card:01043c:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal("5d46f0222e65f6ad5e77ba53da68cb01c8bf6cb8cdf548e534fdd74a482d8561", results["behavior:card:01043d:resolve-special-ability-on-each-black-panther"].Digest);
        Assert.Equal("34723119f9a612f9be6c45993a66a6ef6aaa90b478a9f78e23fe7bf41843cc63", results["behavior:card:01057:play-under-any-player-s-control"].Digest);
        Assert.Equal("8486867192254787a9cfe5b016f0ea4ec76f4205690b2c25517ba410225486cf", results["behavior:card:01005:deal-8-damage-enemy"].Digest);
        Assert.Equal("0579c6c5a42c1d884ba69da7b454fcda1dd650dc146c170e861211a17f866c7c", results["behavior:card:01049:move-1-damage-from-your-hero-enemy-condition-met"].Digest);
        Assert.Equal("1a9ccd8ccbf06d01fe6adeca21190dfe148d3eaf4be06bd12607c7d3262da165", results["behavior:card:01046:deal-1-damage-villain-and-each-enemy-condition-met"].Digest);
        Assert.Equal("aa398dadd22e0f5f7d289bb74c77e48ca3b2600674cceb89856f958fbcc42a7c", results["behavior:card:01046:deal-1-damage-villain-and-each-enemy-condition-not-met"].Digest);
        Assert.Equal("2b242580b7adc4bca7ee27d3e262e321396f69820c8476c56f4c9f23b70312e7", results["behavior:rr:cancel.3:published-result"].Digest);
        Assert.Equal("6f3bffcbb266aa74969dd55ce7c81b06db2f5399f641c5835e7fc528baa0e0c3", results["behavior:card:01013:if-you-paid-for-card-using-energy-condition-met"].Digest);
        Assert.Equal("d713c9dfa7cbb326234010475c597d26585d43a27bcfc353274a19614ab5a8f9", results["behavior:card:01013:if-you-paid-for-card-using-energy-condition-not-met"].Digest);
        Assert.Equal("3339211a8bf324f504588ae49d1d1490e879db839849c4c33d3ed53221785a42", results["behavior:card:01022:deal-1-damage-each-enemy"].Digest);
        Assert.Equal("bf5d280b72e08ada96c6430d49dc2b0e1990a246819ce70feff2bebd197c5ffb", results["behavior:card:01054:deal-5-damage-enemy"].Digest);
        Assert.Equal("85ad10ed2a025f7b4a6da9584ebdc3e7cd09bbdbad310a37c72da253ae60e4d2", results["behavior:card:01053:if-you-paid-for-card-using-physical-condition-met"].Digest);
        Assert.Equal("def15a569df6ce78c1c6cf98cbe2cc551db07a10d6506fc345a9c4d7dd6221ce", results["behavior:card:01053:if-you-paid-for-card-using-physical-condition-not-met"].Digest);
        Assert.Equal("a03619dc8e9f18a4dc7eaa53c6ad0fcd4f4ebf5a5d75c2af61c8b60456943750", results["behavior:card:01023:choose-and-discard-up-5-cards-from-minimum"].Digest);
        Assert.Equal("bc357c331453eca2c6dfb43a15481f2d265f0bc5e18e96cb84c6902d727169c3", results["behavior:card:01023:choose-and-discard-up-5-cards-from-intermediate"].Digest);
        Assert.Equal("db7eeb85635a20ea55a896cd948893ff9791e7378c0fc3a86b483ea85929c410", results["behavior:card:01023:choose-and-discard-up-5-cards-from-maximum"].Digest);
        Assert.Equal("2a7ba5f8dc8f951880d482e36ed6879714f0714fccf035b3c7b3126a9beaaf75", results["behavior:card:01056:uses-3-attack-counters"].Digest);
        Assert.Equal("bddd29e4a690d0b4c73b52054b8c239fd32ccc4d4cc5344608f67092ee7f0d87", results["behavior:card:01064:uses-3-snoop-counters"].Digest);
        Assert.Equal("2ac062d8b713f80d9784363d996afb412ca15c97094604db60a5f604d9d246f6", results["behavior:card:01080:uses-3-medical-counters"].Digest);
        Assert.Equal("7f10f5ed7c08c18b5a941332b17dd1751afafd56910575896e48e3ade93f1254", results["behavior:card:01087:deal-3-damage-enemy"].Digest);
        Assert.Equal("99ae6ae1a8dc8440646eadf2fd602f7b27a0f0dfe0d0efe0daa400c05ff0e66d", results["behavior:card:01030:exhaust-war-machine-and-deal-2-damage"].Digest);
        Assert.Equal("a0756f3a9c1e739a0962d73d9e36f90faeb841e0f36c2cb232a2b72b0299b1a2", results["behavior:card:01027:exhaust-focused-rage-and-take-1-damage"].Digest);
        Assert.Equal("c1454d116abd1e2b542f09c6a2d83d38fde64b10f2819352bcf6f37cfb79ef06", results["behavior:rr:cost.12:damage-prevented"].Digest);
        Assert.Equal("dd9a5a6f13c366191eb6fea29824ac9d66a497809c4113056fff76cbb2786e16", results["behavior:rr:ability.3:requires-valid-target"].Digest);
        Assert.Equal("9c07a4db24ca1f4c60fb550233202b6d37c8189fb3e7835bc22ccd4465ae3ae3", results["behavior:rr:ability.2:in-play-player-card-ability"].Digest);
        Assert.Equal("35652d532d579d1d65df57ffb5eb611a94547ccbfbd47cdaefa2b113f49d1abc", results["behavior:rr:ability.13:hero-form-required"].Digest);
        Assert.Equal("bd69a7f34d91755879bf226d15055d9e38aff504ad31d04a7c8fe5a7ee4187e5", results["behavior:card:01018:spend-x-energy-resources-put-x-energy"].Digest);
        Assert.Equal("6d28c1d9f4102bfa9842e82d0a9849b2db4451d1da3039387c354b46f9fc6eeb", results["behavior:card:01018:below-damage-cap"].Digest);
        Assert.Equal("d97071f0378d04f90388971c8219ca8ca7f6e407e5e0ac23a6b9292fde4fee43", results["behavior:card:01018:at-damage-cap"].Digest);
        Assert.Equal("212721d5b8c5dee7aab8e5082313330cfc14140b0b8e7da2162156029dd0609b", results["behavior:rr:max-maximum.3:published-result"].Digest);
        Assert.Equal("98696f372cca0fc452aacadebc060c11e69413ab716376a03dd3ded31ab6a3c3", results["behavior:card:01010a:spend-energy-resource-and-heal-1-damage"].Digest);
        Assert.Equal("aa4e7a9c6cf0a8121ad187052c45c7bf4d7da5bcd0bcb2de6e10d618c15ffb8d", results["behavior:card:01071:pay-printed-cost-ally-in-any-player"].Digest);
        Assert.Equal("fde4a3aa5c2d43424f5b538dbfa0b07def3b72f7581ecf9785152f5baeed1dbf", results["behavior:card:01008:exhaust-web-shooter-and-remove-1-web"].Digest);
        Assert.Equal("dcb9c0ec2bb0000fb1593ac982e6d36d1b9019e036a768232430f7708962feef", results["behavior:faq:01071:power-of-aspect-pays-printed-ally-cost"].Digest);
        Assert.Equal("9ddc8409a7e222765184d393b2c7a758c833b7547dcf12b1539e82432ef317e1", results["behavior:card:01012:then-if-you-have-aerial-trait-remove-condition-not-met"].Digest);
    }

    [Fact]
    public void TriggeredKeywordBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/core-keywords.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(5, results.Count);
        Assert.Equal("d93fff29299b8934a2719c81f8edc2b89ddecdddbda309446b703b3ddcb8308e", results["behavior:card:01121:surge"].Digest);
        Assert.Equal("4cf6bd80fe975ddd7a12b0f0ee5c77fe233eb22398156f216c6afae8af1c60ca", results["behavior:card:01121:put-weapons-runner-into-play-engaged-with"].Digest);
        Assert.Equal("8013e072e668b544997fcccc544f11f206232988d22da4657b4c6cc894cf7918", results["behavior:card:01167:quickstrike"].Digest);
        Assert.Equal("5c46e66cc5ad4feffbb644dd08abcac7313c6956f02d49ba227c3273f5e5ced1", results["behavior:card:01040a:retaliate-1"].Digest);
        Assert.Equal("13336473ca0fd885d03db4506820a9f3cca89d66af2cd164977d438efdb52ca0", results["behavior:card:01119:klaw-gains-retaliate-1"].Digest);
    }

    [Fact]
    public void UltronAndroidEfficiencyBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-android-efficiency.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(12, results.Count);
        Assert.Equal("36547a038fd66ac38dd67caaf67c136a518f8b94ed77bd9e7ebe52ce02121d3e", results["behavior:card:01144a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("08925f836d7264eecf1beca4487622723dbde54a5d7d482531641a7ba352b5e5", results["behavior:card:01144a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("47db26b8bca4b40152c98cd08b0cd47b03684f1e7e8f8250b1994b4f8faefc2d", results["behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-1"].Digest);
        Assert.Equal("2643b65ad1c6bb9f03f3693a60ce21af89d4fcbf86b3f211b6b0b442f2296f34", results["behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-2"].Digest);
        Assert.Equal("f97bef5c61fc6fc2755c5a2c6154bec71a1c3de33816559524d23d4425d0909d", results["behavior:card:01144b:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("7c22e1cf869c1bcd65b4360e9aeec75f0b095eb0fc35d3f02ef95359888165a2", results["behavior:card:01144b:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("d054bef849790ffc79fa63abc4b1a90eea20237a048ff22fa7ce3d7534579070", results["behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-1"].Digest);
        Assert.Equal("54e479150523d33e621bf1b540b9dee47b2f09a7415192c066a4ac28ae9b356f", results["behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-2"].Digest);
        Assert.Equal("f35e35c9d4010ad06ff98a40fd19fcd1b8996a5fb31496da09ed3ef221d4642d", results["behavior:card:01144c:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("85f786f534dbc9e1e667d026be414ea141fb13e8c8f9dfccc15cc927cda853db", results["behavior:card:01144c:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("961f226c8a259582212b0124f92bace781c7c482238e58f0342ead58a3b832ea", results["behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-1"].Digest);
        Assert.Equal("51c053e21004c18a61b9a81756008f7e96025780974fd1872b2686f835e3a589", results["behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-2"].Digest);
    }

    [Fact]
    public void StandardAndExpertEncounterCardBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/standard-expert-encounter-cards.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["behavior:card:01186:villain-schemes"] = "b1da1eb67dcc27f433858e9ea34198caa01090f61121398c27309edef8cf7246",
            ["behavior:card:01187:card-gains-surge"] = "ad124b57050dcd45e6c4fb108d87abd327b9a6d733dc81780f8240d18548e7b8",
            ["behavior:card:01187:villain-attacks-you"] = "436863b54235e2ee34511d9c8bd770fea02ecdb0548818ad787e68846fcd7c38",
            ["behavior:card:01188:if-no-cards-were-discarded-way-card-condition-not-met"] = "19bfeb6e63526d11f2b454ca738a6229bf8140ca44458e72e7cb1c574a3db796",
            ["behavior:faq:01036:published-clarification-1"] = "d5cd181b55764a8beda6fc96a920fdcf1ad91191bf40563d41907ffbc18ec201",
            ["behavior:faq:01039:published-clarification-1"] = "dabb90fb62dd1e29b083de59eae70ee2d7d3cdd7a61052f1425b23aba030e8e7",
            ["behavior:card:01189:card-gains-surge"] = "887e1ba7fe97bd34d9ddc384184e46595a2b593b1fb3520fa087202abc3eb9d8",
            ["behavior:card:01189:villain-and-each-minion-engaged-with-you"] = "dd3644cc7e8afa77208893e403b19843c3fd378b23c072d2800dbb02712ef776",
            ["behavior:card:01190:reveal-your-set-aside-nemesis-minion-and"] = "1c2391c78e5c12cc1ac9137290dff08789c899fde86203af1a8f04062fa11cb5",
            ["behavior:card:01190:if-your-nemesis-minion-does-not-enter-condition-met"] = "0174c4151e80375dcad55e7c67d860230d52aa1d2d763b66978bf24cc2e052d6",
            ["behavior:card:01192:place-4-threat-on-each-side-scheme"] = "21536d1d1943c06533abfa56423e2f3176cd4f85adfb2f2d01f2a12cf3004f8a",
            ["behavior:card:01192:if-there-are-no-side-schemes-in-condition-met"] = "124a799bf838121982143a9b8c0b917214645d0f14bb7af2fdd452b91d22d0cc",
            ["behavior:card:01193:surge"] = "c070e527f94c1d8120da4b5b29c7736b9027f6c46a3033428ffe762f23d6b189",
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
            ["behavior:card:01157:killmonger-cannot-take-damage-from-black-panther"] = "ab3cdc4e572ff3318ca091d726c8ea107bfac2847a1cc101359853c86d72aa4f",
            ["behavior:card:01158:surge-after-card-resolves-reveal-1-additional"] = "f522be5a8f591cc4579049d974a8cd26daaad53f901b569d6f06ca030dcf4d9d",
            ["behavior:card:01158:give-villain-tough-status-card"] = "e2736d6346e383b1828be3ef4c58949561c7413a634499a56b7be7cf36eaa974",
            ["behavior:card:01159:discard-top-card-encounter-deck"] = "08e39ecc3e264473a8af253c9eb3a7b72ef96f17e3fba22398ac181856802138",
            ["behavior:card:01159:then-choose-either-deal-x-damage-your-choice-2"] = "fb3789acff1a3be2185de2f6f142db57fa095e93c6c919f5781d8d5031bf1921",
            ["behavior:card:01159:x-is-1-more-than-number-boost"] = "2929179a2ae936c07a1abe7b1e1b6d04940b5dd22c10b2683b55989c36ceb70a",
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
            ["behavior:card:01161:place-additional-1-per-hero-threat-here"] = "03b317a3f084edabbaf9e427f812f23b9be3036445a78f7c3b17512cae3b1ecb",
            ["behavior:card:01162:x-is-equal-titania-s-remaining-hit"] = "1e36745012d50263720f109b780cc0f277052627bb86ea6a073825beed930010",
            ["behavior:card:01164:titania-attacks-your-hero"] = "ef327bb4e4c77500a73e8d27fb9c7f62efd5a86ced33d548f90131a1a7e8cfbd",
            ["behavior:card:01164:if-titania-did-not-attack-heal-all-condition-met"] = "87c56c648d8cc245b4ce5215c920e8e62c8683e3b05912c1b2593f8dde984862",
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
            ["behavior:card:01166:each-player-places-random-card-from-their-one-player"] = "f279399465f01ae100a580d6986be086a4d16c7d137d068a427df14c281f8e1a",
            ["behavior:card:01168:stun-your-hero"] = "ae4a18079bbdd55241cd2f0c1829780134f11aad9e3bc4bfd270cfde346e1d68",
            ["behavior:card:01168:if-vulture-is-in-play-card-gains-condition-met"] = "4dfedd06fe0ccac146f152d0729604614bf7327348e3389b6fcfd523d49175e2",
            ["behavior:card:01168:if-activation-deals-damage-friendly-character-stun-condition-met"] = "764a3486b3eb570000635c7ee9710a250fb108bc6d985c57b7e0d2af6d7f6ce3",
            ["behavior:card:01168:if-activation-deals-damage-friendly-character-stun-condition-not-met"] = "5810769d709af6629b0af70d33f2d674a8647db9e6dc12854764045f6307c31c",
            ["behavior:card:01169:discard-1-card-at-random-from-each-one-player"] = "f9fa46de87d58559410fadda4c9491d0793bc7427546e2289e050334288b0bea",
            ["behavior:card:01169:discard-1-card-at-random-from-each-multiple-players"] = "05928f75e6ff913224175a61eb46861cf066cb832634423ba236b5f24722515f",
            ["behavior:card:01169:place-1-threat-on-main-scheme-for-zero"] = "060079870b4cbb9fa212009fe875e12816d955a96e9ee70e53cd397941a9edc5",
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
            ["behavior:card:01171:place-additional-1-per-hero-threat-here"] = "bcef98902124335f30016f54997e3e1201be351bb16a20ac75eff066bc606291",
            ["behavior:card:01172:retaliate-1-after-character-is-attacked-deal"] = "6d3d896480b754b8fab28862b721bb15d8401e68fbc2d4e9c5b0bb01a75848f5",
            ["behavior:card:01173:choose-either-deal-1-damage-your-hero-zero"] = "2fb2e14ede939c8d462804d6d0cc2fc24a12ace488f2c856dfa3a0930ce814ab",
            ["behavior:card:01173:choose-either-deal-1-damage-your-hero-choice-1"] = "a8b181f3997718f9ee3a25ee3a02a6691d005bca1d299aea8bc555aab9418ddc",
            ["behavior:card:01173:choose-either-deal-1-damage-your-hero-choice-2"] = "761e0055eaf165028419bc2121ca7b7b8c58928101cdd93cc7b2fefa33c56b46",
            ["behavior:card:01173:if-villain-is-making-undefended-attack-choose-condition-met"] = "8418677a949054e5c94f1b9ab9488c4a01db8f9084e5f040b3c39d2b78062ca8",
            ["behavior:card:01173:if-villain-is-making-undefended-attack-choose-condition-not-met"] = "dadaf3509e9ae7a8299f9af35534320d93814c9f10accb6a9d9d9847b4b61a9e",
            ["behavior:card:01174:each-player-discards-top-5-cards-their-one-player"] = "6176e59d48702f303bae6d979b2f63592d33b9ddbd616143a49d0d867de7dd48",
            ["behavior:card:01174:for-each-printed-energy-resource-player-discards-one"] = "e5b88439f5802e246ee96f3777a14112554e03b51abece8cbc5991591f131c42",
            ["behavior:card:01174:for-each-printed-energy-resource-player-discards-multiple"] = "1e213024f17615152bae3901e0ad8dbb5cef86d957d7154e9642ad7fe2f66cf1",
            ["behavior:card:01174:each-player-discards-top-5-cards-their-multiple-players"] = "dfefbcb52c318f566ced08b9644ad10dbbfd5f59f027f1d33b8530499f205702",
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
            ["behavior:card:01176:place-additional-1per-hero-threat-here"] = "74993aa12c9eaefff12db6e700c081414e71c22076dadd36a265722e3de379f0",
            ["behavior:card:01177:after-yon-rogg-attacks-place-1-threat"] = "10721377c3a37d383936468581b6571e604e2e86dd790690d85ebc74db5ab956",
            ["behavior:card:01178:surge"] = "a4b15eaeaa4a4107ba82fea8119a474b1d297c5a66eab0d4351fc30d6de13048",
            ["behavior:card:01179:discard-each-energy-resource-from-your-hand"] = "5f34e65fe8c403e467a35a2a2dae4da4402d3718490852ea6e364eac574598ae",
            ["behavior:card:01179:if-you-discarded-no-cards-way-card-condition-met"] = "e45003f9466a7985d8a26fb1d9ddcf8e3d3aa7098004cd2fa903324cea666e83",
        };
        Assert.Equal(expected.Count, results.Count);
        foreach ((string obligation, string digest)in expected)
        {
            Assert.Equal(digest, results[obligation].Digest);
        }
    }
}
