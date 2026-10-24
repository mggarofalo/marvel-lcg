using System.Text.Json;
using Marvel.Content.Setup;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class ScenarioModeMetadataTests
{
    [Theory]
    [InlineData("standard")]
    [InlineData("expert")]
    [InlineData("custom")]
    [InlineData("extreme")]
    public void ModeAndOrderedVersionLabelsRemainAuthoredFacts(string mode)
    {
        var document = WreckingRecipeFixture.Document(5, mode == "extreme");
        var campaign = document["campaigns"]!["wrecking_fixture"]!;
        campaign["setup_mode"] = mode;
        string[] variants = mode == "extreme" ? ["A → B", "A → B", "A → B", "A → B"] : ["B", "A", "B", "A"];
        for (int index = 0; index < 4; index++) campaign["encounter_groups"]!["groups"]![index]!["variant"] = variants[index];
        var parsed = SetupCatalog.Parse(document.ToJsonString()).Campaign("wrecking_fixture");
        Assert.Equal(mode, parsed.SetupMode);
        Assert.Equal(variants, parsed.EncounterGroups!.Groups.Select(group => group.Variant));
    }

    [Theory]
    [InlineData("mode")]
    [InlineData("variant")]
    public void InvalidMetadataIsRejectedInsteadOfInventingDisplayChoices(string field)
    {
        var document = WreckingRecipeFixture.Document();
        var campaign = document["campaigns"]!["wrecking_fixture"]!;
        if (field == "mode") campaign["setup_mode"] = "guessed";
        else campaign["encounter_groups"]!["groups"]![0]!["variant"] = " ";
        Assert.Throws<JsonException>(() => SetupCatalog.Parse(document.ToJsonString()));
    }
}
