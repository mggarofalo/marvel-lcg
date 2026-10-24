using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Finds declarative observers without evaluating or offering their optional responses.</summary>
internal static class AbilityPlayerTurnTiming
{
    internal static bool Observed(World world, AbilityProgram program)
    {
        var faces = world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type))
            .Select(card => card.FaceId).ToHashSet(StringComparer.Ordinal);
        return program.Abilities.Any(ability => faces.Contains(ability.Card)
            && ability.Trigger.Event == PlayerTurnTiming.Begins);
    }
}
