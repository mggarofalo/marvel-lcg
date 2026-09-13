using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card-defined setup facts.</summary>
public interface ICardSetupAbilities
{
    int? SetupController(World world, Card card);
    void ValidateForPlay(World world);
    IReadOnlyList<Card> PlayerSetupCards(World world, int player);
    IReadOnlyList<GameEvent> Setup(World world, Card card);
}
#pragma warning restore CS1591
