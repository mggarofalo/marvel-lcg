using static Marvel.Cards.Run.AbilityPowerTargetAdmission;
using static Marvel.Cards.Run.AbilityPlayerSelectorRelations;
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

internal static class AbilityInitiationConstraints
{
    internal static void PreflightInitiationConstraints(
        AbilityEffect node, AbilityAdmissionScope cast, bool requireCurrentTargets)
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

    internal static bool CanInitiateAnd(AbilityEffect node, AbilityAdmissionScope cast)
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

    internal static bool CanInitiateDependent(
        AbilityEffect node, AbilityAdmissionScope cast, AdmissionResolution required, string branch)
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

    internal static bool ChoiceHasNestedChoice(AbilityEffect choice) => choice.OperationName() switch
    {
        "chooseCard" => Choices(EffectBody(choice)).Any(),
        "choose" => ((AbilityEffect.Choose)choice).Options.Any(option => Choices(option).Any()),
        _ => false,
    };

    internal static bool CanInitiateLeaf(AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
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

    internal static bool CanInitiateCounters(AbilityEffect.PlaceCounters counters, AbilityAdmissionScope cast)
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

    internal static bool CanInitiateChooseCard(AbilityEffect node, AbilityAdmissionScope cast)
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

    internal static bool CanInitiateDraw(AbilityEffect node, AbilityAdmissionScope cast)
    {
        bool CanDrawFromBinding() =>
            !BindingCanChange(((AbilityEffect.Draw)node).Players)
            || (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 }
                && AbilityRepeatedStatusTrace.CanDraw(node, cast);

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

    internal static bool EveryCandidateCan(AbilityAdmissionScope cast, Func<bool> test)
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

}
