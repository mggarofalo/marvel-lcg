using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmissionMutation;
using static Marvel.Cards.Run.AbilityBindingReachability;
using static Marvel.Cards.Run.AbilityInitiationConstraints;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityAdmission
{
    internal enum AdmissionResolution
    {
        None,
        Partial,
        Full,
    }

    internal static bool TargetsAreValid(
        AbilityEffect effect, AbilityAdmissionContext context) =>
        TargetLegalityOf(effect, new AbilityAdmissionScope(context, []))
            != TargetLegality.Invalid;

    internal static bool IsOptionLegal(
        AbilityEffect option, AbilityAdmissionContext context)
    {
        var scope = new AbilityAdmissionScope(context, []);
        return OptionIsLegal(option, scope);
    }

    internal static List<Card> LegalCardChoices(
        AbilityEffect choice, AbilityAdmissionContext context) =>
        LegalCardChoices(choice, new AbilityAdmissionScope(context, []));

    internal static bool OptionIsLegal(
        AbilityEffect option, AbilityAdmissionScope scope)
    {
        bool canInitiate = CanInitiate(option, scope);
        return canInitiate
            && TargetLegalityOf(option, scope) != TargetLegality.Invalid
            && (!IsPlayerCard(scope) || CanPartiallyResolve(option, scope));
    }

    internal static List<Card> LegalCardChoices(
        AbilityEffect choice, AbilityAdmissionScope scope)
    {
        var legal = new List<Card>();
        foreach (var card in Every(
            EffectOf<AbilityEffect.ChooseCard>(choice, scope).From, scope))
        {
            var candidate = scope.ForReachability(scope.Reachability);
            candidate.ChooseSelection(card);
            if (AbilityConditionalCardChoice.HasEffect(EffectBody(choice), candidate)
                && TargetLegalityOf(EffectBody(choice), candidate) != TargetLegality.Invalid)
            {
                legal.Add(card);
            }
        }
        return legal;
    }

    internal static bool IsContinuationAdmissible(
        AbilityEffect effect, AbilityAdmissionContext context) =>
        CanInitiateSequence(effect, new AbilityAdmissionScope(context, []))
        && TargetLegalityOf(effect, new AbilityAdmissionScope(context, []))
            != TargetLegality.Invalid;

    internal static BindingCandidateState BindingCandidates(
        AbilityEffect effect, AbilityAdmissionContext context,
        BindingCandidateState before) =>
        BindingCandidatesAfter(effect, new AbilityAdmissionScope(context, []), before);

    internal static long ForEachCount(AbilityEffect effect, AbilityAdmissionContext context) =>
        AbilityEffectAdmissionConstraints.ForEachCount(
            effect, new AbilityAdmissionScope(context, []));
    internal static bool CanDraw(AbilityEffect effect, AbilityAdmissionContext context) =>
        AbilityRepeatedStatusTrace.CanDraw(
            effect, new AbilityAdmissionScope(context, []));
    internal static bool LastingPeriodIsOpen(string until, AbilityAdmissionContext context) =>
        AbilityEffectAdmissionConstraints.LastingPeriodIsOpen(
            until, new AbilityAdmissionScope(context, []));
    internal static void PreflightContinuationBoundaries(
        AbilityEffect effect, AbilityAdmissionContext context) =>
        AbilityBindingReachability.PreflightContinuationBoundaries(
            effect, new AbilityAdmissionScope(context, []));
    internal static bool PriorStepCanChange(
        AbilityCondition condition, AbilityAdmissionContext context) =>
        AbilityAdmissionMutation.PriorStepCanChange(
            condition, new AbilityAdmissionScope(context, []));

    internal static IEnumerable<AbilityEffect> PowerEffects(AbilityEffect effect, string power) =>
        PowerNodes(effect, power);

    internal sealed class AbilityAdmissionScope(
        AbilityAdmissionContext context, HashSet<AbilityEffect> evidence)
    {
        internal AbilityAdmissionContext Context { get; private set; } = context;
        internal World World => Context.World;
        internal Card Source => Context.Source;
        internal Occurrence Occurrence => Context.Expressions.Occurrence;
        internal int Player => Context.Expressions.Player;
        internal Card? Chosen => Context.Query.Chosen;
        internal Card? PlayerSelection => Context.Query.PlayerSelection;
        internal AbilityReachabilityContext Reachability => Context.Reachability;
        internal bool HasContinuation => Context.HasContinuation;
        internal long PowerAmount => Context.Expressions.PowerAmount;
        internal string? Power => Context.Power;

        internal AbilityAdmissionScope AfterBoostCancellation(AbilityEffect effect) =>
            effect is AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelBoostIcons }
                ? new(Context with { Expressions = Context.Expressions with
                    { Results = Context.Expressions.Results.SetItem("cancelledBoostIcons", Occurrence.BoostIcons?.Remaining ?? 0) } }, evidence)
                : this;

        internal AbilityAdmissionScope ForReachability(AbilityReachabilityContext reachability) =>
            new(Context.WithReachability(reachability), evidence);

        internal void SetContinuation(bool value) =>
            Context = Context.WithContinuation(value);

        internal AbilityCardReference? CaptureChosen() => Context.Query.ChosenBinding;
        internal AbilityCardReference? CapturePlayerSelection() =>
            Context.Query.PlayerSelectionBinding;

        internal void RestoreChosen(AbilityCardReference? binding) =>
            Context = Context.WithQuery(Context.Query with { ChosenBinding = binding });

        internal void RestorePlayerSelection(AbilityCardReference? binding) =>
            Context = Context.WithQuery(Context.Query with { PlayerSelectionBinding = binding });

        internal void Choose(Card? card) => Context = Context.WithChosen(card);
        internal void ChooseSelection(Card? card) => Context = Context.WithSelection(card);
        internal void RestorePlayer(int player) => Context = Context.WithPlayer(player);
        internal void ValidateCrisisIgnoringThwart(AbilityEffect node) => evidence.Add(node);

        internal AbilityAdmissionScope ForConstant(Card source)
        {
            var bindings = new AbilityQueryContext(
                World, source, new Occurrence(0, []),
                AbilityCardQueries.ControllerOf(World, source), source.Incarnation,
                null, null, null, []);
            var expressions = new AbilityExpressionContext(
                bindings, ImmutableDictionary<string, long>.Empty, [],
                string.Empty, -1, false, null);
            return new AbilityAdmissionScope(
                new AbilityAdmissionContext(
                    Context.Program, Context.ResourceAbilities,
                    expressions, Reachability, Power), evidence);
        }
    }

    internal static bool HasPartialResolutionTargets(AbilityEffect node, AbilityAdmissionScope cast) =>
        AbilityLeafTargetAvailability.HasTargets(node, cast);

    /// <summary>Whether every choice required to initiate this effect has an answer.</summary>
    internal static bool CanInitiate(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (HasNestedEachPlayer(
            node, cast, bindingMayChange: cast.Reachability.PriorBindingMayChange))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' nests one each-player frame inside another, "
                + "which is not implemented");
        }
        if (ContainsUnsupportedPower(
            node, cast, bindingMayChange: cast.Reachability.PriorBindingMayChange))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented");
        }
        return node.OperationName() switch
        {
            "seq" => CanInitiateSequence(node, cast),
            "and" => CanInitiateAnd(node, cast),
            "if" => CanInitiateIf(node, cast),
            "forEach" => CanInitiateForEach(node, cast),
            "eachTime" => CanInitiateEachTime(node, cast),
            "then" => CanInitiateDependent(
                node, cast, AdmissionResolution.Full, "then"),
            "otherwise" => CanInitiateDependent(
                node, cast, AdmissionResolution.None, "otherwise"),
            _ => CanInitiateLeaf(node, cast),
        };
    }

    internal static bool CanInitiateSequence(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var steps = OrderedEffects(node).ToList();
        var state = new SequenceAdmissionState(cast);
        for (int step = 0; step < steps.Count; step++)
        {
            var scope = state.ScopeFor(step);
            scope.SetContinuation(cast.HasContinuation || step < steps.Count - 1);
            if (!SequenceStepCanInitiate(steps, step, scope))
            {
                return false;
            }
            state.Advance(steps, step, scope);
        }
        return true;
    }

    private static bool SequenceStepCanInitiate(
        List<AbilityEffect> steps, int step, AbilityAdmissionScope scope)
    {
        if (step > 0)
        {
            PreflightDependentOutcomesAfterMutation(steps[step], scope);
        }
        return CanInitiate(steps[step], scope)
            && (step + 1 == steps.Count
                || AbilityChoiceStability.ChoicesHaveStableAreaContinuation(
                    steps[step], steps.Skip(step + 1).ToList(), scope));
    }

    private sealed class SequenceAdmissionState
    {
        private readonly AbilityAdmissionScope _cast;
        private readonly AbilityReachabilityContext _before;
        private ulong _priorFormChanges;
        private bool _priorBinding;
        private BindingCandidateState _priorCandidates;
        private readonly List<AbilityEffect> _priorSteps;

        internal SequenceAdmissionState(AbilityAdmissionScope cast)
        {
            _cast = cast;
            _before = cast.Reachability;
            _priorFormChanges = _before.PriorFormsMayChange;
            _priorBinding = _before.PriorBindingMayChange;
            _priorCandidates = new BindingCandidateState(
                _before.PriorBindingCandidates,
                _before.PriorBindingMayBeEmpty
                    || _before.PriorBindingCandidates.Count == 0 && cast.Chosen is null);
            _priorSteps = _before.PriorSteps.ToList();
        }

        internal AbilityAdmissionScope ScopeFor(int step) =>
            _cast.ForReachability(_before with
            {
                PriorStepMayMutate = _before.PriorStepMayMutate || step > 0,
                PriorSteps = _priorSteps.ToImmutableList(),
                PriorFormsMayChange = _priorFormChanges,
                PriorBindingMayChange = _priorBinding,
                PriorBindingCandidates = _priorCandidates.Cards.ToImmutableList(),
                PriorBindingMayBeEmpty = _priorCandidates.MayBeEmpty,
            });

        internal void Advance(
            List<AbilityEffect> steps, int step, AbilityAdmissionScope scope)
        {
            var effect = steps[step];
            _priorFormChanges = FormsMayDifferAfter(
                effect, scope, _priorFormChanges, _priorBinding);
            _priorBinding = BindingMayChangeAfter(effect, scope, _priorBinding);
            _priorCandidates = effect.OperationName() == "choose" && step + 1 < steps.Count
                ? ChoiceBindingCandidatesAfter(
                    effect, scope, _priorCandidates, steps.Skip(step + 1).ToList())
                : BindingCandidatesAfter(effect, scope, _priorCandidates);
            _priorSteps.Add(effect);
        }
    }

}
