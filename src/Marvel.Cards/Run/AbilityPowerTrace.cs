using static Marvel.Cards.Run.AbilityPowerAlternatives;
using static Marvel.Cards.Run.AbilityPowerSuspension;
using static Marvel.Cards.Run.AbilitySelectorTrace;
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
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerStateMutation;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
namespace Marvel.Cards.Run;

internal static class AbilityPowerTrace
{
    internal static Card? PowerFind(
        AbilityCardSelection value, AbilityAdmissionScope cast, AbilityPowerState reachability)
    {
        if (SelectorMembershipCanChange(value)
            || PotentialVillainSelector(value, cast))
        {
            return PowerEvery(value, cast, reachability).FirstOrDefault();
        }
        var found = Find(value, cast);
        int liveVillain = EncounterRouting.Villain(cast.World)?.ObjectId ?? -1;
        found = CurrentVillainCard(found, liveVillain, cast, reachability);
        return found is not null && !reachability.Discarded.Contains(found.ObjectId)
            ? found : null;
    }

    private static Card? CurrentVillainCard(
        Card? found, int liveVillain, AbilityAdmissionScope cast,
        AbilityPowerState reachability)
    {
        if (found is not null && reachability.EncounterStages is not null)
            return AbilityPowerEncounterProjection.Current(found, cast, reachability.EncounterStages);
        if (found?.ObjectId != liveVillain
            || reachability.CurrentVillain == liveVillain) return found;
        return reachability.Finished || reachability.CurrentVillain < 0
            ? null : cast.World.Cards[reachability.CurrentVillain];
    }

    internal static List<Card> PowerEvery(
        AbilityCardSelection value, AbilityAdmissionScope cast, AbilityPowerState reachability)
    {
        int liveVillain = EncounterRouting.Villain(cast.World)?.ObjectId ?? -1;
        bool dynamic = SelectorMembershipCanChange(value)
            || PotentialVillainSelector(value, cast);
        if (dynamic)
        {
            return DynamicPowerCards(value, cast, reachability, liveVillain);
        }
        return StablePowerCards(value, cast, reachability, liveVillain);
    }

    private static List<Card> DynamicPowerCards(
        AbilityCardSelection value, AbilityAdmissionScope cast,
        AbilityPowerState reachability, int liveVillain)
    {
        var candidates = TraceCandidateCards(value, cast);
        if (liveVillain >= 0 && PotentialVillainSelector(value, cast)
            && candidates.All(card => card.ObjectId != liveVillain))
        {
            candidates.Insert(0, cast.World.Cards[liveVillain]);
        }
        List<Card> selected =
        [
            .. AbilityEncounterCandidateOrder.Apply(candidates.Select(card => CurrentVillainCard(card, liveVillain, cast, reachability))
                .Where(card => card is not null).Cast<Card>()
                .DistinctBy(card => card.ObjectId), cast.World, reachability.EncounterOrder)
                .Where(card => !reachability.Discarded.Contains(card.ObjectId)
                    && TraceSelectorMatches(
                        value, card, new(reachability.CurrentVillain, cast,
                        reachability.Discarded, reachability.Traits,
                            reachability.Modifiers, reachability.Engagement)
                            { EncounterStages = reachability.EncounterStages, EncounterOrder = reachability.EncounterOrder })),
        ];
        return reachability.EncounterOrder is not null && UsesObjectIdOrder(value)
            ? [.. selected.OrderBy(card => card.ObjectId)] : selected;
    }

    private static List<Card> StablePowerCards(
        AbilityCardSelection value, AbilityAdmissionScope cast,
        AbilityPowerState reachability, int liveVillain)
    {
        var cards = new List<Card>();
        foreach (var found in Every(value, cast))
        {
            Card? card = CurrentVillainCard(found, liveVillain, cast, reachability);
            if (card is not null
                && !reachability.Discarded.Contains(card.ObjectId)
                && cards.All(existing => existing.ObjectId != card.ObjectId))
            {
                cards.Add(card);
            }
        }
        return cards;
    }

    internal static PowerReachability PowerStateAfter(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (reachability is PowerReachability.Unsupported)
        {
            return reachability;
        }
        try
        {
            return MergePowerAlternatives(PowerPaths(reachability).Select(path =>
                PowerStateAfterKnown(node, cast, stateMayChange, bindingMayChange, path)));
        }
        catch (RulesNotImplementedException unsupported)
        {
            return new PowerReachability.Unsupported(unsupported.Message);
        }
    }

    internal static PowerReachability PowerStateAfterKnown(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState reachability)
    {
        AbilityProjectedDefeatWindows.ValidateContinuation(node, reachability, cast);
        // SuspendsPowerEffect rejects this simultaneous choice by shape. Do
        // not eagerly replay its children while computing a later sibling's
        // abstract state; each replay would invent another damage instance.
        if (node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any())
        {
            return reachability;
        }
        if (node.OperationName() == "changeForm")
        {
            return ChangeFormState(FormChangeOf(node, cast), cast, bindingMayChange, reachability);
        }
        if (node.OperationName() == "forEach")
            return RepeatedPowerStateAfter(
                node, cast, stateMayChange, bindingMayChange, reachability);
        if (node.OperationName() is "then" or "otherwise")
        {
            return PowerDependentStateAfter(
                node, cast, stateMayChange, bindingMayChange, reachability);
        }

        return ChildPowerStateAfter(
            node, cast, stateMayChange, bindingMayChange, reachability);
    }

