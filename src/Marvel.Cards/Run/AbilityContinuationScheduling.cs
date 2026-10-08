using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityContinuationWireCodec;

namespace Marvel.Cards.Run;

/// <summary>Encodes the continuation contract for suspended costs, effects and powers.</summary>
internal static class AbilityContinuationScheduling
{
    internal static AbilityContinuationCapture ForCostProcedure(
        AbilityContinuationCapture capture) => capture with
        {
            Frames = [],
            Results = capture.Results.SetItem("costProcedurePending", 1),
        };

    internal static AbilityContinuationCapture ForEffectProcedure(
        AbilityContinuationCapture capture) => capture with
        { Results = capture.Results.SetItem("procedureApplied", 1) };

    internal static AbilityContinuationCapture ForActivations(
        AbilityContinuationCapture capture, bool dynamic)
    {
        var results = capture.Results
            .Remove("activationMade")
            .Remove("activationDamage")
            .Remove("activationThreat");
        if (dynamic)
            results = results.SetItem("repeatDynamicActivation", 1);
        return capture with { Results = results };
    }

    internal static CardPowerContinuation Power(
        AbilityContinuationCapture capture, int powerOrdinal, bool hasContinuation) => new(
            capture.Address.Ordinal, powerOrdinal,
            hasContinuation ? capture.Position + 1 : -1, capture.FinalStep,
            [], capture.SurgeGained, [.. capture.Frames.Select(EncodeFrame)], capture.Address.Face,
            capture.Results, capture.Occurrence, capture.Discarded,
            capture.EachPlayerFrame, capture.FinalPlayer, capture.AbilityPlayer,
            hasContinuation);

}
