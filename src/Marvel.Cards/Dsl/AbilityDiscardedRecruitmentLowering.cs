using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Checks exact-face recruitment and ordered printed-trait recruitment.</summary>
internal static class AbilityDiscardedRecruitmentLowering
{
    internal static AbilityEffect.RecruitDiscarded Lower(AbilityValue value, AbilityLocation location)
    {
        if (value is AbilityValue.Word)
            return new(Text(value, location));
        var fields = Fields(value, location, "trait");
        return new(null) { Trait = Text(Required(fields, "trait", location), location.Child("trait")) };
    }
}
