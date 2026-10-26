using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Describes boost uncertainty at the current attack stage without creating areas.</summary>
internal static class AttackBoostDescription
{
    internal static bool IsUnresolved(World world, ICardFacts facts, Card enemy, string step)
    {
        if (world.Attack?.CalculatedDamage is not null) return false;
        // rr:boost-boost-icon.6: a previously supplied boost waits on the enemy
        // until it activates, including an ordinary non-Villainous minion.
        if (HasWaitingBoost(world, enemy)) return true;
        bool beforeDealing = step == Steps.Attack
            || step == Steps.GiveBoostCard && world.Agenda.Stage != Stage.Responses;
        // rr:attack-enemy-activation.step.1: skip the boost deal for a minion
        // without Villainous. Eligibility alone says nothing after that step.
        return beforeDealing && Keywords.IsBoosted(world, enemy, facts, world.Players);
    }

    private static bool HasWaitingBoost(World world, Card enemy) => world.Areas.Any(area =>
        area.Type == DeckType.BoostCardsDeck && area.Host == enemy.ObjectId && area.Cards.Count > 0
        || area.Type == DeckType.BoostingArea && area.Cards.Count > 0);

    internal static string Faceup(World world, ICardFacts facts, PhaseStep step)
    {
        if (world.Agenda.Stage != Stage.Interrupts
            || step.Operation is not BoostFaceupOperation { Attacking: true } faceup
            || !faceup.Icons.IsCurrent(world)) return string.Empty;
        long icons = faceup.Icons.Remaining;
        string noun = icons == 1 ? "icon" : "icons";
        return $" {EffectiveCards.Title(world.Cards[step.Subject], facts)} currently has {icons} uncancelled boost {noun}. "
            + "Boost abilities and later effects can change the result.";
    }

    internal static string Remaining(World world, ICardFacts facts, Card enemy, string step) =>
        IsUnresolved(world, facts, enemy, step)
            ? "Boosts and later effects are unresolved."
            : "Later effects can change the result.";

    internal static string Before(World world, ICardFacts facts, Card enemy, string step) =>
        IsUnresolved(world, facts, enemy, step)
            ? "before boost icons and defense"
            : "before defense and later effects";
}