    private static PowerReachability RepeatedPowerStateAfter(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState reachability)
    {
        if (HasUnboundPowerAmount(node, cast))
        {
            return MergePowerStates(
                reachability,
                PowerStateAfter(
                    EffectBody(node), cast, stateMayChange, bindingMayChange,
                    reachability), cast);
        }
        long count = ForEachCount(node, cast);
        var effect = EffectBody(node);
        if (!Choices(effect).Any() && effect.OperationName() == "dealDamage")
            return ApplyPowerLeafState(
                effect, cast, bindingMayChange, reachability, count);
        PowerReachability repeated = reachability;
        for (long iteration = 0; iteration < count; iteration++)
        {
            var next = PowerStateAfter(
                effect, cast, stateMayChange || iteration > 0,
                bindingMayChange, repeated);
            if (SamePowerState(next, repeated)) break;
            repeated = next;
        }
        return repeated;
    }

    private static PowerReachability ChildPowerStateAfter(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState reachability)
    {
        var advanced = ApplyPowerLeafState(node, cast, bindingMayChange, reachability);
        var children = PowerSuspensionChildren(
            node, cast, stateMayChange, bindingMayChange, advanced).ToList();
        if (children.Count == 0)
        {
            return advanced;
        }
        if (node.OperationName() == "seq"
            || (node.OperationName() == "and" && children.Count == 1))
        {
            PowerReachability ordered = advanced;
            foreach (var child in children)
            {
                ordered = PowerStateAfter(
                    child.Node, cast, child.StateMayChange,
                    child.BindingMayChange, child.Reachability);
            }
            return ordered;
        }

        bool includeBaseline = node.OperationName() != "if"
            || ConditionalCanSkipBranch(
                node, cast, stateMayChange, bindingMayChange, advanced);
        PowerReachability? merged = includeBaseline ? advanced : null;
        foreach (var child in children)
        {
            var branch = PowerStateAfter(
                child.Node, cast, child.StateMayChange,
                child.BindingMayChange, child.Reachability);
            merged = merged is { } prior
                ? MergePowerStates(prior, branch, cast)
                : branch;
        }
        return merged ?? advanced;
    }

    internal static PowerReachability PowerDependentStateAfter(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        var effect = EffectBody(node);
        var dependent = EffectFollowing(node);
        var required = node.OperationName() == "then"
            ? AdmissionResolution.Full
            : AdmissionResolution.None;
        bool answered = ActiveChoices(effect, cast).Any();
        var outcomes = PowerOutcomeStates(
            effect, cast, stateMayChange, bindingMayChange, reachability);
        PowerReachability? merged = null;
        foreach (var outcome in outcomes)
        {
            var branch = outcome.Outcome == required
                ? PowerStateAfter(
                    dependent, cast,
                    node.OperationName() == "then" || answered || outcomes.Count > 1,
                    bindingMayChange, outcome.State)
                : outcome.State;
            merged = merged is { } prior
                ? MergePowerStates(prior, branch, cast)
                : branch;
        }
        return merged ?? reachability;
    }

    internal readonly record struct PowerOutcomeState(
        AdmissionResolution Outcome, PowerReachability State);

    internal static List<PowerOutcomeState> PowerOutcomeStates(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
        => [.. PowerPaths(reachability).SelectMany(path => PowerOutcomeStatesKnown(
            node, cast, stateMayChange, bindingMayChange, path))];

    internal static List<PowerOutcomeState> PowerOutcomeStatesKnown(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState reachability)
    {
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            bool canSwitch = PowerTestCanChange(
                test, cast, stateMayChange, bindingMayChange, reachability);
            IEnumerable<AbilityEffect?> branches = canSwitch
                ? new AbilityEffect?[] { ((AbilityEffect.Conditional)node).Then, ((AbilityEffect.Conditional)node).Else }
                : new AbilityEffect?[] { ConditionalBranch(node, Test(test, cast) ? "then" : "else") };
            return [.. branches.SelectMany(branch => branch is null
                ? [new PowerOutcomeState(AdmissionResolution.None, reachability)]
                : PowerOutcomeStates(
                    branch, cast, stateMayChange,
                    bindingMayChange, reachability))];
        }
        if (node.OperationName() == "seq")
        {
            var states = new List<PowerOutcomeState>
            {
                new(AdmissionResolution.None, reachability),
            };
            int index = 0;
            foreach (var child in OrderedEffects(node))
            {
                int childIndex = index++;
                states = [.. states.SelectMany(prior => PowerOutcomeStates(
                    child, cast, stateMayChange || childIndex > 0,
                    bindingMayChange, prior.State).Select(next => new PowerOutcomeState(
                        childIndex == 0
                            ? next.Outcome
                            : CombinePowerOutcomes(prior.Outcome, next.Outcome),
                        next.State)))];
            }
            return states;
        }

        var after = PowerStateAfter(
            node, cast, stateMayChange, bindingMayChange, reachability);
        var outcomes = PowerOutcomes(
            node, cast, stateMayChange, bindingMayChange, reachability);
        return [.. PowerPaths(after).SelectMany(state => outcomes.Select(outcome =>
            new PowerOutcomeState(outcome, state)))];
    }

    internal static AdmissionResolution CombinePowerOutcomes(
        AdmissionResolution left, AdmissionResolution right) =>
        left == right ? left : AdmissionResolution.Partial;

}
