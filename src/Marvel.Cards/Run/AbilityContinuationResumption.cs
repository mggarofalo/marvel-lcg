using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityContinuationWireCodec;

namespace Marvel.Cards.Run;

/// <summary>Consumes continuation markers before resuming one authored node.</summary>
internal static class AbilityContinuationResumption
{
    /// <summary>
    /// Consumes one-shot wire markers before the executor can execute a node that
    /// might suspend again. The returned state is the only state a later capture
    /// may persist.
    /// </summary>
    internal static AbilityContinuationTransition Begin(
        AbilityProgram program, Card source, PhaseStep step)
    {
        var decoded = Decode(program, source, step, step.Tier);
        var state = decoded.State;
        if (state.Results.ContainsKey(AbilitySpecialSequence.Repeat))
            return new RunResumedNode(decoded.Ability, decoded.Node, state with
            { Results = state.Results.Remove(AbilitySpecialSequence.Repeat).Remove("procedureApplied") },
                EffectApplied: true);
        if (state.Results.TryGetValue("costProcedurePending", out _))
            return new RestartAfterPaidCost(decoded.Ability, state with
            { Results = state.Results.Remove("costProcedurePending") });
        if (state.Results.TryGetValue("repeatDynamicActivation", out _))
        {
            var results = state.Results.Remove("repeatDynamicActivation");
            if (results.GetValueOrDefault("activationMade") > 0)
                results = results.SetItem("dynamicActivationMade", 1);
            return new RunResumedNode(
                decoded.Ability, decoded.Node, state with { Results = results },
                EffectApplied: results.GetValueOrDefault("activationMade") > 0);
        }
        bool effectApplied = state.Results.GetValueOrDefault("activationMade") > 0
            || state.Results.ContainsKey("procedureApplied");
        return new ContinueAfterResumedNode(
            decoded.Ability,
            state with { Results = state.Results.Remove("procedureApplied") },
            effectApplied);
    }

}
