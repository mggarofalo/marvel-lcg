using Marvel.Rules.Play;

namespace Marvel.Rules.State;

/// <summary>Owns the game's terminal result and the rule-supplied reason, once.</summary>
internal sealed class GameConclusion
{
    internal Outcome Result { get; private set; }
    internal PlayerLossReason LossReason { get; private set; }

    internal void Finish(Outcome outcome, PlayerLossReason lossReason)
    {
        if (Result is not Outcome.Unfinished)
            throw new RulesNotImplementedException(
                $"the game already ended as {Result} and cannot also end as {outcome}");

        Result = outcome;
        LossReason = lossReason;
    }
}
