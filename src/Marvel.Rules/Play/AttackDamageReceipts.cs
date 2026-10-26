using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Captures actual placements for the containing enemy attack's step-six window.</summary>
internal static class AttackDamageReceipts
{
    internal static void Record(World world, OccurrenceCard attacker, Damage.PlacedDamage damage)
    {
        if (!damage.Landed || !attacker.IsEnemy || world.Attack is not { } attack) return;
        // rr:attack-enemy-activation.step.6: damage triggers share the finished attack's window.
        // rr:villain-defeat.3.1: the new same-title stage "is considered the same character".
        var stages = attacker.IsVillain
            ? VillainStageIdentity.Successors(world, world.Cards[attacker.Card])
                .Select(card => OccurrenceCard.Capture(card, world.Facts) with
                {
                    // Engine copy identity advances on entry; a configured future stage
                    // must enter exactly once to remain this character's successor.
                    Incarnation = card.Incarnation + (DeckTypes.IsInPlay(card.Area.Type) ? 0 : 1),
                }).ToArray()
            : [];
        attack.ReceivedDamage.Record(new(attacker,
            OccurrenceCard.Capture(damage.Target, world.Facts), damage.Taken)
            { AttackerStages = stages });
    }
}
