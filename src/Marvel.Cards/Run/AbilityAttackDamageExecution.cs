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
            // A resumed power can run on its ability-origin frame after an enemy
            // activation. The retained attack owns its damage procedure, defeat
            // facts and settled recipient throughout that suspension.
            bool retained = context.ActivationAttack && context.PowerOccurrence is { } power
                && !ReferenceEquals(power, context.World.Agenda.Occurrence);
            var recipient = CaptureRecipient(context, retained)
                ? Attacked.Capture(target, context.World.Facts) : null;
            if (context.PowerOccurrence?.Retaliation is { } retaliation)
                context.PowerOccurrence.Retaliation = retaliation.Include(target, context.World.Facts);
            var damaged = DamageAttacks.Resolve(context.World, context.World.Facts,
                new AttackDamageRequest(attacker, context.Source, target, amount, context.Trigger, "Attack", Retaliate: false)
                { Origin = retained ? context.PowerOccurrence : null }, context.Events);
            state.Attacked.Add(target);
            (state.Damage ??= new(context.Occurrence)).Add(damaged, amount, recipient, context.PowerOccurrence);
            suspended |= !damaged.IsComplete;
        }
        return suspended;
    }

    private static bool CaptureRecipient(AbilityDamageAndThreatContext context, bool retained) =>
        context.StagedAttack || retained || context.PowerOccurrence?.Attacks.Count > 0;

}
