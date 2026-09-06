using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private sealed record BindingCandidateState(
        IReadOnlyList<Marvel.Rules.State.Card> Cards, bool MayBeEmpty);

    private enum TargetLegality
    {
        Invalid,
        Valid,
    }

    // The interpreter translates its resolving frame into immutable admission
    // facts, then applies only the evidence returned by the independent owner.
    private static AbilityAdmissionContext AdmissionContext(Cast cast) =>
        new(
            cast.Abilities is AbilityRunner runner
                ? runner.program
                : throw new InvalidOperationException(
                    "Admission requires the authored ability program"),
            cast.ExpressionContext(), cast.Reachability, cast.Power,
            cast.HasContinuation);

    private static bool CanInitiate(CompiledCardAbility ability, Cast cast)
    {
        var context = AdmissionContext(cast).WithReachability(
            cast.Reachability with { CheckingInitiation = true });
        if (!AbilityInitiation.LabelsCanInitiate(ability, context))
        {
            return false;
        }

        cast.LabelsPreflighted = true;
        if (ability.Labels.Length > 0
            && Marvel.Rules.Play.LabeledAbilities.WouldBeCancelled(
                cast.World, cast.World.Facts, Resolver(cast),
                cast.Source, ability.Labels))
        {
            return true;
        }

        return ApplyAdmission(AbilityInitiation.Admit(ability.Effect, context), cast);
    }

    private static bool CanInitiate(AbilityEffect effect, Cast cast) =>
        ApplyAdmission(AbilityInitiation.Admit(effect, AdmissionContext(cast)), cast);

    private static bool CanInitiateSequence(AbilityEffect effect, Cast cast) =>
        ApplyAdmission(
            AbilityInitiation.AdmitStructure(effect, AdmissionContext(cast)), cast);

    private static TargetLegality TargetLegalityOf(AbilityEffect effect, Cast cast) =>
        AbilityInitiation.TargetsAreValid(effect, AdmissionContext(cast))
            ? TargetLegality.Valid
            : TargetLegality.Invalid;

    private static BindingCandidateState BindingCandidatesAfter(
        AbilityEffect effect, Cast cast, BindingCandidateState before)
    {
        var result = AbilityInitiation.BindingCandidates(
            effect, AdmissionContext(cast),
            new AbilityInitiation.BindingCandidateState(before.Cards, before.MayBeEmpty));
        return new BindingCandidateState(result.Cards, result.MayBeEmpty);
    }

    private static bool BindingCanChange(AbilityEffect? effect) =>
        AbilityBindingAnalysis.BindingCanChange(effect);

    private static bool BindingCanChange(AbilityPlayerSelection players) =>
        AbilityBindingAnalysis.BindingCanChange(players);

    private static bool SuspendsPowerEffect(
        AbilityEffect effect, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        AbilityInitiation.SuspendsPowerEffect(
            effect, AdmissionContext(cast), stateMayChange, bindingMayChange);

    private static IEnumerable<AbilityEffect> PowerNodes(AbilityEffect effect, string power) =>
        AbilityInitiation.PowerEffects(effect, power);
    private static IEnumerable<AbilityEffect> MutationChildren(AbilityEffect effect) =>
        AbilityInitiation.MutationChildren(effect);
    private static IEnumerable<AbilityEffect> ContinuationChildren(AbilityEffect effect) =>
        AbilityInitiation.ContinuationChildren(effect);
    private static IEnumerable<AbilityEffect> EachPlayers(AbilityEffect effect) =>
        AbilityInitiation.EachPlayers(effect);
    private static bool ContainsEffect(AbilityEffect effect, string kind) =>
        AbilityInitiation.ContainsEffect(effect, kind);
    private static long SoakDiscardThreshold(AbilityEffect effect) =>
        AbilityInitiation.SoakDiscardThreshold(effect);
    private static long SaturatingSum(long own, IEnumerable<long> rest) =>
        AbilityInitiation.SaturatingSum(own, rest);
    private static long SaturatingMultiply(long amount, long multiplier) =>
        AbilityInitiation.SaturatingMultiply(amount, multiplier);
    private static long NonNegativeForEachCount(long count) =>
        AbilityInitiation.NonNegativeForEachCount(count);
    private static long ForEachCount(AbilityEffect effect, Cast cast) =>
        AbilityInitiation.ForEachCount(effect, AdmissionContext(cast));
    private static bool CanDraw(AbilityEffect effect, Cast cast) =>
        AbilityInitiation.CanDraw(effect, AdmissionContext(cast));
    private static bool CanDraw(Marvel.Rules.State.World world, int player) =>
        AbilityInitiation.CanDraw(world, player);
    private static bool LastingPeriodIsOpen(string until, Cast cast) =>
        AbilityInitiation.LastingPeriodIsOpen(until, AdmissionContext(cast));
    private static void PreflightContinuationBoundaries(AbilityEffect effect, Cast cast) =>
        AbilityInitiation.PreflightContinuationBoundaries(effect, AdmissionContext(cast));
    private static bool PriorStepCanChange(AbilityCondition condition, Cast cast) =>
        AbilityInitiation.PriorStepCanChange(condition, AdmissionContext(cast));

    private static bool ApplyAdmission(AbilityAdmissionResult result, Cast cast)
    {
        foreach (var thwart in result.CrisisIgnoringThwarts)
        {
            cast.ValidateCrisisIgnoringThwart(thwart);
        }
        return result.IsAdmissible;
    }
}
