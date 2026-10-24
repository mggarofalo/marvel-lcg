using Marvel.Rules.Setup;
using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Compiles an authored recipe into immutable runtime setup inputs.</summary>
public static class ScenarioDealer
{
    /// <summary>Retains ordinary allocation and binds separate deck relationships when declared.</summary>
    public static ScenarioDealPlan Prepare(SetupCatalog catalog, DealRequest request, ICardFacts facts)
    {
        var blueprints = Blueprints.From(Dealer.Deal(catalog, request, facts), facts);
        var recipe = catalog.Campaign(request.Scenario).EncounterGroups;
        var groups = recipe is null ? null : EncounterRecipeDeal.Bind(recipe, blueprints);
        groups?.Validate(blueprints, facts);
        return new(Array.AsReadOnly(blueprints.ToArray()), groups);
    }
}
