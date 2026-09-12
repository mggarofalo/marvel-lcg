using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text that starts or resumes an ability outside a window.</summary>
public interface ICardContinuationAbilities
{
    IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation);
    IReadOnlyList<GameEvent> ResolveSpecial(World world, Card card, int player, bool finalStep);
    IReadOnlyList<GameEvent> ResolveEachPlayer(World world, Card source, int player, int stoppedAt,
        AbilityType? tier, bool finalStep, bool finalPlayer);
    Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier = null);
    Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep);
    Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier,
        bool finalStep, bool eachPlayerFrame, bool finalPlayer);
    IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier = null);
    IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer, string trigger);
}
#pragma warning restore CS1591
