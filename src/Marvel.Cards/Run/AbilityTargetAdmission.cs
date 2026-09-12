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

internal static class AbilityTargetAdmission
{
    /// <summary>Whether a tree has no current target, an invalid one, or a valid one.</summary>
    /// <remarks>
    /// <c>rr:target.2</c> asks only for “at least one valid target,” and
    /// <c>rr:target.3.4</c> says one effect on that target is enough. Keeping
    /// <see cref="TargetLegality.None"/> separate from
    /// <see cref="TargetLegality.Invalid"/> prevents an
    /// untargeted sibling from manufacturing a target while still allowing a
    /// valid sibling to make a multi-effect ability initiable.
    /// </remarks>
    internal static TargetLegality TargetLegalityOf(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange = false)
    {
        if (cast.Chosen is null
            && cast.Reachability.PriorBindingCandidates.Count > 0
            && BindingCanChange(node)
            && node.OperationName() is not ("seq" or "and" or "if" or "then" or "otherwise"
                or "forEach" or "defense" or "choose" or "eachTime"
                or "delayUntil" or "chooseCard"))
        {
            return CandidateTargetLegality(node, cast);
        }

        string operation = node.OperationName();
        if (operation is "seq" or "and" or "if")
            return CompositeTargetLegality(node, cast, bindingMayChange);
        if (operation is "then" or "otherwise")
            return DependentTargetLegality(node, cast, bindingMayChange);
        if (operation is "forEach" or "attack" or "thwart" or "defense"
            or "choose" or "eachTime" or "delayUntil" or "chooseCard")
        {
            return StructuralTargetLegality(node, cast, bindingMayChange);
        }
        if (operation is "removeFromGame" or "reveal" or "returnToHand"
            or "exhaust" or "ready" or "giveStatus" or "declareDefender"
            or "attachTo" or "grantUntil" or "discard")
        {
            return CardActionTargetLegality(node, cast);
        }
        if (operation is "dealEncounterCard" or "heal" or "dealDamage"
            or "dealAttackDamage" or "indirectDamage" or "placeThreat"
            or "removeThreat")
        {
            return DamageAndThreatTargetLegality(node, cast);
        }
        return OtherTargetLegality(node, cast, bindingMayChange);
    }

    private static TargetLegality CompositeTargetLegality(
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

    private static TargetLegality DependentTargetLegality(
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

    private static TargetLegality StructuralTargetLegality(
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

    private static TargetLegality CardActionTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
            "removeFromGame" => RemoveFromGameTargetLegality(node, cast),
            "reveal" or "returnToHand" => CardsLegality(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)),
            "exhaust" => CardsLegality(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)
                    .Where(card => card.Ready)),
            "ready" => CardsLegality(Every(
                EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Where(card =>
                !card.Ready && AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card))),
            "giveStatus" => CardsLegality(StatusTargets(node, cast)),
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
            _ => throw new InvalidOperationException("Unknown card-action target operation"),
        };

    private static TargetLegality DamageAndThreatTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
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

    private static TargetLegality OtherTargetLegality(
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

    private static TargetLegality CardsLegality(IEnumerable<Card> candidates) =>
        candidates.Any() ? TargetLegality.Valid : TargetLegality.Invalid;

    internal static TargetLegality CandidateTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast)
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

    internal static TargetLegality ChosenPlayerTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast)
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

    internal static TargetLegality SequenceTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange)
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

    internal static TargetLegality CombineTargetLegality(
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

    internal static IReadOnlyList<Card> StatusTargets(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var instruction = EffectOf<AbilityEffect.GiveStatus>(node, cast);
        string status = instruction.Status;
        return [.. Every(instruction.Cards, cast).Where(card =>
            DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(FacedownDrones.Kind(card, cast.World.Facts))
                && Statuses.Count(cast.World, card, status)
                < Statuses.Limit(cast.World, cast.World.Facts, card, status))];
    }

    internal static TargetLegality RemoveFromGameTargetLegality(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        return Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } removed
            && CanRemoveByEffect(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, removed)
                ? TargetLegality.Valid : TargetLegality.Invalid;
    }

    internal enum TargetLegality
    {
        None,
        Invalid,
        Valid,
    }

    internal static bool CanInitiateChoice(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var options = ((AbilityEffect.Choose)node).Options.ToList();
        foreach (var option in options)
        {
            _ = CanInitiate(option, cast);
        }

        return options.Any(option => OptionIsLegal(option, cast));
    }

    internal static bool CanInitiateForEach(AbilityEffect node, AbilityAdmissionScope cast)
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
                return CanInitiateUnchosenDamage(effect, cast, stateMayChange);
            if (effect.OperationName() == "removeThreat")
                return CanInitiateUnchosenThreatRemoval(effect, cast, stateMayChange);
            if (ContainsForEachTarget(effect))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a targeted for-each effect without choose "
                    + "whose one target cannot be persisted");
            }
        }
        return CanInitiate(effect, cast);
    }

    private static bool CanInitiateUnchosenDamage(
        AbilityEffect effect, AbilityAdmissionScope cast, bool stateMayChange)
    {
        if (DamageTargets(DamageSelectionOf(effect, cast), cast).Count != 1) return false;
        EnsureStableForEachTarget(
            ((AbilityEffect.Damage)effect).Cards, AbilityCardQuery.Villain,
            cast, stateMayChange);
        return CanInitiate(effect, cast);
    }

    private static bool CanInitiateUnchosenThreatRemoval(
        AbilityEffect effect, AbilityAdmissionScope cast, bool stateMayChange)
    {
        var selection = ((AbilityEffect.RemoveThreat)effect).Schemes;
        if (Every(selection, cast).Count != 1) return false;
        EnsureStableForEachTarget(
            selection, AbilityCardQuery.MainScheme, cast, stateMayChange);
        return CanInitiate(effect, cast);
    }

    private static void EnsureStableForEachTarget(
        AbilityCardSelection selection, AbilityCardQuery stableQuery,
        AbilityAdmissionScope cast, bool stateMayChange)
    {
        if (!stateMayChange || StableForEachTarget(selection, stableQuery)) return;
        throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' reaches an unbound targeted for-each effect "
            + "after state may change");
    }

    internal static bool CanInitiateEachTime(AbilityEffect node, AbilityAdmissionScope cast)
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

}
