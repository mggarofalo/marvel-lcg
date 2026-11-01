using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Damage a player's identity deals to enemies, and the excess beyond their remaining hit points.</summary>
/// <remarks>
/// rr:excess-damage: "Excess damage is any amount of damage that is dealt to a
/// character beyond that character's remaining hit points." It is measured on the
/// damage dealt, not taken (ruling dd353e3498f6b923). rr:you-your.12-.14 credit
/// damage from a player's events, resources and upgrades to that player's identity;
/// rr:you-your.15 and rr:support.3 credit allies and supports with their own.
/// </remarks>
public static class ExcessDamageTiming
{
    /// <summary>A player's identity dealt excess damage to an enemy. The condition name is an engine choice.</summary>
    public const string ExcessDamageDealt = "WhenExcessDamageDealt";

    /// <summary>A player identity's attack is about to deal excess damage to an enemy — rr:damage step 1. The condition name is an engine choice.</summary>
    public const string AttackExcessDamageWouldBeDealt = "WhenAttackExcessDamageWouldBeDealt";

    /// <summary>The seat whose identity is credited with damage from <paramref name="source"/>, or -1.</summary>
    public static int IdentityPlayer(World world, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        // Equivalent mutant: no admitted card is in play under a player other than its
        // owner, so reading the owner alone, or dropping the seat bound, survives.
        int player = DeckTypes.IsInPlay(source.Area.Type) && source.Area.PlayArea.IsPlayers
            ? source.Area.PlayArea.Player : source.Owner;
        if (player < 0 || player >= world.Players) return -1;
        return LabeledAbilities.IdentityPlayer(world, LabeledAbilities.Performer(world, world.Facts, player, source));
    }

    /// <summary>The identity whose attack would deal excess damage to an enemy, or null.</summary>
    internal static Card? WouldDealExcess(World world, DamageRequest request)
    {
        // Equivalent mutant: an identity's attack targets only enemies, so dropping the
        // enemy test survives.
        if (!request.FromAttack || !CardKinds.IsEnemy(EffectiveCards.Kind(request.Target, world.Facts))) return null;
        int player = IdentityPlayer(world, request.Source);
        long remaining = Math.Max(0, DamagePlacement.Health(world, world.Facts, request.Target) - request.Target.Damage);
        return player >= 0 && request.Amount > remaining ? world.Seats[player].IdentityCard : null;
    }

    /// <summary>Records damage an identity dealt to an enemy on its occurrence and resolves lasting effects counting it.</summary>
    internal static void Record(World world, Card source, Damage.PlacedDamage placed, long remaining,
        Occurrence? occurrence, List<GameEvent> events)
    {
        // Equivalent mutant: no admitted Rocket Raccoon path deals 0 damage or damages a
        // friendly character, so loosening either test survives. The condition is read
        // only through instances with excess, so adding it unconditionally survives.
        if (placed.Dealt <= 0 || !CardKinds.IsEnemy(EffectiveCards.Kind(placed.Target, world.Facts))) return;
        int player = IdentityPlayer(world, source);
        if (player < 0) return;
        var identity = world.Seats[player].IdentityCard;
        long excess = Math.Max(0, placed.Dealt - remaining);
        if (occurrence is not null)
        {
            occurrence.DealtEnemyDamage.Record(new(OccurrenceCard.Capture(identity, world.Facts),
                Attacked.Capture(placed.Target, world.Facts, placed.Taken), placed.Dealt, excess));
            if (excess > 0) occurrence.Also(ExcessDamageDealt);
        }
        EnemyDamageHealing.Resolve(world, identity, events);
    }
}
