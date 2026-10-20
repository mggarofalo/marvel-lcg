using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Finds the selected identity's need for round history across all of its printed forms.</summary>
internal static class AbilityAllyPlayHistory
{
    internal static bool Required(World world, AbilityProgram program)
    {
        var faces = world.Seats.SelectMany(seat => seat.IdentityCard.Faces).ToHashSet(StringComparer.Ordinal);
        return program.Abilities.Any(ability => faces.Contains(ability.Card)
            && ability.Effect is AbilityEffect.ReduceFirstAllyCost);
    }
}
