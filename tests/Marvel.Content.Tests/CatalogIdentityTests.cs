using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

/// <summary>Ambiguous identities fail ingestion rather than selecting a record by order.</summary>
public sealed class CatalogIdentityTests
{
    private static readonly string[] SetupSections = ["campaigns", "heroes", "encounter_sets"];

    [Theory]
    [InlineData("First", "Second")]
    [InlineData("Second", "First")]
    public void DuplicateCardIdentitiesAreRejectedRegardlessOfRecordOrder(string first, string second)
    {
        // Synthetic input isolates our ingestion policy from printed game rules.
        string json = $$"""
            {"cards":[{"card_id":"duplicate","name":"{{first}}"},
                      {"card_id":"duplicate","name":"{{second}}"}]}
            """;

        var error = Assert.Throws<JsonException>(() => CardCatalog.Parse(json));

        Assert.Contains("duplicate card identity 'duplicate'", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("campaigns", "campaign")]
    [InlineData("heroes", "hero")]
    [InlineData("encounter_sets", "encounter set")]
    public void DuplicateSetupIdentitiesAreRejectedWithinTheirNamespace(string section, string kind)
    {
        using var source = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        JsonProperty repeated = source.RootElement.GetProperty(section).EnumerateObject().First();
        string entry = $"{JsonSerializer.Serialize(repeated.Name)}:{repeated.Value.GetRawText()}";
        string json = "{" + string.Join(",", source.RootElement.EnumerateObject().Select(property =>
            JsonSerializer.Serialize(property.Name) + ":" + (property.Name == section
                ? "{" + entry + "," + entry + "}" : property.Value.GetRawText()))) + "}";

        var error = Assert.Throws<JsonException>(() => SetupCatalog.Parse(json));

        Assert.Contains($"duplicate {kind} identity '{repeated.Name}'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupIdentityNamespacesRemainIndependent()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        string json = "{" + string.Join(",", SetupSections.Select(section =>
            JsonSerializer.Serialize(section) + ":{\"shared\":"
            + source.RootElement.GetProperty(section).EnumerateObject().First().Value.GetRawText() + "}")) + "}";

        SetupCatalog catalog = SetupCatalog.Parse(json);

        Assert.Equal(["shared"], catalog.CampaignNames);
        Assert.Equal(["shared"], catalog.HeroNames);
        Assert.Equal(["shared"], catalog.EncounterSetNames);
    }
}
