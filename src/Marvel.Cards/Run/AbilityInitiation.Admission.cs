using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityDelayedReachability;
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

    internal static AbilityAdmissionResult Admit(
        AbilityEffect effect, AbilityAdmissionContext context)
    {
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
        var evidence = new HashSet<AbilityEffect>(ReferenceEqualityComparer.Instance);
        bool admissible = CanInitiate(
            effect, new AbilityAdmissionScope(context, evidence));
        return new AbilityAdmissionResult(
            admissible,
            ImmutableHashSet.CreateRange<AbilityEffect>(
                ReferenceEqualityComparer.Instance, evidence));
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
            if (TargetLegalityOf(EffectBody(choice), candidate) != TargetLegality.Invalid)
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

    /// <summary>Target existence for leaf options without a dedicated partial-resolution check.</summary>
    internal static bool HasPartialResolutionTargets(AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
    {
        "reveal" or "returnToHand" =>
            Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Count > 0,
        "soakDamage" or "attachTo" or "discard" =>
            Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is not null,
        "giveStatus" => StatusTargets(node, cast).Count > 0,
        "declareDefender" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } declared
            && Attack.CanDeclareByAbility(
                cast.World, cast.World.Facts, declared,
                ReplaceableDefenseDefender(cast)),
        "grantUntil" => Find(GrantSelectionOf(node, cast), cast) is not null,
        "dealEncounterCard" => Find(EffectOf<AbilityEffect.DealEncounterCard>(node, cast).Card, cast) is not null,
        "indirectDamage" => Amount(EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast) <= 0
            || Assignable(DamageSelectionOf(node, cast), cast).Count > 0,
        "dealDamage" or "dealAttackDamage" => DamageTargets(DamageSelectionOf(node, cast), cast).Count > 0,
        "placeThreat" => Every(ThreatSelectionOf(node, cast), cast).Count > 0,
        "placeAccelerationToken" => cast.World.TheCardIn(DeckType.MainSchemesArea) is not null,
        "enemyAttacks" or "enemySchemes" => Every(ActivationOf(node, cast).Enemies, cast).Count > 0,
        "putIntoPlay" => Find(EffectOf<AbilityEffect.PutIntoPlay>(node, cast).Card, cast) is not null,
        "placeAtRandom" => Find(EffectOf<AbilityEffect.PlaceAtRandom>(node, cast).Host, cast) is not null,
        "search" => HasSearchableArea(node, cast),

        // The delayed effect's game element comes from its future occurrence;
        // rr:target.5 requires no target at initiation.
        "delayUntil" => true,
        "generate" or "preventDamage" or "cancelWhenRevealed" or "cancelOccurrence"
            or "dealEncounterCards" or "revealTop" or "discardUntil"
            or "recoverDiscardedByResource" or "shuffleInto" or "shuffle" => true,
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses '{node.OperationName()}' in an option whose target "
            + "legality is not implemented"),
    };

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
                || ChoicesHaveStableAreaContinuation(
                    steps[step], steps.Skip(step + 1).ToList(), scope));
    }

    internal static bool ChoicesHaveStableAreaContinuation(
        AbilityEffect effect, IReadOnlyList<AbilityEffect> suffix, AbilityAdmissionScope cast)
    {
        var sensitiveAreas = new HashSet<DeckType>();
        foreach (var step in suffix)
        {
            AbilityAdmissionAreaDependencies.Collect(
                step, cast.Context, sensitiveAreas);
        }
        if (sensitiveAreas.Count == 0)
        {
            return true;
        }

        return ChoicesAreStable(effect, sensitiveAreas, cast);
    }

    internal static bool ChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        if (effect.OperationName() == "seq")
        {
            return SequenceChoicesAreStable(effect, sensitiveAreas, cast);
        }

        var priorChosen = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            return ChoicesAreStableByOperation(effect, sensitiveAreas, cast);
        }
        finally
        {
            cast.RestoreChosen(priorChosen);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static bool SequenceChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        var steps = OrderedEffects(effect).ToList();
        for (int step = 0; step < steps.Count; step++)
        {
            var after = new HashSet<DeckType>(sensitiveAreas);
            foreach (var later in steps.Skip(step + 1))
            {
                AbilityAdmissionAreaDependencies.Collect(later, cast.Context, after);
            }
            if (!ChoicesAreStable(steps[step], after, cast))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ChoicesAreStableByOperation(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast) =>
        effect.OperationName() switch
        {
            "choose" => ChoiceOptionsAreStable(effect, sensitiveAreas, cast),
            "chooseCard" => CardChoicesAreStable(effect, sensitiveAreas, cast),
            "forEach" when CurrentlyZeroForEach(effect, cast) => true,
            "eachPlayer" => EachPlayerChoicesAreStable(effect, sensitiveAreas, cast),
            _ => ChildChoicesAreStable(effect, sensitiveAreas, cast),
        };

    private static bool ChoiceOptionsAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast) =>
        ((AbilityEffect.Choose)effect).Options.Any(option =>
            OptionIsLegal(option, cast)
            && !MayChangeAnyArea(option, sensitiveAreas, cast)
            && ChoicesAreStable(option, sensitiveAreas, cast));

    private static bool CardChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        var chosenEffect = EffectBody(effect);
        return LegalCardChoices(effect, cast).Any(candidate =>
        {
            cast.ChooseSelection(candidate);
            return !MayChangeAnyArea(chosenEffect, sensitiveAreas, cast)
                && ChoicesAreStable(chosenEffect, sensitiveAreas, cast);
        });
    }

    private static bool EachPlayerChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        int priorPlayer = cast.Player;
        try
        {
            return cast.World.PlayerOrder.All(player =>
            {
                cast.RestorePlayer(player);
                return ChoicesAreStable(EffectBody(effect), sensitiveAreas, cast);
            });
        }
        finally
        {
            cast.RestorePlayer(priorPlayer);
        }
    }

    private static bool ChildChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        var children = effect.OperationName() switch
        {
            "if" => ReachableMutationBranches(effect, cast),
            "forEach" => [EffectBody(effect)],
            _ => ResolutionChildren(effect),
        };
        return children.All(child => ChoicesAreStable(child, sensitiveAreas, cast));
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
