using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Installs and queries damage reflection within one begun player attack.</summary>
public static class PlayerAttackReflection
{
    internal static CharacterAttackOperation? Current(World world) => world.Agenda.items
        .Where(item => item.Occurrence is not null)
        .Select(item => item.Step.Operation).OfType<CharacterAttackOperation>().FirstOrDefault();

    /// <summary>Whether the attachment hosts the target of the active player attack.</summary>
    public static bool CanInstall(World world, Card source) => Current(world) is { Reflection: null } operation
        && source.Area.Host == operation.Attack.Enemy;

    /// <summary>Retain the replacement after its source attachment leaves play.</summary>
    public static void Install(World world, Card source, string trigger)
    {
        if (!CanInstall(world, source)) throw new RulesNotImplementedException("attack reflection requires the attached attack target");
        var operation = Current(world)!;
        var target = world.Cards[operation.Attack.Enemy];
        operation.Reflection = new(source, target, target.Incarnation,
            world.Seats[operation.Attack.Player].IdentityCard, trigger);
    }

    /// <summary>The active replacement, limited to the same physical target incarnation.</summary>
    public static AttackDamageReflection? For(World world, Card target) =>
        Current(world)?.Reflection is { } reflection && ReferenceEquals(reflection.Target, target)
            && reflection.TargetIncarnation == target.Incarnation ? reflection : null;
}
