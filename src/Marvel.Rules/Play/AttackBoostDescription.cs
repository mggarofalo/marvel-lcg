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

    /// <summary>The uncancelled icons on the boost card turned faceup, while its interrupt window is open; null otherwise.</summary>
    internal static long? FaceupIcons(World world, PhaseStep step) =>
        world.Agenda.Stage == Stage.Interrupts
        && step.Operation is BoostFaceupOperation { Attacking: true } faceup && faceup.Icons.IsCurrent(world)
            ? faceup.Icons.Remaining : null;

    /// <summary>
    /// The boost cards still facedown on the attacking enemy while a boost card's interrupt window is open; null
    /// otherwise. They resolve one at a time after the faceup card (rr:attack-enemy-activation.step.3.e: "If the enemy has any boost cards remaining, repeat these steps with the next boost card.").
    /// </summary>
    internal static long? FacedownCards(World world, PhaseStep step) =>
        FaceupIcons(world, step) is null || step.Operation is not BoostFaceupOperation faceup ? null
            : world.Areas.Where(area => area.Type == DeckType.BoostCardsDeck && area.Host == faceup.Enemy)
                .Sum(area => (long)area.Cards.Count);

    internal static string Faceup(World world, ICardFacts facts, PhaseStep step)
    {
        if (FaceupIcons(world, step) is not { } icons) return string.Empty;
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
