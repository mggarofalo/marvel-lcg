using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>Whether every card target required by an effect exists.</summary>
    private static bool HasRequiredTargets(AbilityNode node, Cast cast) => node.Kind switch
    {
        "seq" or "and" => Nodes(node.Argument).All(step => HasRequiredTargets(step, cast)),
        "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
            is not { } branch || HasRequiredTargets(Tree(branch), cast),
        "then" => HasRequiredTargets(Tree(node.Require("effect")), cast)
            && (ResolutionOf(Tree(node.Require("effect")), cast) != ResolutionOutcome.Full
                || HasRequiredTargets(Tree(node.Require("then")), cast)),
        "otherwise" => ResolutionOf(Tree(node.Require("effect")), cast) switch
        {
            ResolutionOutcome.None => HasRequiredTargets(
                Tree(node.Require("otherwise")), cast),
            _ => HasRequiredTargets(Tree(node.Require("effect")), cast),
        },
        "choose" => Nodes(node.Require("options")).Any(option => OptionIsLegal(option, cast)),
        "chooseCard" => LegalCardChoices(node, cast).Count > 0,
        "forEach" => ForEachCount(node, cast) <= 0
            || HasRequiredTargets(Tree(node.Require("effect")), cast),
        "eachTime" => true,
        "removeFromGame" or "exhaust" or "reveal" or "returnToHand" =>
            Every(node.Argument, cast).Count > 0,
        "ready" => Every(node.Argument, cast).Any(target =>
            !target.Ready && cast.Abilities.CanReady(cast.World, target, cast.Source)),
        "soakDamage" => Find(node.Require("onto"), cast) is not null,
        "giveStatus" => StatusTargets(node, cast).Count > 0,
        "declareDefender" => Find(node.Require("card"), cast) is { } declared
            && Attack.CanDeclareByAbility(
                cast.World, cast.World.Facts, declared,
                ReplaceableDefenseDefender(cast)),
        "attachTo" => Find(node.Argument, cast) is not null,
        "grantUntil" => Find(node.Require("card"), cast) is not null,
        // The delayed effect's game element is supplied by its future
        // occurrence, so rr:target.5 requires no target at initiation.
        "delayUntil" => true,
        "defense" => HasRequiredTargets(Tree(node.Require("effect")), cast),
        "discard" or "dealEncounterCard" =>
            Find(node.Field("card") ?? node.Argument, cast) is not null,
        "heal" => Find(node.Require("card"), cast) is not null,
        "indirectDamage" => Amount(node.Require("amount"), cast) <= 0
            || Assignable(node.Require("among"), cast).Count > 0,
        "dealDamage" => DamageTargets(node.Require("cards"), cast).Count > 0,
        "dealAttackDamage" => DamageTargets(node.Require("cards"), cast).Count > 0,
        "placeThreat" => Every(node.Require("scheme"), cast).Count > 0,
        "placeAccelerationToken" => cast.World.TheCardIn(DeckType.MainSchemesArea) is not null,
        "advanceMainScheme" => CanAdvanceMainScheme(cast),
        "removeThreat" => Find(node.Require("scheme"), cast) is not null,
        "enemyAttacks" or "enemySchemes" => Every(ActivationOf(node, cast).Enemies, cast).Count > 0,
        "putIntoPlay" => Find(node.Require("card"), cast) is not null,
        "placeAtRandom" => Find(node.Require("on"), cast) is not null,
        "createDrones" => CanCreateDrones(node, cast),
        "draw" => CanDraw(node, cast),
        "search" => HasSearchableArea(node, cast),

        // These effects need no separate card-target existence check in this
        // legacy option-reachability pass. TargetLegalityOf is the authority
        // for the complete rr:target initiation rule, including players.
        "generate" or "changeForm" or "removeCounters" or "preventDamage"
            or "cancelWhenRevealed" or "cancelOccurrence" or "dealEncounterCards" or "revealTop"
            or "discardAtRandom" or "discardUntil" or "discardTop"
            or "recoverDiscardedByResource" or "shuffleInto"
            or "gainSurge" or "shuffle" or "drawToHandSize"
            or "drawToPrintedHandSize" or "preventThreat"
            or "replaceThreatWithDamage" or "grantCharactersControlledBy"
            or "reduceNextCardCost" => true,
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses '{node.Kind}' in an option whose target "
            + "legality is not implemented"),
    };

    /// <summary>Whether one authored ability can begin before any cost is paid.</summary>
    private static bool CanInitiate(CardAbility ability, Cast cast)
    {
        bool outer = cast.CheckingInitiation;
        cast.SetCheckingInitiation(true);
        try
        {
            if (!CanInitiateLabels(ability, cast))
            {
                return false;
            }
            cast.LabelsPreflighted = true;
            if ((ability.Labels?.Count ?? 0) > 0
                && LabeledAbilities.WouldBeCancelled(
                    cast.World, cast.World.Facts, Resolver(cast),
                    cast.Source, ability.Labels!))
            {
                return true;
            }
            return CanInitiate(ability.Effect, cast)
                && TargetLegalityOf(ability.Effect, cast) != TargetLegality.Invalid;
        }
        finally
        {
            cast.SetCheckingInitiation(outer);
        }
    }

    /// <summary>Whether an ability envelope can establish every labeled lifecycle.</summary>
    private static bool CanInitiateLabels(CardAbility ability, Cast cast)
    {
        var labels = ability.Labels ?? [];
        if (labels.Count == 0)
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
    private static bool GuaranteesOneLabeledPower(AbilityNode node, string power)
    {
        if (string.Equals(
            node.Kind, power.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return PowerNodes(node, power).Count() == 1;
        }

        if (node.Kind == "chooseCard")
        {
            return GuaranteesOneLabeledPower(Tree(node.Require("effect")), power);
        }

        if (node.Kind == "choose")
        {
            var options = Nodes(node.Require("options")).ToList();
            return options.Count >= 2
                && options.All(option => GuaranteesOneLabeledPower(option, power));
        }

        if (node.Kind == "if")
        {
            return node.Field("then") is { } then
                && node.Field("else") is { } otherwise
                && GuaranteesOneLabeledPower(Tree(then), power)
                && GuaranteesOneLabeledPower(Tree(otherwise), power);
        }

        if (node.Kind == "seq")
        {
            var steps = Nodes(node.Argument).ToList();
            return steps.Count > 0
                && GuaranteesOneLabeledPower(steps[0], power)
                && steps.Skip(1).All(step => !PowerNodes(step, power).Any());
        }

        return false;
    }

    /// <summary>Whether every choice required to initiate this effect has an answer.</summary>
    private static bool CanInitiate(AbilityNode node, Cast cast)
    {
        if (HasNestedEachPlayer(
            node, cast, bindingMayChange: cast.PriorBindingMayChange))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' nests one each-player frame inside another, "
                + "which is not implemented");
        }
        if (ContainsUnsupportedPower(
            node, cast, bindingMayChange: cast.PriorBindingMayChange))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented");
        }
        return node.Kind switch
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

    private static bool CanInitiateSequence(AbilityNode node, Cast cast)
    {
        var steps = Nodes(node.Argument).ToList();
        bool outerContinuation = cast.HasContinuation;
        bool outerPriorMutation = cast.PriorStepMayMutate;
        var outerPriorSteps = cast.PriorSteps;
        ulong outerPriorFormChanges = cast.PriorFormsMayChange;
        bool outerPriorBinding = cast.PriorBindingMayChange;
        var outerPriorCandidates = cast.PriorBindingCandidates;
        bool outerPriorBindingMayBeEmpty = cast.PriorBindingMayBeEmpty;
        ulong priorFormChanges = outerPriorFormChanges;
        bool priorBinding = outerPriorBinding;
        var priorCandidates = new BindingCandidateState(
            outerPriorCandidates,
            outerPriorBindingMayBeEmpty
                || outerPriorCandidates.Count == 0 && cast.Chosen is null);
        var priorSteps = outerPriorSteps.ToList();
        try
        {
            for (int step = 0; step < steps.Count; step++)
            {
                cast.SetContinuation(outerContinuation || step < steps.Count - 1);
                cast.SetPriorStepMayMutate(outerPriorMutation || step > 0);
                cast.SetPriorSteps(priorSteps);
                cast.SetPriorFormsMayChange(priorFormChanges);
                cast.SetPriorBindingMayChange(priorBinding);
                cast.SetPriorBindingCandidates(priorCandidates.Cards);
                cast.SetPriorBindingMayBeEmpty(priorCandidates.MayBeEmpty);
                if (step > 0)
                {
                    PreflightDependentOutcomesAfterMutation(steps[step], cast);
                }
                if (!CanInitiate(steps[step], cast))
                {
                    return false;
                }
                if (step + 1 < steps.Count
                    && !ChoicesHaveStableAreaContinuation(
                        steps[step], steps.Skip(step + 1).ToList(), cast))
                {
                    return false;
                }
                priorFormChanges = FormsMayDifferAfter(
                    steps[step], cast, priorFormChanges, priorBinding);
                priorBinding = BindingMayChangeAfter(
                    steps[step], cast, priorBinding);
                priorCandidates = steps[step].Kind == "choose"
                    && step + 1 < steps.Count
                        ? ChoiceBindingCandidatesAfter(
                            steps[step], cast, priorCandidates,
                            steps.Skip(step + 1).ToList())
                        : BindingCandidatesAfter(
                            steps[step], cast, priorCandidates);
                priorSteps.Add(steps[step]);
            }

            return true;
        }
        finally
        {
            cast.SetContinuation(outerContinuation);
            cast.SetPriorStepMayMutate(outerPriorMutation);
            cast.SetPriorSteps(outerPriorSteps);
            cast.SetPriorFormsMayChange(outerPriorFormChanges);
            cast.SetPriorBindingMayChange(outerPriorBinding);
            cast.SetPriorBindingCandidates(outerPriorCandidates);
            cast.SetPriorBindingMayBeEmpty(outerPriorBindingMayBeEmpty);
        }
    }

    private static bool ChoicesHaveStableAreaContinuation(
        AbilityNode effect, IReadOnlyList<AbilityNode> suffix, Cast cast)
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
        AbilityNode effect, HashSet<DeckType> sensitiveAreas, Cast cast)
    {
        if (effect.Kind == "seq")
        {
            var steps = Nodes(effect.Argument).ToList();
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
            if (effect.Kind == "choose")
            {
                return Nodes(effect.Require("options")).Any(option =>
                    OptionIsLegal(option, cast)
                    && !MayChangeAnyArea(option, sensitiveAreas, cast)
                    && ChoicesAreStable(option, sensitiveAreas, cast));
            }

            if (effect.Kind == "chooseCard")
            {
                var chosenEffect = Tree(effect.Require("effect"));
                return LegalCardChoices(effect, cast).Any(candidate =>
                {
                    cast.ChooseSelection(candidate);
                    return !MayChangeAnyArea(
                            chosenEffect, sensitiveAreas, cast)
                        && ChoicesAreStable(
                            chosenEffect, sensitiveAreas, cast);
                });
            }

            if (effect.Kind == "forEach" && CurrentlyZeroForEach(effect, cast))
            {
                return true;
            }
            if (effect.Kind == "eachPlayer")
            {
                int priorPlayer = cast.Player;
                try
                {
                    return cast.World.PlayerOrder.All(player =>
                    {
                        cast.RestorePlayer(player);
                        return ChoicesAreStable(
                            Tree(effect.Require("effect")), sensitiveAreas, cast);
                    });
                }
                finally
                {
                    cast.RestorePlayer(priorPlayer);
                }
            }

            var children = effect.Kind switch
            {
                "if" => ReachableMutationBranches(effect, cast),
                "forEach" => [Tree(effect.Require("effect"))],
                _ => StructuralChildren(effect),
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

    private static void CollectSingularAreaDependencies(
        AbilityNode node, Cast cast, HashSet<DeckType> areas)
    {
        if (node.Kind == "search")
        {
            areas.UnionWith(SearchAreaTypes(node, cast));
        }

        foreach (var selector in SingularSelectors(node))
        {
            CollectCardsInDependencies(selector, cast, areas);
        }
        CollectSingularAreaDependencies(node.Argument, cast, areas);
    }

    private static IEnumerable<AbilityValue> SingularSelectors(
        AbilityNode node) => node.Kind switch
    {
        "attachTo" or "exhaust" or "removeFromGame" or "reveal"
            or "returnToHand" or "addToHand" or "returnOwnedToHand" =>
            [node.Argument],
        "discard" or "dealEncounterCard" =>
            [node.Field("card") ?? node.Argument],
        "grantUntil" or "heal" or "putIntoPlay" or "placeCounters"
            or "removeCounters" => [node.Require("card")],
        "removeThreat" => [node.Require("scheme")],
        "placeAtRandom" => [node.Require("on")],
        "soakDamage" => [node.Require("onto")],
        "moveDamage" or "moveAttackDamage" =>
            [node.Require("from"), node.Require("to")],
        "hasStatus" or "hasTrait" or "cardSet" or "isTitle" or "isKind" =>
            [node.Require("card")],
        "wasDefeated" => [node.Argument],
        _ => [],
    };

    private static void CollectCardsInDependencies(
        AbilityValue value, Cast cast, HashSet<DeckType> areas)
    {
        switch (value)
        {
            case AbilityValue.List list:
                foreach (var item in list.Values)
                {
                    CollectCardsInDependencies(item, cast, areas);
                }
                break;
            case AbilityValue.Map map:
                foreach (var (kind, argument) in map.Entries)
                {
                    if (kind == "cardsIn")
                    {
                        areas.UnionWith(CardsInAreaTypes(
                            new AbilityNode(kind, argument), cast));
                    }
                    else
                    {
                        CollectCardsInDependencies(argument, cast, areas);
                    }
                }
                break;
        }
    }

    private static void CollectSingularAreaDependencies(
        AbilityValue value, Cast cast, HashSet<DeckType> areas)
    {
        switch (value)
        {
            case AbilityValue.List list:
                foreach (var item in list.Values)
                {
                    CollectSingularAreaDependencies(item, cast, areas);
                }
                break;
            case AbilityValue.Map map:
                foreach (var (kind, argument) in map.Entries)
                {
                    CollectSingularAreaDependencies(
                        new AbilityNode(kind, argument), cast, areas);
                }
                break;
        }
    }

    private static void PreflightDependentOutcomesAfterMutation(
        AbilityNode node, Cast cast)
    {
        if (node.Kind == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.Kind is "then" or "otherwise")
        {
            PreflightResolutionBranches(
                Tree(node.Require("effect")), cast, allBranches: true);
        }

        var children = node.Kind switch
        {
            "choose" => Nodes(node.Require("options")),
            "eachPlayer" => [Tree(node.Require("effect"))],
            _ => StructuralChildren(node),
        };
        foreach (var child in children)
        {
            PreflightDependentOutcomesAfterMutation(child, cast);
        }
    }

    private static bool CanInitiateIf(AbilityNode node, Cast cast)
    {
        // Payment happens after an action is offered and can change the facts
        // tested by the branch. Validate every structurally reachable
        // continuation boundary now, while no cost has been paid, then use
        // only the currently active branch for ordinary target eligibility.
        var test = Tree(node.Require("test"));
        bool paymentCanSwitch = cast.PaymentMayMutate && PaymentCanChange(test);
        bool bindingCanSwitch = cast.PriorBindingMayChange
            && BindingCanChange(test.Argument);
        bool stateCanSwitch = bindingCanSwitch
            || PriorStepCanChange(test, cast) || paymentCanSwitch;
        bool reachableLabelledTargetsAreValid = true;
        foreach (var branch in Branches.Select(node.Field).Where(value => value is not null))
        {
            var effect = Tree(branch!);
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
            return Branches.Select(node.Field)
                .Where(value => value is not null)
                .All(value => CanInitiate(Tree(value!), cast));
        }
        return node.Field(Test(test, cast) ? "then" : "else")
            is not { } active || CanInitiate(Tree(active), cast);
    }

    private static bool PriorStepCanChange(AbilityNode test, Cast cast) => test.Kind switch
    {
        "and" or "or" => Nodes(test.Argument).Any(child =>
            PriorStepCanChange(child, cast)),
        "not" => PriorStepCanChange(Tree(test.Argument), cast),
        "inForm" => cast.PriorBindingMayChange
                && BindingCanChange(test.Argument)
            || Seats(test.Require("player"), cast)
                .Any(seat => SeatMayChange(cast.PriorFormsMayChange, seat)),
        _ => cast.PriorStepMayMutate,
    };

    /// <summary>Seats whose final form may differ after a reachable effect.</summary>
    private static ulong FormsMayDifferAfter(
        AbilityNode node, Cast cast, ulong before,
        bool bindingMayChange = false)
    {
        if (node.Kind == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.Kind == "changeForm")
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

        ulong outer = cast.PriorFormsMayChange;
        try
        {
            cast.SetPriorFormsMayChange(before);
            if (node.Kind == "if")
            {
                var test = Tree(node.Require("test"));
                bool canSwitch = bindingMayChange
                        && BindingCanChange(test.Argument)
                    || PriorStepCanChange(test, cast)
                    || cast.PaymentMayMutate && PaymentCanChange(test);
                var branches = canSwitch
                    ? Branches.Select(node.Field).Where(value => value is not null)
                    : node.Field(Test(test, cast) ? "then" : "else") is { } active
                        ? [active]
                        : [];
                return branches.Select(branch =>
                        FormsMayDifferAfter(
                            Tree(branch!), cast, before, bindingMayChange))
                    .DefaultIfEmpty(before)
                    .Aggregate((left, right) => left | right);
            }
            if (node.Kind == "choose")
            {
                return Nodes(node.Require("options")).Select(option =>
                        FormsMayDifferAfter(
                            option, cast, before, bindingMayChange))
                    .DefaultIfEmpty(before)
                    .Aggregate((left, right) => left | right);
            }
            if (node.Kind == "eachPlayer")
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
                            cast.SetPriorFormsMayChange(after);
                            after = FormsMayDifferAfter(
                                Tree(node.Require("effect")), cast, after,
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
                || node.Kind is "chooseCard" or "thwartSchemes"
                    or "thwartDifferentSchemes" or "legalPractice";
            foreach (var child in MutationChildren(node))
            {
                cast.SetPriorFormsMayChange(state);
                state = FormsMayDifferAfter(
                    child, cast, state, childBindingMayChange);
            }
            return state;
        }
        finally
        {
            cast.SetPriorFormsMayChange(outer);
        }
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
        AbilityNode node, Cast cast, bool before)
    {
        if (node.Kind is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            return true;
        }
        if (node.Kind == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = before && BindingCanChange(test.Argument)
                || PriorStepCanChange(test, cast)
                || cast.PaymentMayMutate && PaymentCanChange(test);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Any(branch =>
                BindingMayChangeAfter(Tree(branch!), cast, before));
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
        AbilityNode node, Cast cast, BindingCandidateState before)
    {
        if (node.Kind is "attack" or "thwart")
        {
            // A labelled power owns `chosen` only inside its wrapper. Its
            // effect cannot suspend, and the outer binding resumes afterwards.
            return before;
        }
        if (node.Kind == "chooseCard")
        {
            return ChooseCardBindingCandidatesAfter(node, cast, before);
        }
        if (node.Kind == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.Kind == "eachPlayer")
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
                        Tree(node.Require("effect")), cast, before);
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
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = before.Cards.Count > 0
                    && BindingCanChange(test.Argument)
                || PriorStepCanChange(test, cast)
                || cast.PaymentMayMutate && PaymentCanChange(test);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            var outcomes = branches.Select(branch => BindingCandidatesAfter(
                    Tree(branch!), cast, before))
                .ToList();
            return new BindingCandidateState(
                outcomes.SelectMany(outcome => outcome.Cards)
                    .DistinctBy(card => card.ObjectId)
                    .ToList(),
                outcomes.Count == 0 || outcomes.Any(outcome => outcome.MayBeEmpty));
        }
        if (node.Kind == "choose")
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
        AbilityNode node, Cast cast, BindingCandidateState before)
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
                            Tree(node.Require("effect")), cast,
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
        AbilityNode node, Cast cast, BindingCandidateState before,
        IReadOnlyList<AbilityNode>? continuation = null)
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
                foreach (var option in Nodes(node.Require("options"))
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
        IReadOnlyList<AbilityNode> continuation,
        Cast cast)
    {
        var suffix = new AbilityNode(
            "seq",
            new AbilityValue.List(continuation.Select(NodeValue).ToList()));
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var outerCandidates = cast.PriorBindingCandidates;
        bool outerMayBeEmpty = cast.PriorBindingMayBeEmpty;
        bool outerBindingMayChange = cast.PriorBindingMayChange;
        try
        {
            var legal = candidates.Cards.Where(candidate =>
            {
                cast.ChooseSelection(candidate);
                cast.SetPriorBindingCandidates([candidate]);
                cast.SetPriorBindingMayBeEmpty(false);
                cast.SetPriorBindingMayChange(false);
                return CanInitiateSequence(suffix, cast)
                    && TargetLegalityOf(suffix, cast) != TargetLegality.Invalid;
            }).ToList();
            // An explicit empty option is the authored decline branch for
            // “may.” It remains reachable rather than being silently removed;
            // the enclosing sequence will reject it if the suffix needs a
            // binding. Card-bearing alternatives can be filtered individually.
            return new BindingCandidateState(legal, candidates.MayBeEmpty);
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
            cast.SetPriorBindingCandidates(outerCandidates);
            cast.SetPriorBindingMayBeEmpty(outerMayBeEmpty);
            cast.SetPriorBindingMayChange(outerBindingMayChange);
        }
    }

    private static void PreflightContinuationBoundaries(AbilityNode node, Cast cast)
    {
        if (node.Kind == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.Kind == "seq")
        {
            var steps = Nodes(node.Argument).ToList();
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

        if (node.Kind == "and")
        {
            _ = CanInitiateAnd(node, cast);
            return;
        }

        var children = node.Kind switch
        {
            "choose" => Nodes(node.Require("options")),
            "eachPlayer" => [Tree(node.Require("effect"))],
            _ => StructuralChildren(node),
        };
        foreach (var child in children)
        {
            PreflightContinuationBoundaries(child, cast);
        }
    }

    private static void PreflightInitiationConstraints(
        AbilityNode node, Cast cast, bool requireCurrentTargets)
    {
        if (node.Kind == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.Kind == "grantUntil" && !LastingPeriodIsOpen(node, cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a lasting effect outside its named period");
        }
        if (node.Kind == "grantUntil"
            && requireCurrentTargets
            && Find(node.Require("card"), cast) is null)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' may reach a lasting effect with no target after payment");
        }

        var children = node.Kind switch
        {
            "choose" => Nodes(node.Require("options")),
            "eachPlayer" => [Tree(node.Require("effect"))],
            _ => StructuralChildren(node),
        };
        foreach (var child in children)
        {
            PreflightInitiationConstraints(child, cast, requireCurrentTargets);
        }
    }

    private static bool CanInitiateAnd(AbilityNode node, Cast cast)
    {
        var effects = Nodes(node.Argument).ToList();
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
        AbilityNode node, Cast cast, ResolutionOutcome required, string branch)
    {
        var effect = Tree(node.Require("effect"));
        if (ActiveChoices(effect, cast).Any())
        {
            var choices = ActiveChoices(effect, cast).ToList();
            if (effect.Kind is not ("choose" or "chooseCard")
                || choices.Any(ChoiceHasNestedChoice))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has multiple-stage player choices before "
                    + $"'{node.Kind}', whose combined resolution outcome is not implemented");
            }
            PreflightAnsweredOutcome(effect, cast);
            return CanInitiate(effect, cast);
        }
        var outcome = EnsureDependentSupported(
            node, cast, effect, Tree(node.Require(branch)), required);
        return outcome == required
            ? CanInitiate(Tree(node.Require(branch)), cast)
            : CanInitiate(effect, cast);
    }

    private static bool ChoiceHasNestedChoice(AbilityNode choice) => choice.Kind switch
    {
        "chooseCard" => Choices(Tree(choice.Require("effect"))).Any(),
        "choose" => Nodes(choice.Require("options")).Any(option => Choices(option).Any()),
        _ => false,
    };

    private static bool CanInitiateLeaf(AbilityNode node, Cast cast) => node.Kind switch
    {
        "resolveSpecials" when cast.HasContinuation =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' continues after ordered Special abilities, "
                + "which is not implemented"),
        "chooseCard" => CanInitiateChooseCard(node, cast),
        "choose" => CanInitiateChoice(node, cast),
        "draw" => CanInitiateDraw(node, cast),
        "thwartDifferentSchemes" => Every(node.Require("schemes"), cast).Count > 0,
        "legalPractice" => cast.World.Seats[cast.Player].Hand.Cards.Any(card =>
                card.ObjectId != cast.Source.ObjectId)
            && Every(node.Require("schemes"), cast).Count > 0,
        "thwartSchemes" when SuspendsPowerEffect(
            Tree(Tree(node.Require("power")).Require("effect")), cast) =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented"),
        "thwartSchemes" => Every(node.Require("schemes"), cast).Count > 0,
        "attack" or "thwart" when SuspendsPowerEffect(
            Tree(node.Require("effect")), cast) =>
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a labelled power, "
                + "which is not implemented"),
        "attack" => CanTargetAttack(node, cast),
        "thwart" => CanTargetThwart(node, cast),
        "enemyAttacks" or "enemySchemes" => true,
        "defense" => Attack.CanUseDefenseAbility(cast.World, cast.Player)
            && CanInitiate(Tree(node.Require("effect")), cast),
        // A missing dynamic target gets the resolver's specific exception
        // (for example, no activating enemy). When the target exists, the
        // lasting period itself is an initiation constraint.
        "grantUntil" => Find(node.Require("card"), cast) is not null
            ? LastingPeriodIsOpen(node, cast)
            : !IsPlayerCard(cast)
                && !cast.PaymentMayMutate
                && !cast.PriorStepMayMutate,
        _ => true,
    };

    private static bool CanInitiateChooseCard(AbilityNode node, Cast cast)
    {
        bool CanChooseFromCurrentBinding() =>
            (!RequiresChosenPlayer(node.Require("from"))
                || (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 })
            && LegalCardChoices(node, cast).Count > 0;

        if (cast.PriorBindingCandidates.Count == 0
            && !cast.PriorBindingMayBeEmpty)
        {
            return CanChooseFromCurrentBinding();
        }

        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            bool any = false;
            foreach (var candidate in cast.PriorBindingCandidates)
            {
                cast.ChooseSelection(candidate);
                any |= CanChooseFromCurrentBinding();
            }
            if (cast.PriorBindingMayBeEmpty)
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

    private static bool CanInitiateDraw(AbilityNode node, Cast cast)
    {
        bool CanDrawFromBinding() =>
            !BindingCanChange(node.Require("player"))
            || (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 }
                && CanDraw(node, cast);

        if (cast.Chosen is null && BindingCanChange(node.Require("player"))
            && cast.PriorBindingCandidates.Count > 0)
        {
            if (cast.PriorBindingMayBeEmpty)
            {
                return false;
            }
            var prior = cast.CaptureChosen();
            var priorSelection = cast.CapturePlayerSelection();
            try
            {
                bool any = cast.PriorBindingCandidates.Any(candidate =>
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

    private static bool CanTargetAttack(AbilityNode node, Cast cast)
    {
        var target = node.Require("target");
        if (cast.Chosen is null && BindingCanChange(target)
            && cast.PriorBindingCandidates.Count > 0)
        {
            return EveryCandidateCan(cast, () => CanTargetAttack(node, cast));
        }
        return Find(target, cast) is { } enemy
            && BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                .Any(candidate => candidate.ObjectId == enemy.ObjectId)
            && cast.World.Abilities.CanTakeDamage(cast.World, enemy, cast.Source);
    }

    private static bool CanTargetThwart(AbilityNode node, Cast cast)
    {
        var target = node.Require("target");
        if (cast.Chosen is null && BindingCanChange(target)
            && cast.PriorBindingCandidates.Count > 0)
        {
            return EveryCandidateCan(cast, () => CanTargetThwart(node, cast));
        }
        if (Find(target, cast) is not { } scheme)
        {
            return false;
        }

        if (node.Field("automaticTarget") is not null)
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
                Tree(node.Require("effect")), cast, scheme);
        if (crisisException)
        {
            cast.ValidateCrisisIgnoringThwart(node);
        }
        return crisisException;
    }

    private static bool EveryCandidateCan(Cast cast, Func<bool> test)
    {
        if (cast.PriorBindingMayBeEmpty)
        {
            return false;
        }
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            return cast.PriorBindingCandidates.All(candidate =>
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
        AbilityNode node, Cast cast, Card scheme)
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
        AbilityNode node, Cast cast, Card scheme)
    {
        if (node.Kind == "removeThreat")
        {
            return IgnoresCrisis(node)
                && Every(node.Require("scheme"), cast).Any(candidate =>
                    candidate.ObjectId == scheme.ObjectId)
                && scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && Amount(node.Require("amount"), cast) > 0
                && CanRemoveThreatFrom(node, cast, scheme);
        }

        return node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument).Any(child =>
                CrisisIgnoringRemovalCanAffectBound(child, cast, scheme)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch
                && CrisisIgnoringRemovalCanAffectBound(Tree(branch), cast, scheme),
            "forEach" => ForEachCount(node, cast) > 0
                && CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme),
            "choose" => Nodes(node.Require("options")).Any(option =>
                CrisisIgnoringRemovalCanAffectBound(option, cast, scheme)),
            "then" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme),
            "then" => CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme)
                || ResolutionOf(Tree(node.Require("effect")), cast)
                    == ResolutionOutcome.Full
                && CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("then")), cast, scheme),
            "otherwise" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme),
            "otherwise" => CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("effect")), cast, scheme)
                || ResolutionOf(Tree(node.Require("effect")), cast)
                    == ResolutionOutcome.None
                && CrisisIgnoringRemovalCanAffectBound(
                    Tree(node.Require("otherwise")), cast, scheme),
            "eachPlayer" or "defense" => CrisisIgnoringRemovalCanAffectBound(
                Tree(node.Require("effect")), cast, scheme),
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
        AbilityNode node, Cast cast, bool bindingMayChange = false)
    {
        TargetLegality Cards(IEnumerable<Card> candidates) =>
            candidates.Any() ? TargetLegality.Valid : TargetLegality.Invalid;

        if (cast.Chosen is null
            && cast.PriorBindingCandidates.Count > 0
            && BindingCanChange(node.Argument)
            && node.Kind is not ("seq" or "and" or "if" or "then" or "otherwise"
                or "forEach" or "defense" or "choose" or "eachTime"
                or "delayUntil" or "chooseCard"))
        {
            return CandidateTargetLegality(node, cast);
        }

        return node.Kind switch
        {
            "seq" => SequenceTargetLegality(node, cast, bindingMayChange),
            "and" => CombineTargetLegality(
                Nodes(node.Argument).Select(child =>
                    TargetLegalityOf(child, cast, bindingMayChange))),
            "if" when bindingMayChange
                    && BindingCanChange(Tree(node.Require("test")).Argument) =>
                CombineTargetLegality(Branches.Select(node.Field)
                    .Where(value => value is not null)
                    .Select(value => TargetLegalityOf(
                        Tree(value!), cast, bindingMayChange))),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch
                    ? TargetLegalityOf(Tree(branch), cast, bindingMayChange)
                    : TargetLegality.None,
            "then" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                TargetLegalityOf(
                    Tree(node.Require("effect")), cast, bindingMayChange),
            "then" => ResolutionOf(Tree(node.Require("effect")), cast)
                == ResolutionOutcome.Full
                    ? CombineTargetLegality(
                    [
                        TargetLegalityOf(
                            Tree(node.Require("effect")), cast, bindingMayChange),
                        TargetLegalityOf(
                            Tree(node.Require("then")), cast, bindingMayChange),
                    ])
                    : TargetLegalityOf(
                        Tree(node.Require("effect")), cast, bindingMayChange),
            "otherwise" when ActiveChoices(Tree(node.Require("effect")), cast).Any() =>
                TargetLegalityOf(
                    Tree(node.Require("effect")), cast, bindingMayChange),
            "otherwise" => ResolutionOf(Tree(node.Require("effect")), cast)
                == ResolutionOutcome.None
                    ? TargetLegalityOf(
                        Tree(node.Require("otherwise")), cast, bindingMayChange)
                    : TargetLegalityOf(
                        Tree(node.Require("effect")), cast, bindingMayChange),
            "forEach" => ForEachCount(node, cast) <= 0
                ? TargetLegality.None
                : TargetLegalityOf(
                    Tree(node.Require("effect")), cast, bindingMayChange),
            "attack" => CanTargetAttack(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "thwart" => CanTargetThwart(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "defense" => TargetLegalityOf(
                Tree(node.Require("effect")), cast, bindingMayChange),

            // The choice itself is validated by CanInitiateChoice and
            // OptionIsLegal. Future-target lasting effects have no target node
            // in this tree, so they fall through to None.
            "choose" or "eachTime" => TargetLegality.None,
            "delayUntil" => TargetLegality.None,
            "chooseCard" => CanInitiateChooseCard(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,

            "removeFromGame" => RemoveFromGameTargetLegality(node, cast),
            "reveal" or "returnToHand" => Cards(Every(node.Argument, cast)),
            "exhaust" => Cards(Every(node.Argument, cast).Where(card => card.Ready)),
            "ready" => Cards(Every(node.Argument, cast).Where(card =>
                !card.Ready && cast.Abilities.CanReady(cast.World, card, cast.Source))),
            "giveStatus" => Cards(StatusTargets(node, cast)),
            "declareDefender" => Find(node.Require("card"), cast) is { } declared
                && Attack.CanDeclareByAbility(
                    cast.World, cast.World.Facts, declared,
                    ReplaceableDefenseDefender(cast))
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "attachTo" => Find(node.Argument, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "grantUntil" => Find(node.Require("card"), cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "discard" => (node.Field("card") ?? node.Argument) is { } discardTarget
                && Find(discardTarget, cast) is { } discarded
                && CanRemoveByEffect(discardTarget, cast, discarded)
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "dealEncounterCard" => Find(node.Field("card") ?? node.Argument, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "heal" => Find(node.Require("card"), cast) is { Damage: > 0 }
                && Amount(node.Require("amount"), cast) > 0
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "dealDamage" or "dealAttackDamage" =>
                Amount(node.Require("amount"), cast) > 0
                    ? Cards(DamageTargets(node.Require("cards"), cast))
                    : TargetLegality.Invalid,
            "indirectDamage" => Amount(node.Require("amount"), cast) <= 0
                ? TargetLegality.Invalid
                : Cards(Assignable(node.Require("among"), cast)),
            "placeThreat" => Amount(node.Require("amount"), cast) <= 0
                ? TargetLegality.Invalid
                : Cards(Every(node.Require("scheme"), cast)),
            "removeThreat" => Every(node.Require("scheme"), cast).Any(scheme =>
                scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && Amount(node.Require("amount"), cast) > 0
                && CanRemoveThreatFrom(node, cast, scheme))
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "enemyAttacks" or "enemySchemes" =>
                Cards(Every(ActivationOf(node, cast).Enemies, cast)),
            "putIntoPlay" => Find(node.Require("card"), cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "placeAtRandom" => Find(node.Require("on"), cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "draw" when cast.Chosen is null
                    && bindingMayChange
                    && BindingCanChange(node.Require("player")) =>
                CanInitiateDraw(node, cast)
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "draw" when BindingCanChange(node.Require("player"))
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
        AbilityNode node, Cast cast)
    {
        if (cast.PriorBindingMayBeEmpty)
        {
            return TargetLegality.Invalid;
        }
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            var outcomes = new List<TargetLegality>();
            foreach (var candidate in cast.PriorBindingCandidates)
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
        AbilityNode node, Cast cast)
    {
        var outerCandidates = cast.PriorBindingCandidates;
        try
        {
            if (outerCandidates.Count == 0)
            {
                cast.SetPriorBindingCandidates(
                    cast.World.PlayerOrder.Select(player =>
                        cast.World.Seats[player].IdentityCard).ToList());
            }
            return CandidateTargetLegality(node, cast);
        }
        finally
        {
            cast.SetPriorBindingCandidates(outerCandidates);
        }
    }

    private static TargetLegality SequenceTargetLegality(
        AbilityNode node, Cast cast, bool bindingMayChange)
    {
        var found = new List<TargetLegality>();
        bool binding = bindingMayChange;
        var outerCandidates = cast.PriorBindingCandidates;
        bool outerMayBeEmpty = cast.PriorBindingMayBeEmpty;
        var outerPriorSteps = cast.PriorSteps;
        var priorSteps = outerPriorSteps.ToList();
        var candidates = new BindingCandidateState(
            outerCandidates,
            outerMayBeEmpty
                || outerCandidates.Count == 0 && cast.Chosen is null);
        try
        {
            var children = Nodes(node.Argument).ToList();
            for (int index = 0; index < children.Count; index++)
            {
                var child = children[index];
                cast.SetPriorSteps(priorSteps);
                cast.SetPriorBindingCandidates(candidates.Cards);
                cast.SetPriorBindingMayBeEmpty(candidates.MayBeEmpty);
                found.Add(TargetLegalityOf(child, cast, binding));
                binding = BindingMayChangeAfter(child, cast, binding);
                candidates = child.Kind == "choose" && index + 1 < children.Count
                    ? ChoiceBindingCandidatesAfter(
                        child, cast, candidates,
                        children.Skip(index + 1).ToList())
                    : BindingCandidatesAfter(child, cast, candidates);
                binding |= candidates.Cards.Count > 0
                    || ContainsNode(child, "chooseCard", cast);
                priorSteps.Add(child);
            }
            return CombineTargetLegality(found);
        }
        finally
        {
            cast.SetPriorBindingCandidates(outerCandidates);
            cast.SetPriorBindingMayBeEmpty(outerMayBeEmpty);
            cast.SetPriorSteps(outerPriorSteps);
        }
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

    private static IReadOnlyList<Card> StatusTargets(AbilityNode node, Cast cast)
    {
        string status = Word(node.Require("status"));
        return [.. Every(node.Require("card"), cast).Where(card =>
            DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(FacedownDrones.Kind(card, cast.World.Facts))
                && Statuses.Count(cast.World, card, status)
                < Statuses.Limit(cast.World, cast.World.Facts, card, status))];
    }

    private static TargetLegality RemoveFromGameTargetLegality(
        AbilityNode node, Cast cast)
    {
        return Find(node.Argument, cast) is { } removed
            && CanRemoveByEffect(node.Argument, cast, removed)
                ? TargetLegality.Valid : TargetLegality.Invalid;
    }

    private enum TargetLegality
    {
        None,
        Invalid,
        Valid,
    }

    private static bool CanInitiateChoice(AbilityNode node, Cast cast)
    {
        var options = Nodes(node.Require("options")).ToList();
        foreach (var option in options)
        {
            _ = CanInitiate(option, cast);
        }

        return options.Any(option => OptionIsLegal(option, cast));
    }

    private static bool CanInitiateForEach(AbilityNode node, Cast cast)
    {
        bool stateMayChange = cast.PaymentMayMutate || cast.PriorStepMayMutate;
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

        var effect = Tree(node.Require("effect"));
        if (!Choices(effect).Any())
        {
            if (effect.Kind == "dealDamage")
            {
                if (DamageTargets(effect.Require("cards"), cast).Count != 1)
                {
                    return false;
                }
                if (stateMayChange && !StableForEachTarget(effect.Require("cards"), "villain"))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' reaches an unbound targeted for-each effect "
                        + "after state may change");
                }
                return CanInitiate(effect, cast);
            }
            if (effect.Kind == "removeThreat")
            {
                if (Every(effect.Require("scheme"), cast).Count != 1)
                {
                    return false;
                }
                if (stateMayChange
                    && !StableForEachTarget(effect.Require("scheme"), "mainScheme"))
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

    private static bool CanInitiateEachTime(AbilityNode node, Cast cast)
    {
        var preceding = EachTimePreceding(node, cast);
        var authoredCount = preceding.Count;
        if ((cast.PaymentMayMutate || cast.PriorStepMayMutate)
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

    private static bool StableForEachTarget(AbilityValue value, string query) =>
        value is AbilityValue.Word word
            && string.Equals(word.Value, query, StringComparison.Ordinal)
        || value is AbilityValue.Map
            && Tree(value) is { Kind: "query", Argument: AbilityValue.Word named }
            && string.Equals(named.Value, query, StringComparison.Ordinal);

    private static bool AmountMayChange(AbilityValue value)
    {
        if (value is not AbilityValue.Map)
        {
            return false;
        }
        var amount = Tree(value);
        return amount.Kind switch
        {
            "perPlayer" or "powerAmount" => false,
            "min" or "add" or "mul" => Values(amount.Argument).Any(AmountMayChange),
            _ => true,
        };
    }

    private static bool HasUnboundPowerAmount(AbilityNode node, Cast cast) =>
        cast.PowerAmount < 0 && ContainsPowerAmount(ForEachOf(node, cast).Count);

    private static bool ContainsMutableAmount(AbilityNode node, Cast cast) =>
        (node.Field("amount") is { } amount && AmountMayChange(amount))
        || (node.Kind == "forEach" && AmountMayChange(ForEachOf(node, cast).Count))
        || ContinuationChildren(node).Any(child => ContainsMutableAmount(child, cast));

    private static long ForEachCount(AbilityNode node, Cast cast)
    {
        long count = Amount(ForEachOf(node, cast).Count, cast);
        if (count < 0)
        {
            throw new AbilityException("'forEach' needs a non-negative 'count'");
        }
        return count;
    }

    private static bool SkipForEachPreflight(AbilityNode node, Cast cast)
    {
        if ((cast.PaymentMayMutate || cast.PriorStepMayMutate)
            && AmountMayChange(ForEachOf(node, cast).Count))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
        }
        return StableZeroForEach(node, cast);
    }

    private static bool StableZeroForEach(AbilityNode node, Cast cast)
    {
        if (node.Kind != "forEach")
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

    private static bool CurrentlyZeroForEach(AbilityNode node, Cast cast)
    {
        if (node.Kind != "forEach" || HasUnboundPowerAmount(node, cast))
        {
            return false;
        }
        if ((cast.PaymentMayMutate || cast.PriorStepMayMutate)
            && AmountMayChange(ForEachOf(node, cast).Count))
        {
            return false;
        }
        return ForEachCount(node, cast) == 0;
    }

    private static bool LastingPeriodIsOpen(AbilityNode node, Cast cast) =>
        LastingPeriodIsOpen(Word(node.Require("until")), cast);

    private static bool LastingPeriodIsOpen(string until, Cast cast) =>
        until switch
        {
            TimingPoints.EndOfAttack => cast.World.Attack is not null
                || cast.World.CharacterAttack is not null
                || cast.Occurrence.Is(Steps.AttackInitiated),
            TimingPoints.EndOfActivation => cast.World.Activation is not null,
            _ => true,
        };

    private static bool HasLabelledPower(AbilityNode node) =>
        PowerNodes(node, BasicPowers.AttackVerb).Any()
        || PowerNodes(node, BasicPowers.ThwartVerb).Any()
        || PowerNodes(node, Attack.DefenseVerb).Any();

    private static bool HasInitiationConstraint(AbilityNode node) =>
        HasLabelledPower(node)
        || node.Kind == "grantUntil"
        || StructuralChildren(node).Any(HasInitiationConstraint);

    private static IEnumerable<AbilityNode> StructuralChildren(AbilityNode node) =>
        node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => Branches.Select(node.Field).Where(value => value is not null)
                .Select(value => Tree(value!)),
            "then" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            "otherwise" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("otherwise")),
            ],
            "defense" or "delayUntil" or "forEach" =>
                [Tree(node.Require("effect"))],
            "eachTime" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            _ => [],
        };

}
