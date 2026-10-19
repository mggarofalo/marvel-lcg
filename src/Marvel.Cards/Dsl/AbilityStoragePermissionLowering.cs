using Marvel.Rules.State;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityStoragePermissionLowering
{
    internal static AbilityEffect.AllowPlayBeneath Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "kind", "trait");
        FixedWord(Required(fields, "kind", location), location.Child("kind"), "Event");
        string trait = Text(Required(fields, "trait", location), location.Child("trait"));
        if (string.IsNullOrWhiteSpace(trait)) throw location.Child("trait").Error("expected a printed trait");
        return new(CardKind.Event, trait);
    }
}
