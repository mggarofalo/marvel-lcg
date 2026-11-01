using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Lowers moving named counters from one card to another.</summary>
internal static class AbilityCounterMoveLowering
{
    // Engine spelling: {"moveCounters": {"from", "to", "counter", "count"}}.
    internal static AbilityCounterEffect.MoveCounters Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "to", "counter", "count");
        string counter = Text(Required(fields, "counter", location), location.Child("counter"));
        if (counter.Length == 0)
            throw location.Child("counter").Error("expected a nonempty counter name");
        return new(Selected(fields, "from", location), Selected(fields, "to", location), counter,
            PositiveInteger(Required(fields, "count", location), location.Child("count")));
    }
}
