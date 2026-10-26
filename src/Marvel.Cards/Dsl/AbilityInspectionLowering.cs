namespace Marvel.Cards.Dsl;

internal static class AbilityInspectionLowering
{
    internal static AbilityEffect.InspectDiscardOrderEncounterTop DiscardOrder(AbilityValue value, AbilityLocation location)
    {
        int count = checked((int)AbilityCostLowering.PositiveInteger(value, location));
        if (count > 3) throw location.Error("private inspection supports at most three cards");
        return new(count);
    }

    internal static AbilityEffect.InspectEncounterTop Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = AbilitySelectorLowering.Fields(value, location, "count", "result");
        int count = checked((int)AbilityCostLowering.PositiveInteger(
            AbilitySelectorLowering.Required(fields, "count", location), location.Child("count")));
        string result = AbilityLowering.Text(
            AbilitySelectorLowering.Required(fields, "result", location), location.Child("result"));
        if (result != "treacheriesLookedAt") throw location.Child("result").Error("expected treacheriesLookedAt");
        return new(count, result);
    }
}
