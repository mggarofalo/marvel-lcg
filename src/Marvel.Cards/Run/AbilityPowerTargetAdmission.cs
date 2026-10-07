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

using static Marvel.Cards.Run.AbilityInitiationConstraints;

/// <summary>Validates declared attack and thwart targets and their scoped exceptions.</summary>
internal static class AbilityPowerTargetAdmission
{
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
                cast.World, cast.Context.Program, enemy, cast.Source)
            && AttackEffectHasValidTargets(node, cast, enemy);
    }

    private static bool AttackEffectHasValidTargets(
        AbilityEffect node, AbilityAdmissionScope cast, Card target)
    {
        // rr:stun-stunned.5.1: A stunned character can use an attack ability
        // even if it has no valid target for an attack.
        if (LabeledAbilities.WouldBeCancelled(
            cast.World, cast.World.Facts, Resolver(cast), cast.Source,
            [BasicPowers.AttackVerb]))
        {
            return true;
        }

        var prior = cast.CaptureChosen();
        try
        {
            // Power resolution binds its target before evaluating the effect.
            cast.Choose(target);
            return TargetLegalityOf(EffectBody(node), cast) != TargetLegality.Invalid;
        }
        finally
        {
            cast.RestoreChosen(prior);
        }
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
