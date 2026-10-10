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
    internal IReadOnlyList<GameEvent> Resolve(
        World world, Card source, int player, int stoppedAt,
        AbilityType? tier, bool finalStep, bool finalPlayer)
    {
        var step = world.Agenda.Current;
        var restored = AbilityContinuationCodec.DecodeEachPlayer(
            execution.program, source, step, stoppedAt, tier, player, finalPlayer);

        var cast = execution.Resolving(
            world, source, player, tier, finalStep, step?.AbilityOccurrence) with
        {
            EachPlayerFrame = true,
            FinalPlayer = finalPlayer,
            AbilityPlayer = step?.AbilityPlayer ?? player,
            GainedKeywords = world.Agenda.Current is
            { What: Steps.ResolveEachPlayer, SurgeGained: true }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
        execution.RestorePersisted(cast, step);
        cast.RestoreAbility(
            restored.Ordinal, restored.Frames,
            step?.AbilityFace);
        cast.TrackResolution(restored.Ordinal);
        execution.RestoreAlteredFromFrames(cast, cast.StructuralPath.ToImmutableArray());
        cast.At(stoppedAt - 1);
        cast.SetContinuation(restored.HasContinuation);
        execution.Run(restored.Body, cast);
        var next = AbilityContinuationCodec.AfterEachPlayer(
            execution.program, source, restored, execution.Capture(cast, restored.Ordinal),
            world.Agenda.Current?.Round ?? 0, tier, finalPlayer, cast.Suspended);
        if (next is not null)
        {
            return execution.ResumeContinuation(cast, source, next);
        }
        cast.CompleteResolution();
        return cast.Events;
    }

}
