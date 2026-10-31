using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class KangModularSetupTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static readonly string[] Keys = ["temporal", "anachronauts", "mot"];

    public static IEnumerable<object[]> Recipes() =>
    [
        // Authority: pack:mc11:adjustable-difficulty names the three modular sets;
        // the pinned MarvelSDB pack/toafk_encounter.json supplies the quantities.
        ["temporal", "Temporal", new[] { "11030", "11030", "11030", "11031", "11031", "11032", "11033" }],
        ["anachronauts", "Anachronauts",
            new[] { "11040", "11041", "11042", "11043", "11044", "11045", "11045", "11046", "11046" }],
        ["mot", "Master of Time",
            new[] { "11047", "11048", "11048", "11049", "11049", "11050", "11051", "11051" }],
    ];

    public static IEnumerable<object[]> SetKeys() => Keys.Select(key => new object[] { key });

    [Rule("rr:modular-encounter-set.3")]
    [Theory]
    [MemberData(nameof(Recipes))]
    public void ModularRecipeMatchesThePrintedSetAndPhysicalQuantities(
        string key, string name, string[] encounters)
    {
        // "A modular encounter set card is designated by the modular encounter
        // set's name ... printed at the bottom of the card": every recorded face
        // carries this set's printed icon, so the recipe is the whole set.
        Assert.Equal(name, Setup.EncounterSetDisplayName(key));
        Assert.Equal(EncounterSetRole.Modular, Setup.EncounterRole(key));
        Assert.Equal(encounters, Setup.EncounterSet(key));
        Assert.All(Setup.EncounterSet(key), face =>
        {
            Assert.Equal("toafk", Cards.Pack(face));
            Assert.Equal(key, Cards.EncounterSet(face));
        });
        Assert.Equal(
            Cards.Faces.Where(face => Cards.EncounterSet(face) == key).Order(StringComparer.Ordinal),
            encounters.Distinct().Order(StringComparer.Ordinal));
    }

    [Rule("rr:modular-encounter-set.1")]
    [Rule("rr:modular-encounter-set.2")]
    [Theory]
    [MemberData(nameof(SetKeys))]
    public void TheDeclaredRecipeFillsAnotherScenariosModularSlotAsAWholeSet(string key)
    {
        // The insert: "All three encounter sets can be included in other
        // scenarios, like the modular encounter sets from the core set." This is
        // the declared composition only; public admission is closed below.
        var dealt = Dealer.DealOrder(Setup, "rhino", ["spider_man"], [key], Cards);

        Assert.Equal(Setup.EncounterSet(key),
            dealt.Where(card => card.Source == CreationSource.EncounterSet
                    && Cards.EncounterSet(card.Spec) == key)
                .Select(card => card.Spec));
        Assert.DoesNotContain(dealt, card => card.Source == CreationSource.EncounterSet
            && Cards.EncounterSet(card.Spec) == "bomb_scare");
    }

    [Fact]
    public void AdmissionStaysClosedEvenWhenEveryFaceIsAuthored()
    {
        // The Once and Future Kang is outside the accepted product boundary.
        // Supplying every printed face as authored must not open these sets.
        var admitted = RuntimeProductAdmission.Discover(Setup, Cards,
            Cards.Faces.ToHashSet(StringComparer.Ordinal));

        Assert.All(Keys, key =>
        {
            Assert.Contains(key, Setup.EncounterSetNames);
            Assert.DoesNotContain(key, admitted.Setup.EncounterSetNames);
            Assert.Throws<KeyNotFoundException>(() =>
                Dealer.DealOrder(admitted.Setup, "rhino", ["spider_man"], [key], Cards));
        });
        Assert.DoesNotContain(admitted.Playable, face => Cards.Pack(face) == "toafk");
        Assert.Contains("kree_fanatic", admitted.Setup.EncounterSetNames);
        Assert.Contains("bomb_scare", admitted.Setup.EncounterSetNames);
        Assert.DoesNotContain("kang", admitted.Setup.CampaignNames);
    }
}
