using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    // The interpreter translates its resolving frame into immutable admission
    // facts, then applies only the evidence returned by the independent owner.
    private AbilityAdmissionContext AdmissionContext(AbilityResolutionState cast) =>
        new(
            program, resourceAbilities,
            cast.ExpressionContext(), cast.Reachability, cast.Power,
            cast.HasContinuation);

    // The executor owns this one-way snapshot boundary. Structural execution
    // receives values, never AbilityResolutionState or an execution callback.
    private AbilityStructuralContext StructuralContext(AbilityResolutionState cast) =>
        new(
            program, resourceAbilities,
            cast.ExpressionContext(), cast.Reachability, cast.Trigger,
            cast.Source.FaceId, cast.AbilityFace,
            cast.Player, cast.Position, cast.HasContinuation, cast.Tier,
            cast.Power, cast.AbilityActor, cast.HasPendingDependency,
            cast.ValidatedCrisisIgnoringThwarts, cast.RestoredCrisisIgnoringThwarts,
            [.. cast.StructuralPath]);

    private bool CanInitiate(CompiledCardAbility ability, AbilityResolutionState cast)
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

    private bool CanInitiate(AbilityEffect effect, AbilityResolutionState cast) =>
        ApplyAdmission(AbilityInitiation.Admit(effect, AdmissionContext(cast)), cast);

    private static bool BindingCanChange(AbilityEffect? effect) =>
        AbilityBindingAnalysis.BindingCanChange(effect);

    private static bool BindingCanChange(AbilityPlayerSelection players) =>
        AbilityBindingAnalysis.BindingCanChange(players);

    private bool SuspendsPowerEffect(
        AbilityEffect effect, AbilityResolutionState cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        AbilityInitiation.SuspendsPowerEffect(
            effect, AdmissionContext(cast), stateMayChange, bindingMayChange);

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
    private long ForEachCount(AbilityEffect effect, AbilityResolutionState cast) =>
        AbilityInitiation.ForEachCount(effect, AdmissionContext(cast));
    private bool CanDraw(AbilityEffect effect, AbilityResolutionState cast) =>
        AbilityInitiation.CanDraw(effect, AdmissionContext(cast));
    private static bool CanDraw(Marvel.Rules.State.World world, int player) =>
        AbilityInitiation.CanDraw(world, player);
    private bool LastingPeriodIsOpen(string until, AbilityResolutionState cast) =>
        AbilityInitiation.LastingPeriodIsOpen(until, AdmissionContext(cast));
    private void PreflightContinuationBoundaries(AbilityEffect effect, AbilityResolutionState cast) =>
        AbilityInitiation.PreflightContinuationBoundaries(effect, AdmissionContext(cast));
    private bool PriorStepCanChange(AbilityCondition condition, AbilityResolutionState cast) =>
        AbilityInitiation.PriorStepCanChange(condition, AdmissionContext(cast));

    private static bool ApplyAdmission(AbilityAdmissionResult result, AbilityResolutionState cast)
    {
        foreach (var thwart in result.CrisisIgnoringThwarts)
        {
            cast.ValidateCrisisIgnoringThwart(thwart);
        }
        return result.IsAdmissible;
    }
}
