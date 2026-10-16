using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityContinuationFrames;

namespace Marvel.Cards.Run;

/// <summary>Consumes continuation markers before resuming one authored node.</summary>
internal static class AbilityContinuationResumption
{
    /// <summary>
    /// Consumes the resumption reason before executing a node that may suspend again.
    /// </summary>
    internal static AbilityContinuationTransition Begin(
        AbilityProgram program, Card source, PhaseStep step)
    {
        var decoded = Decode(program, source, step, null);
        var state = decoded.State;
        if (state.Reason == AbilityResumeReason.StartAbility)
            return new RunResumedNode(decoded.Ability, decoded.Ability.Effect,
                state with { Reason = AbilityResumeReason.Choice });
        if (state.Reason == AbilityResumeReason.RepeatSpecial)
            return new RunResumedNode(decoded.Ability, decoded.Node, state with
            { Reason = AbilityResumeReason.Choice },
                EffectApplied: true);
        if (state.Reason == AbilityResumeReason.CostProcedure)
            return new RestartAfterPaidCost(decoded.Ability, state with
            { Reason = AbilityResumeReason.Choice });
        if (state.Reason == AbilityResumeReason.DynamicActivations)
        {
            var results = state.Memory.Results;
            if (results.GetValueOrDefault("activationMade") > 0)
                results = results.SetItem("dynamicActivationMade", 1);
            return new RunResumedNode(
                decoded.Ability, decoded.Node, state with { Reason = AbilityResumeReason.Choice, Memory = state.Memory with { Results = results } },
                EffectApplied: results.GetValueOrDefault("activationMade") > 0);
        }
        bool effectApplied = state.Memory.Results.GetValueOrDefault("activationMade") > 0
            || state.Reason == AbilityResumeReason.EffectProcedure;
        return new ContinueAfterResumedNode(
            decoded.Ability,
            state with { Reason = AbilityResumeReason.Choice },
            effectApplied);
    }

}
