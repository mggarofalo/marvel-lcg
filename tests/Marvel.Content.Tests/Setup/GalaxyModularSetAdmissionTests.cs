using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

// Galactic Artifacts, Kree Militants, Menagerie Medley and Space Pirates are modular sets of
// The Galaxy's Most Wanted (ruling 408decdf6cd8c4b5), each admitted on its own. Crystal Ball
// (16130) is unauthored, so Galactic Artifacts stays closed.
public sealed class GalaxyModularSetAdmissionTests
{
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    public static TheoryData<string> Admitted => new() { "kree_militant", "menagerie_medley", "space_pirates" };

    [Theory]
    [MemberData(nameof(Admitted))]
    public void TheCompleteSetOpensAsAModular(string key)
    {
        var setup = SetupCatalog.Parse(SetupJson);
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);

        Assert.Equal(EncounterSetRole.Modular, setup.EncounterRole(key));
        Assert.Contains(key, result.Setup.EncounterSetNames);
        Assert.All(GalaxyModularSetAdmission.Faces(key), face => Assert.Contains(face, result.Playable));
    }

    [Fact]
    public void TheRecipesHoldTheSnapshotsPhysicalQuantities()
    {
        var setup = SetupCatalog.Parse(SetupJson);
        Assert.Equal(["16131", "16132", "16132", "16132", "16133", "16133", "16134", "16134", "16134"], setup.EncounterSet("kree_militant"));
        Assert.Equal(["16135", "16135", "16135", "16135", "16136", "16136", "16136", "16137", "16137"], setup.EncounterSet("menagerie_medley"));
        Assert.Equal(["16138", "16139", "16139", "16139", "16139", "16140", "16141", "16141"], setup.EncounterSet("space_pirates"));
        Assert.Equal([.. Enumerable.Range(16122, 9).Select(code => code.ToString(System.Globalization.CultureInfo.InvariantCulture))],
            setup.EncounterSet("galactic_artifacts"));
    }

    [Fact]
    public void GalacticArtifactsStaysClosedWhileCrystalBallIsUnauthored()
    {
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);

        Assert.DoesNotContain("16130", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("galactic_artifacts", result.Setup.EncounterSetNames);
        Assert.All(GalaxyModularSetAdmission.Faces("galactic_artifacts"), face => Assert.DoesNotContain(face, result.Playable));
        Assert.True(GalaxyModularSetAdmission.Admits("galactic_artifacts", SetupCatalog.Parse(SetupJson), Cards,
            AuthoredCards.Book.Authored.Append("16130").ToHashSet(StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData("kree_militant", "16131")]
    [InlineData("kree_militant", "16134")]
    [InlineData("menagerie_medley", "16135")]
    [InlineData("menagerie_medley", "16137")]
    [InlineData("space_pirates", "16138")]
    [InlineData("space_pirates", "16141")]
    public void AMissingAuthoredFaceClosesThatSetAlone(string key, string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(value => value != face).ToHashSet(StringComparer.Ordinal);
        AssertClosedAlone(key, SetupCatalog.Parse(SetupJson), Cards, authored);
    }

    [Theory]
    [InlineData("kree_militant", "16133")]
    [InlineData("menagerie_medley", "16136")]
    [InlineData("space_pirates", "16140")]
    public void AMissingOrForeignCatalogFaceClosesTheSet(string key, string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        var rows = json["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card_id"]!.GetValue<string>() == face));
        AssertClosedAlone(key, SetupCatalog.Parse(SetupJson), CardCatalog.Parse(json.ToJsonString()), AuthoredCards.Book.Authored);

        var moved = JsonNode.Parse(CardJson)!;
        moved["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "core";
        Assert.False(GalaxyModularSetAdmission.Admits(key, SetupCatalog.Parse(SetupJson),
            CardCatalog.Parse(moved.ToJsonString()), AuthoredCards.Book.Authored));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("short")]
    [InlineData("foreign")]
    [InlineData("reordered")]
    [InlineData("role")]
    public void AWrongOrMissingRecipeClosesTheSet(string change)
    {
        foreach (string key in new[] { "kree_militant", "menagerie_medley", "space_pirates" })
        {
            var json = JsonNode.Parse(SetupJson)!;
            var sets = json["encounter_sets"]!.AsObject();
            var recipe = sets[key]!;
            var cards = recipe["encounters"]!.AsArray();
            switch (change)
            {
                case "missing": sets.Remove(key); break;
                case "extra": cards.Add(cards[^1]!.GetValue<string>()); break;
                case "short": cards.RemoveAt(cards.Count - 1); break;
                case "foreign": cards[0] = "01109"; break;
                case "reordered": var first = cards[0]!.GetValue<string>(); cards.RemoveAt(0); cards.Add(first); break;
                case "role": recipe["role"] = "scenario"; break;
            }
            var setup = SetupCatalog.Parse(json.ToJsonString());
            Assert.False(GalaxyModularSetAdmission.Admits(key, setup, Cards, AuthoredCards.Book.Authored));
            var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
            Assert.All(GalaxyModularSetAdmission.Faces(key), face => Assert.DoesNotContain(face, result.Playable));
        }
    }

    private static void AssertClosedAlone(string key, SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored)
    {
        var baseline = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);
        var result = RuntimeProductAdmission.Discover(setup, cards, authored);
        var faces = GalaxyModularSetAdmission.Faces(key);
        Assert.DoesNotContain(key, result.Setup.EncounterSetNames);
        Assert.All(faces, face => Assert.DoesNotContain(face, result.Playable));
        Assert.Equal(baseline.Playable.Where(face => !faces.Contains(face)).Order(StringComparer.Ordinal),
            result.Playable.Order(StringComparer.Ordinal));
        Assert.Equal(baseline.Setup.HeroNames, result.Setup.HeroNames);
        Assert.Equal(baseline.Setup.EncounterSetNames.Where(name => name != key), result.Setup.EncounterSetNames);
    }
}
