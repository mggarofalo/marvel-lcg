using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

/// <summary>Bounds opaque self-recovery without reading an encounter deck.</summary>
internal static class AbilityProjectedRecoveryWindow
{
    internal static AbilityPowerState Defer(AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        if (!CanDefer(state, subject, cast))
            throw new RulesNotImplementedException(
                $"character '{subject.FaceId}' would be defeated before a "
                + "labelled-power continuation reads a step-6 interrupt, "
                + "which is not implemented");
        return state with { DefeatWindowDeferred = true, RecoveryWindowDeferred = true };
    }

    private static bool CanDefer(AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        if (EffectiveCards.Kind(subject, cast.World.Facts) != CardKind.Minion
            || cast.World.Areas.Any(area => area.Host == subject.ObjectId && area.Cards.Count > 0)) return false;
        var occurrence = new Occurrence(0, [Steps.CardWouldBeDefeated],
            Subject: subject.ObjectId, Player: subject.Owner);
        var candidates = AbilityWindowAdmission.Waiting(cast.Context.Program, cast.World, occurrence,
            WindowKind.Interrupt, cast.Context.ResourceAbilities)
            .Where(candidate => !state.Discarded.Contains(candidate.Card.ObjectId)).ToArray();
        return candidates.Length > 0 && candidates.All(candidate => candidate.Card == subject
            && candidate.Ability.Trigger.Timing == AbilityType.ForcedInterrupt
            && candidate.Ability.When is null && candidate.Ability.Cost is null
            && IsSelfRecovery(candidate.Ability.Effect));
    }

    private static bool IsSelfRecovery(AbilityEffect effect) => effect is AbilityEffect.EachTime
    {
        AfterDiscard: true,
        Effect: AbilityEffect.DiscardTop
        {
            From: AbilitySearchArea.EncounterDeck, Players: null,
            Count: AbilityNumber.Constant { Value: 1 },
        },
        When: AbilityCondition.IsKind
        {
            Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.That },
            Kind: CardKind.Treachery,
        },
        Then: AbilityEffect.Heal
        {
            Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
            Amount: AbilityNumber.CardValue
            {
                Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
                Property: AbilityCardNumberProperty.Damage,
            },
        },
    };

    internal static bool PreservesReads(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node is not AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.ReturnToHand, Selection: AbilityCardSelection.Titled } returned)
            return false;
        if (cast.World.Effects.Registered.Any(effect => effect.Kind == "health")) return false;
        var cards = Every(returned.Selection, cast).ToArray();
        if (cards.Length != 1 || cards[0].Owner != cast.Player
            || EffectiveCards.Kind(cards[0], cast.World.Facts) != CardKind.Upgrade
            || !DeckTypes.IsInPlay(cards[0].Area.Type)
            || cast.World.Areas.Any(area => area.Host == cards[0].ObjectId && area.Cards.Count > 0)) return false;
        // The projection does not know whether the minion survives. Restrict the
        // entire live constant dependency graph to non-health modifiers: a health
        // predicate elsewhere could otherwise make this departure defeat a card.
        return cast.World.Areas.Where(area => DeckTypes.IsInPlay(area.Type)).SelectMany(area => area.Cards)
            .SelectMany(card => AbilityProgramQueries.ActiveOn(cast.World, cast.Context.Program, card))
            .Where(ability => ability.Trigger.Timing == AbilityType.Constant)
            .All(ability => NonHealthConstant(ability.Effect));
    }

    private static bool NonHealthConstant(AbilityEffect effect) => effect switch
    {
        AbilityEffect.GrantField { Field: "attack", Until: null } => true,
        AbilityEffect.GrantTrait { Until: null } => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(NonHealthConstant),
        AbilityEffect.Conditional conditional => (conditional.Then is null || NonHealthConstant(conditional.Then))
            && (conditional.Else is null || NonHealthConstant(conditional.Else)),
        _ => false,
    };
}
