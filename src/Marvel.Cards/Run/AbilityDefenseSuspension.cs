using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Preserves a defense body while its committed-defense interrupts resolve.</summary>
internal static class AbilityDefenseSuspension
{
    internal static bool Begin(AbilityResolutionExecution execution, AbilityResolutionState cast,
        Marvel.Rules.State.Card defender, AbilityEffect body, bool nested)
    {
        var owner = cast.World.Agenda.Current;
        var occurrence = cast.World.Agenda.Occurrence;
        if (!Attack.BeginDefenseAbility(cast.World, execution.Resolver(cast), defender)) return false;
        if (nested) cast.StructuralPath.Add(new DefenseFrame());
        var capture = execution.Capture(cast, execution.AbilityOrdinal(body, cast)) with
        { Reason = AbilityResumeReason.DefenseBody };
        if (nested) cast.StructuralPath.RemoveAt(cast.StructuralPath.Count - 1);
        var continuation = AbilityContinuations.Step(capture, AbilityContinuationStage.Resume,
            owner?.Round ?? 0, plan: true);
        if (owner is null) cast.World.Agenda.Then(continuation);
        else cast.World.Agenda.ContinueBeforeOwner(
            occurrence ?? throw new InvalidOperationException("a defense body has no containing occurrence"),
            owner.Value, continuation);
        cast.Suspend();
        return true;
    }
}
