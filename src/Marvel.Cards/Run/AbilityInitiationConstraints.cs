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

    internal static bool CanTargetAttack(AbilityEffect node, AbilityAdmissionScope cast)
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
            && AbilityProgramQueries.CanTakeDamage(
                cast.World, cast.Context.Program, enemy, cast.Source);
    }

    internal static bool CanTargetThwart(AbilityEffect node, AbilityAdmissionScope cast)
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
            return BasicThwartPowers.CanAutomaticallyThwart(
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
        bool crisisException = BasicThwartPowers.CanAutomaticallyThwart(
                cast.World, cast.World.Facts, Resolver(cast), scheme)
            && CrisisIgnoringRemovalCanAffect(
                EffectBody(node), cast, scheme);
        if (crisisException)
        {
            cast.ValidateCrisisIgnoringThwart(node);
        }
        return crisisException;
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

    internal static bool CrisisIgnoringRemovalCanAffect(
        AbilityEffect node, AbilityAdmissionScope cast, Card scheme)
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

    internal static bool CrisisIgnoringRemovalCanAffectBound(
        AbilityEffect node, AbilityAdmissionScope cast, Card scheme)
    {
        if (node.OperationName() == "removeThreat")
            return DirectCrisisIgnoringRemovalCanAffect(node, cast, scheme);

        bool? structural = StructuralCrisisIgnoringRemovalCanAffect(node, cast, scheme);
        return structural ?? DependentCrisisIgnoringRemovalCanAffect(node, cast, scheme);
    }

    private static bool DirectCrisisIgnoringRemovalCanAffect(
        AbilityEffect node, AbilityAdmissionScope cast, Card scheme) =>
        IgnoresCrisis(node, cast)
        && Every(ThreatSelectionOf(node, cast), cast).Any(candidate =>
            candidate.ObjectId == scheme.ObjectId)
        && scheme.Tokens.GetValueOrDefault("k_threat") > 0
        && Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast) > 0
        && CanRemoveThreatFrom(node, cast, scheme);

    private static bool? StructuralCrisisIgnoringRemovalCanAffect(
        AbilityEffect node, AbilityAdmissionScope cast, Card scheme) =>
        node.OperationName() switch
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
            "eachPlayer" or "defense" => CrisisIgnoringRemovalCanAffectBound(
                EffectBody(node), cast, scheme),
            "then" or "otherwise" => null,
            _ => false,
        };

    private static bool DependentCrisisIgnoringRemovalCanAffect(
        AbilityEffect node, AbilityAdmissionScope cast, Card scheme)
    {
        var body = EffectBody(node);
        if (ActiveChoices(body, cast).Any())
            return CrisisIgnoringRemovalCanAffectBound(body, cast, scheme);
        var required = node.OperationName() == "then"
            ? AdmissionResolution.Full : AdmissionResolution.None;
        return CrisisIgnoringRemovalCanAffectBound(body, cast, scheme)
            || ResolutionOf(body, cast) == required
            && CrisisIgnoringRemovalCanAffectBound(EffectFollowing(node), cast, scheme);
    }
}
