using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

// Leaf card-action target checks are independent of composite choice traversal.
internal static class AbilityCardActionTargetAdmission
{
    internal static TargetLegality Of(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
            "removeFromGame" => RemoveFromGameTargetLegality(node, cast),
            "reveal" or "returnToHand" => CardsLegality(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)),
            "returnOwnedToHand" or "addToHand" => AbilityCardMovementAdmission.HasHandTarget(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "exhaust" => CardsLegality(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)
                    .Where(card => card.Ready)),
            "ready" => CardsLegality(Every(
                EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Where(card =>
                !card.Ready && AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card))),
            "giveStatus" => CardsLegality(StatusTargets(node, cast)),
            "declareDefender" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } declared
                && Attack.CanDeclareByAbility(
                    cast.World, cast.World.Facts, declared,
                    ReplaceableDefenseDefender(cast))
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "attachTo" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "grantUntil" => Find(GrantSelectionOf(node, cast), cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "discard" => EffectOf<AbilityEffect.CardAction>(node, cast).Selection is var discardTarget
                && Find(discardTarget, cast) is { } discarded
                && CanRemoveByEffect(discardTarget, cast, discarded)
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            _ => throw new InvalidOperationException("Unknown card-action target operation"),
        };

    internal static TargetLegality CardsLegality(IEnumerable<Card> candidates) =>
        candidates.Any() ? TargetLegality.Valid : TargetLegality.Invalid;

    internal static TargetLegality RemoveFromGameTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        return Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } removed
            && CanRemoveByEffect(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, removed)
                ? TargetLegality.Valid : TargetLegality.Invalid;
    }

}
