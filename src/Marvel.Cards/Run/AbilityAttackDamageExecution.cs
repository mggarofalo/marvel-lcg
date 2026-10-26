using static Marvel.Cards.Run.AbilityDamageAndThreatQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves authored attack damage with shared modifiers and explicit placement semantics.</summary>
internal static class AbilityAttackDamageExecution
{
    internal static void Run(AbilityEffect.AttackDamage damage,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        AbilityEffectBoundary.Begin(context, state);
        var attacker = context.PowerActor ?? context.AbilityActor
            ?? context.World.Seats[Resolver(context)].IdentityCard;
        ContinuousEffect? temporaryOverkill = null;
        if (damage.Overkill)
        {
            temporaryOverkill = new ContinuousEffect(EffectSource.LastingEffect, Kind: Keywords.Overkill,
                Amount: 1, Card: context.Source.ObjectId, Affects: attacker.ObjectId,
                Lasts: new Duration(Uses: 1));
            context.World.Effects.Register(temporaryOverkill);
        }
        var attackModifiers = AbilityEventModifiers.Effects(context.World, context.Source, "attackDamage");
        long amount = AbilityAmounts.SaturatingSum(Amount(damage.Amount, context),
            [AbilityEventModifiers.Amount(context.World, context.Source, "eventDamage"),
             AbilityAmounts.SaturatingSum(0, attackModifiers.Select(effect => effect.Amount))]);
        bool suspended;
        if (damage.SimultaneousPlacement)
            suspended = AbilitySimultaneousAttackDamage.Resolve(damage, context, state, attacker, amount);
        else
            suspended = DealSeparateAttackDamage(damage, context, state, attacker, amount);
        foreach (var modifier in context.Power == BasicPowers.AttackVerb ? [] : attackModifiers)
            context.World.Effects.Use(modifier);
        if (temporaryOverkill is not null) context.World.Effects.Use(temporaryOverkill);
        if (suspended) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static bool DealSeparateAttackDamage(AbilityEffect.AttackDamage damage,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state, Card attacker, long amount)
    {
        bool suspended = false;
        foreach (var target in AbilityDamageAndThreatExecution.DamageTargets(damage.Cards, context))
        {
            var recipient = context.StagedAttack ? Attacked.Capture(target, context.World.Facts) : null;
            if (context.PowerOccurrence?.Retaliation is { } retaliation)
                context.PowerOccurrence.Retaliation = retaliation.Include(target, context.World.Facts);
            var damaged = DamageAttacks.Attack(context.World, context.World.Facts, attacker, context.Source, target,
                amount, context.Trigger, "Attack", context.Events, retaliate: false);
            state.Attacked.Add(target);
            (state.Damage ??= new(context.Occurrence)).Add(damaged, amount, recipient, context.PowerOccurrence);
            suspended |= !damaged.IsComplete;
        }
        return suspended;
    }

}
