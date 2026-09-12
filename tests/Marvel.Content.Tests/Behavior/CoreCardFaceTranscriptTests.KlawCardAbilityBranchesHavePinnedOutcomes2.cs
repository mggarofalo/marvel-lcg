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
        Assert.Equal("74977959d3ac2dfd74920cc8f0d69710a890f57f931e64629d02c1c656c095f2", results["behavior:card:01114:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("6f534db787710e0ca4552d994de3ee0590186ffd2ea1498dff8359f6bece2985", results["behavior:card:01114:when-klaw-attacks-give-him-1-additional"].Digest);
        Assert.Equal("58199dccd3d5857dfd05ccbd3aa39266b1440de0c8f2f296d7fff086933beff1", results["behavior:card:01115:toughness"].Digest);
        Assert.Equal("4f2a1589b6867a0c09fbe1c5719694dfb037fd47d40b98b878a5d9a4309afe1f", results["behavior:card:01115:when-klaw-attacks-give-him-1-additional"].Digest);
        Assert.Equal("27ad3c1592156f96a9eb02848083668269d338f2fef86e221fe0b25ae879db9f", results["behavior:card:01118:attach-klaw"].Digest);
        Assert.Equal("6bfe917e81e62df564f9bf54cf28b810000943520caf18e002ca92d2cde8273e", results["behavior:card:01119:attach-klaw"].Digest);
        Assert.Equal("6b1c4cb8cf587e8da881736c299cef41a13fc6f81eaa5d194c3b081d5a482201", results["behavior:card:01125:place-additional-1-per-hero-threat-here"].Digest);
        Assert.Equal("a664274835e5a11a5319eb39ebf6edb4b8126f2e953378ea735867a86fc45752", results["behavior:card:01126:place-additional-1-per-hero-threat-here"].Digest);
        Assert.Equal("a3c8a74ed92a8f8467df642172275878abe4b18b98e0a51c3a1fe7440c6d5006", results["behavior:card:01127:klaw-gets-10-hit-points"].Digest);
        Assert.Equal("cfce6c3697ebaa86705398932303530c9faf120ec77f2b71fbb5d68797a52bd1", results["behavior:card:01122:discard-1-card-at-random-from-your"].Digest);
        Assert.Equal("56cefa785c7f43c551641107ebc7427fac5889ef2748a0107f9290d482cf72a5", results["behavior:card:01122:klaw-attacks-you"].Digest);
        Assert.Equal("7008941842fd2393a2b0e7dc7638af1f5d5f80750755dd8b50c7b1f96b07a080", results["behavior:card:01122:if-attack-deals-damage-place-1-threat-condition-not-met"].Digest);
        Assert.Equal("dadf0d6e7f57439ccc2b63c9bfefa1e21375b7c612a75b68a18381c3e3a25d4c", results["behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-1"].Digest);
        Assert.Equal("1e9b2b1eb0795172f46bde4447a9a96cc092a018d4f6cf171ae6c3f5bb76bc85", results["behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-2"].Digest);
        Assert.Equal("1622f7fb206461f3762d75b0cc452f96a35061ac7f158227102f714bf7135bbf", results["behavior:card:01124:klaw-heals-4-damage"].Digest);
        Assert.Equal("cf9a3a1cde38db8b1d36e2a732b1ad69ce529c8bf024ce3c01e19abb8201e78a", results["behavior:card:01124:if-no-damage-was-healed-way-card-condition-met"].Digest);
        Assert.Equal("d7796479e9a5aac10f1b4bba5df7c755b04e856257be3b73be1425a757702fb6", results["behavior:card:01124:take-2-damage"].Digest);
        Assert.Equal("31bb61ff7897ab1d8a38b6905c27d5d780c8dcaf51f045b47aa6fe13dcb8db93", results["behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-met"].Digest);
        Assert.Equal("e08e3c635f378cd4fd0f5d6e265607566d58c4416823daf07f5cd72c7696c5b2", results["behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-not-met"].Digest);
        Assert.Equal("bb9d773d691c38e0ff9211c388686935de36ee7341c3e725a5534f7774e5215b", results["behavior:card:01128:discard-cards-from-encounter-deck-until-masters"].Digest);
        Assert.Equal("c35146a5397b4034c493e78e15c94dd49a8884b8b1a559461c07c2b86facf4a2", results["behavior:card:01129:after-radioactive-man-attacks-you-discard-1"].Digest);
        Assert.Equal("9e1ed1fc3f66d7b1227715ff0e289c886647d8a9f562a78f4ed4e8b04c872dc4", results["behavior:card:01129:discard-1-card-at-random-from-your"].Digest);
        Assert.Equal("ea8fedaa373dcf704c6249d7181a6799b0f40518e4639019a75a782a2c444982", results["behavior:card:01130:when-whirlwind-attacks-you-also-resolve-his"].Digest);
        Assert.Equal("c8e2c63433a71edfabda4e55193c91ddd462c43fc696edbd64380893adfc3195", results["behavior:card:01130:deal-1-damage-each-hero"].Digest);
        Assert.Equal("15a6c845cc19cfce07bcfd677ece17366490f1100c0a9e4b5a79f90ea74e2878", results["behavior:card:01131:after-tiger-shark-attacks-give-him-tough"].Digest);
        Assert.Equal("c3854c9132ba950b259a65bab78916c149350a3a62f11163fa3460ea3ce1f9df", results["behavior:card:01131:give-villain-tough-status-card"].Digest);
        Assert.Equal("365f216dc1433c3063a922d5f04ab0b11ce16f8274184b89ade052e3ff490734", results["behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-met"].Digest);
        Assert.Equal("dd4db31fdb006158f9b683e0777f91b715738b6521bbe2937101acab4ad3fec7", results["behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-not-met"].Digest);
        Assert.Equal("87408c8683fbd60fa254b2831f55f390962a495af27d1f9850ea0a42f8e07035", results["behavior:card:01132:exhaust-each-ally-you-control"].Digest);
        Assert.Equal("3b8713204120580617e6203f3714b23556716066848663f3579f0e78b6873963", results["behavior:card:01133:each-masters-evil-minion-attacks-hero-it"].Digest);
        Assert.Equal("5dd63a1b5b262081b712eedc4f64ba0ff70a52bd34716cf86a6f56d174bf3fb9", results["behavior:card:01133:if-no-attacks-were-made-way-search-condition-met"].Digest);
    }

    [Fact]
    public void UltronAttachmentBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-attachments.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(8, results.Count);
        Assert.Equal("d47348ca653b6a5fdbc3eeefb922a6c0819d5cb5e3eeaf58c9b36d41ca43023a", results["behavior:card:01141:attach-ultron"].Digest);
        Assert.Equal("fd743efb40ef3ab7fa472ef6c1b5abd1420f345a7d0b8ebe62fc2403ad15999f", results["behavior:card:01141:after-ultron-schemes-place-1-threat-on"].Digest);
        Assert.Equal("9f892b0fa86bb594301459256e76651d183d9e1935550b29ef96eed266241b23", results["behavior:card:01142:attach-ultron-drones-environment"].Digest);
        Assert.Equal("c684041c13b6c765501c2d87fae29b1a0143853ce4d52b740b8042146705eeac", results["behavior:card:01142:each-facedown-drone-minion-gets-1-atk"].Digest);
        Assert.Equal("37cdc0f822ae81c74b505a8ac23bedfb6047b7f3937fad7a10d13f5b5f204ade", results["behavior:card:01152:attach-villain"].Digest);
        Assert.Equal("b1c54c623200c938d57d291c232e8ef2714e3c3e7c7bbc8d57411ac912391cb1", results["behavior:card:01152:exhaust-your-hero-and-spend-physical-physical"].Digest);
        Assert.Equal("91eb6b7c43cb50554760ac8c94eeca57a623bc81498646128b08319d3cc827e2", results["behavior:card:01153:attach-villain"].Digest);
        Assert.Equal("7947684fdac3d097c67aa1b544cf4a9578574867ebe9fb5e301338d4ff5cc80f", results["behavior:card:01153:exhaust-your-hero-and-spend-energy-energy"].Digest);
    }

    [Fact]
    public void UltronMainSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-main-schemes.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(11, results.Count);
        Assert.Equal("1ed9b2e8d664d216742899ecccecf7143a91544f297173255b19e4c9af0f106a", results["behavior:card:01137b:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("481eb54c792ffc96310fb66f99b52cd87c7fc2005f20434ee3018906027ebe5e", results["behavior:card:01138a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("96349bb0461f1b912c277de872799a9281dabe0d771c5643e4e0ce58a44d893f", results["behavior:card:01138a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("8813d11be4464b4fb09a77a5c79407c0be14bb38ded334b2ead82b4c5cfe0c2e", results["behavior:card:01138b:after-placing-threat-here-during-step-one-choice-1"].Digest);
        Assert.Equal("b29655ea24519eb6c79729ebbab3d58ff237a75a728a370d83c7628c1302bd63", results["behavior:card:01138b:after-placing-threat-here-during-step-one-choice-2"].Digest);
        Assert.Equal("efb913d129dfda89fba118d8bd564f8560f964c85aa43a03c6c102491fdea11f", results["behavior:card:01138b:after-placing-threat-here-during-step-one-multiple-players"].Digest);
        Assert.Equal("1b611b59c6739b2390bf13014ff63bd11aa14716efcc2b9d8e58381a9c21af99", results["behavior:card:01139a:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("bdc625bced855385ba07a91e548262db258348b125018474b72f97bbed3b8204", results["behavior:card:01139a:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("2a916c677a5dc8e974b9a7d871416d606f58c086ff443d1cce59d8fa65f75abf", results["behavior:card:01139b:threat-cannot-be-removed-from-scheme"].Digest);
        Assert.Equal("65dd8b02e0eff5f1b2401de90f58635f8f34ceffc6e84f1cb30cd5070bd39649", results["behavior:card:01139b:if-stage-is-completed-players-lose-game-condition-met"].Digest);
    }

    [Fact]
    public void UltronSideSchemeBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-side-schemes.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(6, results.Count);
        Assert.Equal("b2d267387f8aade24f697235f414353116c2d29dba3cef89511e6c3002527a01", results["behavior:card:01148:each-player-puts-top-card-their-deck-one-player"].Digest);
        Assert.Equal("00a373a9decb0150276082f82220269d346e4ecf785f592b0d929cfc978c620f", results["behavior:card:01148:each-player-puts-top-card-their-deck-multiple-players"].Digest);
        Assert.Equal("f7cfcdc519d7f9203f77b1bb2c0224dee8f9aff362f1e64b4143511939a6ca02", results["behavior:card:01150:first-player-puts-top-2-cards-their"].Digest);
        Assert.Equal("2ab9bb12c964bda69776e9b7e2b2ef4e8b7ca5a58b4bab428b53e4d63087474b", results["behavior:card:01151:each-player-chooses-either-place-2-threat-one-player"].Digest);
        Assert.Equal("6bb8381b12adfe8f7d18dd2e25630b1d8993ee9f507c88f9fc750cd7b2c06af7", results["behavior:card:01151:each-player-chooses-either-place-2-threat-multiple-players"].Digest);
    }

    [Fact]
    public void UltronVillainAndDroneBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-villain-and-drones.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(9, results.Count);
        Assert.Equal("aaeb948fc666c867361349845c4701e3df2d81868c87e24c152a8a98e9c4d2ea", results["behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-1"].Digest);
        Assert.Equal("de3133968bda6e0964c9435b7d541cf7f7c0480ab4544e7f1ff26d6eb66eef65", results["behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-2"].Digest);
        Assert.Equal("618ee72094549b0dffe92955fb9f7ce1f6bf5360363fc4f205741c024402b8a6", results["behavior:card:01135:when-ultron-attacks-you-put-top-card"].Digest);
        Assert.Equal("bdf5137cb3cb9f8fdb2538ba98c455be0f315d933d5f32bdc769d8041d93f926", results["behavior:card:01135:until-end-his-attack-ultron-gets-1-multiple"].Digest);
        Assert.Equal("4240266093a99be345fd994d782027252bae1efa014e9d24ffb1c68570555c01", results["behavior:card:01136:search-encounter-deck-and-discard-pile-for"].Digest);
        Assert.Equal("d096f36908bd3cea85f07ce5aff7f14b2539698225ff8df4c73583700e38d2a6", results["behavior:card:01140:each-facedown-drone-minion-engaged-with-player"].Digest);
        Assert.Equal("5b6f1601f9f5524d3384cacc6b854b89ebe4505f4b16c91f4f1b73d0df104d3f", results["behavior:card:01143:guard"].Digest);
    }

    [Fact]
    public void UltronTreacheryBranchesHavePinnedOutcomes()
    {
        var results = CoreTranscriptCorpus.All.Where(candidate => candidate.Scenario.StartsWith("specs/behavior/core/ultron-treacheries.feature::", StringComparison.Ordinal)).ToDictionary(result => result.Obligation, StringComparer.Ordinal);
        Assert.Equal(16, results.Count);
        Assert.Equal("c16629fe4acf20f408201ea8fb4a5dd59904d038a5a0a9790d7c28f7ed4aa910", results["behavior:card:01145:ultron-schemes"].Digest);
        Assert.Equal("cea2e995905d36175fa351b5d584293a8309ec4e0037addca64684e994f1b5a6", results["behavior:card:01145:discard-top-card-your-deck-for-each-zero"].Digest);
        Assert.Equal("09147c5858fbcdf4dfe14be1080229550bcf3d9e67cb39c994d5ebdddbe6b281", results["behavior:card:01145:discard-top-card-your-deck-for-each-multiple"].Digest);
        Assert.Equal("875d4f8cefd3ebc57909722fef4eb5f94f5fc15e8d857d37be5b38e0be10671c", results["behavior:card:01145:ultron-attacks-you"].Digest);
        Assert.Equal("e57d9c90aa0da7b119fa579b0d5ad65bec3befbca4f35b89956db040203fa62e", results["behavior:card:01145:discard-top-card-your-deck-for-each-one-2"].Digest);
        Assert.Equal("e0c4d06ae046bbf60899a7b039a298f7260b2c2a9d18ddc953de73ea4b2cab98", results["behavior:card:01145:discard-top-card-your-deck-for-each-multiple-2"].Digest);
        Assert.Equal("2a7e50bdcf1ead787b8dd6b2db5b386171f7b2e59b2a62d16d77b80a638da6e9", results["behavior:card:01146:ultron-heals-2-damage-for-each-drone-zero"].Digest);
        Assert.Equal("4dde25e48109e3c2abe221ae0276bd952cd4aca11d0ec6ddb8429a19741e9c30", results["behavior:card:01146:ultron-heals-2-damage-for-each-drone-one"].Digest);
        Assert.Equal("26bdaec62ac20b30e608ee46f23fc17e2ae5f143dc0eea2821440e5005fc45c1", results["behavior:card:01146:ultron-heals-2-damage-for-each-drone-multiple"].Digest);
        Assert.Equal("526cfdf1ee73f3d75a279e9496e61d51f6b6707ee282b3e2acbf5818035a5358", results["behavior:card:01146:ultron-heals-1-damage-for-each-drone-zero"].Digest);
        Assert.Equal("1c2ca7defbe12ffe14da38b9f3825cd1300b52ee7799dec39460daad4ae48c4f", results["behavior:card:01146:ultron-heals-1-damage-for-each-drone-one"].Digest);
        Assert.Equal("8dfd1de9e5ed5d58f0b5f74920bde1ecd712bf627fd6edc3526cda984116d9b8", results["behavior:card:01146:ultron-heals-1-damage-for-each-drone-multiple"].Digest);
        Assert.Equal("6dd97999c3bcc2c3c1cd0a82758312cb722b53b2d2a956fef145033994661abf", results["behavior:card:01147:each-drone-minion-engaged-with-your-hero"].Digest);
        Assert.Equal("08064ea4e0d9e557ab393de03b46d72f4033c26ff5ac1b4c0b6c1a3f09978b89", results["behavior:card:01147:if-no-attack-was-made-way-put-condition-met"].Digest);
    }
}
