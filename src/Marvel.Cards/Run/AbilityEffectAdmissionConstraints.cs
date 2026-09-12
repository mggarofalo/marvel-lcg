using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Admission constraints shared by repeated, lasting, and labelled effects.</summary>
internal static class AbilityEffectAdmissionConstraints
{
    internal static bool StableForEachTarget(
        AbilityCardSelection selection, AbilityCardQuery query) =>
        selection is AbilityCardSelection.Query named && named.Kind == query;

    internal static bool HasUnboundPowerAmount(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        cast.PowerAmount < 0 && ContainsPowerAmount(ForEachOf(node, cast).Count);

    internal static bool ContainsMutableAmount(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        (EffectAmount(node) is { } amount && AmountMayChange(amount))
        || (node.OperationName() == "forEach" && AmountMayChange(ForEachOf(node, cast).Count))
        || ContinuationChildren(node).Any(child => ContainsMutableAmount(child, cast));

    internal static long ForEachCount(AbilityEffect node, AbilityAdmissionScope cast) =>
        NonNegativeForEachCount(Amount(ForEachOf(node, cast).Count, cast));

    internal static long NonNegativeForEachCount(long count)
    {
        if (count < 0)
        {
            throw new AbilityException("'forEach' needs a non-negative 'count'");
        }
        return count;
    }

    internal static bool SkipForEachPreflight(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if ((cast.Reachability.PaymentMayMutate || cast.Reachability.PriorStepMayMutate)
            && AmountMayChange(ForEachOf(node, cast).Count))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
        }
        return StableZeroForEach(node, cast);
    }

    internal static bool StableZeroForEach(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node.OperationName() != "forEach")
        {
            return false;
        }

        // A labelled choice such as Legal Practice binds this sentinel only
        // when its power is scheduled. Its body remains reachable during
        // preflight, but the count cannot be validated or pruned yet.
        if (HasUnboundPowerAmount(node, cast))
        {
            return false;
        }

        long count = ForEachCount(node, cast);
        return !AmountMayChange(ForEachOf(node, cast).Count) && count == 0;
    }

    internal static bool CurrentlyZeroForEach(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node.OperationName() != "forEach" || HasUnboundPowerAmount(node, cast))
        {
            return false;
        }
        if ((cast.Reachability.PaymentMayMutate || cast.Reachability.PriorStepMayMutate)
            && AmountMayChange(ForEachOf(node, cast).Count))
        {
            return false;
        }
        return ForEachCount(node, cast) == 0;
    }

    internal static bool LastingPeriodIsOpen(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        LastingPeriodIsOpen(node switch
        {
            AbilityEffect.GrantField { Until: { } until } => until,
            AbilityEffect.GrantTrait { Until: { } until } => until,
            _ => throw new InvalidOperationException("Expected a lasting grant"),
        }, cast);

    internal static bool LastingPeriodIsOpen(
        string until, AbilityAdmissionScope cast) =>
        until switch
        {
            TimingPoints.EndOfAttack => cast.World.Attack is not null
                || cast.World.CharacterAttack is not null
                || cast.Occurrence.Is(Steps.AttackInitiated),
            TimingPoints.EndOfActivation => cast.World.Activation is not null,
            _ => true,
        };

    internal static bool HasLabelledPower(AbilityEffect node) =>
        PowerNodes(node, BasicPowers.AttackVerb).Any()
        || PowerNodes(node, BasicPowers.ThwartVerb).Any()
        || PowerNodes(node, Attack.DefenseVerb).Any();

    internal static bool HasInitiationConstraint(AbilityEffect node) =>
        HasLabelledPower(node)
        || node.OperationName() == "grantUntil"
        || ResolutionChildren(node).Any(HasInitiationConstraint);
}
