using System.Collections.Immutable;

namespace Marvel.Content.Setup;

/// <summary>Pins the complete reviewed Green Goblin product composition.</summary>
internal static class GreenGoblinRecipes
{
    // Repository admission policy pins the printed Contents and mc02 insert quantities.
    // Recipe order is significant for stage/scheme progression; encounter quantities are multisets.
    private static readonly ImmutableArray<string> RiskyEncounters =
    [
        "02007", "02007", "02007", "02008", "02008", "02008", "02008", "02009",
        "02010", "02010", "02011", "02011", "02012", "02012", "02012", "02012", "02013", "02013",
    ];
    private static readonly ImmutableArray<string> MutagenEncounters =
    [
        "02019", "02020", "02021", "02022", "02023", "02023", "02023", "02023",
        "02024", "02024", "02024", "02024", "02024", "02024", "02025", "02026", "02026",
        "02027", "02028", "02028", "02029", "02029", "02030", "02031", "02032", "02032",
    ];

    private static readonly ImmutableArray<string> Scenarios = ["risky_business", "mutagen_formula"];
    private static readonly ImmutableArray<bool> Modes = [false, true];

    internal static bool Complete(SetupCatalog setup) =>
        Scenarios.All(scenario =>
            Modes.All(expert =>
            {
                string key = scenario + (expert ? "_expert" : "");
                return setup.CampaignNames.Contains(key) && Scenario(setup.Campaign(key), scenario, expert);
            }))
        && Modular(setup, "goblin_gimmicks", ["02033", "02033", "02034", "02034", "02035", "02035", "02036", "02036"])
        && Modular(setup, "a_mess_of_things", ["02037", "02038", "02039", "02040", "02040"])
        && Modular(setup, "power_drain", ["02041", "02042", "02043", "02044", "02045"])
        && Modular(setup, "running_interference", ["02046", "02047", "02048", "02049", "02049"]);

    private static bool Scenario(CampaignSetup recipe, string scenario, bool expert)
    {
        bool risky = scenario == "risky_business";
        string[] stages = risky
            ? expert ? ["02002a,02002b", "02003a,02003b"] : ["02001a,02001b", "02002a,02002b"]
            : expert ? ["02015", "02016"] : ["02014", "02015"];
        return Header(recipe, expert) && Layout(recipe, risky)
            && recipe.Villain.SequenceEqual(stages)
            && SameQuantities(recipe.Encounters, risky ? RiskyEncounters : MutagenEncounters);
    }

    private static bool Header(CampaignSetup recipe, bool expert)
    {
        string[] fixedSets = expert ? ["standard", "expert"] : ["standard"];
        return recipe.Expert == expert && recipe.SetupMode is null && recipe.EncounterGroups is null
            && recipe.EncounterSets.SequenceEqual(fixedSets)
            && recipe.ModularSets.SequenceEqual(["goblin_gimmicks"])
            && recipe.Challenges.Count == 0;
    }

    private static bool Layout(CampaignSetup recipe, bool risky)
    {
        string[] schemes = risky ? ["02004a,02004b", "02005a,02005b"] : ["02017a,02017b", "02018a,02018b"];
        return recipe.Schemes.SequenceEqual(schemes)
            && recipe.SetAside.SequenceEqual(risky ? ["02006a,02006b"] : Array.Empty<string>());
    }

    private static bool Modular(SetupCatalog setup, string key, string[] required) =>
        setup.EncounterSetNames.Contains(key) && setup.EncounterRole(key) == EncounterSetRole.Modular
        && SameQuantities(setup.EncounterSet(key), required);

    private static bool SameQuantities(IReadOnlyList<string> actual, IReadOnlyList<string> required) =>
        actual.Order(StringComparer.Ordinal).SequenceEqual(required.Order(StringComparer.Ordinal));
}
