using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityAttachmentLowering
{
    internal static AbilityEffect.AttachCards Attach(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "host");
        return new(Selected(fields, "cards", location), Selected(fields, "host", location));
    }
}

