using Marvel.Rules.Setup;
using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Allocates independent scenario groups and binds their physical blueprint identities.</summary>
internal static class EncounterRecipeDeal
{
    internal static void Validate(CampaignSetup campaign, IReadOnlyList<string>? selected)
    {
        if (campaign.EncounterGroups is not { } recipe) return;
        ValidateSets(campaign, selected);
        ValidatePrincipals(campaign, recipe);
    }

    private static void ValidateSets(CampaignSetup campaign, IReadOnlyList<string>? selected)
    {
        if (campaign.Encounters.Count != 0 || campaign.EncounterSets.Count != 0
            || campaign.ModularSets.Count != 0 || selected is { Count: > 0 })
            throw new ArgumentException("independent encounter groups cannot mix other encounter sets");
    }

    private static void ValidatePrincipals(CampaignSetup campaign, EncounterSetupRecipe recipe)
    {
        if (recipe.Groups.Count < 2 || recipe.ActiveGroup < 0 || recipe.ActiveGroup >= recipe.Groups.Count)
            throw new ArgumentException("independent encounter groups require a valid active group");
        var stages = recipe.Groups.SelectMany(group => group.Stages).ToArray();
        if (!stages.SequenceEqual(campaign.Villain) || stages.Distinct(StringComparer.Ordinal).Count() != stages.Length)
            throw new ArgumentException("scenario villains must exactly match distinct group stages in allocation order");
        if (recipe.Groups.Any(group => group.Stages.Count is < 1 or > 2 || group.Encounters.Count == 0)
            || recipe.Groups.Select(group => group.Stages.Count).Distinct().Count() != 1)
            throw new ArgumentException("independent groups require consistent one-stage or two-stage recipes and nonempty decks");
        if (recipe.Groups.Select(group => group.Scheme).Distinct(StringComparer.Ordinal).Count() != recipe.Groups.Count)
            throw new ArgumentException("each group requires a distinct side scheme");
    }

    internal static void Append(EncounterSetupRecipe recipe, List<Creation> dealt)
    {
        // This engine allocates each entire group after the ordinary main-scheme
        // and set-aside cards, in authored group order. Printed rules do not name IDs.
        foreach (var group in recipe.Groups)
        {
            dealt.AddRange(group.Stages.Select(face => new Creation(face, CreationSource.Villain, Creation.Scenario)));
            dealt.Add(new(group.Scheme, CreationSource.ScenarioSetAside, Creation.Scenario));
            dealt.AddRange(group.Encounters.Select(face => new Creation(face, CreationSource.Encounter, Creation.Scenario)));
        }
    }

    internal static EncounterSetupPlan Bind(EncounterSetupRecipe recipe, IReadOnlyList<CardBlueprint> blueprints)
    {
        int next = blueprints.Count - recipe.Groups.Sum(group => group.Stages.Count + 1 + group.Encounters.Count);
        var groups = new List<EncounterGroupSetup>();
        foreach (var group in recipe.Groups)
        {
            var stages = group.Stages.Select(face => Take(face, SetupSlot.Villain)).ToArray();
            int scheme = Take(group.Scheme, SetupSlot.SetAside);
            var encounters = group.Encounters.Select(face => Take(face, SetupSlot.Encounter)).ToArray();
            groups.Add(new(Array.AsReadOnly(stages), scheme, Array.AsReadOnly(encounters)));
        }
        return new(groups.AsReadOnly(), groups[recipe.ActiveGroup].Stages[0]);

        int Take(string face, SetupSlot slot)
        {
            if (next < 0 || next >= blueprints.Count || blueprints[next] is not { Seat: -1 } blueprint
                || blueprint.Spec != face || blueprint.Slot != slot)
                throw new ArgumentException("encounter group blueprint allocation does not match its recipe");
            return next++;
        }
    }
}
