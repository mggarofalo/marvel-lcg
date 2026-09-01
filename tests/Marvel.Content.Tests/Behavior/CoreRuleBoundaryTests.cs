using System.Text.Json;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreRuleBoundaryTests
{
    [Theory]
    [InlineData("Alliance", "alliance")]
    [InlineData("Amplify", "amplify-icon")]
    [InlineData("Assault", "assault")]
    [InlineData("Hinder", "hinder-x")]
    [InlineData("Incite", "incite-x")]
    [InlineData("Linked", "linked-card-title")]
    [InlineData("Patrol", "patrol")]
    [InlineData("Peril", "peril")]
    [InlineData("Permanent", "permanent")]
    [InlineData("Requirement", "requirement-resources")]
    [InlineData("Stalwart", "stalwart")]
    [InlineData("Steady", "steady")]
    [InlineData("TeamUp", "team-up")]
    [InlineData("Teamwork", "teamwork")]
    [InlineData("Temporary", "temporary")]
    [InlineData("Victory", "victory-x")]
    [InlineData("Villainous", "villainous")]
    [InlineData("Vulnerable", "vulnerable")]
    public void ExpansionOnlyKeywordAttributesStayOutsideTheCoreContract(
        string attribute,
        string ruleFamily)
    {
        using JsonDocument cards = Cards();
        Assert.DoesNotContain(CoreCards(cards), card =>
            card.GetProperty("attributes").TryGetProperty(attribute, out _));

        AssertFamilyOutsideCore(ruleFamily);
    }

    [Theory]
    [InlineData("indirect damage", "indirect-damage")]
    [InlineData("piercing", "piercing")]
    [InlineData("ranged", "ranged")]
    public void ExpansionOnlyRulesWithoutCardAttributesStayOutsideTheCoreContract(
        string printedPhrase,
        string ruleFamily)
    {
        using JsonDocument cards = Cards();
        Assert.DoesNotContain(CoreCards(cards), card =>
            (card.GetProperty("text_plain").GetString() ?? "").Contains(
                printedPhrase, StringComparison.OrdinalIgnoreCase));

        AssertFamilyOutsideCore(ruleFamily);
    }

    private static void AssertFamilyOutsideCore(string family)
    {
        using JsonDocument catalog = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryPaths.Root, "specs", "behavior", "catalog.json")));
        var sources = catalog.RootElement.GetProperty("sources").EnumerateArray()
            .Where(source =>
            {
                string id = source.GetProperty("id").GetString()!;
                return id == $"rr:{family}"
                    || id.StartsWith($"rr:{family}.", StringComparison.Ordinal);
            })
            .ToList();

        Assert.NotEmpty(sources);
        Assert.All(
            sources.SelectMany(source => source.GetProperty("obligations").EnumerateArray()),
            obligation => Assert.NotEqual(
                "executable", obligation.GetProperty("disposition").GetString()));
    }

    private static JsonDocument Cards() => JsonDocument.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static IEnumerable<JsonElement> CoreCards(JsonDocument cards) =>
        cards.RootElement.GetProperty("cards").EnumerateArray()
            .Where(card => card.GetProperty("pack").GetString() == "core");
}
