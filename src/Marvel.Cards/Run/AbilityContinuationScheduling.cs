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
        AbilityContinuationState capture, bool dynamic)
    {
        var results = capture.Memory.Results
            .Remove("activationMade")
            .Remove("activationDamage")
            .Remove("activationThreat");
        return capture with
        {
            Reason = dynamic ? AbilityResumeReason.DynamicActivations : AbilityResumeReason.Activations,
            Memory = capture.Memory with { Results = results },
        };
    }

    internal static AbilityPowerContinuation Power(
        AbilityContinuationState capture, int powerOrdinal, IReadOnlyList<int> targets) => new(
            capture, powerOrdinal, [.. targets]) { Execution = capture.Execution };
}
