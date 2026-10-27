using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Resolves an enemy's ordinary main scheme within its game area.</summary>
public static class EnemySchemeDestination
{
    /// <summary>Finds the main scheme referred to by this scheming enemy.</summary>
    public static Card? Resolve(World world, Card enemy)
    {
        // pack:mc11:game-areas forbids affecting another game area. Players
        // sharing an area also share its scheme even when the enemy is engaged
        // with a different seat. Central stage 2B is outside that game area.
        var schemes = Places.MainSchemes(world, enemy);
        if (Places.GameAreaOf(world, enemy) is { } area)
            schemes = schemes.Where(scheme => ReferenceEquals(
                Places.GameAreaOf(world, scheme), area)).ToList();
        return schemes.Count switch
        {
            0 => null,
            1 => schemes[0],
            _ => throw new RulesNotImplementedException(
                "an enemy's scheme activation has multiple main scheme destinations"),
        };
    }
}
