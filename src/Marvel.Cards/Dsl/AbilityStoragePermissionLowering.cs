using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityStoragePermissionLowering
{
    internal static void Validate(CardAbility ability, AbilityEffect root, AbilityLocation location,
        IEnumerable<AbilityEffect> effects)
    {
        if (!effects.Any(effect => effect is AbilityEffect.AllowPlayBeneath)) return;
        bool direct = root is AbilityEffect.AllowPlayBeneath;
        bool unconditional = ability.Trigger == new AbilityTrigger(null, AbilityType.Constant, "this")
            && ability is { When: null, Cost: null, Limit: null, Maximum: null, AnyPlayer: false };
        if (!direct || !unconditional)
            throw location.Error("allowPlayBeneath requires a direct unconditional Constant ability");
    }

    internal static AbilityEffect.AllowPlayBeneath Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "kind", "trait");
        FixedWord(Required(fields, "kind", location), location.Child("kind"), "Event");
        string trait = Text(Required(fields, "trait", location), location.Child("trait"));
        if (string.IsNullOrWhiteSpace(trait)) throw location.Child("trait").Error("expected a printed trait");
        return new(CardKind.Event, trait);
    }
}
