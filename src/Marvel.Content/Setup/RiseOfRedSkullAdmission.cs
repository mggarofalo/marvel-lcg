using Marvel.Rules.Campaign;

namespace Marvel.Content.Setup;

/// <summary>Admits the complete expansion rather than a partially executable campaign.</summary>
public static class RiseOfRedSkullAdmission
{
    /// <summary>Returns campaign metadata only when both heroes, all faces and all scenarios are admitted.</summary>
    public static CampaignDefinition? Discover(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored)
    {
        string[] faces = cards.Faces.Where(face => cards.Pack(face) == "trors").ToArray();
        // The reviewed product inventory contains 181 printed faces, including
        // both forms of identities and both sides of campaign Conditions.
        if (faces.Length != 181 || faces.Any(face => !authored.Contains(face))) return null;
        var definition = RiseOfRedSkullCampaign.Definition;
        if (definition.Scenarios.Any(scenario => !setup.CampaignNames.Contains(scenario.Standard)
            || !setup.CampaignNames.Contains(scenario.Expert))) return null;
        if (!setup.HeroNames.Contains("hawkeye") || !setup.HeroNames.Contains("spider_woman")) return null;
        return definition;
    }
}
