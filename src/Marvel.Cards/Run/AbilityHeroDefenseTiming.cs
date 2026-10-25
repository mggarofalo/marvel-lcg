using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Finds declarative observers independently of optional ability affordability.</summary>
internal static class AbilityHeroDefenseTiming
{
    internal static bool Observed(World world, AbilityProgram program)
    {
        var faces = world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
                || card.Area.Type == DeckType.HandsArea)
            .Select(card => card.FaceId).ToHashSet(StringComparer.Ordinal);
        return program.Abilities.Any(ability => faces.Contains(ability.Card)
            && ability.Trigger.Event == HeroDefenseTiming.Defends);
    }
}
