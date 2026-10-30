using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Recognizes result values produced by an earlier instruction or pending cost.</summary>
internal static class AbilityFutureResultAdmission
{
    internal static bool MayBePositive(AbilityNumber number, AbilityAdmissionScope scope) => number switch
    {
        AbilityNumber.Result result => HasProducer(result.Name, scope),
        AbilityNumber.Sum sum => sum.Operands.Any(item => MayBePositive(item, scope)),
        AbilityNumber.Product product => !product.Operands.Any(IsZero) && product.Operands.Any(item => MayBePositive(item, scope)),
        AbilityNumber.Minimum minimum => !minimum.Operands.Any(IsNonPositive) && minimum.Operands.Any(item => MayBePositive(item, scope)),
        AbilityNumber.FloorDivide division => MayBePositive(division.Operands[0], scope),
        AbilityNumber.Conditional conditional => MayBePositive(conditional.Then, scope) || MayBePositive(conditional.Else, scope),
        _ => false,
    };

    private static bool IsZero(AbilityNumber number) => number is AbilityNumber.Constant { Value: 0 };
    private static bool IsNonPositive(AbilityNumber number) => number is AbilityNumber.Constant { Value: <= 0 };

    private static bool HasProducer(string name, AbilityAdmissionScope scope)
    {
        if (name == "healed" && scope.Reachability.PriorSteps.SelectMany(HealingSteps).LastOrDefault() is { } heal)
            return HealCanProduce(heal, scope);
        return scope.Reachability.PriorSteps.Any(effect => Produces(effect, name, scope))
            || scope.Reachability.PaymentMayMutate && Produces(scope.Reachability.PaymentCost, name);
    }

    private static IEnumerable<AbilityEffect.Heal> HealingSteps(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.SelectMany(HealingSteps),
        AbilityEffect.Heal heal => [heal],
        _ => [],
    };

    private static bool Produces(AbilityCost? cost, string name) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.Any(item => Produces(item, name)),
        AbilityCost.Heal { Amount: > 0 } when name == "healed" => true,
        _ => false,
    };

    internal static bool HealCanProduce(AbilityEffect.Heal heal, AbilityAdmissionScope scope)
    {
        if (AbilityInitiationPrimitives.Amount(heal.Amount, scope) <= 0) return false;
        var target = AbilityInitiationPrimitives.Find(heal.Card, scope);
        if (target is null) return false;
        if (scope.Reachability.PaymentCost is not { } cost || !HasHealthCost(cost, scope, target))
            return target.Damage > 0;
        return new AbilityHealingCostProjection(scope.World, scope.Source, scope.Player,
            scope.Context.Program, scope.Context.ResourceAbilities, target).LeavesDamage(cost);
    }

    private static bool HasHealthCost(AbilityCost cost, AbilityAdmissionScope scope, Marvel.Rules.State.Card target) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.Any(item => HasHealthCost(item, scope, target)),
        AbilityCost.Damage damage => AbilityPaymentRules.CostTarget(scope.World, scope.Source, scope.Player, damage.Card) == target,
        AbilityCost.Heal heal => AbilityPaymentRules.CostTarget(scope.World, scope.Source, scope.Player, heal.Card) == target,
        _ => false,
    };
    private static bool Produces(AbilityEffect effect, string name, AbilityAdmissionScope scope) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(item => Produces(item, name, scope)),
        AbilityEffect.Heal heal when name == "healed" => HealCanProduce(heal, scope),
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust } exhausted when name == "exhausted" =>
            AbilityCardActionTargetAdmission.Of(exhausted, scope) == AbilityTargetAdmission.TargetLegality.Valid,
        AbilityEffect.DiscardAtRandom discard when name is "discarded" or "resourceTypes" => AbilityInitiationPrimitives.Amount(discard.Count, scope) > 0,
        AbilityEffect.DiscardHandWithResource when name == "discarded" => true,
        AbilityEffect.DiscardHandCards discard when name == "discarded" => AbilityInitiationPrimitives.Amount(discard.Count, scope) > 0,
        AbilityEffect.MoveThreat move when name is "threatMoved" or "threatRemovedByMove" or "destinationForcedResponseTriggered" =>
            AbilityThreatMovementAdmission.CanMove(move, scope),
        AbilityEffect.RemoveThreat remove when name == "schemesCleared" =>
            AbilityEffectTargetAdmission.DamageAndThreatTargetLegality(remove, scope) == AbilityTargetAdmission.TargetLegality.Valid,
        AbilityEffect.Search or AbilityEffect.RecruitDiscarded when name == "found" => true,
        AbilityEffect.InspectEncounterTop inspect when name == inspect.Result =>
            AbilityEncounterInspection.Cards(scope.World, inspect).Count > 0,
        AbilityEffect.InspectDiscardOrderEncounterTop when name == "discarded" =>
            EncounterRouting.Deck(scope.World).Cards.Count > 0,
        AbilityEffect.ActivateEnemies when name.StartsWith("activation", StringComparison.Ordinal) => true,
        _ => false,
    };
}
