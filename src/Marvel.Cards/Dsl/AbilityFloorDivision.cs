using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityFloorDivision
{
    internal static AbilityNumber.FloorDivide Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "value", "divisor");
        long divisor = Integer(Required(fields, "divisor", location), location.Child("divisor"));
        if (divisor <= 0) throw location.Child("divisor").Error("expected a positive integer divisor");
        return new([Number(Required(fields, "value", location), location.Child("value")), new AbilityNumber.Constant(divisor)]);
    }
}
