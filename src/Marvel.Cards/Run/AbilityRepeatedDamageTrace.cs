using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;

namespace Marvel.Cards.Run;

internal static class AbilityRepeatedDamageTrace
{
    internal static DamageTraceState ApplyDamageTrace(
        DamageTraceState state, IReadOnlyList<DamageTransfer> trace,
        Card target, AbilityAdmissionScope cast) =>
        new DamageTraceMutation(state, target, cast).Apply(trace);
}
