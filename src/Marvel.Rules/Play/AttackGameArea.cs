using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Whether a card is in reach of the enemy attack an occurrence belongs to.</summary>
/// <remarks>
/// <c>pack:mc11:game-areas</c>: "Cards and components in one game area cannot
/// affect another game area [...] Players cannot attack or defend enemies in
/// other game areas". In an ordinary game every card shares the one game area,
/// so every card is in reach.
/// </remarks>
public static class AttackGameArea
{
    /// <summary>Whether <paramref name="card"/> may act on the attack behind <paramref name="occurrence"/>.</summary>
    /// <remarks>
    /// The attack is the one underway, or, in the window after an attack ends
    /// (<c>rr:attack-enemy-activation.step.6</c>), the one that just finished.
    /// An occurrence outside any enemy attack is in reach.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="occurrence">What is happening.</param>
    /// <param name="card">The card that would act.</param>
    public static bool Reaches(World world, Occurrence occurrence, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(card);
        var attack = occurrence.CompletedEnemyAttack is not null ? world.FinishedAttack : world.Attack;
        return attack is null || Places.CanAffect(world, world.Cards[attack.Enemy], card);
    }

    /// <summary>Whether <paramref name="card"/> may defend the attack underway.</summary>
    internal static bool CanDefend(World world, Card card) =>
        world.Attack is not { } attack || Places.CanAffect(world, world.Cards[attack.Enemy], card);
}
