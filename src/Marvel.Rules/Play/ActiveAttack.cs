using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Participants of the innermost attack whose resolution is still open.</summary>
public sealed record ActiveAttack(int Source, int Target)
{
    /// <summary>Reads attack ownership without advancing an occurrence or predicting its result.</summary>
    public static ActiveAttack? From(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.IsOver) return null;
        // Engine representation choice: an owner remains active through its response
        // window and nested work. Retained last-attack facts alone do not own a lifetime.
        foreach (var (step, _, occurrence) in world.Agenda.items)
        {
            if (CharacterOwner(world, step, occurrence) is { } character) return character;
            if (step.What == Steps.EndAttack && world.Attack is { } pending)
            {
                return new ActiveAttack(pending.Enemy, pending.Target);
            }
            if (FinishedOwner(world, occurrence) is { } finished) return finished;
        }
        return world.Attack is { } attack ? new ActiveAttack(attack.Enemy, attack.Target) : null;
    }

    private static ActiveAttack? CharacterOwner(World world, PhaseStep step, Occurrence? occurrence)
    {
        if (step.What != Steps.CharacterAttacks || step.CharacterAttack is not { } attack) return null;
        return occurrence is not null || ReferenceEquals(attack, world.CharacterAttack)
            ? new ActiveAttack(attack.Attacker, attack.Enemy) : null;
    }

    private static ActiveAttack? FinishedOwner(World world, Occurrence? occurrence)
    {
        if (occurrence is null || !occurrence.Is(Steps.AttackEnds)
            || world.FinishedAttack is not { } attack || occurrence.Actor != attack.Enemy) return null;
        return new ActiveAttack(attack.Enemy, attack.Target);
    }
}
