using Marvel.Cards.Dsl;
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

    private static bool HasProducer(string name, AbilityAdmissionScope scope) =>
        scope.Reachability.PriorSteps.Any(effect => Produces(effect, name, scope))
        || scope.Reachability.PaymentMayMutate && Produces(scope.Reachability.PaymentCost, name);

    private static bool Produces(AbilityCost? cost, string name) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.Any(item => Produces(item, name)),
        AbilityCost.Heal { Amount: > 0 } when name == "healed" => true,
        _ => false,
    };

    private static bool Produces(AbilityEffect effect, string name, AbilityAdmissionScope scope) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(item => Produces(item, name, scope)),
        AbilityEffect.Heal heal when name == "healed" => AbilityInitiationPrimitives.Amount(heal.Amount, scope) > 0,
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust } when name == "exhausted" => true,
        AbilityEffect.DiscardAtRandom discard when name is "discarded" or "resourceTypes" => AbilityInitiationPrimitives.Amount(discard.Count, scope) > 0,
        AbilityEffect.DiscardHandWithResource when name == "discarded" => true,
        AbilityEffect.DiscardHandCards discard when name == "discarded" => AbilityInitiationPrimitives.Amount(discard.Count, scope) > 0,
        AbilityEffect.MoveThreat when name is "threatMoved" or "threatRemovedByMove" or "destinationForcedResponseTriggered" => true,
        AbilityEffect.RemoveThreat when name == "schemesCleared" => true,
        AbilityEffect.Search or AbilityEffect.RecruitDiscarded when name == "found" => true,
        AbilityEffect.InspectEncounterTop inspect when name == inspect.Result && inspect.Count > 0 => true,
        AbilityEffect.InspectDiscardOrderEncounterTop when name == "discarded" => true,
        AbilityEffect.ActivateEnemies when name.StartsWith("activation", StringComparison.Ordinal) => true,
        _ => false,
    };
}
