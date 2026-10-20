using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

/// <summary>Creates immutable admission evidence at an effect entry boundary.</summary>
internal static class AbilityEntryAdmission
{
    internal static AbilityAdmissionResult Admit(
        AbilityEffect effect, AbilityAdmissionContext context)
    {
        AbilityHitPointAssignment.ValidateShape(effect);
        var evidence = new HashSet<AbilityEffect>(ReferenceEqualityComparer.Instance);
        var scope = new AbilityAdmissionScope(
            context.WithReachability(context.Reachability with { CheckingInitiation = true }),
            evidence);
        bool admissible = CanInitiate(effect, scope)
            && TargetLegalityOf(effect, scope) != TargetLegality.Invalid;
        return new AbilityAdmissionResult(
            admissible,
            ImmutableHashSet.CreateRange<AbilityEffect>(
                ReferenceEqualityComparer.Instance, evidence));
    }

    internal static AbilityAdmissionResult AdmitStructure(
        AbilityEffect effect, AbilityAdmissionContext context)
    {
        AbilityHitPointAssignment.ValidateShape(effect);
        var evidence = new HashSet<AbilityEffect>(ReferenceEqualityComparer.Instance);
        bool admissible = CanInitiate(
            effect, new AbilityAdmissionScope(context, evidence));
        return new AbilityAdmissionResult(
            admissible,
            ImmutableHashSet.CreateRange<AbilityEffect>(
                ReferenceEqualityComparer.Instance, evidence));
    }

}
