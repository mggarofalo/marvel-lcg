using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;

namespace Marvel.Server;

/// <summary>Describes an admitted authored recipe without asking clients to reconstruct it.</summary>
internal static class ScenarioSetupProjection
{
    internal static ScenarioSetupChoice Describe(string key, CampaignSetup campaign, CardCatalog cards) =>
        new(key, campaign.Name, campaign.Expert, [.. campaign.ModularSets])
        {
            Mode = campaign.SetupMode ?? (campaign.Expert ? "expert" : "standard"),
            AllowsModularSets = campaign.EncounterGroups is null,
            Variants = campaign.EncounterGroups is { } groups
                ? groups.Groups.Select(group => new ScenarioVariantChoice(cards.Title(group.Stages[0]),
                    group.Variant ?? throw new InvalidOperationException("discovered encounter groups require version labels"))).ToArray()
                : [],
        };
}
