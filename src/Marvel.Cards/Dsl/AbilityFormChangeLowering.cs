using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityConditionLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityFormChangeLowering
{
    internal static AbilityEffect.ChangeForm Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "to");
        var to = Required(fields, "to", location);
        var destination = to switch
        {
            AbilityValue.Word { Value: "hero" or "alter-ego" } word =>
                (AbilityFormDestination)new AbilityFormDestination.Form(word.Value),
            AbilityValue.Word { Value: "other-hero" } => new AbilityFormDestination.OtherHero(),
            AbilityValue.Map map => FaceDestination(map, location.Child("to")),
            _ => throw location.Child("to").Error("expected a supported form or face destination"),
        };
        return new(LowerPlayer(Required(fields, "player", location), location.Child("player")), destination);
    }

    private static AbilityFormDestination.Face FaceDestination(AbilityValue.Map value, AbilityLocation location)
    {
        var fields = Fields(value, location, "face");
        string face = Text(Required(fields, "face", location), location.Child("face"));
        if (string.IsNullOrWhiteSpace(face)) throw location.Child("face").Error("face must not be empty");
        return new AbilityFormDestination.Face(face);
    }
}
