using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>Target existence for leaf options without a dedicated partial-resolution check.</summary>
    private static bool HasPartialResolutionTargets(AbilityEffect node, Cast cast) => node.OperationName() switch
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

    /// <summary>Whether one authored ability can begin before any cost is paid.</summary>
    private static bool CanInitiate(CompiledCardAbility ability, Cast cast)
    {
        cast = cast.ForReachability(cast.Reachability with { CheckingInitiation = true });
        if (!CanInitiateLabels(ability, cast))
        {
            return false;
        }
        cast.LabelsPreflighted = true;
        if (ability.Labels.Length > 0
            && LabeledAbilities.WouldBeCancelled(
                cast.World, cast.World.Facts, Resolver(cast),
                cast.Source, ability.Labels!))
        {
            return true;
        }
        return CanInitiate(ability.Effect, cast)
            && TargetLegalityOf(ability.Effect, cast) != TargetLegality.Invalid;
    }

    /// <summary>Whether an ability envelope can establish every labeled lifecycle.</summary>
    private static bool CanInitiateLabels(CompiledCardAbility ability, Cast cast)
    {
        var labels = ability.Labels;
        if (labels.Length == 0)
        {
            return true;
        }

        bool cancelled = LabeledAbilities.WouldBeCancelled(
            cast.World, cast.World.Facts, Resolver(cast), cast.Source, labels);

        foreach (string power in LabeledAbilities.Known)
        {
            if (!labels.Contains(power, StringComparer.Ordinal)
                && PowerNodes(ability.Effect, power).Any())
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' contains a {power.ToLowerInvariant()} power "
                    + "that is absent from its ability labels");
            }
        }

        if (!cancelled
            && labels.Contains(Attack.DefenseVerb, StringComparer.Ordinal)
            && !Attack.CanUseDefenseAbility(cast.World, Resolver(cast)))
        {
            return false;
        }

        if (!cancelled)
        {
            bool attack = labels.Contains(BasicPowers.AttackVerb, StringComparer.Ordinal);
            bool thwart = labels.Contains(BasicPowers.ThwartVerb, StringComparer.Ordinal);

            if (attack && thwart)
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has one ability labeled as both attack and "
                    + "thwart, whose single combined power occurrence is not implemented");
            }
            if (attack && !GuaranteesOneLabeledPower(
                    ability.Effect, BasicPowers.AttackVerb))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has an attack label without exactly one "
                    + "saveable attack power");
            }
            if (thwart && !GuaranteesOneLabeledPower(
                    ability.Effect, BasicPowers.ThwartVerb))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a thwart label without exactly one "
                    + "saveable thwart power");
            }
        }

        return true;
    }

    /// <summary>Whether every executable path enters one matching saveable power.</summary>
    private static bool GuaranteesOneLabeledPower(AbilityEffect node, string power)
    {
        if (string.Equals(
            node.OperationName(), power.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return PowerNodes(node, power).Count() == 1;
        }

        if (node.OperationName() == "chooseCard")
        {
            return GuaranteesOneLabeledPower(EffectBody(node), power);
        }

        if (node.OperationName() == "choose")
        {
            var options = ((AbilityEffect.Choose)node).Options.ToList();
            return options.Count >= 2
                && options.All(option => GuaranteesOneLabeledPower(option, power));
        }

        if (node.OperationName() == "if")
        {
            return ConditionalBranch(node, "then") is { } then
                && ConditionalBranch(node, "else") is { } otherwise
                && GuaranteesOneLabeledPower(then, power)
                && GuaranteesOneLabeledPower(otherwise, power);
        }

        if (node.OperationName() == "seq")
        {
            var steps = OrderedEffects(node).ToList();
            return steps.Count > 0
                && GuaranteesOneLabeledPower(steps[0], power)
                && steps.Skip(1).All(step => !PowerNodes(step, power).Any());
        }

        return false;
    }

    /// <summary>Whether every choice required to initiate this effect has an answer.</summary>
    private static bool CanInitiate(AbilityEffect node, Cast cast)
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
                node, cast, ResolutionOutcome.Full, "then"),
            "otherwise" => CanInitiateDependent(
                node, cast, ResolutionOutcome.None, "otherwise"),
            _ => CanInitiateLeaf(node, cast),
        };
    }

    private static bool CanInitiateSequence(AbilityEffect node, Cast cast)
    {
        var steps = OrderedEffects(node).ToList();
        var before = cast.Reachability;
        ulong priorFormChanges = before.PriorFormsMayChange;
        bool priorBinding = before.PriorBindingMayChange;
        var priorCandidates = new BindingCandidateState(
            before.PriorBindingCandidates,
            before.PriorBindingMayBeEmpty
                || before.PriorBindingCandidates.Count == 0 && cast.Chosen is null);
        var priorSteps = before.PriorSteps.ToList();
        for (int step = 0; step < steps.Count; step++)
        {
            var scope = cast.ForReachability(before with
            {
                PriorStepMayMutate = before.PriorStepMayMutate || step > 0,
                PriorSteps = priorSteps.ToImmutableList(),
                PriorFormsMayChange = priorFormChanges,
                PriorBindingMayChange = priorBinding,
                PriorBindingCandidates = priorCandidates.Cards.ToImmutableList(),
                PriorBindingMayBeEmpty = priorCandidates.MayBeEmpty,
            });
            scope.SetContinuation(cast.HasContinuation || step < steps.Count - 1);
            if (step > 0)
            {
                PreflightDependentOutcomesAfterMutation(steps[step], scope);
            }
            if (!CanInitiate(steps[step], scope))
            {
                return false;
            }
            if (step + 1 < steps.Count
                && !ChoicesHaveStableAreaContinuation(
                    steps[step], steps.Skip(step + 1).ToList(), scope))
            {
                return false;
            }
            priorFormChanges = FormsMayDifferAfter(
                steps[step], scope, priorFormChanges, priorBinding);
            priorBinding = BindingMayChangeAfter(
                steps[step], scope, priorBinding);
            priorCandidates = steps[step].OperationName() == "choose"
                && step + 1 < steps.Count
                    ? ChoiceBindingCandidatesAfter(
                        steps[step], scope, priorCandidates,
                        steps.Skip(step + 1).ToList())
                    : BindingCandidatesAfter(
                        steps[step], scope, priorCandidates);
            priorSteps.Add(steps[step]);
        }
        return true;
    }

    private static bool ChoicesHaveStableAreaContinuation(
        AbilityEffect effect, IReadOnlyList<AbilityEffect> suffix, Cast cast)
    {
        var sensitiveAreas = new HashSet<DeckType>();
        foreach (var step in suffix)
        {
            CollectSingularAreaDependencies(step, cast, sensitiveAreas);
        }
        if (sensitiveAreas.Count == 0)
        {
            return true;
        }

        return ChoicesAreStable(effect, sensitiveAreas, cast);
    }

    private static bool ChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, Cast cast)
    {
        if (effect.OperationName() == "seq")
        {
            var steps = OrderedEffects(effect).ToList();
            for (int step = 0; step < steps.Count; step++)
            {
                var after = new HashSet<DeckType>(sensitiveAreas);
                foreach (var later in steps.Skip(step + 1))
                {
                    CollectSingularAreaDependencies(later, cast, after);
                }
                if (!ChoicesAreStable(steps[step], after, cast))
                {
                    return false;
                }
            }
            return true;
        }

        var priorChosen = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();

        try
        {
            if (effect.OperationName() == "choose")
            {
                return ((AbilityEffect.Choose)effect).Options.Any(option =>
                    OptionIsLegal(option, cast)
                    && !MayChangeAnyArea(option, sensitiveAreas, cast)
                    && ChoicesAreStable(option, sensitiveAreas, cast));
            }

            if (effect.OperationName() == "chooseCard")
            {
                var chosenEffect = EffectBody(effect);
                return LegalCardChoices(effect, cast).Any(candidate =>
                {
                    cast.ChooseSelection(candidate);
                    return !MayChangeAnyArea(
                            chosenEffect, sensitiveAreas, cast)
                        && ChoicesAreStable(
                            chosenEffect, sensitiveAreas, cast);
                });
            }

            if (effect.OperationName() == "forEach" && CurrentlyZeroForEach(effect, cast))
            {
                return true;
            }
            if (effect.OperationName() == "eachPlayer")
            {
                int priorPlayer = cast.Player;
                try
                {
                    return cast.World.PlayerOrder.All(player =>
                    {
                        cast.RestorePlayer(player);
                        return ChoicesAreStable(
                            EffectBody(effect), sensitiveAreas, cast);
                    });
                }
                finally
                {
                    cast.RestorePlayer(priorPlayer);
                }
            }

            var children = effect.OperationName() switch
            {
                "if" => ReachableMutationBranches(effect, cast),
                "forEach" => [EffectBody(effect)],
                _ => ResolutionChildren(effect),
            };
            return children.All(child =>
                ChoicesAreStable(child, sensitiveAreas, cast));
        }
        finally
        {
            cast.RestoreChosen(priorChosen);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static void PreflightDependentOutcomesAfterMutation(
        AbilityEffect node, Cast cast)
    {
        if (node.OperationName() == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.OperationName() is "then" or "otherwise")
        {
            PreflightResolutionBranches(
                EffectBody(node), cast, allBranches: true);
        }

        var children = node.OperationName() switch
        {
            "choose" => ((AbilityEffect.Choose)node).Options,
            "eachPlayer" => [EffectBody(node)],
            _ => ResolutionChildren(node),
        };
        foreach (var child in children)
        {
            PreflightDependentOutcomesAfterMutation(child, cast);
        }
    }

    private static bool CanInitiateIf(AbilityEffect node, Cast cast)
    {
        // Payment happens after an action is offered and can change the facts
        // tested by the branch. Validate every structurally reachable
        // continuation boundary now, while no cost has been paid, then use
        // only the currently active branch for ordinary target eligibility.
        var test = ConditionalOf(node, cast).Test;
        bool paymentCanSwitch = cast.Reachability.PaymentMayMutate && PaymentCanChange(test);
        bool bindingCanSwitch = cast.Reachability.PriorBindingMayChange
            && BindingCanChange(test);
        bool stateCanSwitch = bindingCanSwitch
            || PriorStepCanChange(test, cast) || paymentCanSwitch;
        bool reachableLabelledTargetsAreValid = true;
        foreach (var branch in ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null))
        {
            var effect = branch;
            PreflightContinuationBoundaries(effect, cast);
            if (stateCanSwitch)
            {
                PreflightDependentOutcomesAfterMutation(effect, cast);
                PreflightInitiationConstraints(
                    effect, cast, requireCurrentTargets: paymentCanSwitch);
                if (HasLabelledPower(effect) && !CanInitiate(effect, cast))
                {
                    // A prior step or payment can select this branch after the
                    // ability has begun. Refuse now if its labelled target is
                    // not valid on the initiation board; discovering that only
                    // after the mutation would leave a partial action behind.
                    reachableLabelledTargetsAreValid = false;
                }
            }
        }

        if (!reachableLabelledTargetsAreValid)
        {
            return false;
        }
        if (bindingCanSwitch)
        {
            return ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(value => value is not null)
                .All(value => CanInitiate(value, cast));
        }
        return ConditionalBranch(node, Test(test, cast) ? "then" : "else")
            is not { } active || CanInitiate(active, cast);
    }

    private static bool PriorStepCanChange(AbilityCondition test, Cast cast) => test switch
    {
        AbilityCondition.All all => all.Operands.Any(child =>
            PriorStepCanChange(child, cast)),
        AbilityCondition.Any any => any.Operands.Any(child =>
            PriorStepCanChange(child, cast)),
        AbilityCondition.Negated negated => PriorStepCanChange(negated.Operand, cast),
        AbilityCondition.InForm form => cast.Reachability.PriorBindingMayChange
                && BindingCanChange(test)
            || SeatMayChange(cast.Reachability.PriorFormsMayChange, Seat(form.Player, cast)),
        _ => cast.Reachability.PriorStepMayMutate,
    };

    /// <summary>Seats whose final form may differ after a reachable effect.</summary>
    private static ulong FormsMayDifferAfter(
        AbilityEffect node, Cast cast, ulong before,
        bool bindingMayChange = false)
    {
        if (node.OperationName() == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.OperationName() == "changeForm")
        {
            var change = FormChangeOf(node, cast);
            string destination = change.Form;
            var player = change.Player;
            bool canChangePlayer = player == AbilityPlayer.ChosenPlayer;
            var seats = bindingMayChange && canChangePlayer
                ? cast.World.PlayerOrder.ToList()
                : [Seat(player, cast)];
            ulong ChangeOne(ulong state, int seat)
            {
                ulong bit = PlayerSeat(seat);
                return Forms.In(
                        cast.World, cast.World.Seats[seat], cast.World.Facts,
                        destination)
                    ? state & ~bit
                    : state | bit;
            }
            if (bindingMayChange && canChangePlayer)
            {
                return seats.Aggregate(0UL, (possible, seat) =>
                    possible | ChangeOne(before, seat));
            }
            ulong after = before;
            foreach (int seat in seats)
            {
                after = ChangeOne(after, seat);
            }
            return after;
        }

        cast = cast.ForReachability(cast.Reachability with { PriorFormsMayChange = before });
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            bool canSwitch = bindingMayChange
                    && BindingCanChange(test)
                || PriorStepCanChange(test, cast)
                || cast.Reachability.PaymentMayMutate && PaymentCanChange(test);
            var branches = canSwitch
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Select(branch =>
                    FormsMayDifferAfter(
                        branch, cast, before, bindingMayChange))
                .DefaultIfEmpty(before)
                .Aggregate((left, right) => left | right);
        }
        if (node.OperationName() == "choose")
        {
            return ((AbilityEffect.Choose)node).Options.Select(option =>
                    FormsMayDifferAfter(
                        option, cast, before, bindingMayChange))
                .DefaultIfEmpty(before)
                .Aggregate((left, right) => left | right);
        }
        if (node.OperationName() == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                ulong possible = 0;
                foreach (var order in PlayerPermutations(
                    cast.World.PlayerOrder.ToList()))
                {
                    ulong after = before;
                    foreach (int player in order)
                    {
                        cast.RestorePlayer(player);
                        cast = cast.ForReachability(cast.Reachability with { PriorFormsMayChange = after });
                        after = FormsMayDifferAfter(
                            EffectBody(node), cast, after,
                            bindingMayChange);
                    }
                    possible |= after;
                }
                return possible;
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }

        ulong state = before;
        bool childBindingMayChange = bindingMayChange
            || node.OperationName() is "chooseCard" or "thwartSchemes"
                or "thwartDifferentSchemes" or "legalPractice";
        foreach (var child in MutationChildren(node))
        {
            cast = cast.ForReachability(cast.Reachability with { PriorFormsMayChange = state });
            state = FormsMayDifferAfter(
                child, cast, state, childBindingMayChange);
        }
        return state;
    }

    private static IEnumerable<IReadOnlyList<int>> PlayerPermutations(
        List<int> players)
    {
        if (players.Count == 0)
        {
            yield return [];
            yield break;
        }
        for (int index = 0; index < players.Count; index++)
        {
            int player = players[index];
            var rest = players.Where((_, candidate) => candidate != index).ToList();
            foreach (var tail in PlayerPermutations(rest))
            {
                yield return [player, .. tail];
            }
        }
    }

    private static bool BindingMayChangeAfter(
        AbilityEffect node, Cast cast, bool before)
    {
        if (node.OperationName() is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            return true;
        }
        if (node.OperationName() == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            bool canSwitch = before && BindingCanChange(test)
                || PriorStepCanChange(test, cast)
                || cast.Reachability.PaymentMayMutate && PaymentCanChange(test);
            var branches = canSwitch
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Any(branch =>
                BindingMayChangeAfter(branch, cast, before));
        }
        bool after = before;
        foreach (var child in MutationChildren(node))
        {
            after = BindingMayChangeAfter(child, cast, after);
        }
        return after;
    }

    private sealed record BindingCandidateState(
        IReadOnlyList<Card> Cards, bool MayBeEmpty);

    private static BindingCandidateState BindingCandidatesAfter(
        AbilityEffect node, Cast cast, BindingCandidateState before)
    {
        if (node.OperationName() is "attack" or "thwart")
        {
            // A labelled power owns `chosen` only inside its wrapper. Its
            // effect cannot suspend, and the outer binding resumes afterwards.
            return before;
        }
        if (node.OperationName() == "chooseCard")
        {
            return ChooseCardBindingCandidatesAfter(node, cast, before);
        }
        if (node.OperationName() == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.OperationName() == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var possible = new List<Card>();
                bool mayBeEmpty = false;
                foreach (var order in PlayerPermutations(
                    cast.World.PlayerOrder.ToList()))
                {
                    // Each scheduled frame restores the same persisted outer
                    // binding. Only the final frame supplies the binding seen
                    // by the continuation; earlier frames do not feed it.
                    cast.RestorePlayer(order[^1]);
                    var finalFrame = BindingCandidatesAfter(
                        EffectBody(node), cast, before);
                    possible.AddRange(finalFrame.Cards);
                    mayBeEmpty |= finalFrame.MayBeEmpty;
                }
                return new BindingCandidateState(
                    possible.DistinctBy(card => card.ObjectId).ToList(),
                    mayBeEmpty);
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            bool canSwitch = before.Cards.Count > 0
                    && BindingCanChange(test)
                || PriorStepCanChange(test, cast)
                || cast.Reachability.PaymentMayMutate && PaymentCanChange(test);
            var branches = canSwitch
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            var outcomes = branches.Select(branch => BindingCandidatesAfter(
                    branch, cast, before))
                .ToList();
            return new BindingCandidateState(
                outcomes.SelectMany(outcome => outcome.Cards)
                    .DistinctBy(card => card.ObjectId)
                    .ToList(),
                outcomes.Count == 0 || outcomes.Any(outcome => outcome.MayBeEmpty));
        }
        if (node.OperationName() == "choose")
        {
            return ChoiceBindingCandidatesAfter(node, cast, before);
        }
        var candidates = before;
        foreach (var child in MutationChildren(node))
        {
            candidates = BindingCandidatesAfter(child, cast, candidates);
        }
        return candidates;
    }

    private static BindingCandidateState ChooseCardBindingCandidatesAfter(
        AbilityEffect node, Cast cast, BindingCandidateState before)
    {
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var possible = new List<Card>();
        bool mayBeEmpty = false;
        try
        {
            void AddOutcome(Card? binding)
            {
                cast.ChooseSelection(binding);
                var legal = LegalCardChoices(node, cast);
                if (legal.Count > 0)
                {
                    foreach (var chosen in legal)
                    {
                        cast.ChooseSelection(chosen);
                        var afterEffect = BindingCandidatesAfter(
                            EffectBody(node), cast,
                            new BindingCandidateState([chosen], MayBeEmpty: false));
                        possible.AddRange(afterEffect.Cards);
                        mayBeEmpty |= afterEffect.MayBeEmpty;
                    }
                }
                else if (binding is not null)
                {
                    // Runtime choice resolution is a no-op when this frame has
                    // no legal target, so an earlier binding survives it.
                    possible.Add(binding);
                }
                else
                {
                    mayBeEmpty = true;
                }
            }

            foreach (var candidate in before.Cards)
            {
                AddOutcome(candidate);
            }
            if (before.MayBeEmpty)
            {
                AddOutcome(null);
            }
            if (before.Cards.Count == 0 && !before.MayBeEmpty)
            {
                AddOutcome(prior?.Card);
            }
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
        return new BindingCandidateState(
            possible.DistinctBy(card => card.ObjectId).ToList(), mayBeEmpty);
    }

    private static BindingCandidateState ChoiceBindingCandidatesAfter(
        AbilityEffect node, Cast cast, BindingCandidateState before,
        IReadOnlyList<AbilityEffect>? continuation = null)
    {
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var outcomes = new List<BindingCandidateState>();
        try
        {
            void AddOutcomes(Card? binding)
            {
                cast.ChooseSelection(binding);
                var incoming = new BindingCandidateState(
                    binding is null ? [] : [binding], binding is null);
                foreach (var option in ((AbilityEffect.Choose)node).Options
                    .Where(option => OptionIsLegal(option, cast)))
                {
                    var outcome = BindingCandidatesAfter(option, cast, incoming);
                    outcomes.Add(continuation is null
                        ? outcome
                        : FilterCandidatesForContinuation(
                            outcome, continuation, cast));
                }
            }

            foreach (var candidate in before.Cards)
            {
                AddOutcomes(candidate);
            }
            if (before.MayBeEmpty)
            {
                AddOutcomes(null);
            }
            if (before.Cards.Count == 0 && !before.MayBeEmpty)
            {
                AddOutcomes(prior?.Card);
            }
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
        return new BindingCandidateState(
            outcomes.SelectMany(outcome => outcome.Cards)
                .DistinctBy(card => card.ObjectId).ToList(),
            outcomes.Any(outcome => outcome.MayBeEmpty));
    }

    private static BindingCandidateState FilterCandidatesForContinuation(
        BindingCandidateState candidates,
        IReadOnlyList<AbilityEffect> continuation,
        Cast cast)
    {
        var suffix = new AbilityEffect.Sequence([.. continuation]);
        var legal = candidates.Cards.Where(candidate =>
        {
            var scope = cast.ForReachability(cast.Reachability with
            {
                PriorBindingCandidates = [candidate],
                PriorBindingMayBeEmpty = false,
                PriorBindingMayChange = false,
            });
            scope.ChooseSelection(candidate);
            return CanInitiateSequence(suffix, scope)
                && TargetLegalityOf(suffix, scope) != TargetLegality.Invalid;
        }).ToList();
        // An explicit empty option is the authored decline branch for “may.”
        // It remains reachable; the enclosing sequence rejects it if its
        // suffix requires a binding. Card-bearing alternatives filter separately.
        return new BindingCandidateState(legal, candidates.MayBeEmpty);
    }

    private static void PreflightContinuationBoundaries(AbilityEffect node, Cast cast)
    {
        if (node.OperationName() == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.OperationName() == "seq")
        {
            var steps = OrderedEffects(node).ToList();
            bool outerContinuation = cast.HasContinuation;
            try
            {
                for (int step = 0; step < steps.Count; step++)
                {
                    cast.SetContinuation(outerContinuation || step < steps.Count - 1);
                    PreflightContinuationBoundaries(steps[step], cast);
                }
            }
            finally
            {
                cast.SetContinuation(outerContinuation);
            }
            return;
        }

        if (node.OperationName() == "and")
        {
            _ = CanInitiateAnd(node, cast);
            return;
        }

        var children = node.OperationName() switch
        {
            "choose" => ((AbilityEffect.Choose)node).Options,
            "eachPlayer" => [EffectBody(node)],
            _ => ResolutionChildren(node),
        };
        foreach (var child in children)
        {
            PreflightContinuationBoundaries(child, cast);
        }
    }

    private static void PreflightInitiationConstraints(
        AbilityEffect node, Cast cast, bool requireCurrentTargets)
    {
        if (node.OperationName() == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.OperationName() == "grantUntil" && !LastingPeriodIsOpen(node, cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a lasting effect outside its named period");
        }
        if (node.OperationName() == "grantUntil"
            && requireCurrentTargets
            && Find(GrantSelectionOf(node, cast), cast) is null)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' may reach a lasting effect with no target after payment");
        }

        var children = node.OperationName() switch
        {
            "choose" => ((AbilityEffect.Choose)node).Options,
            "eachPlayer" => [EffectBody(node)],
            _ => ResolutionChildren(node),
        };
        foreach (var child in children)
        {
            PreflightInitiationConstraints(child, cast, requireCurrentTargets);
        }
    }

    private static bool CanInitiateAnd(AbilityEffect node, Cast cast)
    {
        var effects = OrderedEffects(node).ToList();
        if (effects.Count > 1 && effects.Any(effect => SuspendsInsideAnd(effect, cast)))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' orders simultaneous effects around a threat "
                + "placement continuation, which is not implemented");
        }
        bool outerContinuation = cast.HasContinuation;
        try
        {
            foreach (var effect in effects)
            {
                cast.SetContinuation(outerContinuation || effects.Count > 1);
                if (!CanInitiate(effect, cast))
                {
                    return false;
                }
            }
            return true;
        }
        finally
        {
            cast.SetContinuation(outerContinuation);
        }
    }

    private static bool CanInitiateDependent(
        AbilityEffect node, Cast cast, ResolutionOutcome required, string branch)
    {
        var effect = EffectBody(node);
        if (ActiveChoices(effect, cast).Any())
        {
            var choices = ActiveChoices(effect, cast).ToList();
            if (effect.OperationName() is not ("choose" or "chooseCard")
                || choices.Any(ChoiceHasNestedChoice))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has multiple-stage player choices before "
                    + $"'{node.OperationName()}', whose combined resolution outcome is not implemented");
            }
            PreflightAnsweredOutcome(effect, cast);
            return CanInitiate(effect, cast);
        }
        var outcome = EnsureDependentSupported(
            node, cast, effect, ContinuationChild(node, branch), required);
        return outcome == required
            ? CanInitiate(ContinuationChild(node, branch), cast)
            : CanInitiate(effect, cast);
    }

    private static bool ChoiceHasNestedChoice(AbilityEffect choice) => choice.OperationName() switch
    {
        "chooseCard" => Choices(EffectBody(choice)).Any(),
        "choose" => ((AbilityEffect.Choose)choice).Options.Any(option => Choices(option).Any()),
        _ => false,
    };

    private static bool CanInitiateLeaf(AbilityEffect node, Cast cast) => node.OperationName() switch
    {
        "resolveSpecials" when cast.HasContinuation =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' continues after ordered Special abilities, "
                + "which is not implemented"),
        "chooseCard" => CanInitiateChooseCard(node, cast),
        "choose" => CanInitiateChoice(node, cast),
        "draw" => CanInitiateDraw(node, cast),
        "placeCounters" => CanInitiateCounters((AbilityEffect.PlaceCounters)node, cast),
        "thwartDifferentSchemes" => Every(EffectOf<AbilityEffect.ThwartGroup>(node, cast).Schemes, cast).Count > 0,
        "legalPractice" => cast.World.Seats[cast.Player].Hand.Cards.Any(card =>
                card.ObjectId != cast.Source.ObjectId)
            && Every(EffectOf<AbilityEffect.ThwartGroup>(node, cast).Schemes, cast).Count > 0,
        "thwartSchemes" when SuspendsPowerEffect(
            ((AbilityEffect.ThwartGroup)node).Thwart.Effect, cast) =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented"),
        "thwartSchemes" => Every(EffectOf<AbilityEffect.ThwartGroup>(node, cast).Schemes, cast).Count > 0,
        "attack" or "thwart" when SuspendsPowerEffect(
            EffectBody(node), cast) =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented"),
        "attack" => CanTargetAttack(node, cast),
        "thwart" => CanTargetThwart(node, cast),
        "enemyAttacks" or "enemySchemes" => true,
        "defense" => Attack.CanUseDefenseAbility(cast.World, cast.Player)
            && CanInitiate(EffectBody(node), cast),
        // A missing dynamic target gets the resolver's specific exception
        // (for example, no activating enemy). When the target exists, the
        // lasting period itself is an initiation constraint.
        "grantUntil" => Find(GrantSelectionOf(node, cast), cast) is not null
            ? LastingPeriodIsOpen(node, cast)
            : !IsPlayerCard(cast)
                && !cast.Reachability.PaymentMayMutate
                && !cast.Reachability.PriorStepMayMutate,
        _ => true,
    };

    private static bool CanInitiateCounters(AbilityEffect.PlaceCounters counters, Cast cast)
    {
        // Admit the reads before payment or preceding effects can change their
        // candidates. Resolution computes the amount again; this is not a
        // prediction of a result binding or of counters after payment.
        if (BindingCanChange(counters.Count)
            && (cast.Chosen is null || cast.Reachability.PriorBindingMayChange)
            && cast.Reachability.PriorBindingCandidates.Count > 0)
        {
            foreach (var candidate in cast.Reachability.PriorBindingCandidates)
            {
                var probe = cast.ForReachability(cast.Reachability);
                probe.ChooseSelection(candidate);
                _ = Amount(counters.Count, probe);
            }
            if (cast.Reachability.PriorBindingMayBeEmpty)
            {
                var probe = cast.ForReachability(cast.Reachability);
                probe.ChooseSelection(null);
                _ = Amount(counters.Count, probe);
            }
        }
        else
        {
            _ = Amount(counters.Count, cast);
        }
        return true;
    }

    private static bool CanInitiateChooseCard(AbilityEffect node, Cast cast)
    {
        bool CanChooseFromCurrentBinding() =>
            (!RequiresChosenPlayer(((AbilityEffect.ChooseCard)node).From)
                || (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 })
            && LegalCardChoices(node, cast).Count > 0;

        if (cast.Reachability.PriorBindingCandidates.Count == 0
            && !cast.Reachability.PriorBindingMayBeEmpty)
        {
            return CanChooseFromCurrentBinding();
        }

        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            bool any = false;
            foreach (var candidate in cast.Reachability.PriorBindingCandidates)
            {
                cast.ChooseSelection(candidate);
                any |= CanChooseFromCurrentBinding();
            }
            if (cast.Reachability.PriorBindingMayBeEmpty)
            {
                cast.ChooseSelection(null);
                any |= CanChooseFromCurrentBinding();
            }
            return any;
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static bool CanInitiateDraw(AbilityEffect node, Cast cast)
    {
        bool CanDrawFromBinding() =>
            !BindingCanChange(((AbilityEffect.Draw)node).Players)
            || (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 }
                && CanDraw(node, cast);

        if (cast.Chosen is null && BindingCanChange(((AbilityEffect.Draw)node).Players)
            && cast.Reachability.PriorBindingCandidates.Count > 0)
        {
            if (cast.Reachability.PriorBindingMayBeEmpty)
            {
                return false;
            }
            var prior = cast.CaptureChosen();
            var priorSelection = cast.CapturePlayerSelection();
            try
            {
                bool any = cast.Reachability.PriorBindingCandidates.Any(candidate =>
                {
                    cast.ChooseSelection(candidate);
                    return CanDrawFromBinding();
                });
                return any;
            }
            finally
            {
                cast.RestoreChosen(prior);
                cast.RestorePlayerSelection(priorSelection);
            }
        }
        return CanDrawFromBinding();
    }

    private static bool CanTargetAttack(AbilityEffect node, Cast cast)
    {
        var target = EffectOf<AbilityEffect.Power>(node, cast).Target!;
        if (cast.Chosen is null && BindingCanChange(target)
            && cast.Reachability.PriorBindingCandidates.Count > 0)
        {
            return EveryCandidateCan(cast, () => CanTargetAttack(node, cast));
        }
        return Find(target, cast) is { } enemy
            && BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                .Any(candidate => candidate.ObjectId == enemy.ObjectId)
            && cast.World.Abilities.CanTakeDamage(cast.World, enemy, cast.Source);
    }

    private static bool CanTargetThwart(AbilityEffect node, Cast cast)
    {
        var power = EffectOf<AbilityEffect.Power>(node, cast);
        var target = power.Target!;
        if (cast.Chosen is null && BindingCanChange(target)
            && cast.Reachability.PriorBindingCandidates.Count > 0)
        {
            return EveryCandidateCan(cast, () => CanTargetThwart(node, cast));
        }
        if (Find(target, cast) is not { } scheme)
        {
            return false;
        }

        if (power.AutomaticTarget)
        {
            return BasicPowers.CanAutomaticallyThwart(
                cast.World, cast.World.Facts, Resolver(cast), scheme);
        }

        if (BasicPowers.Thwartable(cast.World, cast.World.Facts, Resolver(cast))
            .Any(candidate => candidate.ObjectId == scheme.ObjectId))
        {
            return true;
        }

        // rr:cannot.3 lets an explicit exception win. Crisis normally removes
        // the main scheme from Thwartable, but a scoped ignoresCrisis removal
        // can still make that declared thwart target valid. Automatic thwart
        // checks the remaining scheme-level prohibition (notably Patrol).
        bool crisisException = BasicPowers.CanAutomaticallyThwart(
                cast.World, cast.World.Facts, Resolver(cast), scheme)
            && CrisisIgnoringRemovalCanAffect(
                EffectBody(node), cast, scheme);
        if (crisisException)
        {
            cast.ValidateCrisisIgnoringThwart(node);
        }
        return crisisException;
    }

    private static bool EveryCandidateCan(Cast cast, Func<bool> test)
    {
        if (cast.Reachability.PriorBindingMayBeEmpty)
        {
            return false;
        }
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            return cast.Reachability.PriorBindingCandidates.All(candidate =>
            {
                cast.ChooseSelection(candidate);
                return test();
            });
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static bool CrisisIgnoringRemovalCanAffect(
        AbilityEffect node, Cast cast, Card scheme)
    {
        var prior = cast.CaptureChosen();
        try
        {
            // SchedulePower binds the declared power target before it runs the
            // nested effect. Offer-time legality must expose the same binding.
            cast.Choose(scheme);
            return CrisisIgnoringRemovalCanAffectBound(node, cast, scheme);
        }
        finally
        {
            cast.RestoreChosen(prior);
        }
    }

    private static bool CrisisIgnoringRemovalCanAffectBound(
        AbilityEffect node, Cast cast, Card scheme)
    {
        if (node.OperationName() == "removeThreat")
        {
            return IgnoresCrisis(node, cast)
                && Every(ThreatSelectionOf(node, cast), cast).Any(candidate =>
                    candidate.ObjectId == scheme.ObjectId)
                && scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast) > 0
                && CanRemoveThreatFrom(node, cast, scheme);
        }

        return node.OperationName() switch
        {
            "seq" or "and" => OrderedEffects(node).Any(child =>
                CrisisIgnoringRemovalCanAffectBound(child, cast, scheme)),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch
                && CrisisIgnoringRemovalCanAffectBound(branch, cast, scheme),
            "forEach" => ForEachCount(node, cast) > 0
                && CrisisIgnoringRemovalCanAffectBound(
                    EffectBody(node), cast, scheme),
            "choose" => ((AbilityEffect.Choose)node).Options.Any(option =>
                CrisisIgnoringRemovalCanAffectBound(option, cast, scheme)),
            "then" when ActiveChoices(EffectBody(node), cast).Any() =>
                CrisisIgnoringRemovalCanAffectBound(
                    EffectBody(node), cast, scheme),
            "then" => CrisisIgnoringRemovalCanAffectBound(
                    EffectBody(node), cast, scheme)
                || ResolutionOf(EffectBody(node), cast)
                    == ResolutionOutcome.Full
                && CrisisIgnoringRemovalCanAffectBound(
                    EffectFollowing(node), cast, scheme),
            "otherwise" when ActiveChoices(EffectBody(node), cast).Any() =>
                CrisisIgnoringRemovalCanAffectBound(
                    EffectBody(node), cast, scheme),
            "otherwise" => CrisisIgnoringRemovalCanAffectBound(
                    EffectBody(node), cast, scheme)
                || ResolutionOf(EffectBody(node), cast)
                    == ResolutionOutcome.None
                && CrisisIgnoringRemovalCanAffectBound(
                    EffectFollowing(node), cast, scheme),
            "eachPlayer" or "defense" => CrisisIgnoringRemovalCanAffectBound(
                EffectBody(node), cast, scheme),
            _ => false,
        };
    }

    /// <summary>Whether a tree has no current target, an invalid one, or a valid one.</summary>
    /// <remarks>
    /// <c>rr:target.2</c> asks only for “at least one valid target,” and
    /// <c>rr:target.3.4</c> says one effect on that target is enough. Keeping
    /// <see cref="TargetLegality.None"/> separate from
    /// <see cref="TargetLegality.Invalid"/> prevents an
    /// untargeted sibling from manufacturing a target while still allowing a
    /// valid sibling to make a multi-effect ability initiable.
    /// </remarks>
    private static TargetLegality TargetLegalityOf(
        AbilityEffect node, Cast cast, bool bindingMayChange = false)
    {
        TargetLegality Cards(IEnumerable<Card> candidates) =>
            candidates.Any() ? TargetLegality.Valid : TargetLegality.Invalid;

        if (cast.Chosen is null
            && cast.Reachability.PriorBindingCandidates.Count > 0
            && BindingCanChange(node)
            && node.OperationName() is not ("seq" or "and" or "if" or "then" or "otherwise"
                or "forEach" or "defense" or "choose" or "eachTime"
                or "delayUntil" or "chooseCard"))
        {
            return CandidateTargetLegality(node, cast);
        }

        return node.OperationName() switch
        {
            "seq" => SequenceTargetLegality(node, cast, bindingMayChange),
            "and" => CombineTargetLegality(
                OrderedEffects(node).Select(child =>
                    TargetLegalityOf(child, cast, bindingMayChange))),
            "if" when bindingMayChange
                    && BindingCanChange(ConditionalOf(node, cast).Test) =>
                CombineTargetLegality(ConditionalBranches((AbilityEffect.Conditional)node)
                    .Where(value => value is not null)
                    .Select(value => TargetLegalityOf(
                        value, cast, bindingMayChange))),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch
                    ? TargetLegalityOf(branch, cast, bindingMayChange)
                    : TargetLegality.None,
            "then" when ActiveChoices(EffectBody(node), cast).Any() =>
                TargetLegalityOf(
                    EffectBody(node), cast, bindingMayChange),
            "then" => ResolutionOf(EffectBody(node), cast)
                == ResolutionOutcome.Full
                    ? CombineTargetLegality(
                    [
                        TargetLegalityOf(
                            EffectBody(node), cast, bindingMayChange),
                        TargetLegalityOf(
                            EffectFollowing(node), cast, bindingMayChange),
                    ])
                    : TargetLegalityOf(
                        EffectBody(node), cast, bindingMayChange),
            "otherwise" when ActiveChoices(EffectBody(node), cast).Any() =>
                TargetLegalityOf(
                    EffectBody(node), cast, bindingMayChange),
            "otherwise" => ResolutionOf(EffectBody(node), cast)
                == ResolutionOutcome.None
                    ? TargetLegalityOf(
                        EffectFollowing(node), cast, bindingMayChange)
                    : TargetLegalityOf(
                        EffectBody(node), cast, bindingMayChange),
            "forEach" => ForEachCount(node, cast) <= 0
                ? TargetLegality.None
                : TargetLegalityOf(
                    EffectBody(node), cast, bindingMayChange),
            "attack" => CanTargetAttack(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "thwart" => CanTargetThwart(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "defense" => TargetLegalityOf(
                EffectBody(node), cast, bindingMayChange),

            // The choice itself is validated by CanInitiateChoice and
            // OptionIsLegal. Future-target lasting effects have no target node
            // in this tree, so they fall through to None.
            "choose" or "eachTime" => TargetLegality.None,
            "delayUntil" => TargetLegality.None,
            "chooseCard" => CanInitiateChooseCard(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,

            "removeFromGame" => RemoveFromGameTargetLegality(node, cast),
            "reveal" or "returnToHand" => Cards(Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)),
            "exhaust" => Cards(Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Where(card => card.Ready)),
            "ready" => Cards(Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Where(card =>
                !card.Ready && cast.Abilities.CanReady(cast.World, card, cast.Source))),
            "giveStatus" => Cards(StatusTargets(node, cast)),
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
            "dealEncounterCard" => Find(EffectOf<AbilityEffect.DealEncounterCard>(node, cast).Card, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "heal" => Find(EffectOf<AbilityEffect.Heal>(node, cast).Card, cast) is { Damage: > 0 }
                && Amount(EffectOf<AbilityEffect.Heal>(node, cast).Amount, cast) > 0
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "dealDamage" or "dealAttackDamage" =>
                Amount(DamageAmountOf(node, cast), cast) > 0
                    ? Cards(DamageTargets(DamageSelectionOf(node, cast), cast))
                    : TargetLegality.Invalid,
            "indirectDamage" => Amount(EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast) <= 0
                ? TargetLegality.Invalid
                : Cards(Assignable(DamageSelectionOf(node, cast), cast)),
            "placeThreat" => Amount(EffectOf<AbilityEffect.PlaceThreat>(node, cast).Amount, cast) <= 0
                ? TargetLegality.Invalid
                : Cards(Every(ThreatSelectionOf(node, cast), cast)),
            "removeThreat" => Every(ThreatSelectionOf(node, cast), cast).Any(scheme =>
                scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast) > 0
                && CanRemoveThreatFrom(node, cast, scheme))
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "enemyAttacks" or "enemySchemes" =>
                Cards(Every(ActivationOf(node, cast).Enemies, cast)),
            "putIntoPlay" => Find(EffectOf<AbilityEffect.PutIntoPlay>(node, cast).Card, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "placeAtRandom" => Find(EffectOf<AbilityEffect.PlaceAtRandom>(node, cast).Host, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "draw" when cast.Chosen is null
                    && bindingMayChange
                    && BindingCanChange(((AbilityEffect.Draw)node).Players) =>
                CanInitiateDraw(node, cast)
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "draw" when BindingCanChange(((AbilityEffect.Draw)node).Players)
                    && (cast.PlayerSelection ?? cast.Chosen) is { Owner: < 0 } =>
                TargetLegality.Invalid,
            "draw" => CanDraw(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "search" => HasSearchableArea(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            _ => TargetLegality.None,
        };
    }

    private static TargetLegality CandidateTargetLegality(
        AbilityEffect node, Cast cast)
    {
        if (cast.Reachability.PriorBindingMayBeEmpty)
        {
            return TargetLegality.Invalid;
        }
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            var outcomes = new List<TargetLegality>();
            foreach (var candidate in cast.Reachability.PriorBindingCandidates)
            {
                cast.ChooseSelection(candidate);
                outcomes.Add(TargetLegalityOf(node, cast));
            }
            if (outcomes.Contains(TargetLegality.Invalid))
            {
                return TargetLegality.Invalid;
            }
            return outcomes.Count > 0
                ? TargetLegality.Valid
                : TargetLegality.None;
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static TargetLegality ChosenPlayerTargetLegality(
        AbilityEffect node, Cast cast)
    {
        var scope = cast.Reachability.PriorBindingCandidates.Count == 0
            ? cast.ForReachability(cast.Reachability with
            {
                PriorBindingCandidates = cast.World.PlayerOrder.Select(player =>
                    cast.World.Seats[player].IdentityCard).ToImmutableList(),
            })
            : cast;
        return CandidateTargetLegality(node, scope);
    }

    private static TargetLegality SequenceTargetLegality(
        AbilityEffect node, Cast cast, bool bindingMayChange)
    {
        var found = new List<TargetLegality>();
        bool binding = bindingMayChange;
        var before = cast.Reachability;
        var priorSteps = before.PriorSteps.ToList();
        var candidates = new BindingCandidateState(
            before.PriorBindingCandidates,
            before.PriorBindingMayBeEmpty
                || before.PriorBindingCandidates.Count == 0 && cast.Chosen is null);
        var children = OrderedEffects(node).ToList();
        for (int index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var scope = cast.ForReachability(before with
            {
                PriorSteps = priorSteps.ToImmutableList(),
                PriorBindingCandidates = candidates.Cards.ToImmutableList(),
                PriorBindingMayBeEmpty = candidates.MayBeEmpty,
            });
            found.Add(TargetLegalityOf(child, scope, binding));
            binding = BindingMayChangeAfter(child, scope, binding);
            candidates = child.OperationName() == "choose" && index + 1 < children.Count
                ? ChoiceBindingCandidatesAfter(
                    child, scope, candidates,
                    children.Skip(index + 1).ToList())
                : BindingCandidatesAfter(child, scope, candidates);
            binding |= candidates.Cards.Count > 0
                || ContainsNode(child, "chooseCard", scope);
            priorSteps.Add(child);
        }
        return CombineTargetLegality(found);
    }

    private static TargetLegality CombineTargetLegality(
        IEnumerable<TargetLegality> children)
    {
        var found = children.ToList();
        if (found.Contains(TargetLegality.Valid))
        {
            return TargetLegality.Valid;
        }
        return found.Contains(TargetLegality.Invalid)
            ? TargetLegality.Invalid
            : TargetLegality.None;
    }

    private static IReadOnlyList<Card> StatusTargets(AbilityEffect node, Cast cast)
    {
        var instruction = EffectOf<AbilityEffect.GiveStatus>(node, cast);
        string status = instruction.Status;
        return [.. Every(instruction.Cards, cast).Where(card =>
            DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(FacedownDrones.Kind(card, cast.World.Facts))
                && Statuses.Count(cast.World, card, status)
                < Statuses.Limit(cast.World, cast.World.Facts, card, status))];
    }

    private static TargetLegality RemoveFromGameTargetLegality(
        AbilityEffect node, Cast cast)
    {
        return Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } removed
            && CanRemoveByEffect(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, removed)
                ? TargetLegality.Valid : TargetLegality.Invalid;
    }

    private enum TargetLegality
    {
        None,
        Invalid,
        Valid,
    }

    private static bool CanInitiateChoice(AbilityEffect node, Cast cast)
    {
        var options = ((AbilityEffect.Choose)node).Options.ToList();
        foreach (var option in options)
        {
            _ = CanInitiate(option, cast);
        }

        return options.Any(option => OptionIsLegal(option, cast));
    }

    private static bool CanInitiateForEach(AbilityEffect node, Cast cast)
    {
        bool stateMayChange = cast.Reachability.PaymentMayMutate || cast.Reachability.PriorStepMayMutate;
        if (stateMayChange && AmountMayChange(ForEachOf(node, cast).Count))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
        }
        long count = ForEachCount(node, cast);
        if (count == 0)
        {
            return true;
        }

        var effect = EffectBody(node);
        if (!Choices(effect).Any())
        {
            if (effect.OperationName() == "dealDamage")
            {
                if (DamageTargets(DamageSelectionOf(effect, cast), cast).Count != 1)
                {
                    return false;
                }
                if (stateMayChange && !StableForEachTarget(((AbilityEffect.Damage)effect).Cards, AbilityCardQuery.Villain))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' reaches an unbound targeted for-each effect "
                        + "after state may change");
                }
                return CanInitiate(effect, cast);
            }
            if (effect.OperationName() == "removeThreat")
            {
                if (Every(EffectOf<AbilityEffect.RemoveThreat>(effect, cast).Schemes, cast).Count != 1)
                {
                    return false;
                }
                if (stateMayChange
                    && !StableForEachTarget(((AbilityEffect.RemoveThreat)effect).Schemes, AbilityCardQuery.MainScheme))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' reaches an unbound targeted for-each effect "
                        + "after state may change");
                }
                return CanInitiate(effect, cast);
            }
            if (ContainsForEachTarget(effect))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a targeted for-each effect without choose "
                    + "whose one target cannot be persisted");
            }
        }
        return CanInitiate(effect, cast);
    }

    private static bool CanInitiateEachTime(AbilityEffect node, Cast cast)
    {
        var preceding = EachTimePreceding(node, cast);
        var authoredCount = preceding.Count;
        if ((cast.Reachability.PaymentMayMutate || cast.Reachability.PriorStepMayMutate)
            && AmountMayChange(authoredCount))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches an each-time count after state may change");
        }
        long requested = Amount(authoredCount, cast);
        if (requested < 0)
        {
            throw new AbilityException("'eachTime' needs a non-negative discard count");
        }
        if (requested > 0)
        {
            ValidateEachTimeBody(node, cast);
        }
        return true;
    }

    private static bool StableForEachTarget(AbilityCardSelection selection, AbilityCardQuery query) =>
        selection is AbilityCardSelection.Query named && named.Kind == query;

    private static bool HasUnboundPowerAmount(AbilityEffect node, Cast cast) =>
        cast.PowerAmount < 0 && ContainsPowerAmount(ForEachOf(node, cast).Count);

    private static bool ContainsMutableAmount(AbilityEffect node, Cast cast) =>
        (EffectAmount(node) is { } amount && AmountMayChange(amount))
        || (node.OperationName() == "forEach" && AmountMayChange(ForEachOf(node, cast).Count))
        || ContinuationChildren(node).Any(child => ContainsMutableAmount(child, cast));

    private static long ForEachCount(AbilityEffect node, Cast cast) =>
        NonNegativeForEachCount(Amount(ForEachOf(node, cast).Count, cast));

    private static long NonNegativeForEachCount(long count)
    {
        if (count < 0)
        {
            throw new AbilityException("'forEach' needs a non-negative 'count'");
        }
        return count;
    }

    private static bool SkipForEachPreflight(AbilityEffect node, Cast cast)
    {
        if ((cast.Reachability.PaymentMayMutate || cast.Reachability.PriorStepMayMutate)
            && AmountMayChange(ForEachOf(node, cast).Count))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
        }
        return StableZeroForEach(node, cast);
    }

    private static bool StableZeroForEach(AbilityEffect node, Cast cast)
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

    private static bool CurrentlyZeroForEach(AbilityEffect node, Cast cast)
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

    private static bool LastingPeriodIsOpen(AbilityEffect node, Cast cast) =>
        LastingPeriodIsOpen(node switch
        {
            AbilityEffect.GrantField { Until: { } until } => until,
            AbilityEffect.GrantTrait { Until: { } until } => until,
            _ => throw new InvalidOperationException("Expected a lasting grant"),
        }, cast);

    private static bool LastingPeriodIsOpen(string until, Cast cast) =>
        until switch
        {
            TimingPoints.EndOfAttack => cast.World.Attack is not null
                || cast.World.CharacterAttack is not null
                || cast.Occurrence.Is(Steps.AttackInitiated),
            TimingPoints.EndOfActivation => cast.World.Activation is not null,
            _ => true,
        };

    private static bool HasLabelledPower(AbilityEffect node) =>
        PowerNodes(node, BasicPowers.AttackVerb).Any()
        || PowerNodes(node, BasicPowers.ThwartVerb).Any()
        || PowerNodes(node, Attack.DefenseVerb).Any();

    private static bool HasInitiationConstraint(AbilityEffect node) =>
        HasLabelledPower(node)
        || node.OperationName() == "grantUntil"
        || ResolutionChildren(node).Any(HasInitiationConstraint);

    private static IEnumerable<AbilityEffect> ResolutionChildren(AbilityEffect node) => node switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects,
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects,
        AbilityEffect.Conditional conditional => ConditionalBranches(conditional),
        AbilityEffect.Dependent dependent => [dependent.Effect, dependent.Continuation],
        AbilityEffect.Power { Kind: AbilityPowerKind.Defense } power => [power.Effect],
        AbilityEffect.DelayedDiscard => [EffectBody(node)],
        // A delayed stun has no child that can run or inspect its future
        // recipient now. Its occurrence binding is resolved by DelayedEffects.
        AbilityEffect.DelayedStun => [],
        AbilityEffect.ForEach repeated => [repeated.Effect],
        AbilityEffect.EachTime each => [each.Effect, each.Then],
        _ => [],
    };

}
