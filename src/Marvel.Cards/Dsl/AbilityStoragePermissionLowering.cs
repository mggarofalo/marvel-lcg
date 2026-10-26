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
        if (!effects.Any(effect => effect is AbilityEffect.AllowPlayBeneath or AbilityEffect.AllowPlayFromDiscard)) return;
        bool direct = root is AbilityEffect.AllowPlayBeneath or AbilityEffect.AllowPlayFromDiscard;
        bool unconditional = ability.Trigger == new AbilityTrigger(null, AbilityType.Constant, "this")
            && ability is { When: null, Cost: null, Limit: null, Maximum: null, AnyPlayer: false };
        if (!direct || !unconditional)
            throw location.Error("play permission requires a direct unconditional Constant ability");
    }

    internal static AbilityEffect.AllowPlayFromDiscard LowerDiscard(AbilityValue value, AbilityLocation location)
    {
        // The DSL chooses an explicit marker for the bounded self-ally permission.
        FixedWord(value, location, "this");
        return new();
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
