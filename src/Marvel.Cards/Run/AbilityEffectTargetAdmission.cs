using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityCardActionTargetAdmission;
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

using static Marvel.Cards.Run.AbilityPowerTargetAdmission;

/// <summary>Classifies the targets affected by each compiled effect shape.</summary>
internal static class AbilityEffectTargetAdmission
{
    internal static TargetLegality CompositeTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange) =>
        node.OperationName() switch
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
            _ => throw new InvalidOperationException("Unknown composite target operation"),
        };

    internal static TargetLegality DependentTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange) =>
        node.OperationName() switch
        {
            "then" when ActiveChoices(EffectBody(node), cast).Any() =>
                TargetLegalityOf(
                    EffectBody(node), cast, bindingMayChange),
            "then" => ResolutionOf(EffectBody(node), cast)
                == AdmissionResolution.Full
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
                == AdmissionResolution.None
                    ? TargetLegalityOf(
                        EffectFollowing(node), cast, bindingMayChange)
                    : TargetLegalityOf(
                        EffectBody(node), cast, bindingMayChange),
            _ => throw new InvalidOperationException("Unknown dependent target operation"),
        };

    internal static TargetLegality StructuralTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange) =>
        node.OperationName() switch
        {
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
            _ => throw new InvalidOperationException("Unknown structural target operation"),
        };

    internal static TargetLegality DamageAndThreatTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
            "moveDamage" or "moveAttackDamage" => MoveDamageTargetLegality((AbilityEffect.MoveDamage)node, cast),
            "dealEncounterCard" => Find(EffectOf<AbilityEffect.DealEncounterCard>(node, cast).Card, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "heal" => Find(EffectOf<AbilityEffect.Heal>(node, cast).Card, cast) is { Damage: > 0 }
                && Amount(EffectOf<AbilityEffect.Heal>(node, cast).Amount, cast) > 0
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "dealDamage" or "dealAttackDamage" =>
                Amount(DamageAmountOf(node, cast), cast) > 0
                    ? CardsLegality(DamageTargets(DamageSelectionOf(node, cast), cast))
                    : TargetLegality.Invalid,
            "indirectDamage" => Amount(EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast) <= 0
                ? TargetLegality.Invalid
                : CardsLegality(Assignable(DamageSelectionOf(node, cast), cast)),
            "placeThreat" => Amount(EffectOf<AbilityEffect.PlaceThreat>(node, cast).Amount, cast) <= 0
                ? TargetLegality.Invalid
                : CardsLegality(Every(ThreatSelectionOf(node, cast), cast)),
            "removeThreat" => Every(ThreatSelectionOf(node, cast), cast).Any(scheme =>
                scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast) > 0
                && CanRemoveThreatFrom(node, cast, scheme))
                ? TargetLegality.Valid : TargetLegality.Invalid,
            _ => throw new InvalidOperationException("Unknown damage or threat target operation"),
        };

    internal static TargetLegality OtherTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange) =>
        node.OperationName() switch
        {
            "enemyAttacks" or "enemySchemes" =>
                CardsLegality(Every(ActivationOf(node, cast).Enemies, cast)),
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
            "draw" => AbilityRepeatedStatusTrace.CanDraw(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "search" => HasSearchableArea(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            _ => TargetLegality.None,
        };

    private static TargetLegality MoveDamageTargetLegality(
        AbilityEffect.MoveDamage move, AbilityAdmissionScope cast)
    {
        // rr:target.3: a target is valid only if the ability can affect it.
        // rr:move.2: without a valid source or destination, no move can be made.
        var from = Find(move.From, cast);
        var to = Find(move.To, cast);
        return from is { Damage: > 0 } && to is not null && from != to
            && Amount(move.Amount, cast) > 0
            && AbilityProgramQueries.CanTakeDamage(cast.World, cast.Context.Program, to, cast.Source)
                ? TargetLegality.Valid : TargetLegality.Invalid;
    }
}
