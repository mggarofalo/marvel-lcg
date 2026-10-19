using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class RiseOfRedSkullCampaignTests
{
    [Fact]
    public void CampaignCompositionResolvesToItsPrintedCardRolesAndConditionHitPoints()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var cards = document.RootElement.GetProperty("cards").EnumerateArray()
            .ToDictionary(card => card.GetProperty("card_id").GetString()!, card => card);
        var definition = RiseOfRedSkullCampaign.Definition;
        foreach (var tech in definition.Tech) AssertCard(tech.Face, "Upgrade", cards);
        foreach (var captive in definition.CaptiveAllyFaces) AssertCard(captive, "Ally", cards);
        foreach (var weapon in definition.ExperimentalWeaponFaces) AssertCard(weapon, "Attachment", cards);
        foreach (var obligation in definition.ObligationFaces) AssertCard(obligation, "Obligation", cards);
        foreach (var condition in definition.Conditions)
        {
            foreach (var face in new[] { condition.Basic.Face, condition.ImprovedFace })
            {
                AssertCard(face, "Upgrade", cards);
                Assert.Contains($"You get +{condition.HitPointBonus} hit points.",
                    cards[face].GetProperty("text_plain").GetString(), StringComparison.Ordinal);
            }
        }
        var instances = definition.Tech.Concat(definition.Conditions.Select(condition => condition.Basic)).ToArray();
        Assert.Equal(instances.Length, instances.Select(card => card.Key).Distinct(StringComparer.Ordinal).Count());
    }

    private static void AssertCard(string face, string kind, Dictionary<string, JsonElement> cards)
    {
        Assert.True(cards.TryGetValue(face, out var card), $"Campaign face {face} is missing");
        Assert.Equal("trors", card.GetProperty("pack").GetString());
        Assert.Equal(kind, card.GetProperty("type").GetString());
    }
}
