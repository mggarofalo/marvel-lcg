using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// The narrow mutable counterpart to AbilityAvailability: it records one use
// after payment has committed, using the same stable identities availability
// inspected before initiation.
internal static class AbilityUseRecording
{
    internal static void Record(
        World world, AbilityProgram program, Card card, CompiledCardAbility ability,
        Occurrence? occurrence = null)
    {
        int index = AbilityAvailability.IndexOf(program, card, ability);
        if (ability.Limit is not null)
        {
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: AbilityAvailability.Spent(card, ability, index),
                Card: card.ObjectId, Affects: card.ObjectId,
                Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        }

        if (ability.Maximum is not { } maximum) return;
        Duration lasts = maximum.Period switch
        {
            MaximumPeriod.Round => Duration.UntilEndOf(TimingPoints.EndOfRound),
            MaximumPeriod.Phase => Duration.UntilEndOf(TimingPoints.EndOfPhase),
            MaximumPeriod.Game or MaximumPeriod.Instance => Duration.WhileInPlay,
            _ => throw new ArgumentOutOfRangeException(nameof(ability)),
        };
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: AbilityAvailability.MaximumSpent(world, card, maximum.Period, occurrence),
            Card: card.ObjectId, Lasts: lasts));
    }
}
