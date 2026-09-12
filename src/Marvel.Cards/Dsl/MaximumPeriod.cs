using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>The period printed by a card maximum.</summary>
public enum MaximumPeriod
{
    /// <summary>Until the end of the round.</summary>
    Round,
    /// <summary>Until the end of the current phase.</summary>
    Phase,
    /// <summary>For the rest of the game.</summary>
    Game,
    /// <summary>For one triggering occurrence.</summary>
    Instance,
}
