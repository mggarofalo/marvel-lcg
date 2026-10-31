using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Assesses and commits replacement of one host's imminent ready.</summary>
internal static class AbilityReadyReplacement
{
    // rr:replacement-effect: "when [triggering condition] would happen, do
    // [replacement effect] instead." The replaced condition is this attachment's
    // host readying in the triggering would-ready occurrence.
    // In a would-ready window the trigger's subject matching already requires
    // this same imminent ready; admission states it for the instruction itself.
    internal static bool CanReplace(World world, Occurrence occurrence, Card source) =>
        Host(world, source) is { } host && occurrence.Readying is { } readying
        && readying.WouldReady(world, host);

    internal static AbilityImmediateResult? TryRun(AbilityEffect.Fixed effect, AbilityImmediateContext context) =>
        effect.Instruction != AbilityFixedInstruction.ReplaceReady ? null
            : new(true, Host(context.Admission.World, context.Admission.Source) is { } host
                && context.Admission.Query.Occurrence.Readying is { } readying
                && readying.Replace(context.Admission.World, host));

    private static Card? Host(World world, Card source) =>
        source.Area.Host >= 0 && source.Area.Host < world.Cards.Count ? world.Cards[source.Area.Host] : null;
}
