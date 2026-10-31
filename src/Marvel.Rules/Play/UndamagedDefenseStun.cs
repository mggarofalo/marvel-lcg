using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Stuns the attacking enemy after a captured hero defense takes no damage from its exact attack.</summary>
/// <remarks>
/// Printed 14014 Never Back Down: "If you take no damage from this attack, stun
/// the attacking enemy." The hero, its incarnation and the activation are captured
/// as <see cref="UndamagedDefenseReady"/> captures them; only the outcome differs.
/// </remarks>
public static class UndamagedDefenseStun
{
    /// <summary>Engine-chosen delayed instruction spelling.</summary>
    public const string Kind = "StunUndamagedDefendersAttacker";

    /// <summary>Retains the physical hero and activation through the existing effect registry.</summary>
    public static void Install(World world, Occurrence occurrence, Card source, int player)
    {
        if (!UndamagedDefenseReady.CanInstall(world, occurrence, player))
            throw new RulesNotImplementedException("delayed undamaged defense stun requires a committed hero defense");
        var hero = world.Seats[player].IdentityCard;
        world.Effects.Register(new(EffectSource.DelayedEffect, Kind,
            Card: source.ObjectId, Affects: hero.ObjectId,
            Lasts: Duration.NextTime(Steps.AttackEnds))
        { TargetIncarnation = hero.Incarnation });
    }

    internal static void Resolve(World world, ContinuousEffect effect, string condition, List<GameEvent> events)
    {
        if (UndamagedDefenseReady.UndamagedHero(world, effect) is null || world.Attack is not { } attack) return;
        var enemy = world.Cards[attack.Enemy];
        // rr:delayed-effect.1 resolves this before responses to the attack's end;
        // an attacker no longer in play has nothing to stun.
        if (!DeckTypes.IsInPlay(enemy.Area.Type)) return;
        Reveal.Afflict(world, world.Facts, enemy, Statuses.Stunned, condition, events);
    }
}
