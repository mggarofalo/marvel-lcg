using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

// Hand movement resolves one card. Admission shares that cardinality and the
// physical-owner requirement without predicting or committing the movement.
internal static class AbilityCardMovementAdmission
{
    internal static bool HasHandTarget(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var movement = EffectOf<AbilityEffect.CardAction>(node, cast);
        return Find(movement.Selection, cast) is { } card
            && (movement.Instruction != AbilityCardInstruction.ReturnOwnedToHand || card.Owner >= 0);
    }
}
