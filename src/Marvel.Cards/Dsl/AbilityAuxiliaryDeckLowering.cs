using Marvel.Rules.Play;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityAuxiliaryDeckLowering
{
    internal static AbilityEffect.InitializeAuxiliaryDeck Initialize(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "deck", "cards");
        return new(Deck(Required(fields, "deck", location), location.Child("deck")),
            Selected(fields, "cards", location));
    }

    internal static AuxiliaryDeckKind Deck(AbilityValue value, AbilityLocation location) => Text(value, location) switch
    {
        "experimentalWeapons" => AuxiliaryDeckKind.ExperimentalWeapons,
        "sideSchemes" => AuxiliaryDeckKind.SideSchemes,
        _ => throw location.Error("expected experimentalWeapons or sideSchemes"),
    };
}
