using static Marvel.Cards.Run.AbilityContinuationFrames;
using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Restores and resumes one player's portion of an each-player ability.</summary>
internal sealed class EachPlayerAbilityResolution(AbilityResolutionExecution execution)
{
    /// <inheritdoc/>
    internal IReadOnlyList<GameEvent> Resolve(World world, PhaseStep step)
    {
        var state = AbilityContinuationFrames.StateOf(step)
            ?? throw new InvalidOperationException("the each-player operation has no interpreter frame");
        var source = world.Cards[step.Subject];
        int player = step.Seat;
        int stoppedAt = step.Index;
        var tier = state.Invocation.Address.Tier;
        bool finalStep = state.Cursor.FinalStep;
        bool finalPlayer = step.Require<AbilityContinuationAgendaOperation>().FinalPlayer;
        var restored = AbilityContinuations.DecodeEachPlayer(
            execution.program, source, step, stoppedAt, tier, player, finalPlayer);

        var cast = execution.Resolving(
            world, source, player, tier, finalStep, state.Invocation.Occurrence) with
        {
            EachPlayerFrame = true,
            FinalPlayer = finalPlayer,
            AbilityPlayer = StateOf(step)?.Invocation.AbilityPlayer ?? player,
            GainedKeywords = world.Agenda.Current is
            { Operation: AbilityContinuationAgendaOperation { SurgeGained: true } }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
        execution.RestorePersisted(cast, step);
        cast.RestoreAbility(
            restored.Ordinal, restored.Frames,
            StateOf(step)?.Invocation.Address.Face);
        cast.TrackResolution(restored.Ordinal);
        execution.RestoreAlteredFromFrames(cast, cast.StructuralPath.ToImmutableArray());
        cast.At(stoppedAt - 1);
        cast.SetContinuation(restored.HasContinuation);
        execution.Run(restored.Body, cast);
        var next = AbilityContinuations.AfterEachPlayer(
            restored, execution.Capture(cast, restored.Ordinal), cast.Suspended);
        if (next is not null)
        {
            return execution.ResumeContinuation(cast, source, next);
        }
        cast.CompleteResolution();
        return cast.Events;
    }

}
