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
            "moveActiveCounter" => AbilityActiveCounterExecution.CanMove(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "removeFromGame" => RemoveFromGameTargetLegality(node, cast),
            "revealFromVillainDeck" => AbilitySelectedDeckReveal.HasTarget(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "reveal" or "returnToHand" => CardsLegality(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)),
            "placeOnBottomOfDeck" or "returnOwnedToHand" or "addToHand" => AbilityCardMovementAdmission.HasHandTarget(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "exhaust" or "ready" => Readiness(node, cast),
            "giveStatus" => CardsLegality(StatusTargets(node, cast)),
            "declareDefender" => DeclaredDefenderLegality(node, cast),
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

    private static TargetLegality DeclaredDefenderLegality(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } declared
            && Attack.CanDeclareByAbility(cast.World, cast.World.Facts, declared,
                ReplaceableDefenseDefender(cast))
                ? TargetLegality.Valid : TargetLegality.Invalid;

    internal static TargetLegality CardsLegality(IEnumerable<Card> candidates) =>
        candidates.Any() ? TargetLegality.Valid : TargetLegality.Invalid;

    private static TargetLegality Readiness(AbilityEffect node, AbilityAdmissionScope cast) =>
        CardsLegality(Every(((AbilityEffect.CardAction)node).Selection, cast).Where(card =>
            node.OperationName() == "exhaust" ? card.Ready
                : !card.Ready && AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card)));

    internal static TargetLegality RemoveFromGameTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        return Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } removed
            && CanRemoveByEffect(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, removed)
                ? TargetLegality.Valid : TargetLegality.Invalid;
    }

}
