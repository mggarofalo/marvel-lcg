using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityContinuationFrames;

namespace Marvel.Cards.Run;

/// <summary>Constructs the continuation state for suspended costs, effects and powers.</summary>
internal static class AbilityContinuationScheduling
{
    internal static AbilityContinuationState ForCostProcedure(
        AbilityContinuationState capture) => capture with
        { Cursor = capture.Cursor with { Frames = [] }, Reason = AbilityResumeReason.CostProcedure };

    internal static AbilityContinuationState ForEffectProcedure(
        AbilityContinuationState capture) => capture with
        { Reason = AbilityResumeReason.EffectProcedure };

    internal static AbilityContinuationState ForActivations(
        AbilityContinuationState capture, bool dynamic, int requested)
    {
        // This engine's numeric receipt counts requested activations, independently
        // of damage or threat. Status replacement does not make an activation.
        var results = capture.Memory.Results
            .Remove("activationMade")
            .Remove("activationDamage")
            .Remove("activationThreat")
            .SetItem("activationRequested", requested);
        return capture with
        {
            Reason = dynamic ? AbilityResumeReason.DynamicActivations : AbilityResumeReason.Activations,
            Memory = capture.Memory with { Results = results },
        };
    }

    internal static AbilityPowerContinuation Power(
        AbilityContinuationState capture, SchedulePowerCommand command, ICardFacts facts)
    {
        var continuation = new AbilityPowerContinuation(capture, command.PowerOrdinal,
            [.. command.Targets.Select(card => card.ObjectId)]) { Execution = capture.Execution };
        if (command.Effect.TargetCount is null) return continuation;
        return continuation with
        {
            DeclaredRecipients = [.. command.Targets.Select(card => Attacked.Capture(card, facts))],
            SelectedTargets = [.. command.Targets.Select(card => new AbilityContinuationCardBinding(
                card.ObjectId, card.Area.Id, card.Incarnation))],
        };
    }
}
