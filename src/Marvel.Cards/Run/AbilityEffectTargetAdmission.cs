using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityCardActionTargetAdmission;
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
            "then" when EffectBody(node) is AbilityEffect.ActivateEnemies { Dynamic: false }
                || ActiveChoices(EffectBody(node), cast).Any() =>
                TargetLegalityOf(
                    EffectBody(node), cast, bindingMayChange),
            "then" => ResolutionOf(EffectBody(node), cast)
                == AdmissionResolution.Full
                    ? CombineTargetLegality(
                    [
                        TargetLegalityOf(
                            EffectBody(node), cast, bindingMayChange),
                        TargetLegalityOf(
                            EffectFollowing(node), cast.AfterBoostCancellation(EffectBody(node)), bindingMayChange),
                    ])
                    : TargetLegalityOf(
                        EffectBody(node), cast, bindingMayChange),
            "otherwise" when EffectBody(node) is AbilityEffect.ActivateEnemies { Dynamic: false } activation =>
                TargetLegalityOf(ActivationCandidates(activation, cast).Count == 0
                    ? EffectFollowing(node) : EffectBody(node), cast, bindingMayChange),
            "otherwise" when ActiveChoices(EffectBody(node), cast).Any() =>
                TargetLegalityOf(
                    EffectBody(node), cast, bindingMayChange),
            "otherwise" => ResolutionOf(EffectBody(node), cast)
                == AdmissionResolution.None
                    ? TargetLegalityOf(
                        EffectFollowing(node), cast.AfterBoostCancellation(EffectBody(node)), bindingMayChange)
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
            "moveThreat" => AbilityThreatMovementAdmission.CanMove((AbilityEffect.MoveThreat)node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "moveDamage" or "moveAttackDamage" => MoveDamageTargetLegality((AbilityEffect.MoveDamage)node, cast),
            "dealEncounterCard" => Find(EffectOf<AbilityEffect.DealEncounterCard>(node, cast).Card, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "heal" => AbilityFutureResultAdmission.HealCanProduce(EffectOf<AbilityEffect.Heal>(node, cast), cast)
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "setHitPoints" => AbilityHitPointAssignment.TargetLegalityOf(node, cast),
            "dealDamage" when node is AbilityEffect.Damage { Maximum: not null } group =>
                AbilityDamageRecipients.Candidates(group, cast.Context).Count > 0
                    ? TargetLegality.Valid : TargetLegality.Invalid,
            "dealDamage" or "dealAttackDamage" =>
                AbilityFutureDiscardAdmission.MayHavePositiveAmount(DamageAmountOf(node, cast), cast)
                    ? CardsLegality(DamageTargets(DamageSelectionOf(node, cast), cast))
                    : TargetLegality.Invalid,
            "indirectDamage" => (AbilityFutureDiscardAdmission.MayHavePositiveAmount(
                EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast)
                || AbilityEventModifiers.DamageAmount(cast.World, cast.Source,
                    Amount(EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast)) > 0)
                    ? CardsLegality(Assignable(DamageSelectionOf(node, cast), cast))
                    : TargetLegality.Invalid,
            "placeThreat" => Amount(EffectOf<AbilityEffect.PlaceThreat>(node, cast).Amount, cast) <= 0
                ? TargetLegality.Invalid
                : CardsLegality(Every(ThreatSelectionOf(node, cast), cast)),
            "removeThreat" => RemoveThreatTargetLegality(node, cast),
            _ => throw new InvalidOperationException("Unknown damage or threat target operation"),
        };

    private static TargetLegality BoostCancellationTargetLegality(AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() == "cancelBoostAbility"
            ? cast.Occurrence.BoostAbility is { } boost && boost.CanCancel(cast.World)
                ? TargetLegality.Valid : TargetLegality.Invalid
            :
        cast.Occurrence.BoostIcons is { Remaining: > 0 } icons && icons.IsCurrent(cast.World)
            ? TargetLegality.Valid : TargetLegality.Invalid;

    internal static TargetLegality OtherTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange) =>
        CombatTimingLegality(node, cast) ?? (node.OperationName() switch
        {
            // ruling:8c9c9d30f4c01189: an Invocation can affect itself by
            // entering its discard pile even when its other targets are invalid.
            "discardInvocation" => InvocationDiscardTargetLegality(cast),
            "placeCounters" => AbilityCounterTargetAdmission.Of((AbilityEffect.PlaceCounters)node, cast),
            // rr:target.3.4: "at least one of those effects must be able to
            // affect the target." Deferring resolution retains the bound target.
            "cancelBoostIcons" or "cancelBoostAbility" => BoostCancellationTargetLegality(node, cast),
            "preventThreat" => ThreatPreventionTargetLegality(node, cast),
            "boostBasicUse" => BasicUseLegality((AbilityEffect.BoostBasicUse)node, cast),
            "modifyPlayedEvent" => AbilityPlayedEventModifier.CanModify(EffectOf<AbilityEffect.ModifyPlayedEvent>(node, cast), cast.Context)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "afterActivation" => TargetLegalityOf(EffectBody(node), cast, bindingMayChange),
            // rr:target.1 includes "you"; rr:target.3.5 excludes a target
            // that "cannot perform" the requested game function.
            "changeForm" or "replaceStatus" or "removeStatuses" => CharacterChangeTargetLegality(node, cast),
            "enemyAttacks" or "enemySchemes" =>
                CardsLegality(Every(ActivationOf(node, cast).Enemies, cast)),
            "attachCards" => AbilityAttachmentExecution.Target((AbilityEffect.AttachCards)node, cast),
            "chooseStatusRecipients" => AbilityStatusRecipients.Target((AbilityEffect.ChooseStatusRecipients)node, cast.Context),
            "chooseCardsToHand" => AbilityGroupRetrieval.Target((AbilityEffect.ChooseCardsToHand)node, cast.Context),
            "releaseBeneath" => AbilityBeneathAdmission.Target((AbilityEffect.ReleaseBeneath)node, cast),
            "storeBeneath" => AbilityBeneathAdmission.Target((AbilityEffect.StoreBeneath)node, cast),
            "storeDeckTopBeneath" => AbilityBeneathAdmission.Target((AbilityEffect.StoreDeckTopBeneath)node, cast),
            "putIntoPlay" => AbilityAllyEntry.HasTarget(EffectOf<AbilityEffect.PutIntoPlay>(node, cast), cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "placeAtRandom" => Find(EffectOf<AbilityEffect.PlaceAtRandom>(node, cast).Host, cast) is null
                ? TargetLegality.Invalid : TargetLegality.Valid,
            "draw" => DrawTargetLegality((AbilityEffect.Draw)node, cast, bindingMayChange),
            // rr:target.1 lists "a card" among targets, as rr:target.2.3 does for
            // drawing: a random hand discard affects a card only while one is in hand.
            "discardAtRandom" => AbilityCardStateAdmission.CardStatePartialResolution(node, cast) == true
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "discardUntil" => DiscardUntilTargetLegality(node, cast),
            "search" => HasSearchableArea(node, cast)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "grantCharactersWithTrait" => TraitGroupTargetLegality((AbilityEffect.GrantTraitCharacters)node, cast),
            "inspectEncounterTop" or "inspectDiscardOrderEncounterTop" => EncounterRouting.Deck(cast.World).Cards.Count > 0
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "peekTop" => AbilityDeckPeek.Decks(cast.World).Count > 0
                ? TargetLegality.Valid : TargetLegality.Invalid,
            _ => TargetLegality.None,
        });

    private static TargetLegality? CombatTimingLegality(AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
            "reflectAttackDamage" => PlayerAttackReflection.CanInstall(cast.World, cast.Source)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "readyUndamagedDefender" => UndamagedDefenseReady.CanInstall(cast.World, cast.Occurrence, cast.Player)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "useAttackForThwart" => ThwartStatSubstitution.CanUseAttack(cast.World, cast.Occurrence)
                ? TargetLegality.Valid : TargetLegality.Invalid,
            "preventAttackDamage" => cast.World.Attack is { PreventAllDamage: false }
                ? TargetLegality.Valid : TargetLegality.Invalid,
            _ => null,
        };

    private static TargetLegality InvocationDiscardTargetLegality(AbilityAdmissionScope cast) =>
        cast.Player >= 0 && cast.Player < cast.World.Seats.Count
            && cast.World.Seats[cast.Player].InvocationDeck?.CanDiscardTop(cast.Source) == true
                ? TargetLegality.Valid : TargetLegality.Invalid;

    private static TargetLegality CharacterChangeTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
    {
        "changeForm" => CanChangeForm(FormChangeOf(node, cast), cast)
            ? TargetLegality.Valid : TargetLegality.Invalid,
        "replaceStatus" => AbilityStatusReplacement.CanReplace((AbilityEffect.ReplaceStatus)node, cast)
            ? TargetLegality.Valid : TargetLegality.Invalid,
        "removeStatuses" => AbilityStatusRemoval.Candidates((AbilityEffect.RemoveStatuses)node, cast).Count > 0
            ? TargetLegality.Valid : TargetLegality.Invalid,
        _ => TargetLegality.None,
    };
    private static TargetLegality DiscardUntilTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node is not AbilityEffect.DiscardUntil { From: AbilitySearchArea.YourDeck })
            return TargetLegality.None;
        return HasPartialResolutionTargets(node, cast) ? TargetLegality.Valid : TargetLegality.Invalid;
    }

    private static TargetLegality TraitGroupTargetLegality(
        AbilityEffect.GrantTraitCharacters grant, AbilityAdmissionScope cast) =>
        AbilityTraitGroupExecution.HasRecipients(grant, cast.World)
            ? TargetLegality.Valid : TargetLegality.None;

    private static TargetLegality DrawTargetLegality(
        AbilityEffect.Draw draw, AbilityAdmissionScope cast, bool bindingMayChange)
    {
        bool variablePlayer = BindingCanChange(draw.Players);
        if (cast.Chosen is null && bindingMayChange && variablePlayer)
            return CanInitiateDraw(draw, cast) ? TargetLegality.Valid : TargetLegality.Invalid;
        if (variablePlayer && (cast.PlayerSelection ?? cast.Chosen) is { Owner: < 0 })
            return TargetLegality.Invalid;
        return AbilityRepeatedStatusTrace.CanDraw(draw, cast) ? TargetLegality.Valid : TargetLegality.Invalid;
    }

    private static TargetLegality RemoveThreatTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        Every(ThreatSelectionOf(node, cast), cast).Any(scheme =>
            scheme.Tokens.GetValueOrDefault("k_threat") > 0
            && AbilityEventModifiers.ThreatRemovalAmount(EffectOf<AbilityEffect.RemoveThreat>(node, cast), cast.World, cast.Source, Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast)) > 0
            && CanRemoveThreatFrom(node, cast, scheme))
            ? TargetLegality.Valid : TargetLegality.Invalid;

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
    private static TargetLegality BasicUseLegality(
        AbilityEffect.BoostBasicUse effect, AbilityAdmissionScope cast) =>
        BasicUseAllyReceipt.CanApply(effect, cast.Context)
            ? TargetLegality.Valid : TargetLegality.Invalid;
    private static TargetLegality ThreatPreventionTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        // rr:target.3: "if any part of that ability can affect that target".
        // rr:prevent.2 reduces the assignment before placement; zero remaining
        // threat cannot be affected by this prevention effect.
        CanPartiallyResolve(node, cast) ? TargetLegality.Valid : TargetLegality.Invalid;

}
