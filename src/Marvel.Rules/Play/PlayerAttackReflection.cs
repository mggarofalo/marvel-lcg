using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Installs and queries damage reflection within one begun player attack.</summary>
public static class PlayerAttackReflection
{
    internal static CharacterAttackOperation? Current(World world) => world.Agenda.items
        .Where(item => item.Occurrence is not null)
        .Select(item => item.Step.Operation).OfType<CharacterAttackOperation>().FirstOrDefault();

    /// <summary>Whether the attachment's host is attacked by the player attack <paramref name="occurrence"/> initiates.</summary>
    /// <remarks>
    /// The host may be the attack's first target, a recipient it declared beside
    /// it, or an enemy it reaches later (rr:attack-player-ability-type.5); the
    /// triggering ability's own match against <paramref name="occurrence"/> decides
    /// that the host is the attacked enemy. Every reflecting card is an attachment,
    /// and its "Max 1 per attack" already keeps a second reflection off one attack;
    /// the host and empty-reflection clauses restate those facts.
    /// </remarks>
    public static bool CanInstall(World world, Occurrence occurrence, Card source) =>
        world.Agenda.InitiatingPlayerAttack(occurrence) is { Reflection: null } && source.Area.Host >= 0;

    /// <summary>Retain the replacement after its source attachment leaves play.</summary>
    public static void Install(World world, Occurrence occurrence, Card source, string trigger)
    {
        if (!CanInstall(world, occurrence, source)) throw new RulesNotImplementedException("attack reflection requires the attached attack target");
        var operation = world.Agenda.InitiatingPlayerAttack(occurrence)!;
        var target = world.Cards[source.Area.Host];
        operation.Reflection = new(source, target, target.Incarnation,
            world.Seats[operation.Attack.Player].IdentityCard, trigger);
    }

    /// <summary>The active replacement, limited to the same physical target incarnation.</summary>
    public static AttackDamageReflection? For(World world, Card target) =>
        Current(world)?.Reflection is { } reflection && ReferenceEquals(reflection.Target, target)
            && reflection.TargetIncarnation == target.Incarnation ? reflection : null;
}
