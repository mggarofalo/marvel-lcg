using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Registers delayed instructions with their originating combat period.</summary>
internal static class AbilityDelayedEffects
{
    internal static void Register(AbilityEffect.DelayedStun delayed, AbilityImmediateContext context)
    {
        if (delayed.Within == TimingPoints.EndOfActivation && context.Admission.World.Activation is null)
            throw new RulesNotImplementedException("an activation-bound delayed effect has no activation");
        context.Admission.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: delayed.HeroOnly ? DelayedEffects.StunTheDamagedHero : DelayedEffects.StunTheSubject,
            Card: context.Admission.Source.ObjectId, Affects: null,
            Lasts: new Duration(Until: delayed.Within, OnCondition: Steps.DamageDealt,
                Uses: delayed.Within is null ? 1 : null))
        {
            RepeatsWithinDuration = delayed.Within is not null,
            AwaitingActivation = delayed.Within == TimingPoints.EndOfAttack
                && !context.Admission.Query.Occurrence.Conditions.Contains(Steps.AttackInitiated)
                ? context.Admission.Query.Occurrence.Id : null,
        });
    }

    internal static void Register(AbilityEffect.DelayedDiscard delayed, AbilityImmediateContext context,
        Func<AbilityCardSelection, Card?> find)
    {
        var target = find(delayed.Card) ?? throw new RulesNotImplementedException(
            $"'{context.Admission.Source.FaceId}' would delay a discard of a card that is not there");
        if (delayed.Condition == Steps.AttackEnds && context.Admission.Query.Occurrence.ActorFacts is { IsEnemy: false }
            && !CharacterAttackEnd.Bounded(context.Admission.World))
            throw new RulesNotImplementedException(
                $"'{context.Admission.Source.FaceId}' would delay past a player attack made during an enemy attack");
        context.Admission.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, Kind: DelayedEffects.DiscardFromPlay,
            Card: context.Admission.Source.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.NextTime(delayed.Condition)));
    }

}
