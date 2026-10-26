using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

/// <summary>Bounds opaque minion recovery without reading an encounter deck.</summary>
internal static class AbilityProjectedRecoveryWindow
{
    internal static AbilityPowerState Defer(AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        if (!CanDefer(state, subject, cast))
            throw new RulesNotImplementedException(
                $"character '{subject.FaceId}' would be defeated before a "
                + "labelled-power continuation reads a step-6 interrupt, "
                + "which is not implemented");
        return state with { DefeatWindowDeferred = true, RecoveryWindowDeferred = true,
            RecoverySubject = subject.ObjectId };
    }

    private static bool CanDefer(AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        if (EffectiveCards.Kind(subject, cast.World.Facts) != CardKind.Minion
            || AbilityProjectedDefeatWindows.HasWork(state, subject, cast)) return false;
        var occurrence = new Occurrence(0, [Steps.CardWouldBeDefeated],
            Subject: subject.ObjectId, Player: subject.Owner);
        var candidates = AbilityWindowAdmission.Waiting(cast.Context.Program, cast.World, occurrence,
            WindowKind.Interrupt, cast.Context.ResourceAbilities)
            .Where(candidate => !state.Discarded.Contains(candidate.Card.ObjectId)).ToArray();
        return candidates.Length > 0 && candidates.All(candidate => candidate.Ability.Trigger.Timing == AbilityType.ForcedInterrupt
            && candidate.Ability.When is null && candidate.Ability.Cost is null
            && (candidate.Card == subject && IsSelfRecovery(candidate.Ability.Effect)
                || IsAttachedRecovery(candidate, subject, cast)));
    }

    private static bool IsAttachedRecovery(AbilityWindowAdmission.Candidate candidate,
        Card subject, AbilityAdmissionScope cast)
    {
        var card = candidate.Card;
        if (card.Area.Host != subject.ObjectId
            || !DeckTypes.IsInPlay(card.Area.Type)
            || StateFields.Modified(cast.World, card, "permanent", cast.World.Facts, cast.World.Players) > 0)
            return false;
        // The live interrupt owns healing, its attachment departure and first-player
        // ordering. Only this exact full-heal/self-discard body is opaque here.
        return candidate.Ability.Effect is AbilityEffect.Sequence
        {
            Effects: [AbilityEffect.Heal
            {
                Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo },
                Amount: AbilityNumber.CardValue
                {
                    Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo },
                    Property: AbilityCardNumberProperty.Damage,
                },
            }, AbilityEffect.CardAction
            {
                Instruction: AbilityCardInstruction.Discard,
                Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
            }],
        };
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

    internal static bool PreservesReads(AbilityEffect node, AbilityPowerState state, AbilityAdmissionScope cast)
    {
        if (node is not AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.ReturnToHand, Selection: AbilityCardSelection.Titled } returned)
            return false;
        var cards = Every(returned.Selection, cast).ToArray();
        if (cards.Length != 1 || cards[0].Owner != cast.Player
            || EffectiveCards.Kind(cards[0], cast.World.Facts) != CardKind.Upgrade
            || !DeckTypes.IsInPlay(cards[0].Area.Type)
            || state.RecoverySubject is not { } subject) return false;
        if (!new AbilityRecoveryDependencies(cast, state, cast.World.Cards[subject]).Preserves(cards[0])) return false;
        // The settled live departure owns the returned card's constant-ending
        // cascade. Prove its supported shape without predicting the encounter draw.
        cast.World.Effects.PreflightConstantsEnding(cards[0]);
        return true;
    }

}
