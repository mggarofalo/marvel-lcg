using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityEngagingDiscardLowering
{
    // Engine spelling for "Discard the top N cards of the encounter deck. Each time a minion is
    // discarded this way, put it into play engaged with the player who is engaged with the
    // fewest minions" (Drang III, 16060). The engagement rule is named so that no other is
    // accepted silently.
    internal static AbilityEffect.DiscardTop EngagingDiscardEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "count", "engage");
        if (Text(Required(fields, "engage", location), location.Child("engage")) != "fewestMinions")
            throw location.Child("engage").Error("discardTopEngagingMinions engages only the player with the fewest minions ('fewestMinions')");
        return new(AbilitySearchArea.EncounterDeck, null, Numeric(fields, "count", location), EngagesMinions: true);
    }
}
