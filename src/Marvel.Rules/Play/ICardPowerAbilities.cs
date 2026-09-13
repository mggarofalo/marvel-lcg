using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text that resolves a labelled basic power.</summary>
public interface ICardPowerAbilities
{
    void ResolveCardAttack(World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events);
    void ResolveCardThwart(World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events);
}
#pragma warning restore CS1591
