using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityBeneathLowering
{
    internal static AbilityEffect.ChooseCardsToHand ChooseToHand(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "maximum");
        return new(Selected(fields, "from", location),
            PositiveCount(Required(fields, "maximum", location), location.Child("maximum")));
    }

    internal static AbilityEffect.ReleaseBeneath Release(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "removeHost", "rescue", "invocationDeck");
        bool invocation = Boolean(fields, "invocationDeck", location);
        bool rescue = Boolean(fields, "rescue", location);
        bool remove = Boolean(fields, "removeHost", location);
        if (invocation && (rescue || remove)) throw location.Error("Invocation return cannot rescue or remove its host");
        return new(remove, rescue) { InvocationDeck = invocation };
    }

    internal static AbilityEffect.StoreBeneath Store(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "host", "faceUp", "randomCount");
        int? randomCount = fields.TryGetValue("randomCount", out var count)
            ? PositiveCount(count, location.Child("randomCount")) : null;
        return new(Selected(fields, "cards", location), Selected(fields, "host", location),
            Boolean(fields, "faceUp", location), randomCount);
    }
}
