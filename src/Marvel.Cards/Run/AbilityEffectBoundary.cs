using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>The suspended outer frame to resume after an effect's own windows finish.</summary>
internal sealed record AbilityEffectBoundary(PhaseStep Owner, Occurrence Occurrence)
{
    internal static void Begin(AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        if (state.Boundary is not null || context.Power is not null
            || context.ResolutionAbility is not { } ability || !IsNested(ability.Type)) return;
        var agenda = context.World.Agenda;
        if (agenda.Current is not { } owner || agenda.Occurrence is not { } occurrence) return;
        state.Boundary = new(owner, occurrence);
        CardEffectWindows.Begin(agenda, context.Source.ObjectId, context.Player);
        state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static bool IsNested(AbilityType type) => type is
        AbilityType.Interrupt or AbilityType.ForcedInterrupt or AbilityType.Response
        or AbilityType.ForcedResponse or AbilityType.WhenDefeated or AbilityType.WhenCompleted;
}
