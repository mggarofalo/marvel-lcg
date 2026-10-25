using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
namespace Marvel.Cards.Dsl;

internal static class AbilityThreatMovementLowering
{
    internal static AbilityEffect.MoveThreat Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "to", "amount");
        return new(Endpoint(Selected(fields, "from", location), location.Child("from")),
            Endpoint(Selected(fields, "to", location), location.Child("to")),
            Numeric(fields, "amount", location));
    }
    private static AbilityCardSelection Endpoint(AbilityCardSelection selection, AbilityLocation location) =>
        selection is AbilityCardSelection.Bound or AbilityCardSelection.Titled
            or AbilityCardSelection.FirstAvailable or AbilityCardSelection.Last
            or AbilityCardSelection.InAreas
            or AbilityCardSelection.Query { Kind: AbilityCardQuery.MainScheme
                or AbilityCardQuery.YourAsideSideScheme or AbilityCardQuery.ActiveVillainSideScheme
                or AbilityCardQuery.VillainOfAttachedScheme }
            ? selection : throw location.Error("moveThreat requires a single-card endpoint; bind a choice first");
}
