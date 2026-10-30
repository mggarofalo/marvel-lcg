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
    AbilityExecutionResult ResolveSpecial(World world, AbilityExecution entry, Card card, int player, bool finalStep);
    IReadOnlyList<GameEvent> ResolveEachPlayer(World world, PhaseStep continuation);
    Prompt? Choosing(World world, PhaseStep continuation);
    void ValidateChoice(World world, PhaseStep continuation, Decision input);
    IReadOnlyList<GameEvent> Chose(World world, PhaseStep continuation, Decision input);
}
#pragma warning restore CS1591
