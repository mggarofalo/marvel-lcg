using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Settles an entering ally's live health before entry responses.</summary>
internal static class AllyEntryHealth
{
    internal static bool Settle(World world, ICardFacts facts, Card ally, int player, List<GameEvent> events)
    {
        if (StateFields.Modified(world, ally, "is_infinite_health", facts, world.Players) > 0
            || ally.Damage < DamagePlacement.Health(world, facts, ally)) return false;

        var identity = world.Seats[player].IdentityCard;
        if (identity is not null && PlayerLimitProcedure.CountedAllies(world, player).Count
            > StateFields.Modified(world, identity, "ally_limit", facts, world.Players))
            throw new RulesNotImplementedException(
                "simultaneous entering ally zero-health defeat and ally-limit enforcement ordering is unresolved");

        // rr:hit-points.3: an ally with zero or fewer remaining hit points is defeated.
        // The entry occurrence owns the normal would-be-defeated and defeat lifecycle.
        world.Effects.SettleHealthDefeat(ally, "ally entry", events);
        return true;
    }
}
