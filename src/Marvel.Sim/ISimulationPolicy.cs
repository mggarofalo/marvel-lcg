using Marvel.Rules.Play;

namespace Marvel.Sim;

/// <summary>Answers one game's prompts for the simulation harness.</summary>
internal interface ISimulationPolicy
{
    /// <summary>Counters from decisions that have resolved.</summary>
    PolicyMetrics Metrics { get; }

    /// <summary>Answers the game's current prompt.</summary>
    Decision Answer(Game game);

    /// <summary>Commits the counters of the answer the engine just resolved.</summary>
    void DecisionResolved();
}
