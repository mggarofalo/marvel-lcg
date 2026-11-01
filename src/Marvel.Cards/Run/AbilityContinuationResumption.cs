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
        if (state.Reason == AbilityResumeReason.PowerAftermath)
            return new ResumeComplete(state with { Reason = AbilityResumeReason.Choice });
        if (Resumed(decoded, state) is { } resumed) return resumed;
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
            state with
            {
                Reason = AbilityResumeReason.Choice,
                CompletedOutcome = state.Reason == AbilityResumeReason.Activations
                    ? AbilityCompletion.Amount(state.Memory.Results.GetValueOrDefault("activationMade"),
                        state.Memory.Results.GetValueOrDefault("activationRequested"))
                    : state.CompletedOutcome,
            },
            effectApplied);
    }

    // Reasons that resume the suspended node itself.
    private static RunResumedNode? Resumed(DecodedAbilityContinuation decoded, AbilityContinuationState state)
    {
        var choice = state with { Reason = AbilityResumeReason.Choice };
        return state.Reason switch
        {
            AbilityResumeReason.StartAbility => new RunResumedNode(decoded.Ability, decoded.Ability.Effect, choice),
            AbilityResumeReason.BoostCount => new RunResumedNode(decoded.Ability, decoded.Node, choice with
            {
                BoostCounts = [],
                Memory = state.Memory with { Results = AbilityBoostCounting.Record(state.Memory.Results, state.BoostCounts) },
            }),
            AbilityResumeReason.DelayedEffect or AbilityResumeReason.DefenseBody =>
                new RunResumedNode(decoded.Ability, decoded.Node, choice),
            AbilityResumeReason.RepeatSpecial => new RunResumedNode(decoded.Ability, decoded.Node, choice, EffectApplied: true),
            _ => null,
        };
    }

}
